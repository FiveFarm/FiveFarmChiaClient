using Chia.ClientCore.Core;
using Chia.Common;
using Chia.DB;
using Chia.DB.Common;
using Chia.DB.Repositories;
using Chia.Net;
using Chia.Net.CLI_Interface;
using Chia.Net.LogExplorer;
using Chia.NET.Clients;
using KeyCloakApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chia.ClientCore.Core
{
    public class Worker : IDisposable
    {
        #region Private Members
        private readonly ChiaServerApi _wrapper;
        private readonly ChiaClientManager _chiaClient;
        string token;
        string userId;
        string clientId;
        SettingsRepository _settings;
        LogsRepository _logsRepo;
        clsStatus clsstatus;
        object initializationLock = new object();
        static bool RunThread = true;

        string LogMesssage
        {
            set
            {
                try
                {
                    if (SaveLogToDB)
                        _logsRepo.AddLog(value);
                }
                catch (Exception ex) { }
            }
        }

        private void AddLogMessage(string msg)
        {
            LogMesssage = msg;
        }
        #endregion
        #region Properties
        public bool SaveLogToDB { get; set; } = true;
        #endregion

        #region Constructor
        public Worker()
        {
            DBContext dB = new DBContext();
            _wrapper = new KeyCloakApi.ChiaServerApi();
            _chiaClient = new ChiaClientManager(
               new FarmerClient(),
               new HarvesterClient(),
               new FullNodeClient(),
               new WalletClient(),
               new Models.NodeInfo());
            _settings = new SettingsRepository();
            _logsRepo = new LogsRepository(dB);
            dB.Database.Migrate();
            clsstatus = new clsStatus(_logsRepo);
            clsStatus.LogMessageAction = AddLogMessage;
        }

        #endregion

        DateTime DateChange = DateTime.MinValue;
        public async Task RunClientProcessing()
        {
            try
            {
                CommonConstants.DeletePassphraseFiles();

                try
                {
                    DateTime.TryParse(_settings.GetValue(SettingKeys.LastLogTimePoolError), out CommonConstants.LastLogTime_PoolStopped);
                }
                catch (Exception ex)
                {
                    CommonConstants.LastLogTime_PoolStopped = DateTime.MinValue;
                    CommonConstants.SaveDebugLog($"Get LastLogTimePoolError: ex: {ex.Message}", false, true);
                }
                try
                {
                    DateTime.TryParse(_settings.GetValue(SettingKeys.LastLogTimeBlockFound), out CommonConstants.LastLogTime_BlockFound);
                }
                catch (Exception ex)
                {
                    CommonConstants.LastLogTime_PoolStopped = DateTime.MinValue;
                    CommonConstants.SaveDebugLog($"Get LastLogTimeBlockFound: ex: {ex.Message}", false, true);
                }
                DateTime prevTime;
                while (RunThread)
                {
                    prevTime = DateTime.Now;

                    try
                    {
                        //Save Version on App Startup and each date change
                        if (DateChange.Date < prevTime.Date)
                        {
                            CommandLineExec.GetChiaVersion();
                            CommandLineExec.GetAndSaveChiaFolderFileList();
                            DateChange = prevTime;
                        }
                        await ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        LogMesssage = ($"Error:{ex.Message}");
                    }
                    while (DateTime.Now < prevTime.AddMinutes(1))
                    {
                        await Task.Delay(50);
                    }

                    LogMesssage = ($"Next Call:{DateTime.Now.ToString()}");
                    LogMesssage = ($"====================================");
                }
            }
            catch (Exception ex)
            {
                LogMesssage = ($"Error:{ex.Message}");
            }
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await ProcessDataRequest();
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"ExecuteAsync:ex: {ex.Message}", false, true);
                LogMesssage = $"Error : {ex.Message}";
                try
                {
                    await Initialize();
                }
                catch (Exception ex1)
                {
                    CommonConstants.SaveDebugLog($"ExecuteAsync:ex: {ex.Message}", false, true);
                    LogMesssage = ex1.Message;
                }
            }
        }

        #region Private Methods
        private async Task<bool> Initialize()
        {
            string tokenPrevToken = token;
            token = _settings.GetValue(SettingKeys.TokenKeyName);
            if (string.IsNullOrEmpty(token))
            {
                LogMesssage = ($"token:Unauthenticated access . . .");
                return false;
            }
            else
            {
                if (tokenPrevToken != token)
                {
                    LogMesssage = ("==== Updated Token ===");
                    userId = "";
                    clientId = "";
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                userId = await _wrapper.GetUser(CommonConstants.InfoUrl, token);
                if (string.IsNullOrEmpty(userId))
                {
                    LogMesssage = ($"user: Unauthenticated access . . .");
                    return false;
                }
            }

            if (string.IsNullOrEmpty(clientId))
            {
                clientId = CommandLineExec.GetFingerPrint(CommonConstants.DataFolder);

                if (string.IsNullOrEmpty(clientId))
                {
                    LogMesssage = ($"Client Id not Found.");
                    return false;
                }
                else
                {
                    clientId = NodeIdentification.GetClientId(clientId, userId);
                    CommonConstants.SaveDebugLog($"clientId:{clientId}", false, true);
                    LogMesssage = $"ClientId: {clientId}";
                    CommonConstants.DeletePassphraseFiles();
                }
            }

            if (!_wrapper.IsConnected)
            {
                LogMesssage = ($"Connecting to Websocket.");
                _wrapper.Connect(CommonConstants.SocketAddress, token, clientId);

                if (!_wrapper.IsConnected)
                {
                    LogMesssage = ($"Websocket not connected.");
                }
            }
            return true;
        }

        private async Task ProcessDataRequest(bool postGlobalData = false)
        {
            bool initialized = await Initialize();

            if (!initialized)
            {
                LogMesssage = ($"Initialization failed . . .");
                return;
            }

            LogMesssage = $"Getting Chia Status . . .";
            CommonConstants.SaveDebugLog($"Getting Chia Status . . .", false, true);
            string statusString = string.Empty;
            DateTime LastBlockDt = CommonConstants.LastLogTime_BlockFound;
            DateTime LastPoolErrDt = CommonConstants.LastLogTime_PoolStopped;

                bool IsConnected = !(string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(clientId));
                statusString = await _chiaClient.GetChiaStatus(userId, clientId, IsConnected);
          
            LogMesssage = $"Sending Chia Status to Server . . .";
            _wrapper.SendData(statusString);
            try
            {
                string filename = postGlobalData ? "last_pool_datag.json" : "last_pool_data.json";
                string path = Path.Combine(CommonConstants.DataFolder, filename);
                FileHandler.SaveFile(path, statusString);
            }
            catch (Exception ex)
            {
                LogMesssage = $"Error While Logging data:{ex.Message}";
                CommonConstants.SaveDebugLog($"Exception in ProcessDataRequest: {ex.Message}", false, true);
            }
            try
            {
                if (LastBlockDt < CommonConstants.LastLogTime_BlockFound)
                {
                    LastBlockDt = CommonConstants.LastLogTime_BlockFound;
                    _settings.SetValue(SettingKeys.LastLogTimeBlockFound, CommonConstants.LastLogTime_BlockFound.ToString());
                }
                if (LastPoolErrDt < CommonConstants.LastLogTime_PoolStopped)
                {
                    LastPoolErrDt = CommonConstants.LastLogTime_PoolStopped;
                    _settings.SetValue(SettingKeys.LastLogTimePoolError, CommonConstants.LastLogTime_PoolStopped.ToString());
                }
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Failed To Update Last Log Accessed Time for BlockFound / Pooling Error: {ex.Message}", false, true);
            }
            CommonConstants.SaveDebugLog($"Transaction Completed * * * * * * * * * * * * * * * * * * * * * * * * * * * * ", false, true);
            LogMesssage = $"Transaction Completed . . .";
        }

        private async void ProcessEventRequest(string data)
        {
            try
            {
                bool initialized = await Initialize();

                if (!initialized)
                {
                    LogMesssage = $"Initialization failed . . .";
                    return;
                }
                LogMesssage = "Recieved server event request";
                ChiaTaskModel receivedData = JsonConvert.DeserializeObject<ChiaTaskModel>(data);
                JObject requestParameters = receivedData.data;
                string taskType = receivedData.type;

                switch (taskType)
                {
                    case "generate-address":
                        LogMesssage = "Generating new wallet address.";
                        receivedData.status = "inProgress";
                        _wrapper.SendData(receivedData.ToString());
                        int wallet_id = requestParameters.Value<int>("wallet_id");
                        string address = await _chiaClient.GetNewWalletAddress(wallet_id == 0 ? 1 : wallet_id);
                        LogMesssage = $"New wallet address:{address}";
                        receivedData.data = new JObject();
                        receivedData.data["new_Address"] = address;
                        receivedData.status = "completed";
                        break;
                }
                _wrapper.SendData(receivedData.ToString());
                await ProcessDataRequest();
            }
            catch (Exception ex)
            {
                LogMesssage = $"Error while processing server event requrest:{ex.Message}{Environment.NewLine}Trace => {ex.StackTrace}";
            }
        }
        #endregion

        public void Dispose()
        {
            RunThread = false;

        }
    }
}
