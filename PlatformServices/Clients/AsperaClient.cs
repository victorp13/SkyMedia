using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Newtonsoft.Json.Linq;

namespace AzureSkyMedia.PlatformServices
{
    public class AsperaClient
    {
        private string _serviceNode;
        private string _serviceStats;
        private WebClient _serviceClient;
        private string _accountId;
        private string _accountKey;
        private string _iterationToken;

        public AsperaClient(string authToken)
        {
            string attributeName = Constants.UserAttribute.AsperaServiceGateway;
            _serviceNode = AuthToken.GetClaimValue(authToken, attributeName);
            if (!_serviceNode.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                _serviceNode = string.Concat("https://", _serviceNode);
            }

            string[] serviceInfo = _serviceNode.Split('.');
            serviceInfo[0] = string.Concat(serviceInfo[0], Constants.Storage.Partner.AsperaWorker);
            _serviceStats = string.Join(".", serviceInfo);
            if (!_serviceStats.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                _serviceStats = string.Concat("https://", _serviceStats);
            }

            attributeName = Constants.UserAttribute.AsperaAccountId;
            _accountId = AuthToken.GetClaimValue(authToken, attributeName);

            attributeName = Constants.UserAttribute.AsperaAccountKey;
            _accountKey = AuthToken.GetClaimValue(authToken, attributeName);

            string apiVersion = string.Empty;
            _serviceClient = new WebClient(_accountId, _accountKey, apiVersion);

            string settingKey = Constants.AppSettingKey.AsperaTransferInfo;
            string transferInfo = AppSetting.GetValue(settingKey);
            string transferApi = string.Concat(_serviceStats, transferInfo);

            HttpWebRequest webRequest = _serviceClient.GetRequest(transferApi, "GET", null);
            JObject transfer = _serviceClient.GetResponse<JObject>(webRequest);
            _iterationToken = transfer["info_result"]["iteration_token"].ToString();
        }

        public JObject GetTransferSpecs(string storageRoot, string containerName, string[] filePaths, bool fileDownload)
        {
            string settingKey = Constants.AppSettingKey.AsperaUploadSetup;
            string uploadSetup = AppSetting.GetValue(settingKey);
            string transferApi = string.Concat(_serviceNode, uploadSetup);
            if (fileDownload)
            {
                settingKey = Constants.AppSettingKey.AsperaDownloadSetup;
                string downloadSetup = AppSetting.GetValue(settingKey);
                transferApi = string.Concat(_serviceNode, downloadSetup);
            }

            TransferRequest transferRequest = new TransferRequest();
            if (fileDownload)
            {
                transferRequest.SourceRoot = storageRoot;
            }
            else
            {
                transferRequest.DestinationRoot = storageRoot;
            }

            List<TransferPath> transferPaths = new List<TransferPath>(); 
            foreach (string filePath in filePaths)
            {
                TransferPath transferPath = new TransferPath();
                transferPath.Source = filePath;
                transferPath.Destination = string.Concat("/", containerName, "/", Path.GetFileName(filePath));
                transferPaths.Add(transferPath);
            }
            transferRequest.Paths = transferPaths.ToArray();

            TransferRequestItem requestItem = new TransferRequestItem();
            requestItem.TransferRequest = transferRequest;

            AsperaRequest asperaRequest = new AsperaRequest();
            asperaRequest.TransferRequests = new TransferRequestItem[] { requestItem };

            SetSecurityProtocol();
            HttpWebRequest webRequest = _serviceClient.GetRequest(transferApi, "POST", asperaRequest);
            JObject webResponse = _serviceClient.GetResponse<JObject>(webRequest);
            JToken transferSettings = webResponse["transfer_specs"][0];

            JToken transferSpec = transferSettings["transfer_spec"];
            transferSpec["remote_user"] = _accountId;
            transferSpec["remote_password"] = _accountKey;

            JObject connectSpec = new JObject();
            connectSpec.Add("allow_dialogs", true);
            connectSpec.Add("return_paths", true);
            connectSpec.Add("use_absolute_destination_path", true);

            transferSettings["aspera_connect_settings"] = connectSpec;
            return webResponse;
        }

        private static void SetSecurityProtocol()
        {
            ServicePointManager.ServerCertificateValidationCallback += delegate
                (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
            {
                return true;
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}
