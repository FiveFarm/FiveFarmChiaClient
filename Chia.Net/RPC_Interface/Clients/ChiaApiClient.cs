using Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public abstract class ChiaApiClient : Service
    {
        private readonly string SslDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".chia/mainnet/config/ssl");

        private HttpClient Client;
        private readonly string CertName;
        public readonly string ApiUrl;

        public ChiaApiClient(string certName, string apiUrl)
        {
            CertName = certName;
            ApiUrl = apiUrl;
            InitializeAsync();
        }
        public ChiaApiClient(string apiUrl)
        {
            ApiUrl = apiUrl;
            Client = new HttpClient();
        }

        protected override ValueTask InitializeAsync()
        {
            string pass = "password";
            string publicKeyPath = Path.Combine(SslDirectory, CertName, $"private_{CertName}.crt");
            string keyPath = Path.Combine(SslDirectory, CertName, $"private_{CertName}.key");
            string certificatePath = Path.Combine(SslDirectory, CertName, $"private_{CertName}.pfx");
            //var certificate = X509Certificate2.CreateFromPemFile(certificatePath, keyPath);
            if (!File.Exists(certificatePath))
            {
                byte[] certificateData = ExportCertificate(publicKeyPath, keyPath, pass);
                File.WriteAllBytes(certificatePath, certificateData);
            }

            X509Certificate2 certificate = new X509Certificate2(certificatePath, pass, X509KeyStorageFlags.Exportable);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            handler.ClientCertificates.Add(certificate);
            Client = new HttpClient(handler);

            return ValueTask.CompletedTask;
        }


        public virtual async Task<string> PostAsync(Uri requestUri, IDictionary<string, string> parameters = null)
        {
            //var requestUri = new Uri(ApiUrl + rpcName);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(parameters ?? new Dictionary<string, string>())
            };

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Common.CommonConstants.SaveDebugLog($"PostAsync: {requestUri}, Data Length :{result.ToString().Length}", false, true);

            return result;
        }
        public virtual async Task<string> GetAsync(Uri requestUri, IDictionary<string, string> parameters = null, int timeout_ss = -1)
        {
            //var requestUri = new Uri(ApiUrl + rpcName);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Content = JsonContent.Create(parameters ?? new Dictionary<string, string>())
            };

            if(timeout_ss!=-1)
            {
                Client.Timeout=new TimeSpan(0,0,timeout_ss);
            }
            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Common.CommonConstants.SaveDebugLog($"GetAsync: {requestUri}, Data Length :{result.ToString().Length}", false, true);

            return result;
        }


        private static byte[] ExportCertificate(string crtPath, string keyPath, string pass)
        {
            var abyPrivateKey = File.ReadAllBytes(keyPath);
            byte[] privateKeyPkcs1DER = ConvertPKCS1PemToDer(Encoding.UTF8.GetString(abyPrivateKey));

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(privateKeyPkcs1DER, out int bytesRead);
                using (X509Certificate2 pubOnly = new X509Certificate2(crtPath))
                using (X509Certificate2 pubPrivEphemeral = pubOnly.CopyWithPrivateKey(rsa))
                {
                    return pubPrivEphemeral.Export(X509ContentType.Pfx, pass);
                }
            }

        }
        private static byte[] ConvertPKCS1PemToDer(string pemContents)
        {
            string base64 = pemContents.Replace("-----BEGIN RSA PRIVATE KEY-----", "");
            base64 = base64.Replace("-----END RSA PRIVATE KEY-----", "");
            base64 = base64.Replace("\r", "");
            base64 = base64.Replace("\n", "");
            return Convert.FromBase64String(base64);
        }

        public async Task<string> GetConnections()
        {
            var result = await PostAsync(SharedRoutes.GetConnections(ApiUrl));
            Common.CommonConstants.SaveDebugLog($"GetConnections Data Length: {result.ToString().Length}", false, true);
            return result;
        }

    }
}
