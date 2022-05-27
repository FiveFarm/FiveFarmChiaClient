using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace KeyCloakApi
{
    public class ChiaServerApi
    {
        public bool IsConnected => socket != null && socket.ReadyState == WebSocketState.Open;

        public delegate void EventRequestHandler(string serverData);
        public EventRequestHandler ServerEventsHandler;
        WebSocket socket;
        string user = string.Empty;
        object websocketLock = new object();

        public async Task<string> Login(string loginUrl, string userName, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "crypto-desktop-client"),
                new KeyValuePair<string, string>("username", userName),
                new KeyValuePair<string, string>("password", password),
            });
            HttpClient httpClient = new HttpClient();

            var response = await httpClient.PostAsync(loginUrl, content);

            //will throw an exception if not successful
            response.EnsureSuccessStatusCode();

            string contentRead = await response.Content.ReadAsStringAsync();

            JObject receivedData = (JObject)JsonConvert.DeserializeObject(contentRead);
            string token = receivedData.Value<string>("access_token");

            return token;

        }

        public async Task<string> GetUser(string infoUrl, string token)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await httpClient.GetAsync(infoUrl);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return string.Empty;

            string contentRead = await response.Content.ReadAsStringAsync();

            JObject receivedData = (JObject)JsonConvert.DeserializeObject(contentRead);
            user = receivedData.Value<string>("sub");

            return user;
        }
        public void Connect(string socketURL, string token, string clientId)
        {
            if (IsConnected) return;

            socketURL = $"{socketURL}test?token={token}&clientId={clientId}";

            socket = new WebSocket(socketURL);
            socket.OnMessage += Socket_OnMessage;
            socket.Connect();
        }
        public void SendData(string data)
        {
            lock (websocketLock)
            {
                if (IsConnected)
                {
                    socket.Send(data);
                }
                else
                {
                    user = string.Empty;
                    if (socket != null) socket.Close();
                    throw new Exception("Socket not connected");
                }
            }
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            if (ServerEventsHandler != null)
                ServerEventsHandler(e.Data);
        }
    }
}
