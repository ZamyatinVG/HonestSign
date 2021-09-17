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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
        private static string token = string.Empty;
        public class BarcodeResult
        {
            public string Barcode { get; set; }
            public string Error { get; set; }
        }
        public static string GetBarcode(string id, string nodoc)
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
                RestClient client;
                if (nodoc == null)
                    client = new RestClient($"https://apps.shatura.com/shatura1/bc/getbarcode/token={token}/ingred={id}");
                else
                    client = new RestClient($"https://apps.shatura.com/shatura1/bc/getsimplebarcodeorder/{token}/{id}/{nodoc}");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                logger.Info($"Данные по товару Шатуры (id = {id}, nodoc = {nodoc}) успешно получены.");
                BarcodeResult barcode = JsonConvert.DeserializeObject<BarcodeResult>(response.Content);
                if (barcode.Error != null)
                {
                    logger.Error($"Сервис Шатуры вернул сообщение об ошибке по товару (id = {id}, nodoc = {nodoc}): {barcode.Error}");
                    return "";
                }
                else
                    return ($"{barcode.Barcode}");
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка получения данных по товару Шатуры ({id})!\n" + ex.Message);
                return "";
            }
        }
    }
}