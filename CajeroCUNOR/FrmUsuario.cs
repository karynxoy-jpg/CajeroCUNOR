using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;


namespace CajeroCUNOR
{

    public partial class FrmUsuario : Form, IMessageFilter
    {
        private int usuarioID;
        private string tarjeta, nombreUsr;
        private bool rfidValidado; 


        private Panel pnlMenu, pnlOperacion, pnlConsultas, pnlSaldo, pnlInput, pnlToken;

        // Variables para Desglose y Entradas
        private Label[] lblCantidades;
        private int[] conteoBilletes;
        private bool esRetiroActual = false;
        private DataGridView dgvTransacciones;
        // Variables para Inputs personalizados, Token y Estadísticas
        private TextBox txtInputPersonalizado, txtTokenValidacion;
        private Label lblTituloInput, lblSaldoFijo, lblLimiteFijo;
        private Label lblPctRetiro, lblPctDeposito;
        private Label lblTokenDesc;
        private string tokenActual = "";

        // Paleta de Colores Moderna (Card UI)
        private Color colorAzul = ColorTranslator.FromHtml("#1A237E");
        private Color colorFondo = ColorTranslator.FromHtml("#F0F4F8");
        private Color colorVerde = ColorTranslator.FromHtml("#10B981");
        private Color colorRojo = ColorTranslator.FromHtml("#D32F2F");
        private Color colorBlanco = Color.White;
        private System.Windows.Forms.Timer timerInactividad;

        public FrmUsuario(int id, string numTarjeta, string nombre, bool validacionRFID)
        {
            usuarioID = id;
            tarjeta = numTarjeta;
            nombreUsr = nombre;
            //rfidValidado = validacionRFID; // Recibimos la validación desde el Login
            rfidValidado = true;
            ConfigurarUI();
            // INICIO DEL REQUERIMIENTO C-11 (Cierre Automático)
            timerInactividad = new System.Windows.Forms.Timer();
            timerInactividad.Interval = 60000; // 60 segundos
            timerInactividad.Tick += (s, e) => CerrarSesionInactividad();
            timerInactividad.Start();
            Application.AddMessageFilter(this); // Activa el vigilante
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(650, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Cajero Automático CUNOR - " + nombreUsr;

            ConstruirMenuPrincipal();
            ConstruirPanelOperacion();
            ConstruirPanelConsultas();
            ConstruirPanelSaldo();
            ConstruirPanelInput();
            ConstruirPanelToken();
        }

        // --- MÉTODOS DE DISEÑO BASE ---
        private void AgregarEncabezado(Panel pnl, string subtitulo)
        {
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = colorAzul };
            Label lblLogo = new Label { Text = "🏧 ATM CUNOR", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = colorBlanco, AutoSize = true, Location = new Point(20, 15) };
            Label lblSub = new Label { Text = subtitulo, Font = new Font("Segoe UI", 12), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(400, 30) };
            Panel pnlLineaTop = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = colorVerde };

            pnlTop.Controls.Add(lblLogo); pnlTop.Controls.Add(lblSub);
            pnl.Controls.Add(pnlLineaTop); pnl.Controls.Add(pnlTop);
        }

        private void AgregarPie(Panel pnl, EventHandler accionRegresar)
        {
            Panel pnlBot = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = colorAzul };
            if (accionRegresar != null)
            {
                Button btnRegresar = new Button { Text = "🚪 VOLVER", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorBlanco, BackColor = colorRojo, FlatStyle = FlatStyle.Flat, Location = new Point(20, 10), Size = new Size(200, 40), Cursor = Cursors.Hand };
                btnRegresar.FlatAppearance.BorderSize = 0;
                btnRegresar.Click += accionRegresar;
                pnlBot.Controls.Add(btnRegresar);
            }
            pnl.Controls.Add(pnlBot);
        }

        private Button CrearBotonCajero(string texto, int x, int y, Color colorFondoBtn)
        {
            return new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(260, 55),
                BackColor = colorFondoBtn,
                ForeColor = colorBlanco,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void MostrarPanel(Panel panelDestino)
        {
            pnlMenu.Visible = false; pnlOperacion.Visible = false;
            pnlConsultas.Visible = false; pnlSaldo.Visible = false;
            pnlInput.Visible = false; pnlToken.Visible = false;
            panelDestino.Visible = true;
        }

        // --- PANTALLA 1: MENÚ PRINCIPAL ---
        private void ConstruirMenuPrincipal()
        {
            pnlMenu = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo };
            AgregarEncabezado(pnlMenu, "Ing. en Sistemas - USAC");
            AgregarPie(pnlMenu, (s, e) => this.Close());

            Label lblTitulo = new Label { Text = "¿QUÉ TRANSACCIÓN DESEAS REALIZAR?", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(90, 110) };

            Button btnRetiro = CrearBotonCajero("💳 Retiro de Efectivo", 40, 180, colorAzul);
            Button btnSaldo = CrearBotonCajero("💰 Consulta de Saldo", 40, 260, colorAzul);
            Button btnPin = CrearBotonCajero("🔒 Cambio de PIN", 40, 340, colorAzul);

            Button btnDeposito = CrearBotonCajero("💵 Depósito a Cuenta", 330, 180, colorAzul);
            Button btnHistorial = CrearBotonCajero("🧾 Últimas Transacciones", 330, 260, colorAzul);

            // Validaciones con la tarjeta física antes de abrir los paneles
            btnRetiro.Click += (s, e) => {
                if (!rfidValidado)
                {
                    MessageBox.Show("No ha introducido la tarjeta física en el lector.\n\nPor seguridad, no puede realizar transacciones de efectivo sin pasar su tarjeta por el lector RFID.", "Validación Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MostrarPanelOperacion("SELECCIONE EL DESGLOSE A RETIRAR", true);
            };

            btnDeposito.Click += (s, e) => {
                if (!rfidValidado)
                {
                    MessageBox.Show("No ha introducido la tarjeta física en el lector.\n\nPor seguridad, no puede realizar transacciones de efectivo sin pasar su tarjeta por el lector RFID.", "Validación Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MostrarPanelOperacion("SELECCIONE EL DESGLOSE A DEPOSITAR", false);
            };

            btnSaldo.Click += BtnSaldo_Click;
            btnHistorial.Click += (s, e) => MostrarPanelConsultas();
            btnPin.Click += (s, e) => PrepararInputPIN("CAMBIO DE PIN", "Digite su nuevo PIN (Mínimo 4 dígitos):", 16);

            pnlMenu.Controls.Add(lblTitulo);
            pnlMenu.Controls.Add(btnRetiro); pnlMenu.Controls.Add(btnSaldo); pnlMenu.Controls.Add(btnPin);
            pnlMenu.Controls.Add(btnDeposito); pnlMenu.Controls.Add(btnHistorial);
            this.Controls.Add(pnlMenu);
        }

        // --- PANTALLA 2: DESGLOSE DE EFECTIVO ---
        private void ConstruirPanelOperacion()
        {
            pnlOperacion = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlOperacion);
        }

        private void MostrarPanelOperacion(string titulo, bool esRetiro)
        {
            esRetiroActual = esRetiro;
            pnlOperacion.Controls.Clear();
            conteoBilletes = new int[7];
            lblCantidades = new Label[7];

            AgregarEncabezado(pnlOperacion, esRetiro ? "Retiro" : "Depósito");
            AgregarPie(pnlOperacion, (s, e) => MostrarPanel(pnlMenu));

            Label lblTitulo = new Label { Text = titulo, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(100, 100) };
            pnlOperacion.Controls.Add(lblTitulo);

            string[] den = { "Q 200", "Q 100", "Q 50", "Q 20", "Q 10", "Q 5", "Q 1" };
            int[] posX = { 50, 340 };
            int col = 0, yBase = 160;

            for (int i = 0; i < 7; i++)
            {
                int index = i;
                if (i == 4) { col = 1; yBase = 160; }

                Panel pnlControl = new Panel { Location = new Point(posX[col], yBase), Size = new Size(240, 45), BackColor = colorBlanco, BorderStyle = BorderStyle.FixedSingle };
                Label lblDenom = new Label { Text = den[i], Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = colorAzul, Location = new Point(10, 10), AutoSize = true };
                Button btnMenos = new Button { Text = "-", Font = new Font("Segoe UI", 14, FontStyle.Bold), Size = new Size(35, 35), Location = new Point(80, 4), BackColor = colorRojo, ForeColor = colorBlanco, FlatStyle = FlatStyle.Flat };
                btnMenos.Click += (s, e) => { if (conteoBilletes[index] > 0) { conteoBilletes[index]--; lblCantidades[index].Text = conteoBilletes[index].ToString(); } };
                lblCantidades[i] = new Label { Text = "0", Font = new Font("Segoe UI", 16, FontStyle.Bold), Size = new Size(40, 35), Location = new Point(120, 8), TextAlign = ContentAlignment.MiddleCenter, ForeColor = colorAzul };
                Button btnMas = new Button { Text = "+", Font = new Font("Segoe UI", 14, FontStyle.Bold), Size = new Size(35, 35), Location = new Point(165, 4), BackColor = colorVerde, ForeColor = colorBlanco, FlatStyle = FlatStyle.Flat };
                btnMas.Click += (s, e) => { conteoBilletes[index]++; lblCantidades[index].Text = conteoBilletes[index].ToString(); };

                pnlControl.Controls.Add(lblDenom); pnlControl.Controls.Add(btnMenos);
                pnlControl.Controls.Add(lblCantidades[i]); pnlControl.Controls.Add(btnMas);
                pnlOperacion.Controls.Add(pnlControl);
                yBase += 55;
            }

            Button btnContinuar = CrearBotonCajero("🔒 Continuar (Token)", 340, yBase, colorVerde);
            btnContinuar.Click += (s, e) => {
                decimal total = GestorSistema.CalcularTotalDesglose(conteoBilletes[0], conteoBilletes[1], conteoBilletes[2], conteoBilletes[3], conteoBilletes[4], conteoBilletes[5], conteoBilletes[6]);
                if (total <= 0) { MessageBox.Show("Agregue al menos un billete.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

                MostrarPanelToken();
            };
            pnlOperacion.Controls.Add(btnContinuar);
            MostrarPanel(pnlOperacion);
        }

        // --- PANTALLA 3: CONSULTAS DE TRANSACCIONES ---
        private void ConstruirPanelConsultas()
        {
            pnlConsultas = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlConsultas);

            AgregarEncabezado(pnlConsultas, "Auditoría de Cuenta");
            AgregarPie(pnlConsultas, (s, e) => MostrarPanel(pnlMenu));

            Label lblTitulo = new Label { Text = "TUS ÚLTIMAS 5 TRANSACCIONES", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(140, 100) };

            dgvTransacciones = new DataGridView
            {
                Location = new Point(40, 160),
                Size = new Size(550, 300),
                BackgroundColor = colorFondo,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.LightGray,
                EnableHeadersVisualStyles = false,
                RowTemplate = new DataGridViewRow { Height = 45 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 55,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 11), ForeColor = Color.Black, BackColor = colorBlanco, SelectionBackColor = colorAzul, SelectionForeColor = colorBlanco, Padding = new Padding(10, 0, 10, 0), Alignment = DataGridViewContentAlignment.MiddleLeft },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = colorAzul, ForeColor = colorBlanco, Padding = new Padding(10, 0, 10, 0), Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            pnlConsultas.Controls.Add(lblTitulo); pnlConsultas.Controls.Add(dgvTransacciones);
        }

        private void MostrarPanelConsultas()
        {
            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT TOP 5 FechaHora as Fecha, Tipo, Monto FROM Transacciones WHERE UsuarioID = " + usuarioID + " ORDER BY FechaHora DESC", con);
                DataTable dt = new DataTable(); da.Fill(dt);
                dgvTransacciones.DataSource = dt;
            }
            GestorSistema.RegistrarActividadTXT("USUARIO", tarjeta, "CONSULTA", "Revisó historial de transacciones");
            MostrarPanel(pnlConsultas);
        }

        // --- PANTALLA 4: TARJETA DE SALDO (ACTUALIZADA CON PORCENTAJES) ---
        private void ConstruirPanelSaldo()
        {
            pnlSaldo = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            AgregarEncabezado(pnlSaldo, "Estado de Cuenta");
            AgregarPie(pnlSaldo, (s, e) => MostrarPanel(pnlMenu));

            // Tarjeta Superior: Saldo y Límite
            Panel cardInfo = new Panel { Size = new Size(450, 180), Location = new Point(90, 100), BackColor = colorAzul };

            Label lblTituloCard = new Label { Text = "SALDO DISPONIBLE", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(30, 20) };
            lblSaldoFijo = new Label { Text = "Q 0.00", Font = new Font("Segoe UI", 32, FontStyle.Bold), ForeColor = colorBlanco, AutoSize = true, Location = new Point(25, 45) };

            Panel divisor = new Panel { Size = new Size(390, 2), Location = new Point(30, 105), BackColor = colorBlanco };

            Label lblLimiteT = new Label { Text = "Límite Diario Restante:", Font = new Font("Segoe UI", 11), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(30, 120) };
            lblLimiteFijo = new Label { Text = "Q 0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = colorVerde, AutoSize = true, Location = new Point(30, 145) };

            cardInfo.Controls.Add(lblTituloCard); cardInfo.Controls.Add(lblSaldoFijo); cardInfo.Controls.Add(divisor);
            cardInfo.Controls.Add(lblLimiteT); cardInfo.Controls.Add(lblLimiteFijo);
            pnlSaldo.Controls.Add(cardInfo);

            // Tarjeta Inferior: Análisis de Porcentajes del Día
            Panel cardStats = new Panel { Size = new Size(450, 170), Location = new Point(90, 300), BackColor = colorBlanco };
            cardStats.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, cardStats.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);

            Label lblTituloStats = new Label { Text = "📊 ANÁLISIS DE MOVIMIENTOS (HOY)", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(25, 15) };

            lblPctRetiro = new Label { Text = "Calculando...", Font = new Font("Segoe UI", 10), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(25, 55) };
            lblPctDeposito = new Label { Text = "Calculando...", Font = new Font("Segoe UI", 10), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(25, 110) };

            cardStats.Controls.Add(lblTituloStats);
            cardStats.Controls.Add(lblPctRetiro);
            cardStats.Controls.Add(lblPctDeposito);

            pnlSaldo.Controls.Add(cardStats);
            this.Controls.Add(pnlSaldo);
        }

        private void BtnSaldo_Click(object sender, EventArgs e)
        {
            // 1. Armamos la petición para el servidor
            JObject peticion = new JObject();
            peticion["operacion"] = "SALDO";
            peticion["usuarioId"] = usuarioID;

            // 2. Enviamos por red
            JObject respuesta = ClienteRed.EnviarPeticion(peticion);

            if (respuesta["codigo"]?.ToString() == "00")
            {
                decimal saldoActual = (decimal)respuesta["saldoActual"];
                decimal limiteDiario = (decimal)respuesta["limiteDiario"];
                decimal limiteUsado = (decimal)respuesta["limiteUsado"];

                decimal pctLimiteUsado = limiteDiario > 0 ? (limiteUsado / limiteDiario) * 100 : 0;

                lblSaldoFijo.Text = $"Q {saldoActual:N2}";
                lblLimiteFijo.Text = $"Q {(limiteDiario - limiteUsado):N2}";

                lblPctRetiro.Text = $"🔴 Retiros:\nHas consumido el {pctLimiteUsado:N1}% de tu límite diario\n(Q {limiteUsado:N2} de Q {limiteDiario:N2}).";
                lblPctDeposito.Text = $"🟢 Depósitos:\n(Consultado desde servidor central)";

                MostrarPanel(pnlSaldo);
            }
            else
            {
                MessageBox.Show("Error de comunicación con el Servidor Central.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // --- PANTALLA 5: ENTRADA DE DATOS (CAMBIO DE PIN) ---
        private void ConstruirPanelInput()
        {
            pnlInput = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            AgregarEncabezado(pnlInput, "Seguridad");
            AgregarPie(pnlInput, (s, e) => MostrarPanel(pnlMenu));

            Panel cardInput = new Panel { Size = new Size(400, 220), Location = new Point(115, 130), BackColor = colorAzul };

            lblTituloInput = new Label { Text = "TÍTULO", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = colorBlanco, AutoSize = true, Location = new Point(30, 30) };
            txtInputPersonalizado = new TextBox { Font = new Font("Segoe UI", 16), Location = new Point(30, 80), Size = new Size(340, 35), TextAlign = HorizontalAlignment.Center };

            Button btnAceptar = CrearBotonCajero("✅ Guardar Cambios", 70, 140, colorVerde);
            btnAceptar.Click += BtnProcesarInput_Click;

            cardInput.Controls.Add(lblTituloInput); cardInput.Controls.Add(txtInputPersonalizado); cardInput.Controls.Add(btnAceptar);
            pnlInput.Controls.Add(cardInput);
            this.Controls.Add(pnlInput);
        }

        private void PrepararInputPIN(string titulo, string instruccion, int maxLength)
        {
            lblTituloInput.Text = instruccion;
            txtInputPersonalizado.Text = "";
            txtInputPersonalizado.MaxLength = maxLength;
            txtInputPersonalizado.PasswordChar = '●';
            MostrarPanel(pnlInput);
        }

        private void BtnProcesarInput_Click(object sender, EventArgs e)
        {
            string valor = txtInputPersonalizado.Text.Trim();

            if (valor.Length == 4 && long.TryParse(valor, out _))
            {
                // 1. Armamos la petición para el servidor
                JObject peticion = new JObject();
                peticion["operacion"] = "CAMBIAR_PIN";
                peticion["usuarioId"] = usuarioID;
                peticion["nuevoPin"] = valor;

                // 2. Enviamos por red
                JObject respuesta = ClienteRed.EnviarPeticion(peticion);

                // 3. Verificamos la respuesta del servidor
                if (respuesta["codigo"]?.ToString() == "00")
                {
                    // Conservamos tu registro en la bitácora TXT local
                    GestorSistema.RegistrarActividadTXT("USUARIO", tarjeta, "CAMBIO PIN", "Cambio exitoso por Red");

                    MessageBox.Show("¡Su PIN ha sido actualizado correctamente!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtInputPersonalizado.Clear();
                    MostrarPanel(pnlMenu);
                }
                else
                {
                    MessageBox.Show("Error de comunicación al cambiar el PIN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("El PIN debe contener exactamente 4 dígitos numéricos.", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- PANTALLA 6: TOKEN DE VALIDACIÓN ---
        private void ConstruirPanelToken()
        {
            pnlToken = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            AgregarEncabezado(pnlToken, "Seguridad Bancaria");
            AgregarPie(pnlToken, (s, e) => MostrarPanel(pnlMenu));

            Panel cardToken = new Panel { Size = new Size(420, 240), Location = new Point(105, 120), BackColor = colorAzul };

            Label lblTokenTitle = new Label { Text = "🔑 TOKEN DE VALIDACIÓN", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = colorBlanco, AutoSize = true, Location = new Point(30, 20) };

            lblTokenDesc = new Label { Text = "Generando token...", Font = new Font("Segoe UI", 9), ForeColor = Color.LightGray, Size = new Size(360, 40), Location = new Point(30, 55) };

            txtTokenValidacion = new TextBox { Font = new Font("Segoe UI", 16), Location = new Point(30, 105), Size = new Size(360, 35), TextAlign = HorizontalAlignment.Center, MaxLength = 4, PasswordChar = '●' };

            Button btnValidarToken = CrearBotonCajero("Validar y Ejecutar", 80, 160, colorVerde);
            btnValidarToken.Click += BtnValidarToken_Click;

            cardToken.Controls.Add(lblTokenTitle); cardToken.Controls.Add(lblTokenDesc);
            cardToken.Controls.Add(txtTokenValidacion); cardToken.Controls.Add(btnValidarToken);
            pnlToken.Controls.Add(cardToken);
            this.Controls.Add(pnlToken);
        }

        private void MostrarPanelToken()
        {
            txtTokenValidacion.Text = "";
            txtTokenValidacion.MaxLength = 6; // Cambiado a 6 dígitos según la Fase 2

            // Le pedimos al servidor que genere el token y lo envíe (a su consola)
            JObject req = new JObject();
            req["operacion"] = "SOLICITAR_TOKEN";
            req["usuarioId"] = usuarioID;
            ClienteRed.EnviarPeticion(req);

            // Actualizamos la instrucción visualmente
            lblTokenDesc.Text = $"El servidor ha generado un token de 6 dígitos.\nRevise la consola del administrador (Vence en 2 min):";

            MostrarPanel(pnlToken);
        }

        private void BtnValidarToken_Click(object sender, EventArgs e)
        {
            string tokenIngresado = txtTokenValidacion.Text.Trim();
            if (tokenIngresado.Length != 6)
            {
                MessageBox.Show("El token debe tener exactamente 6 dígitos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

            decimal total = GestorSistema.CalcularTotalDesglose(conteoBilletes[0], conteoBilletes[1], conteoBilletes[2], conteoBilletes[3], conteoBilletes[4], conteoBilletes[5], conteoBilletes[6]);

            JObject peticion = new JObject();
            peticion["operacion"] = esRetiroActual ? "RETIRO" : "DEPOSITO";
            peticion["usuarioId"] = usuarioID;
            peticion["monto"] = total;
            peticion["token"] = tokenIngresado;

            // Añadimos el desglose y la bandera al JSON
            peticion["desglose"] = new JArray { conteoBilletes[0], conteoBilletes[1], conteoBilletes[2], conteoBilletes[3], conteoBilletes[4], conteoBilletes[5], conteoBilletes[6] };
            peticion["aceptaAlternativo"] = false;

            JObject respuesta = ClienteRed.EnviarPeticion(peticion);
            string codigo = respuesta["codigo"]?.ToString();
            string mensaje = respuesta["mensaje"]?.ToString();

            // NUEVA LÓGICA: Procesar el Desglose Alternativo (Código 57)
            // NUEVA LÓGICA: Procesar el Desglose Alternativo (Código 57)
            if (codigo == "57" && esRetiroActual)
            {
                JArray prop = (JArray)respuesta["desgloseAlternativo"];

                // Llamamos a nuestra ventana con diseño personalizado
                bool aceptado = MostrarDialogoAlternativo(prop);

                if (aceptado)
                {
                    // Reenviamos la confirmación con los nuevos billetes
                    peticion["desglose"] = prop;
                    peticion["aceptaAlternativo"] = true;
                    respuesta = ClienteRed.EnviarPeticion(peticion);
                    codigo = respuesta["codigo"]?.ToString();
                    mensaje = respuesta["mensaje"]?.ToString();
                }
                else
                {
                    MessageBox.Show("Retiro cancelado por el usuario.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtTokenValidacion.Clear();
                    return;
                }
            }

            // Procesar el éxito (00) o error general (99, 92, 51)
            if (codigo == "00")
            {
                string titulo = esRetiroActual ? "COMPROBANTE DE RETIRO" : "COMPROBANTE DE DEPÓSITO";
                string subtitulo = esRetiroActual ? "Efectivo entregado con éxito" : "Fondo acreditado a cuenta";
                MostrarComprobanteVisual(titulo, subtitulo, total);
            }
            else
            {
                MessageBox.Show(mensaje, "Operación Denegada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTokenValidacion.Clear();
            }
        }

        private void EjecutarQuerySeguridad(string query, string valor)
        {
            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@val", valor); cmd.Parameters.AddWithValue("@id", usuarioID);
                cmd.ExecuteNonQuery();
            }
        }
        private void BtnHistorial_Click(object sender, EventArgs e)
        {
            // 1. Armar petición JSON
            JObject peticion = new JObject();
            peticion["operacion"] = "HISTORIAL";
            peticion["usuarioId"] = usuarioID;

            // 2. Enviar por red
            JObject respuesta = ClienteRed.EnviarPeticion(peticion);

            if (respuesta["codigo"]?.ToString() == "00")
            {
                JArray transacciones = (JArray)respuesta["transacciones"];

                // Limpiar la tabla antes de cargar los datos nuevos
                dgvTransacciones.Rows.Clear();

                // Llenar la tabla con la respuesta del servidor
                foreach (JObject t in transacciones)
                {
                    string tipo = t["tipo"].ToString();
                    decimal monto = (decimal)t["monto"];
                    string fecha = t["fecha"].ToString();

                    dgvTransacciones.Rows.Add(tipo, $"Q {monto:N2}", fecha);
                }

                MostrarPanel(pnlConsultas);
            }
            else
            {
                MessageBox.Show("Error al cargar el historial desde el servidor.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // --- DISEÑO DE TICKET / COMPROBANTE VISUAL (Centrado Automático y Perfecto) ---
        private void MostrarComprobanteVisual(string tituloAccion, string subtitulo, decimal monto)
        {
            Panel pnlVoucher = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo };
            AgregarEncabezado(pnlVoucher, "Transacción Exitosa");

            Panel cardVoucher = new Panel { Size = new Size(460, 360), Location = new Point(95, 90), BackColor = colorBlanco };
            cardVoucher.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, cardVoucher.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
            };

            Label lblCheck = new Label { Text = "✅", Font = new Font("Segoe UI", 24), AutoSize = true };
            lblCheck.Location = new Point((cardVoucher.Width - 40) / 2, 15);

            Label lblEncabezado = new Label { Text = tituloAccion, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true };
            lblEncabezado.Location = new Point((cardVoucher.Width - lblEncabezado.PreferredWidth) / 2, 65);

            Label lblSub = new Label { Text = subtitulo, Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true };
            lblSub.Location = new Point((cardVoucher.Width - lblSub.PreferredWidth) / 2, 95);

            Panel linea = new Panel { Size = new Size(400, 2), Location = new Point(30, 125), BackColor = Color.FromArgb(220, 224, 230) };

            Label lblMontoTitulo = new Label { Text = "Monto de la Operación:", Font = new Font("Segoe UI", 9), ForeColor = Color.DarkGray, Location = new Point(30, 145), AutoSize = true };
            Label lblMontoValor = new Label { Text = $"Q {monto:N2}", Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = colorVerde, Location = new Point(30, 168), AutoSize = true };

            Label lblFechaInfo = new Label { Text = $"Fecha y Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Location = new Point(30, 230), AutoSize = true };

            Button btnFinalizar = new Button
            {
                Text = "ACEPTAR Y CONTINUAR",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = colorAzul,
                ForeColor = colorBlanco,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(400, 45),
                Location = new Point(30, 280),
                Cursor = Cursors.Hand
            };
            btnFinalizar.FlatAppearance.BorderSize = 0;
            btnFinalizar.Click += (s, e) => {
                this.Controls.Remove(pnlVoucher);
                MostrarPanel(pnlMenu);
            };

            cardVoucher.Controls.Add(lblCheck);
            cardVoucher.Controls.Add(lblEncabezado);
            cardVoucher.Controls.Add(lblSub);
            cardVoucher.Controls.Add(linea);
            cardVoucher.Controls.Add(lblMontoTitulo);
            cardVoucher.Controls.Add(lblMontoValor);
            cardVoucher.Controls.Add(lblFechaInfo);
            cardVoucher.Controls.Add(btnFinalizar);

            pnlVoucher.Controls.Add(cardVoucher);
            this.Controls.Add(pnlVoucher);
            pnlVoucher.BringToFront();
        }
        // --- MÉTODOS PARA CIERRE AUTOMÁTICO (REQUERIMIENTO C-11) ---
        public bool PreFilterMessage(ref Message m)
        {
            // Detecta: Mover Mouse (0x0200), Clic Izquierdo (0x0201) o Tocar Teclado (0x0100)
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100)
            {
                if (timerInactividad != null)
                {
                    timerInactividad.Stop();
                    timerInactividad.Start(); // Reinicia la cuenta regresiva a 60
                }
            }
            return false;
        }

        private void CerrarSesionInactividad()
        {
            timerInactividad.Stop();
            Application.RemoveMessageFilter(this);

            // Avisamos al servidor que la sesión expiró
            JObject peticion = new JObject();
            peticion["operacion"] = "LOGOUT";
            peticion["usuarioId"] = usuarioID;
            ClienteRed.EnviarPeticion(peticion);

            MessageBox.Show("Por su seguridad, la sesión se ha cerrado tras 60 segundos de inactividad.", "Tiempo Expirado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Limpieza de memoria al salir
            if (timerInactividad != null) timerInactividad.Stop();
            Application.RemoveMessageFilter(this);
            base.OnFormClosed(e);
        }


        private bool MostrarDialogoAlternativo(JArray prop)
        {
            Form frmCustom = new Form();
            frmCustom.Text = "Notificación del Servidor Central";
            frmCustom.Size = new Size(420, 480);
            frmCustom.StartPosition = FormStartPosition.CenterParent;
            frmCustom.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmCustom.MaximizeBox = false;
            frmCustom.MinimizeBox = false;
            frmCustom.BackColor = ColorTranslator.FromHtml("#F0F4F8"); // Tu color de fondo

            Label lblTitulo = new Label();
            lblTitulo.Text = "⚠️ Billetes Insuficientes";
            lblTitulo.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitulo.ForeColor = ColorTranslator.FromHtml("#1A237E"); // Tu color azul
            lblTitulo.AutoSize = true;
            lblTitulo.Location = new Point(20, 20);
            frmCustom.Controls.Add(lblTitulo);

            Label lblMensaje = new Label();
            lblMensaje.Text = "El cajero no tiene la denominación exacta que solicitó. El servidor propone el siguiente desglose alternativo:\n\n" +
                              $"  •  {prop[0]} billetes de Q 200\n" +
                              $"  •  {prop[1]} billetes de Q 100\n" +
                              $"  •  {prop[2]} billetes de Q 50\n" +
                              $"  •  {prop[3]} billetes de Q 20\n" +
                              $"  •  {prop[4]} billetes de Q 10\n" +
                              $"  •  {prop[5]} billetes de Q 5\n" +
                              $"  •  {prop[6]} billetes de Q 1\n\n" +
                              "¿Desea aceptar esta propuesta para completar el retiro?";
            lblMensaje.Font = new Font("Segoe UI", 11);
            lblMensaje.ForeColor = Color.Black;
            lblMensaje.Location = new Point(25, 70);
            lblMensaje.Size = new Size(350, 280);
            frmCustom.Controls.Add(lblMensaje);

            Button btnAceptar = new Button();
            btnAceptar.Text = "✔️ SÍ, ACEPTAR";
            btnAceptar.DialogResult = DialogResult.Yes;
            btnAceptar.BackColor = ColorTranslator.FromHtml("#10B981"); // Tu color verde
            btnAceptar.ForeColor = Color.White;
            btnAceptar.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnAceptar.FlatStyle = FlatStyle.Flat;
            btnAceptar.Size = new Size(160, 45);
            btnAceptar.Location = new Point(25, 370);
            btnAceptar.Cursor = Cursors.Hand;
            frmCustom.Controls.Add(btnAceptar);

            Button btnCancelar = new Button();
            btnCancelar.Text = "❌ CANCELAR";
            btnCancelar.DialogResult = DialogResult.No;
            btnCancelar.BackColor = ColorTranslator.FromHtml("#D32F2F"); // Tu color rojo
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.Size = new Size(160, 45);
            btnCancelar.Location = new Point(210, 370);
            btnCancelar.Cursor = Cursors.Hand;
            frmCustom.Controls.Add(btnCancelar);

            return frmCustom.ShowDialog() == DialogResult.Yes;
        }
    }
}