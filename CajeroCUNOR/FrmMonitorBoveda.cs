using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CajeroCUNOR
{
    public partial class FrmMonitorBoveda : Form
    {
        private Color colorAzul = ColorTranslator.FromHtml("#1A237E");
        private Color colorFondo = ColorTranslator.FromHtml("#F0F4F8");
        private Color colorVerde = ColorTranslator.FromHtml("#10B981");
        private Color colorBlanco = Color.White;

        private DataGridView dgvBoveda;
        private Label lblGranTotal;

        public FrmMonitorBoveda()
        {
            ConfigurarUI();
            CargarDatosBoveda();
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = colorFondo;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Monitor de Bóveda en Tiempo Real";

            // Encabezado
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = colorAzul };
            Label lblTitulo = new Label { Text = "📊 ESTADO DE LA BÓVEDA", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorBlanco, Location = new Point(20, 20), AutoSize = true };
            Panel pnlLineaTop = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = colorVerde };
            pnlTop.Controls.Add(lblTitulo); pnlTop.Controls.Add(pnlLineaTop);
            this.Controls.Add(pnlTop);

            // Tarjeta del Gran Total
            Panel cardTotal = new Panel { Size = new Size(420, 110), Location = new Point(30, 95), BackColor = colorAzul };
            Label lblTxtTotal = new Label { Text = "EFECTIVO FÍSICO DISPONIBLE:", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.LightGray, Location = new Point(20, 15), AutoSize = true };
            lblGranTotal = new Label { Text = "Q 0.00", Font = new Font("Segoe UI", 32, FontStyle.Bold), ForeColor = colorVerde, Location = new Point(15, 40), AutoSize = true };
            cardTotal.Controls.Add(lblTxtTotal); cardTotal.Controls.Add(lblGranTotal);
            this.Controls.Add(cardTotal);

            // Tabla de Desglose por Casete
            Label lblDesglose = new Label { Text = "Desglose por Casete (Denominación):", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorAzul, Location = new Point(30, 225), AutoSize = true };
            this.Controls.Add(lblDesglose);

            dgvBoveda = new DataGridView
            {
                Location = new Point(30, 260),
                Size = new Size(420, 210),
                BackgroundColor = colorBlanco,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowTemplate = new DataGridViewRow { Height = 28 },
                ColumnHeadersHeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 11), ForeColor = Color.Black, SelectionBackColor = colorFondo, SelectionForeColor = Color.Black, Alignment = DataGridViewContentAlignment.MiddleCenter },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = colorAzul, ForeColor = colorBlanco, Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            this.Controls.Add(dgvBoveda);

            // Botón Cerrar
            Button btnCerrar = new Button { Text = "Cerrar Monitor", Location = new Point(140, 495), Size = new Size(200, 45), BackColor = ColorTranslator.FromHtml("#D32F2F"), ForeColor = colorBlanco, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCerrar);
        }

        private void CargarDatosBoveda()
        {
            try
            {
                using (SqlConnection con = GestorSistema.ObtenerConexion())
                {
                    con.Open();

                    // 1. Cargar la tabla con la cantidad de billetes y su subtotal
                    string query = "SELECT Denominacion AS 'Billete (Q)', Cantidad AS 'En Casete', (Denominacion * Cantidad) AS 'Subtotal (Q)' FROM BilletesCajero ORDER BY Denominacion DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvBoveda.DataSource = dt;

                    // 2. Calcular la suma de todo el dinero de la bóveda
                    SqlCommand cmd = new SqlCommand("SELECT SUM(Denominacion * Cantidad) FROM BilletesCajero", con);
                    object res = cmd.ExecuteScalar();
                    decimal totalFisico = (res != DBNull.Value) ? Convert.ToDecimal(res) : 0;

                    lblGranTotal.Text = $"Q {totalFisico:N2}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la bóveda: " + ex.Message);
            }
        }
    }
}