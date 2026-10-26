using System;
using System.Data.SqlClient;
using System.IO;

namespace CajeroCUNOR
{
    public static class GestorSistema
    {
        private static readonly string cadenaConexion = "Server=localhost;Database=CajeroATM;User Id=sa;Password=Cajero2026;TrustServerCertificate=True;";
        private static readonly string rutaDirectorio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RegistrosCajero");

        public static SqlConnection ObtenerConexion() => new SqlConnection(cadenaConexion);

        public static void RegistrarActividadTXT(string modulo, string usuario, string accion, string detalle)
        {
            if (!Directory.Exists(rutaDirectorio)) Directory.CreateDirectory(rutaDirectorio);

            string archivo = Path.Combine(rutaDirectorio, $"Bitacora_{DateTime.Now:yyyy_MM_dd}.txt");
            string linea = $"{DateTime.Now:HH:mm:ss} | [{modulo}] | USR: {usuario} | ACCIÓN: {accion} | DETALLE: {detalle}";

            File.AppendAllText(archivo, linea + Environment.NewLine);
        }

        public static decimal CalcularTotalDesglose(int q200, int q100, int q50, int q20, int q10, int q5, int q1)
        {
            return (q200 * 200) + (q100 * 100) + (q50 * 50) + (q20 * 20) + (q10 * 10) + (q5 * 5) + (q1 * 1);
        }

        public static bool VerificarDisponibilidadBilletes(int q200, int q100, int q50, int q20, int q10, int q5, int q1)
        {
            using (SqlConnection con = ObtenerConexion())
            {
                con.Open();
                int[] req = { q200, q100, q50, q20, q10, q5, q1 };
                int[] den = { 200, 100, 50, 20, 10, 5, 1 };

                for (int i = 0; i < 7; i++)
                {
                    if (req[i] > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT Cantidad FROM BilletesCajero WHERE Denominacion = @d", con))
                        {
                            cmd.Parameters.AddWithValue("@d", den[i]);
                            int stock = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                            if (stock < req[i]) return false;
                        }
                    }
                }
                return true;
            }
        }

        public static void ActualizarBilletesCajero(int q200, int q100, int q50, int q20, int q10, int q5, int q1, bool esSuma)
        {
            using (SqlConnection con = ObtenerConexion())
            {
                con.Open();
                int[] cant = { q200, q100, q50, q20, q10, q5, q1 };
                int[] den = { 200, 100, 50, 20, 10, 5, 1 };
                string operador = esSuma ? "+" : "-";

                for (int i = 0; i < 7; i++)
                {
                    if (cant[i] > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand($"UPDATE BilletesCajero SET Cantidad = Cantidad {operador} @c WHERE Denominacion = @d", con))
                        {
                            cmd.Parameters.AddWithValue("@c", cant[i]);
                            cmd.Parameters.AddWithValue("@d", den[i]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}