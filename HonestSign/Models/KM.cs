using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Configuration;
using System.Configuration;
using System.Xml;
using System.IO;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using NLog;

namespace HonestSign.Models
{
    public class KM
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        static Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
        static string token;
        public class TokenResponse
        {
            public string token { get; set; }
            public int code { get; set; }
            public string description { get; set; }
            public string error_message { get; set; }
        }
        public class Cis
        {
            public string uit { get; set; }
            public string cis { get; set; }
            public string gtin { get; set; }
            public string sgtin { get; set; }
            public string tnVedEaesCode { get; set; }
            public string productName { get; set; }
            public DateTime emissionDate { get; set; }
            public string producerName { get; set; }
            public string producerInn { get; set; }
            public string lastDocId { get; set; }
            public string lastDocType { get; set; }
            public string emissionType { get; set; }
            public List<object> prevCises { get; set; }
            public List<object> nextCises { get; set; }
            public string status { get; set; }
            public string packType { get; set; }
            public DateTime introducedDate { get; set; }
            public DateTime lastStatusChangeDate { get; set; }
            public string productGroup { get; set; }
            public bool markWithdraw { get; set; }
            public int code { get; set; }
            public string description { get; set; }
            public string error { get; set; }
            public string error_message { get; set; }
        }
        public static Cis TranslateCis(Cis cis)
        {
            if (cis.status == "EMITTED") cis.status = "Эмитирован. Выпущен";
            if (cis.status == "APPLIED") cis.status = "Эмитирован. Получен";
            if (cis.status == "INTRODUCED") cis.status = "В обороте";
            if (cis.status == "WRITTEN_OFF") cis.status = "Списан";
            if (cis.status == "RETIRED") cis.status = "Выбыл";
            if (cis.emissionType == "REMAINS") cis.emissionType = "Маркировка остатков";
            if (cis.emissionType == "PRODUCTION") cis.emissionType = "Произведено в РФ";
            if (cis.emissionType == "IMPORT") cis.emissionType = "Импорт";
            if (cis.emissionType == "REMARK") cis.emissionType = "Перемаркировка";
            if (cis.emissionType == "COMMISSION") cis.emissionType = "Принят на комиссию";
            return cis;
        }
        public static string GetStatus(string km, bool refreshtoken)
        {
            token = config.AppSettings.Settings["token"].Value;
            if (token == string.Empty || refreshtoken)
                GetToken();
            Cis cis = new Cis();
            try
            {
                var client = new RestClient($"{config.AppSettings.Settings["url"].Value}v4/facade/identifytools/info?childrenPage=1&childrenLimit=50&cis={HttpUtility.UrlEncode(km, Encoding.UTF8)}");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);
                logger.Info($"Данные по КМ ({km}) успешно получены.");
                cis = JsonConvert.DeserializeObject<Cis>(response.Content);
                if (cis.error != null && !refreshtoken)
                    return GetStatus(km, true);
                if (cis.status == null)
                    return (cis.error_message + "!");
                else
                    cis = TranslateCis(cis);
                return($"{cis.status} ({cis.emissionType})");
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка получения данных по КМ ({km})!\n" + ex.Message);
                return("Ошибка получения статуса!");
            }
        }
        public static void GetToken()
        {
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                FileStream fs = new FileStream(config.AppSettings.Settings["TokenGainer"].Value, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);
                token = xmldoc.ChildNodes[1].ChildNodes[1].ChildNodes[2].Attributes[1].Value;
                config.AppSettings.Settings["token"].Value = token;
                config.Save(ConfigurationSaveMode.Modified, true);
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового token!\n" + ex.Message);
            }
        }
    }
}