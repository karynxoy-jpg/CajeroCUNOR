using System;
using System.Windows.Forms;

namespace CajeroCUNOR
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Aquí le indicamos que la primera pantalla en abrirse es Form1
            Application.Run(new Form1());
        }
    }
}