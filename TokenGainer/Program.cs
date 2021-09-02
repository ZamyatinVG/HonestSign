﻿using System;
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
        static readonly Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        static readonly KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static TokenResponse tokenResponse;
        static Auth auth;
        static void Main()
        {
            foreach (var key in settings.AllKeys)
                if (key.StartsWith("thumbprint"))
                    GetToken(key.Substring(11));
        }
        public class Auth
        {
            public string Uuid { get; set; }
            public string Data { get; set; }
        }
        public class TokenResponse
        {
            public string Token { get; set; }
            public int Code { get; set; }
            public string Description { get; set; }
            public string Error_message { get; set; }
        }
        public static TokenResponse GetToken(string filial)
        {
            try
            {
                var client = new RestClient($"{settings["url"].Value}v3/auth/cert/key")
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                auth = JsonConvert.DeserializeObject<Auth>(response.Content);
                logger.Info($"Новый uuid ({auth.Uuid}) успешно получен.");
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового uuid!\n" + ex.Message);
            }
            string thumbprint = settings[$"thumbprint_{filial}"].Value;
            auth.Data = GetSignedData(thumbprint, auth.Data);
            try
            {
                var client = new RestClient($"{settings["url"].Value}v3/auth/cert/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", $@"{{""uuid"":""{auth.Uuid}"",""data"":""{auth.Data}""}}", ParameterType.RequestBody);
                var response = client.Execute(request);
                tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
                if (tokenResponse.Code == 500)
                {
                    logger.Error(tokenResponse.Error_message);
                    return null;
                }
                int count = 0;
                foreach (var key in settings.AllKeys)
                    if (key == $"token_{filial}")
                        count++;
                if (count == 0)
                    settings.Add($"token_{filial}", tokenResponse.Token);
                else
                    settings[$"token_{filial}"].Value = tokenResponse.Token;
                configFile.Save(ConfigurationSaveMode.Full);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                logger.Info($"Новый token_{filial} успешно получен.");
            }
            catch (Exception ex)
            {
                logger.Error("Ошибка получения нового token_{filial}!\n" + ex.Message);
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
                    {
                        certificate = crt;
                        break;
                    }
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