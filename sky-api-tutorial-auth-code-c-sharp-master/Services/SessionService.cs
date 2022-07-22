using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;

namespace Blackbaud.AuthCodeFlowTutorial.Services
{

    /// <summary>
    /// Sets, gets, and destroys session variables.
    /// </summary>
    public class SessionService : ISessionService
    {
        private const string ACCESS_TOKEN_NAME = "token";
        private const string REFRESH_TOKEN_NAME = "refreshToken";
        private readonly IHttpContextAccessor _httpContextAccessor;
         

        public SessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        /// <summary>
        /// Destroys access and refresh tokens from the session.
        /// </summary>
        public void ClearTokens()
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Remove(ACCESS_TOKEN_NAME);
                _httpContextAccessor.HttpContext.Session.Remove(REFRESH_TOKEN_NAME);
            }
            catch (Exception error)
            {
                Console.WriteLine("LOGOUT ERROR: " + error.Message);
            }
        }


        /// <summary>
        /// Return access token, if saved, or an empty string.
        /// </summary>
        public string GetAccessToken()
        {
            return TryGetString(ACCESS_TOKEN_NAME);
        }


        /// <summary>
        /// Return refresh token, if saved, or an empty string.
        /// </summary>
        public string GetRefreshToken()
        {
            return TryGetString(REFRESH_TOKEN_NAME);
        }


        /// <summary>
        /// Sets the access and refresh tokens based on an HTTP response.
        /// </summary>
        public void SetTokens(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string jsonString = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, string> attrs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                MakeRequest(jsonString.Substring(17, 978));
                _httpContextAccessor.HttpContext.Session.SetString(ACCESS_TOKEN_NAME, attrs["access_token"]);
                _httpContextAccessor.HttpContext.Session.SetString(REFRESH_TOKEN_NAME, attrs["refresh_token"]);
            }
        }

        public async void MakeRequest(string token)
        {
            string mytoken = "Bearer " + token;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Bb-Api-Subscription-Key", "86be8db6c53b474e99f9311f928623be");
            client.DefaultRequestHeaders.Add("Authorization", mytoken);


            int batchID = buscarValorMaximo();
            int maxID = buscarMaximo();
            int batchMax = maxID + batchID;
            while (batchID < batchMax) 
            {
                var uri = "https://api.sky.blackbaud.com/generalledger/v1/journalentrybatches/"+ batchID;
                //var uri = "https://api.sky.blackbaud.com/generalledger/v1/journalentrybatches/2500";
                var response="";
                var responseStatus = "";

                try
                {
                response = await client.GetStringAsync(uri);
                }
                    catch (Exception ex)
                {
                //Console.WriteLine("Error de BATCH ID" + ex);
                responseStatus = ex.ToString();
                }


               // Console.WriteLine(response);
            // Cabecera cabecera = JsonConvert.DeserializeObject<Cabecera>(response);
            //Console.WriteLine(cabecera);
                if (! responseStatus.Contains("Response status code does not indicate success: 404 (Entity not found)."))
                {
                Root dato = JsonConvert.DeserializeObject<Root>(response);
                Console.WriteLine(dato);
                guardarEncabezados(dato.batch_id, dato.ui_batch_id, dato.description.Replace("'", "''"), dato.batch_status,dato.source_base_url,
                dato.source_system_name, dato.date_added.ToString(), dato.added_by.Replace("'", "''"), dato.date_modified.ToString(), dato.modified_by.Replace("'", "''"));

                foreach (var item in dato.journal_entries)
                {
                guardarItems(item.journal_entry_id, item.type_code, item.line_number, item.post_date.ToString(), item.encumbrance, item.journal.Replace("'", "''"), item.reference.Replace("'","''"), item.amount, item.notes, item.relative_source_url, item.reverse_transaction_id, item.account_number, dato.batch_id, dato.ui_batch_id);
                //Console.WriteLine(item.account_number);
                }
                }
                batchID++;
            }
            //Abrir la herramienta para generar las pólizas
            
            //Process.Start(@"C:\HtasPolizas\Debug\ConsoleAppCHMX.exe");
            //Process.Start(@"C:\Users\DM-SISTEMAS\Documents\VisualStudioProjects\ConsoleAppCHMX\ConsoleAppCHMX\bin\Debug\ConsoleAppCHMX.exe");
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class TransactionCodeValue
        {
            public string name { get; set; }
            public string value { get; set; }
            public int id { get; set; }
            public bool inactive { get; set; }

        }

        public class Distribution
        {
            public int distribution_id { get; set; }
            public string ui_project_id { get; set; }
            public string account_class { get; set; }
            public List<TransactionCodeValue> transaction_code_values { get; set; }
            public double amount { get; set; }
            public double percent { get; set; }

        }

        public class JournalEntry
        {
            public int journal_entry_id { get; set; }
            public string type_code { get; set; }
            public int line_number { get; set; }
            public string account_number { get; set; }
            public DateTime post_date { get; set; }
            public string encumbrance { get; set; }
            public string journal { get; set; }
            public string reference { get; set; }
            public double amount { get; set; }
            public string notes { get; set; }
            public string relative_source_url { get; set; }
            public List<Distribution> distributions { get; set; }
            public List<object> custom_fields { get; set; }
            public object reverse_date { get; set; }
            public int reverse_transaction_id { get; set; }

        }

        public class Root
        {
            public int batch_id { get; set; }
            public string ui_batch_id { get; set; }
            public string description { get; set; }
            public string batch_status { get; set; }
            public bool create_interfund_sets { get; set; }
            public bool create_bank_account_adjustments { get; set; }
            public string source_base_url { get; set; }
            public string source_system_name { get; set; }
            public List<JournalEntry> journal_entries { get; set; }
            public DateTime date_added { get; set; }
            public string added_by { get; set; }
            public DateTime date_modified { get; set; }
            public string modified_by { get; set; }

        }


        /// <summary>
        /// Return session value as a string (if it exists), or an empty string.
        /// </summary>
        private string TryGetString(string name)
        {
            byte[] valueBytes = new Byte[700];
            bool valueOkay = _httpContextAccessor.HttpContext.Session.TryGetValue(name, out valueBytes);
            if (valueOkay)
            {
                return System.Text.Encoding.UTF8.GetString(valueBytes);
            }
            return null;
        }

        //**INICIA BUSCAR VALOR MÁXIMO DEL MAXIMO EN LA TABLA VALMAX**//
        public int buscarMaximo()
        {
            int valMax = 0;
            string connectionString = null;
            SqlConnection connection;
            connectionString = "Password=dev22;Persist Security Info=True;User ID=sa;Initial Catalog=SKYAPI;Data Source=DESKTOP-9TOA5T5\\SQLEXPRESS;";
            connection = new SqlConnection(connectionString);
            string sql = "SELECT MAX(MAXIMO) BATCH_ID FROM VALMAX";
            SqlCommand sqlCommand;
            SqlDataReader sqlDataReader;
            try
            {
                connection.Open();
                sqlCommand = new SqlCommand(sql, connection);
                sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Console.WriteLine(sqlDataReader.GetValue(0));
                    valMax = (int)sqlDataReader.GetValue(0);
                }
                sqlDataReader.Dispose();
                sqlCommand.Dispose();

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return valMax;

        }
        //**FINALIZA BUSCAR VALOR MÁXIMO DEL MAXIMO EN LA TABLA VALMAX**//

        //**INICIA BUSCAR VALOR MÁXIMO DEL BATCH_ID EN LA TABLA ENCABEZADOS**//
        public int buscarValorMaximo()
        {
            int valMax = 0;
            string connectionString = null;
            SqlConnection connection;
            connectionString = "Password=dev22;Persist Security Info=True;User ID=sa;Initial Catalog=SKYAPI;Data Source=DESKTOP-9TOA5T5\\SQLEXPRESS;";
            connection = new SqlConnection(connectionString);
            string sql= "SELECT MAX(BATCH_ID)+1 BATCH_ID FROM ENCABEZADOS";
            SqlCommand sqlCommand;
            SqlDataReader sqlDataReader;
            try
            {
                connection.Open();
                sqlCommand = new SqlCommand(sql,connection);
                sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read()) 
                {
                    Console.WriteLine(sqlDataReader.GetValue(0));
                    valMax = (int)sqlDataReader.GetValue(0);
                }
                sqlDataReader.Dispose();
                sqlCommand.Dispose();

                connection.Close();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
            return valMax;

        }
        //**FINALIZA BUSCAR VALOR MÁXIMO DEL BATCH_ID EN LA TABLA ENCABEZADOS**//

        //**INICIA GUARDAR ENCABEZADOS**//
        public void guardarEncabezados(int p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9, string p10) 
        {
            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = "INSERT INTO ENCABEZADOS (BATCH_ID , UI_BATCH_ID, DESCRIPTION, BATCH_STATUS, SOURCE_BASE_URL, SOURCE_SYSTEM_NAME, DATE_ADDED, ADDED_BY, DATE_MODIFIED, MODIFIED_BY) " +
                "VALUES ("+ p1 + " ,'" + p2 + "','" + p3 + "','" + p4 + "','" + p5 + "','" + p6 + "','" + p7 + "','" + p8 + "','" + p9 + "','" + p10 + "')";
            connectionString = "Password=dev22;Persist Security Info=True;User ID=sa;Initial Catalog=SKYAPI;Data Source=DESKTOP-9TOA5T5\\SQLEXPRESS;";
            connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                Console.WriteLine("Conexión abierta");
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                command.Dispose();
                connection.Close();
                Console.WriteLine("DATO GUARDADO...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        //**FINALIZA GUARDAR ENCABEZADOS**//


        //**INICIA GUARDAR ITEMS**//
        public void guardarItems(int p1, string p2, int p3, string p4, string p5, string p6, string p7, double p8, string p9, string p10, int p11, string p12, int p13, string p14)
        {
            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = "INSERT INTO ITEMS (JOURNAL_ENTRY_ID, TYPE_CODE, LINE_NUMBER, POST_DATE, ENCUMBRANCE, JOURNAL, REFERENCE, AMOUNT, NOTES ,RELATIVE_SOURCE_URL, REVERSE_TRANSACTION_ID, ACCOUNT_NUMBER, BATCH_ID , UI_BATCH_ID) " +
                "VALUES (" + p1 + " ,'" + p2 + "','" + p3 + "','" + p4 + "','" + p5 + "','" + p6 + "','" + p7 + "','" + p8 + "','" + p9 + "','" + p10 + "','" + p11 + "','" + p12 + "','" + p13 + "','" + p14 + "')";
            connectionString = "Password=dev22;Persist Security Info=True;User ID=sa;Initial Catalog=SKYAPI;Data Source=DESKTOP-9TOA5T5\\SQLEXPRESS;";
            connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                //Console.WriteLine("Conexión abierta");
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                command.Dispose();
                connection.Close();
                //Console.WriteLine("DATO GUARDADO...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        //**FINALIZA GUARDAR ITEMS**//
    }
}