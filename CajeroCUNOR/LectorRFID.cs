using System;
using System.IO.Ports;

namespace CajeroCUNOR
{
    public class LectorRFID
    {
        private SerialPort puerto;

        // Aviso que se dispara cada vez que se lee una tarjeta
        public event Action<string> TarjetaLeida;

        public bool Conectar(string com)
        {
            try
            {
                puerto = new SerialPort(com, 9600);
                puerto.DataReceived += (s, e) =>
                {
                    string uid = puerto.ReadLine().Trim();
                    TarjetaLeida?.Invoke(uid);
                };
                puerto.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Desconectar()
        {
            if (puerto != null && puerto.IsOpen) puerto.Close();
        }
    }
}