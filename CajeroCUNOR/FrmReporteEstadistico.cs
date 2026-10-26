using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CajeroCUNOR
{
    public partial class FrmReporteEstadistico : Form
    {
        private decimal retiros, depositos, totalVolumen;
        private int usuariosPin;

        private Color colorAzul = ColorTranslator.FromHtml("#1A237E");
        private Color colorFondo = ColorTranslator.FromHtml("#F0F4F8");
        private Color colorVerde = ColorTranslator.FromHtml("#10B981");
        private Color colorRojo = ColorTranslator.FromHtml("#D32F2F");

        public FrmReporteEstadistico(decimal ret, decimal dep, decimal total, int pinCambios)
        {
            retiros = ret;
            depositos = dep;
            totalVolumen = total;
            usuariosPin = pinCambios;

            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(550, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = colorFondo;
            this.Text = "Análisis de Transacciones";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Encabezado
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = colorAzul };
            Label lblTitulo = new Label { Text = "📊 REPORTE ESTADÍSTICO DIARIO", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };
            Panel pnlLineaTop = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = colorVerde };
            pnlTop.Controls.Add(lblTitulo);
            pnlTop.Controls.Add(pnlLineaTop);
            this.Controls.Add(pnlTop);

            // Panel para el Gráfico de Pastel
            Panel pnlGrafico = new Panel { Size = new Size(220, 220), Location = new Point(40, 100) };
            pnlGrafico.Paint += PnlGrafico_Paint;
            this.Controls.Add(pnlGrafico);

            // Panel de Leyenda e Información
            Panel cardInfo = new Panel { Size = new Size(240, 250), Location = new Point(280, 100), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            decimal pctRetiros = totalVolumen > 0 ? (retiros / totalVolumen) * 100 : 0;
            decimal pctDepositos = totalVolumen > 0 ? (depositos / totalVolumen) * 100 : 0;

            Label lblDepTitulo = new Label { Text = "🟢 Depósitos", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorVerde, Location = new Point(20, 20), AutoSize = true };
            Label lblDepValor = new Label { Text = $"Q {depositos:N2} ({pctDepositos:N1}%)", Font = new Font("Segoe UI", 11), ForeColor = Color.Black, Location = new Point(20, 45), AutoSize = true };

            Label lblRetTitulo = new Label { Text = "🔴 Retiros", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorRojo, Location = new Point(20, 80), AutoSize = true };
            Label lblRetValor = new Label { Text = $"Q {retiros:N2} ({pctRetiros:N1}%)", Font = new Font("Segoe UI", 11), ForeColor = Color.Black, Location = new Point(20, 105), AutoSize = true };

            Panel div = new Panel { Size = new Size(200, 2), BackColor = Color.LightGray, Location = new Point(20, 145) };

            Label lblTotTitulo = new Label { Text = "Volumen Total:", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = colorAzul, Location = new Point(20, 160), AutoSize = true };
            Label lblTotValor = new Label { Text = $"Q {totalVolumen:N2}", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.Black, Location = new Point(20, 180), AutoSize = true };

            Label lblPin = new Label { Text = $"🔐 Cambios de PIN hoy: {usuariosPin}", Font = new Font("Segoe UI", 9), ForeColor = Color.DimGray, Location = new Point(20, 215), AutoSize = true };

            cardInfo.Controls.Add(lblDepTitulo); cardInfo.Controls.Add(lblDepValor);
            cardInfo.Controls.Add(lblRetTitulo); cardInfo.Controls.Add(lblRetValor);
            cardInfo.Controls.Add(div);
            cardInfo.Controls.Add(lblTotTitulo); cardInfo.Controls.Add(lblTotValor);
            cardInfo.Controls.Add(lblPin);

            this.Controls.Add(cardInfo);

            // Botón Cerrar
            Button btnCerrar = new Button { Text = "CERRAR REPORTE", Location = new Point(40, 370), Size = new Size(450, 45), BackColor = colorAzul, ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCerrar);
        }

        private void PnlGrafico_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Bordes suaves

            Rectangle rect = new Rectangle(10, 10, 200, 200);

            if (totalVolumen == 0)
            {
                // Si no hay transacciones, dibuja un círculo gris neutro
                g.FillEllipse(new SolidBrush(Color.LightGray), rect);
                return;
            }

            float anguloDepositos = (float)(depositos / totalVolumen) * 360f;
            float anguloRetiros = (float)(retiros / totalVolumen) * 360f;

            // Dibujar porciones (SweepAngle)
            g.FillPie(new SolidBrush(colorVerde), rect, -90, anguloDepositos); // Inicia arriba (-90 grados)
            g.FillPie(new SolidBrush(colorRojo), rect, -90 + anguloDepositos, anguloRetiros);
        }
    }
}