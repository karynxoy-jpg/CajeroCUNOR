using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CajeroCUNOR
{
    public partial class FrmEditarUsuario : Form
    {
        private int usuarioIDEdicion;
        private TextBox txtNombre, txtTarjeta, txtPin, txtLimite;
        private CheckBox chkAdmin;

        public FrmEditarUsuario(int idUsr)
        {
            usuarioIDEdicion = idUsr;
            ConfigurarUI();
            CargarDatosActuales();
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(450, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorTranslator.FromHtml("#0F172A");
            this.Text = "Edición de Usuario";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitulo = new Label { Text = "EDITAR USUARIO", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(110, 30), AutoSize = true };
            this.Controls.Add(lblTitulo);

            txtNombre = CrearInput(110, "Nombre Completo:", 100);
            txtTarjeta = CrearInput(180, "Número de Tarjeta (16 dígitos):", 16);

            // 1. Cambiamos la instrucción visual para el administrador
            txtPin = CrearInput(250, "Nuevo PIN (Dejar en blanco para no cambiar):", 4);
            txtLimite = CrearInput(320, "Límite Máximo Diario (Q):", 10);

            chkAdmin = new CheckBox { Text = "Privilegios de Administrador", ForeColor = Color.White, Font = new Font("Segoe UI", 10), Location = new Point(50, 400), AutoSize = true };
            this.Controls.Add(chkAdmin);

            Button btnActualizar = new Button { Text = "ACTUALIZAR DATOS", Location = new Point(50, 470), Size = new Size(330, 50), BackColor = ColorTranslator.FromHtml("#0284C7"), ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            Button btnCancelar = new Button { Text = "CANCELAR", Location = new Point(50, 530), Size = new Size(330, 40), BackColor = ColorTranslator.FromHtml("#D32F2F"), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            btnActualizar.Click += BtnActualizar_Click;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.Add(btnActualizar); this.Controls.Add(btnCancelar);
        }

        private TextBox CrearInput(int y, string labelTexto, int maxLen)
        {
            this.Controls.Add(new Label { Text = labelTexto, ForeColor = Color.LightGray, Location = new Point(50, y - 20), AutoSize = true, Font = new Font("Segoe UI", 9) });
            TextBox tb = new TextBox { Location = new Point(50, y), Size = new Size(330, 30), MaxLength = maxLen, Font = new Font("Segoe UI", 12), BackColor = ColorTranslator.FromHtml("#1E293B"), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(tb);
            return tb;
        }

        private void CargarDatosActuales()
        {
            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                // 2. Ya NO solicitamos el PIN a la base de datos por seguridad
                SqlCommand cmd = new SqlCommand("SELECT Nombre, NumeroTarjeta, LimiteDiario, EsAdmin FROM Usuarios WHERE UsuarioID = @id", con);
                cmd.Parameters.AddWithValue("@id", usuarioIDEdicion);

                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtNombre.Text = reader["Nombre"].ToString();
                            txtTarjeta.Text = reader["NumeroTarjeta"].ToString();
                            // El txtPin se queda vacío automáticamente
                            txtLimite.Text = Convert.ToDecimal(reader["LimiteDiario"]).ToString("0.00");
                            chkAdmin.Checked = Convert.ToBoolean(reader["EsAdmin"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar datos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnActualizar_Click(object sender, EventArgs e)
        {
            bool cambiaPin = txtPin.Text.Trim().Length > 0;

            // 3. Ajustamos la validación: si escribió un PIN, deben ser 4 dígitos. Si lo dejó en blanco, es válido.
            if (txtTarjeta.Text.Length != 16 || !decimal.TryParse(txtLimite.Text, out decimal limite) || (cambiaPin && txtPin.Text.Length != 4))
            {
                MessageBox.Show("Verifique que la tarjeta tenga 16 dígitos, el límite sea válido, y el PIN tenga 4 dígitos (o esté vacío).", "Datos Inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                string query;
                // 4. Si el administrador escribió un PIN, lo encriptamos. Si no, actualizamos el resto de los datos y dejamos el PIN intacto.
                if (cambiaPin)
                {
                    query = @"UPDATE Usuarios 
                              SET Nombre = @N, NumeroTarjeta = @T, 
                                  PIN = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @P + 'CUNOR26'), 2)), 
                                  LimiteDiario = @L, EsAdmin = @A 
                              WHERE UsuarioID = @ID";
                }
                else
                {
                    query = @"UPDATE Usuarios 
                              SET Nombre = @N, NumeroTarjeta = @T, 
                                  LimiteDiario = @L, EsAdmin = @A 
                              WHERE UsuarioID = @ID";
                }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@N", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@T", txtTarjeta.Text.Trim());
                    if (cambiaPin) cmd.Parameters.AddWithValue("@P", txtPin.Text.Trim());
                    cmd.Parameters.AddWithValue("@L", limite);
                    cmd.Parameters.AddWithValue("@A", chkAdmin.Checked ? 1 : 0);
                    cmd.Parameters.AddWithValue("@ID", usuarioIDEdicion);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        GestorSistema.RegistrarActividadTXT("ADMINISTRACIÓN", txtNombre.Text, "MODIFICACIÓN", $"Perfil actualizado por supervisor.");
                        MessageBox.Show("Los datos del usuario han sido actualizados correctamente.", "Actualización Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al actualizar (¿Tarjeta duplicada?): " + ex.Message, "Error BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}