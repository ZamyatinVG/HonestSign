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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
        private static string token = string.Empty;
        public class CIS
        {
            public string Uit { get; set; }
            public string Cis { get; set; }
            public string Gtin { get; set; }
            public string Sgtin { get; set; }
            public string TnVedEaesCode { get; set; }
            public string ProductName { get; set; }
            public DateTime EmissionDate { get; set; }
            public string ProducerName { get; set; }
            public string ProducerInn { get; set; }
            public string LastDocId { get; set; }
            public string LastDocType { get; set; }
            public string EmissionType { get; set; }
            public List<object> PrevCises { get; set; }
            public List<object> NextCises { get; set; }
            public string Status { get; set; }
            public string PackType { get; set; }
            public DateTime IntroducedDate { get; set; }
            public DateTime LastStatusChangeDate { get; set; }
            public string ProductGroup { get; set; }
            public bool MarkWithdraw { get; set; }
            public int Code { get; set; }
            public string Error { get; set; }
            public string Error_message { get; set; }
        }
        public static CIS TranslateCis(CIS cis)
        {
            if (cis.Status == "EMITTED") cis.Status = "Эмитирован. Выпущен";
            if (cis.Status == "APPLIED") cis.Status = "Эмитирован. Получен";
            if (cis.Status == "INTRODUCED") cis.Status = "В обороте";
            if (cis.Status == "WRITTEN_OFF") cis.Status = "Списан";
            if (cis.Status == "RETIRED") cis.Status = "Выбыл";
            if (cis.EmissionType == "REMAINS") cis.EmissionType = "Маркировка остатков";
            if (cis.EmissionType == "PRODUCTION") cis.EmissionType = "Произведено в РФ";
            if (cis.EmissionType == "IMPORT") cis.EmissionType = "Импорт";
            if (cis.EmissionType == "REMARK") cis.EmissionType = "Перемаркировка";
            if (cis.EmissionType == "COMMISSION") cis.EmissionType = "Принят на комиссию";
            return cis;
        }
        public static CIS GetStatus(string km, bool refreshtoken)
        {
            CIS cis = new CIS();
            if (token == string.Empty)
                token = config.AppSettings.Settings["token"].Value;
            if (token == string.Empty || refreshtoken)
                if (!GetToken())
                {
                    cis.Error = "1001";
                    cis.Error_message = "Ошибка получения нового token!";
                    return cis;
                }
            try
            {
                var client = new RestClient($"{config.AppSettings.Settings["url"].Value}v4/facade/identifytools/info?childrenPage=1&childrenLimit=50&cis={HttpUtility.UrlEncode(km, Encoding.UTF8)}")
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);
                logger.Info($"Данные по КМ ({km}) успешно получены.");
                cis = JsonConvert.DeserializeObject<CIS>(response.Content);
                //Пробуем обновить token
                if (cis.Error == "invalid_token" && !refreshtoken)
                    return GetStatus(km, true);
                if (cis.Status != null)
                    cis = TranslateCis(cis);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка получения данных по КМ ({km})\n" + ex.Message);
                cis.Error = "1002";
                cis.Error_message = $"Ошибка получения данных по КМ ({km})";
            }
            return cis;
        }
        public static bool GetToken()
        {
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                FileStream fs = new FileStream(config.AppSettings.Settings["TokenGainer"].Value, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);
                token = xmldoc.ChildNodes[1].ChildNodes[1].ChildNodes[2].Attributes[1].Value;
                config.AppSettings.Settings["token"].Value = token;
                config.Save(ConfigurationSaveMode.Modified, true);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового token!\n" + ex.Message);
                return false;
            }
        }
    }
}