using System;
using System.Web.Configuration;
using System.Configuration;
using RestSharp;
using Newtonsoft.Json;
using NLog;

namespace HonestSign.Models
{
    public class Shatura
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
        private static string token = string.Empty;
        public class Barcode
        {
            public string barcode { get; set; }
            public string error { get; set; }
        }
        public static string GetBarcode(string id)
        {
            try
            {
                if (token == string.Empty)
                    token = config.AppSettings.Settings["ShaturaToken"].Value;
                if (token == string.Empty)
                {
                    logger.Error($"Ошибка получения token Шатуры!");
                    return "";
                }
                var client = new RestClient($"https://apps.shatura.com/shatura1/bc/getbarcode/token={token}/ingred={id}");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                logger.Info($"Данные по товару Шатуры ({id}) успешно получены.");
                Barcode barcode = JsonConvert.DeserializeObject<Barcode>(response.Content);
                if (barcode.error != null)
                {
                    logger.Error($"Сервис Шатуры вернул сообщение об ошибке по товару ({id}): {barcode.error}");
                    return "";
                }
                else
                    return ($"{barcode.barcode}");
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка получения данных по товару Шатуры ({id})!\n" + ex.Message);
                return "";
            }
        }
    }
}