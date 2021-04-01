using System;
using System.Text;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using RestSharp;
using Newtonsoft.Json;
using NLog;

namespace TokenGainer
{
    class Program
    {
        static Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        static KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;
        static Logger logger = LogManager.GetCurrentClassLogger();
        static TokenResponse tokenResponse;
        static Auth auth;
        static void Main(string[] args)
        {
            GetToken();
        }
        public class Auth
        {
            public string uuid { get; set; }
            public string data { get; set; }
        }
        public class TokenResponse
        {
            public string token { get; set; }
            public int code { get; set; }
            public string description { get; set; }
            public string error_message { get; set; }
        }
        public static TokenResponse GetToken()
        {
            try
            {
                var client = new RestClient($"{settings["url"].Value}v3/auth/cert/key");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                auth = JsonConvert.DeserializeObject<Auth>(response.Content);
                logger.Info($"Новый uuid ({auth.uuid}) успешно получен.");
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового uuid!\n" + ex.Message);
            }
            string thumbprint = settings["thumbprint"].Value;
            auth.data = GetSignedData(thumbprint, auth.data);
            try
            {
                var client = new RestClient($"{settings["url"].Value}v3/auth/cert/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", $@"{{""uuid"":""{auth.uuid}"",""data"":""{auth.data}""}}", ParameterType.RequestBody);
                var response = client.Execute(request);
                tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
                settings["token"].Value = tokenResponse.token;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                logger.Info($"Новый token успешно получен.");
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового token!\n" + ex.Message);
            }
            return tokenResponse;
        }
        public static string GetSignedData(string thumbprint, string data)
        {
            try
            {
                X509Certificate2 certificate = null;
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                foreach (var crt in store.Certificates)
                    if (crt.Thumbprint.ToLower() == thumbprint)
                        certificate = crt;
                //данные для подписи
                var content = new ContentInfo(Encoding.ASCII.GetBytes(data));
                var signedCms = new SignedCms(content, false);
                //настраиваем сертификат для подлиси, добавляем дату
                var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate);
                signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.Now));
                //формируем подпись
                signedCms.ComputeSignature(signer, false);
                var sign = signedCms.Encode();
                logger.Info($"Новый data успешно подписан.");
                return Convert.ToBase64String(sign);
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка подписания data!\n" + ex.Message);
                return "";
            }
        }
    }
}