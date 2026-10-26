using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace CajeroCUNOR
{
    public partial class FrmAdmin : Form
    {
        private string adminUser;

        // Paneles de las 5 áreas principales
        private Panel pnlMenu, pnlBoveda, pnlHardware, pnlSeguridad, pnlAuditoria, pnlUsuarios;

        // Variables UI
        private TextBox[] txtBoveda;
        private DataGridView dgvUsuarios;
        private Label lblTotalDinamico;

        // Paleta de Colores Exacta
        private Color colorAzul = ColorTranslator.FromHtml("#1A237E");
        private Color colorFondo = ColorTranslator.FromHtml("#F0F4F8");
        private Color colorVerde = ColorTranslator.FromHtml("#10B981");
        private Color colorRojo = ColorTranslator.FromHtml("#D32F2F");

        public FrmAdmin(string adminName)
        {
            adminUser = adminName;
            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Size = new Size(700, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Modo Supervisor - Cajero CUNOR";

            ConstruirMenuPrincipal();
            ConstruirPanelBoveda();
            ConstruirPanelHardware();
            ConstruirPanelSeguridad();
            ConstruirPanelAuditoria();
            ConstruirPanelUsuarios();
        }

        private void AgregarEncabezado(Panel pnl, string subtitulo)
        {
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = colorAzul };
            Label lblLogo = new Label { Text = "⚙️ MODO SUPERVISOR", Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(20, 15) };
            Label lblSub = new Label { Text = subtitulo, Font = new Font("Segoe UI", 12), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(450, 30) };

            Panel pnlLineaTop = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = colorVerde };

            pnlTop.Controls.Add(lblLogo); pnlTop.Controls.Add(lblSub);
            pnl.Controls.Add(pnlLineaTop); pnl.Controls.Add(pnlTop);
        }

        private void AgregarPie(Panel pnl, EventHandler accionRegresar)
        {
            Panel pnlBot = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = colorAzul };

            if (accionRegresar != null)
            {
                Button btnRegresar = new Button { Text = "🚪 SALIR / VOLVER", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, BackColor = colorRojo, FlatStyle = FlatStyle.Flat, Location = new Point(20, 10), Size = new Size(200, 40), Cursor = Cursors.Hand };
                btnRegresar.FlatAppearance.BorderSize = 0;
                btnRegresar.Click += accionRegresar;
                pnlBot.Controls.Add(btnRegresar);
            }
            pnl.Controls.Add(pnlBot);
        }

        private Button CrearBotonAdmin(string texto, int x, int y, Color colorFondoBtn, int ancho = 280)
        {
            return new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 55),
                BackColor = colorFondoBtn,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        // --- PANTALLA 1: MENÚ PRINCIPAL ---
        private void ConstruirMenuPrincipal()
        {
            pnlMenu = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo };

            AgregarEncabezado(pnlMenu, "Admin: " + adminUser);
            AgregarPie(pnlMenu, (s, e) => this.Close());

            Label lblInstruccion = new Label { Text = "SELECCIONE EL MÓDULO DE MANTENIMIENTO", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(100, 110) };
            pnlMenu.Controls.Add(lblInstruccion);

            Button btnBoveda = CrearBotonAdmin("🏦 1. Gestión de Efectivo", 40, 180, colorAzul);
            Button btnHardware = CrearBotonAdmin("🛠️ 2. Diagnóstico Hardware", 350, 180, colorAzul);
            Button btnRed = CrearBotonAdmin("🔐 3. Red y Seguridad", 40, 260, colorAzul);
            Button btnAuditoria = CrearBotonAdmin("📊 4. Consultas y Logs", 350, 260, colorAzul);
            Button btnUsuarios = CrearBotonAdmin("👥 5. Gestión de Usuarios", 195, 340, colorVerde, 290);

            btnBoveda.Click += (s, e) => MostrarPanel(pnlBoveda);
            btnHardware.Click += (s, e) => MostrarPanel(pnlHardware);
            btnRed.Click += (s, e) => MostrarPanel(pnlSeguridad);
            btnAuditoria.Click += (s, e) => MostrarPanel(pnlAuditoria);
            btnUsuarios.Click += (s, e) => MostrarPanelUsuarios();

            pnlMenu.Controls.Add(btnBoveda); pnlMenu.Controls.Add(btnRed);
            pnlMenu.Controls.Add(btnHardware); pnlMenu.Controls.Add(btnAuditoria);
            pnlMenu.Controls.Add(btnUsuarios);

            this.Controls.Add(pnlMenu);
        }

        // --- NUEVO MÓDULO: GESTIÓN DE USUARIOS ---
        private void ConstruirPanelUsuarios()
        {
            pnlUsuarios = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlUsuarios);
            AgregarEncabezado(pnlUsuarios, "5. Cuentas de Sistema");
            AgregarPie(pnlUsuarios, (s, e) => MostrarPanel(pnlMenu));

            Button btnCrear = CrearBotonAdmin("➕ Registrar Nuevo Usuario", 40, 100, colorVerde, 290);
            Button btnEditar = CrearBotonAdmin("✏️ Editar Usuario Seleccionado", 350, 100, colorAzul, 290);

            dgvUsuarios = new DataGridView
            {
                Location = new Point(40, 170),
                Size = new Size(600, 320),
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(224, 224, 224),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 45,
                RowTemplate = new DataGridViewRow { Height = 40 },
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10), ForeColor = Color.Black, SelectionBackColor = ColorTranslator.FromHtml("#0284C7"), SelectionForeColor = Color.White, Padding = new Padding(5, 0, 0, 0) },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = ColorTranslator.FromHtml("#F8FAFC") },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = colorAzul, ForeColor = Color.White, Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            btnCrear.Click += (s, e) => {
                new FrmRegistro().ShowDialog();
                CargarDatosUsuarios();
            };

            btnEditar.Click += (s, e) => {
                if (dgvUsuarios.SelectedRows.Count > 0)
                {
                    int idSeleccionado = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["UsuarioID"].Value);
                    FrmEditarUsuario frmEditar = new FrmEditarUsuario(idSeleccionado);
                    frmEditar.ShowDialog();
                    CargarDatosUsuarios();
                }
                else
                {
                    MessageBox.Show("Por favor, seleccione toda la fila del usuario haciendo clic sobre ella.", "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            pnlUsuarios.Controls.Add(btnCrear); pnlUsuarios.Controls.Add(btnEditar);
            pnlUsuarios.Controls.Add(dgvUsuarios);
        }

        private void MostrarPanelUsuarios()
        {
            CargarDatosUsuarios();
            MostrarPanel(pnlUsuarios);
        }

        private void CargarDatosUsuarios()
        {
            using (SqlConnection con = GestorSistema.ObtenerConexion())
            {
                string query = @"SELECT 
                                    UsuarioID, 
                                    Nombre, 
                                    NumeroTarjeta as Tarjeta, 
                                    SaldoActual as Saldo, 
                                    LimiteDiario as Limite, 
                                    CASE WHEN EsAdmin = 1 THEN 'Admin' ELSE 'Cliente' END as Rol 
                                 FROM Usuarios";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvUsuarios.DataSource = dt;

                if (dgvUsuarios.Columns["UsuarioID"] != null)
                {
                    dgvUsuarios.Columns["UsuarioID"].Visible = false;
                }
            }
        }

        // --- ÁREA 1: GESTIÓN DE EFECTIVO (BÓVEDA) ---
        private void ConstruirPanelBoveda()
        {
            pnlBoveda = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlBoveda);

            AgregarEncabezado(pnlBoveda, "1. Manejo de Billetes");
            AgregarPie(pnlBoveda, (s, e) => MostrarPanel(pnlMenu));

            Label lblTitulo = new Label { Text = "CONFIGURACIÓN DE CASETES", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = colorAzul, AutoSize = true, Location = new Point(40, 100) };
            pnlBoveda.Controls.Add(lblTitulo);

            string[] den = { "Q 200", "Q 100", "Q 50", "Q 20", "Q 10", "Q 5", "Q 1" };
            txtBoveda = new TextBox[7];
            int yBase = 150;

            for (int i = 0; i < 7; i++)
            {
                pnlBoveda.Controls.Add(new Label { Text = den[i], Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = colorAzul, Location = new Point(50, yBase + 5), AutoSize = true });
                txtBoveda[i] = new TextBox { Text = "0", Font = new Font("Segoe UI", 14), Location = new Point(130, yBase), Size = new Size(100, 30), TextAlign = HorizontalAlignment.Center };
                txtBoveda[i].TextChanged += TxtBoveda_TextChanged;
                pnlBoveda.Controls.Add(txtBoveda[i]);
                yBase += 45;
            }

            lblTotalDinamico = new Label
            {
                Text = "Total a ingresar: Q 0.00",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = colorAzul,
                AutoSize = true,
                Location = new Point(50, yBase + 10)
            };
            pnlBoveda.Controls.Add(lblTotalDinamico);

            // --- AQUÍ ESTÁN LOS BOTONES REACOMODADOS Y EL NUEVO MONITOR ---
            Button btnInit = CrearBotonAdmin("Inicializar (Max Q10k)", 320, 150, colorVerde, 320);
            Button btnAdd = CrearBotonAdmin("Agregar Lote (Max Q30k)", 320, 220, colorVerde, 320);
            Button btnMonitor = CrearBotonAdmin("📊 Ver Estado de Bóveda", 320, 290, colorAzul, 320); // BOTÓN NUEVO
            Button btnPurga = CrearBotonAdmin("🔄 Purga de Dispensador", 320, 360, colorAzul, 320);
            Button btnRechazo = CrearBotonAdmin("🗑️ Vaciado de Rechazo", 320, 430, colorAzul, 320);

            btnInit.Click += (s, e) => ProcesarBoveda(true);
            btnAdd.Click += (s, e) => ProcesarBoveda(false);

            // EVENTO DEL NUEVO BOTÓN
            btnMonitor.Click += (s, e) => {
                FrmMonitorBoveda monitor = new FrmMonitorBoveda();
                monitor.ShowDialog();
            };

            btnPurga.Click += (s, e) => MessageBox.Show("Ejecutando prueba de rodillos y mecanismo...\n\nEstado: OK. No hay atascos.", "Diagnóstico de Dispensador");
            btnRechazo.Click += (s, e) => MessageBox.Show("Cajón de rechazo (billetes dañados) vaciado exitosamente.", "Mantenimiento");

            pnlBoveda.Controls.Add(btnInit); pnlBoveda.Controls.Add(btnAdd);
            pnlBoveda.Controls.Add(btnMonitor); // SE AGREGA AL PANEL
            pnlBoveda.Controls.Add(btnPurga); pnlBoveda.Controls.Add(btnRechazo);
        }

        private void TxtBoveda_TextChanged(object sender, EventArgs e)
        {
            int[] d = new int[7];
            for (int i = 0; i < 7; i++) int.TryParse(txtBoveda[i].Text, out d[i]);
            decimal total = GestorSistema.CalcularTotalDesglose(d[0], d[1], d[2], d[3], d[4], d[5], d[6]);
            lblTotalDinamico.Text = $"Total a ingresar: Q {total:N2}";

            if (total > 30000)
            {
                lblTotalDinamico.ForeColor = colorRojo;
                lblTotalDinamico.Text += " ⚠️ (Excede límite)";
            }
            else
            {
                lblTotalDinamico.ForeColor = colorAzul;
            }
        }

        private void ProcesarBoveda(bool esInicializacion)
        {
            int[] d = new int[7];
            for (int i = 0; i < 7; i++) int.TryParse(txtBoveda[i].Text, out d[i]);
            decimal total = GestorSistema.CalcularTotalDesglose(d[0], d[1], d[2], d[3], d[4], d[5], d[6]);

            if (esInicializacion && total > 10000) { MessageBox.Show("Por normativa, la inicialización no puede superar Q 10,000.", "Límite Excedido"); return; }
            if (!esInicializacion && total > 30000) { MessageBox.Show("Por normativa, los agregados no pueden superar Q 30,000.", "Límite Excedido"); return; }
            if (!esInicializacion && total == 0) return;

            // 1. Armamos el arreglo JSON con las 7 cantidades de billetes
            JArray arregloBilletes = new JArray { d[0], d[1], d[2], d[3], d[4], d[5], d[6] };

            // 2. Armamos la petición para el Servidor
            JObject peticion = new JObject();
            peticion["operacion"] = "ACTUALIZAR_BOVEDA";
            peticion["esInicializacion"] = esInicializacion;
            peticion["billetes"] = arregloBilletes;

            // 3. Enviamos por red
            JObject respuesta = ClienteRed.EnviarPeticion(peticion);

            if (respuesta["codigo"]?.ToString() == "00")
            {
                GestorSistema.RegistrarActividadTXT("ADMIN", adminUser, esInicializacion ? "INICIALIZAR CAJERO" : "AGREGAR EFECTIVO", $"Monto: Q{total} (Vía Red)");

                if (esInicializacion && total == 0)
                    MessageBox.Show("Cajero vaciado por completo.", "Bóveda Reseteada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"Operación de Bóveda por Q{total} registrada con éxito en el servidor.", "Arqueo de Caja", MessageBoxButtons.OK, MessageBoxIcon.Information);

                for (int i = 0; i < 7; i++) txtBoveda[i].Text = "0";
            }
            else
            {
                MessageBox.Show("Error de red al actualizar la bóveda.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- ÁREA 2: DIAGNÓSTICO DE HARDWARE ---
        private void ConstruirPanelHardware()
        {
            pnlHardware = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlHardware);
            AgregarEncabezado(pnlHardware, "2. Mantenimiento Hardware");
            AgregarPie(pnlHardware, (s, e) => MostrarPanel(pnlMenu));

            Button btnTestLector = CrearBotonAdmin("💳 Test Lector Tarjetas", 100, 150, colorAzul, 450);
            Button btnTestImpresora = CrearBotonAdmin("🖨️ Test Impresora Térmica", 100, 220, colorAzul, 450);
            Button btnReinicio = CrearBotonAdmin("🔄 Reinicio de Módulos", 100, 290, colorVerde, 450);
            Button btnFirmware = CrearBotonAdmin("💾 Actualizar Firmware", 100, 360, colorRojo, 450);

            btnTestLector.Click += (s, e) => MessageBox.Show("Lector RFID / Magnético respondiendo correctamente.", "Prueba de Periféricos");
            btnTestImpresora.Click += (s, e) => MessageBox.Show("Imprimiendo ticket de prueba...\n\nNivel de papel: 85%", "Impresora");
            btnReinicio.Click += (s, e) => MessageBox.Show("Reiniciando componentes del teclado EPP y pantalla táctil...", "Reinicio");
            btnFirmware.Click += (s, e) => MessageBox.Show("Conectando con el servidor central para descargar última versión...", "Firmware");

            pnlHardware.Controls.Add(btnTestLector); pnlHardware.Controls.Add(btnTestImpresora);
            pnlHardware.Controls.Add(btnReinicio); pnlHardware.Controls.Add(btnFirmware);
        }

        // --- ÁREA 3: RED Y SEGURIDAD ---
        private void ConstruirPanelSeguridad()
        {
            pnlSeguridad = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlSeguridad);
            AgregarEncabezado(pnlSeguridad, "3. Configuración Red");
            AgregarPie(pnlSeguridad, (s, e) => MostrarPanel(pnlMenu));

            Button btnIp = CrearBotonAdmin("🌐 Parámetros de Red (IP/Host)", 100, 150, colorAzul, 450);
            Button btnLlaves = CrearBotonAdmin("🔑 Gestión de Llaves (3DES/AES)", 100, 220, colorAzul, 450);
            Button btnAlarmas = CrearBotonAdmin("🚨 Configuración de Alarmas", 100, 290, colorRojo, 450);

            btnIp.Click += (s, e) => MessageBox.Show("Host IP: 192.168.1.150\nSubred: 255.255.255.0\nPing al servidor del banco: Exitoso", "Red");
            btnLlaves.Click += (s, e) => MessageBox.Show("Cargando nuevas llaves criptográficas...\n\nEl cifrado del PIN está asegurado.", "Seguridad");
            btnAlarmas.Click += (s, e) => MessageBox.Show("Sensores de vibración y apertura de caja fuerte configurados en nivel ALTO.", "Sensores");

            pnlSeguridad.Controls.Add(btnIp); pnlSeguridad.Controls.Add(btnLlaves); pnlSeguridad.Controls.Add(btnAlarmas);
        }

        // --- ÁREA 4: CONSULTAS Y REGISTROS ---
        private void ConstruirPanelAuditoria()
        {
            pnlAuditoria = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo, Visible = false };
            this.Controls.Add(pnlAuditoria);
            AgregarEncabezado(pnlAuditoria, "4. Auditoría y Logs");
            AgregarPie(pnlAuditoria, (s, e) => MostrarPanel(pnlMenu));

            Button btnReporte = CrearBotonAdmin("📑 Reporte con Porcentajes", 100, 200, colorVerde, 450);
            Button btnTXT = CrearBotonAdmin("📄 Descargar Todo (TXT)", 100, 280, colorRojo, 450);

            btnReporte.Click += BtnGenerarReporte_Click;
            btnTXT.Click += BtnDescargarTXT_Click;

            pnlAuditoria.Controls.Add(btnReporte); pnlAuditoria.Controls.Add(btnTXT);
        }

        private void MostrarPanel(Panel panelDestino)
        {
            pnlMenu.Visible = false; pnlBoveda.Visible = false;
            pnlHardware.Visible = false; pnlSeguridad.Visible = false;
            pnlAuditoria.Visible = false; pnlUsuarios.Visible = false;
            panelDestino.Visible = true;
        }

        private void BtnGenerarReporte_Click(object sender, EventArgs e)
        {
            JObject peticion = new JObject();
            peticion["operacion"] = "REPORTE_ADMIN";

            JObject respuesta = ClienteRed.EnviarPeticion(peticion);

            if (respuesta["codigo"]?.ToString() == "00")
            {
                decimal retiros = (decimal)respuesta["retiros"];
                decimal depositos = (decimal)respuesta["depositos"];
                decimal totalVolumen = (decimal)respuesta["total"];
                int usuariosPin = (int)respuesta["usuariosPin"];

                GestorSistema.RegistrarActividadTXT("ADMIN", adminUser, "REPORTE", "Generó el dashboard estadístico por red");

                FrmReporteEstadistico frmGrafico = new FrmReporteEstadistico(retiros, depositos, totalVolumen, usuariosPin);
                frmGrafico.ShowDialog();
            }
            else
            {
                MessageBox.Show("Error al obtener los datos estadísticos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDescargarTXT_Click(object sender, EventArgs e)
        {
            JObject peticion = new JObject();
            peticion["operacion"] = "HISTORIAL_GLOBAL";

            JObject respuesta = ClienteRed.EnviarPeticion(peticion);

            if (respuesta["codigo"]?.ToString() == "00")
            {
                try
                {
                    JArray transacciones = (JArray)respuesta["transacciones"];

                    using (StreamWriter file = new StreamWriter("Historial_Transacciones.txt"))
                    {
                        file.WriteLine("====================================================================================================");
                        file.WriteLine("                             HISTORIAL COMPLETO DE TRANSACCIONES (VÍA RED)                          ");
                        file.WriteLine("====================================================================================================");
                        file.WriteLine(string.Format("{0,-10} | {1,-20} | {2,-16} | {3,-10} | {4,-12} | {5,-20}", "ID", "CLIENTE", "TARJETA", "TIPO", "MONTO (Q)", "FECHA Y HORA"));
                        file.WriteLine("----------------------------------------------------------------------------------------------------");

                        foreach (JObject t in transacciones)
                        {
                            string cliente = t["nombre"].ToString();
                            if (cliente.Length > 20) cliente = cliente.Substring(0, 17) + "...";

                            file.WriteLine(string.Format("{0,-10} | {1,-20} | {2,-16} | {3,-10} | {4,-12} | {5,-20}",
                                t["id"].ToString(), cliente, t["tarjeta"].ToString(), t["tipo"].ToString(),
                                Convert.ToDecimal(t["monto"]).ToString("N2"), t["fecha"].ToString()));
                        }
                        file.WriteLine("====================================================================================================");
                    }

                    MessageBox.Show("Archivo TXT generado exitosamente.\nBusque 'Historial_Transacciones.txt' en la carpeta bin/Debug.", "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    GestorSistema.RegistrarActividadTXT("ADMIN", adminUser, "DESCARGA TXT", "Exportó el log total vía JSON");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al escribir el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Error al descargar el historial desde el servidor.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



    }
}