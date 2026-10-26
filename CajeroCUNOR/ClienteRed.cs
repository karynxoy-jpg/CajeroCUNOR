using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace CajeroCUNOR
{
    public static class ClienteRed
    {
        private static string ipServidor = "127.0.0.1"; // Dirección local
        private static int puerto = 5000;

        public static string CodigoCajero = "ATM-01"; // Identificador del cajero
        private static Form pantallaBloqueo = null;

        public static JObject EnviarPeticion(JObject peticion)
        {
            try
            {
                using (TcpClient cliente = new TcpClient(ipServidor, puerto))
                using (NetworkStream stream = cliente.GetStream())
                {
                    string jsonEnvio = peticion.ToString();
                    byte[] dataEnvio = Encoding.UTF8.GetBytes(jsonEnvio);
                    stream.Write(dataEnvio, 0, dataEnvio.Length);

                    byte[] buffer = new byte[4096];
                    int bytesLeidos = stream.Read(buffer, 0, buffer.Length);
                    string jsonRecibido = Encoding.UTF8.GetString(buffer, 0, bytesLeidos);

                    return JObject.Parse(jsonRecibido);
                }
            }
            catch (Exception)
            {
                JObject error = new JObject();
                error["codigo"] = "99";
                error["mensaje"] = "El servidor central no responde. Cajero Fuera de Servicio.";
                return error;
            }
        }

        // NUEVO: Método Asíncrono para el Heartbeat y los Comandos Remotos
        public static async void IniciarHeartbeat()
        {
            while (true)
            {
                await Task.Delay(10000); // Latido cada 10 segundos (Requisito S-04)

                try
                {
                    JObject req = new JObject();
                    req["operacion"] = "HEARTBEAT";
                    req["cajero"] = CodigoCajero;

                    JObject res = EnviarPeticion(req);

                    if (res["codigo"]?.ToString() == "00")
                    {
                        string comando = res["comando"]?.ToString();

                        // Ejecutamos la orden remota del servidor (Requisito S-09)
                        if (comando == "FUERA_DE_SERVICIO" && pantallaBloqueo == null)
                        {
                            BloquearCajero();
                        }
                        else if (comando == "HABILITAR" && pantallaBloqueo != null)
                        {
                            DesbloquearCajero();
                        }
                    }
                }
                catch { /* Si hay error de red temporal, el ciclo continúa sin crashear el cajero */ }
            }
        }

        private static void BloquearCajero()
        {
            pantallaBloqueo = new Form();
            pantallaBloqueo.Text = "FUERA DE SERVICIO";
            pantallaBloqueo.FormBorderStyle = FormBorderStyle.None;
            pantallaBloqueo.WindowState = FormWindowState.Maximized;
            pantallaBloqueo.BackColor = System.Drawing.ColorTranslator.FromHtml("#1A237E"); // Tu color azul
            pantallaBloqueo.TopMost = true; // Hace que nadie la pueda minimizar ni tapar

            Label lbl = new Label();
            lbl.Text = "⚠️ CAJERO FUERA DE SERVICIO ⚠️\n\nPor favor, utilice otra terminal de la red.";
            lbl.Font = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.White;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            pantallaBloqueo.Controls.Add(lbl);
            pantallaBloqueo.Show();
        }

        private static void DesbloquearCajero()
        {
            if (pantallaBloqueo != null)
            {
                pantallaBloqueo.Close();
                pantallaBloqueo = null;
            }
        }
    }
}