using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CajeroCUNOR
{
    public partial class FrmRegistro : Form
    {
        // 1. Eliminamos txtRfid de las variables
        private TextBox txtNombre, txtTarjeta, txtPin, txtLimite;
        private CheckBox chkAdmin;

        public FrmRegistro()
        {
            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(450, 620); // Ajustamos la altura al quitar el campo
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 23, 42); // Azul muy oscuro
            this.Text = "Registro de Nuevo Usuario - Módulo Admin";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitulo = new Label { Text = "NUEVO USUARIO", ForeColor = Color.White, Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(100, 30), AutoSize = true };
            this.Controls.Add(lblTitulo);

            txtNombre = CrearInput(110, "Nombre Completo:", 100);
            txtTarjeta = CrearInput(180, "Número de Tarjeta (16 dígitos):", 16);
            txtPin = CrearInput(250, "PIN (4 dígitos):", 4);
            txtLimite = CrearInput(320, "Límite Máximo Diario (Q):", 10);

            // 2. Subimos el checkbox y los botones para rellenar el espacio vacío
            chkAdmin = new CheckBox { Text = "Es Administrador (Supervisor)", ForeColor = Color.White, Font = new Font("Segoe UI", 10), Location = new Point(50, 390), AutoSize = true };
            this.Controls.Add(chkAdmin);

            Button btnGuardar = new Button { Text = "GUARDAR USUARIO", Location = new Point(50, 440), Size = new Size(330, 50), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            Button btnCancelar = new Button { Text = "CANCELAR", Location = new Point(50, 500), Size = new Size(330, 40), BackColor = Color.FromArgb(211, 47, 47), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            btnGuardar.Click += BtnGuardar_Click;
            btnCancelar.Click += (s, e) => this.Close();

            this.Controls.Add(btnGuardar); this.Controls.Add(btnCancelar);
        }

        private TextBox CrearInput(int y, string labelTexto, int maxLen)
        {
            this.Controls.Add(new Label { Text = labelTexto, ForeColor = Color.LightGray, Location = new Point(50, y - 20), AutoSize = true, Font = new Font("Segoe UI", 9) });
            TextBox tb = new TextBox { Location = new Point(50, y), Size = new Size(330, 30), MaxLength = maxLen, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(tb);
            return tb;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (txtTarjeta.Text.Length != 16 || txtPin.Text.Length != 4 || !decimal.TryParse(txtLimite.Text, out decimal limite))
            {
                MessageBox.Show("Verifique que la tarjeta tenga 16 dígitos, el PIN 4, y el límite sea un número válido.", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                // 3. Quitamos el campo TokenRFID y agregamos el cifrado SHA-256 al PIN
                string query = @"INSERT INTO Usuarios (Nombre, NumeroTarjeta, PIN, SaldoActual, LimiteDiario, EsAdmin) 
                                 VALUES (@N, @T, LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @P + 'CUNOR26'), 2)), 0, @L, @A)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@N", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@T", txtTarjeta.Text.Trim());
                    cmd.Parameters.AddWithValue("@P", txtPin.Text.Trim()); // Pasamos el texto plano, SQL lo encripta
                    cmd.Parameters.AddWithValue("@L", limite);
                    cmd.Parameters.AddWithValue("@A", chkAdmin.Checked ? 1 : 0);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        GestorSistema.RegistrarActividadTXT("ADMINISTRACIÓN", txtNombre.Text, "REGISTRO", $"Nuevo usuario creado. Tarjeta: {txtTarjeta.Text}");
                        MessageBox.Show("Usuario registrado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al registrar (¿La tarjeta ya existe?): " + ex.Message, "Error BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}