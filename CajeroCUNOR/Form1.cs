using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace CajeroCUNOR
{
    public partial class Form1 : Form
    {
        private TextBox txtTarjeta, txtPin, txtTokenRFID;
        private Button btnIngresar;
        private LectorRFID lector = new LectorRFID(); // NUEVO

        public Form1()
        {
            InitializeComponent();
            ConfigurarUIPersonalizada();
            ClienteRed.IniciarHeartbeat();

            // NUEVO: cuando llega un UID, se escribe en el campo Token RFID
            lector.TarjetaLeida += uid => this.Invoke(new Action(() => txtTokenRFID.Text = uid));
            if (!lector.Conectar("COM4"))
                MessageBox.Show("No se pudo abrir COM4. Revisa que el Arduino esté conectado y el Monitor Serie cerrado.");
        }

        private void ConfigurarUIPersonalizada()
        {
            this.Size = new Size(420, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorTranslator.FromHtml("#0F172A");
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Cajero Automático USAC - Autenticación";

            Label lblTitulo = new Label
            {
                Text = "SISTEMA CAJERO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Location = new Point(75, 40),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Ing. Jose Alberto Veliz Cruz",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10),
                Location = new Point(110, 80),
                AutoSize = true
            };

            Label lblT = new Label { Text = "Número de Tarjeta:", ForeColor = Color.White, Location = new Point(40, 130), AutoSize = true };
            txtTarjeta = new TextBox
            {
                Location = new Point(40, 155),
                Size = new Size(320, 30),
                MaxLength = 40,
                Font = new Font("Segoe UI", 11),
                BackColor = ColorTranslator.FromHtml("#1E293B"),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblP = new Label { Text = "PIN (4 dígitos):", ForeColor = Color.White, Location = new Point(40, 210), AutoSize = true };
            txtPin = new TextBox
            {
                Location = new Point(40, 235),
                Size = new Size(320, 30),
                MaxLength = 20,
                Font = new Font("Segoe UI", 11),
                BackColor = ColorTranslator.FromHtml("#1E293B"),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };

            Label lblR = new Label { Text = "Token RFID (Opcional):", ForeColor = Color.White, Location = new Point(40, 290), AutoSize = true };
            txtTokenRFID = new TextBox
            {
                Location = new Point(40, 315),
                Size = new Size(320, 30),
                MaxLength = 50,
                Font = new Font("Segoe UI", 11),
                BackColor = ColorTranslator.FromHtml("#1E293B"),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnIngresar = new Button
            {
                Text = "INICIAR SESIÓN",
                Location = new Point(40, 380),
                Size = new Size(320, 45),
                BackColor = ColorTranslator.FromHtml("#0284C7"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnIngresar.FlatAppearance.BorderSize = 0;
            btnIngresar.Click += BtnIngresar_Click;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblSub);
            this.Controls.Add(lblT);
            this.Controls.Add(txtTarjeta);
            this.Controls.Add(lblP);
            this.Controls.Add(txtPin);
            this.Controls.Add(lblR);
            this.Controls.Add(txtTokenRFID);
            this.Controls.Add(btnIngresar);
        }

        private void BtnIngresar_Click(object sender, EventArgs e)
        {
            string tarjeta = txtTarjeta.Text.Trim();
            string pin = txtPin.Text.Trim();

            if (string.IsNullOrEmpty(tarjeta) || string.IsNullOrEmpty(pin))
            {
                MessageBox.Show("Por favor complete los campos obligatorios de Tarjeta y PIN.", "Campos Requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tarjeta.Length != 16)
            {
                MessageBox.Show($"El número de tarjeta debe contener exactamente 16 dígitos.\n(Actualmente tiene {tarjeta.Length})", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pin.Length != 4)
            {
                MessageBox.Show($"El PIN debe contener exactamente 4 dígitos.\n(Actualmente tiene {pin.Length})", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                // 1. ARMAMOS LA PETICIÓN JSON
                JObject peticion = new JObject();
                peticion["operacion"] = "LOGIN";
                peticion["tarjeta"] = tarjeta;
                peticion["pin"] = pin;

                // 2. ENVIAMOS AL SERVIDOR A TRAVÉS DE LA RED
                JObject respuesta = ClienteRed.EnviarPeticion(peticion);

                // 3. LEEMOS LA RESPUESTA
                string codigo = respuesta["codigo"]?.ToString();
                string mensaje = respuesta["mensaje"]?.ToString();

                if (codigo == "00") // "00" significa Aprobado según estándar bancario
                {
                    int usuarioId = (int)respuesta["usuarioId"];
                    bool esAdmin = (bool)respuesta["esAdmin"];
                    string nombreUsr = respuesta["nombre"].ToString();

                    // Conservamos tu validación de seguridad del RFID
                    bool tieneTarjetaFisica = !string.IsNullOrWhiteSpace(txtTokenRFID.Text);

                    this.Hide();
                    if (esAdmin)
                        new FrmAdmin(nombreUsr).ShowDialog();
                    else
                        new FrmUsuario(usuarioId, tarjeta, nombreUsr, tieneTarjetaFisica).ShowDialog();

                    txtTarjeta.Clear(); txtPin.Clear(); txtTokenRFID.Clear();
                    this.Show();
                }
                else
                {
                    MessageBox.Show(mensaje, "Respuesta del Servidor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // NUEVO: cierra el puerto COM al cerrar la ventana
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            lector.Desconectar();
            base.OnFormClosing(e);
        }
    }
}