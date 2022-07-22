using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blackbaud.AuthCodeFlowTutorial.Services
{
    public class Conexion
    {
        public void conectar() 
        {
            SqlConnection conexion = new SqlConnection("server=DIEGO-PC ; database=base1 ; integrated security = true");
            try
            {
                    conexion.Open();

                    conexion.Close();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("IOException source: {0}", e.Source);
            }
            }
    }
}
