
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Moralis.WebGL.Platform.Abstractions;
using Moralis.WebGL.Platform.Services.Models;
using Moralis.WebGL.Platform.Utilities;
//using BCLWebClient = System.Net.Http.HttpClient;
using WebRequest = Moralis.WebGL.Platform.Services.Models.WebRequest;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Moralis.WebGL.Platform.Services
{
    /// <summary>
    /// A universal implementation of <see cref="IWebClient"/>.
    /// </summary>
    public class UniversalWebClient : IWebClient
    {
        static HashSet<string> ContentHeaders { get; } = new HashSet<string>
        {
            { "Allow" },
            { "Content-Disposition" },
            { "Content-Encoding" },
            { "Content-Language" },
            { "Content-Length" },
            { "Content-Location" },
            { "Content-MD5" },
            { "Content-Range" },
            { "Content-Type" },
            { "Expires" },
            { "Last-Modified" }
        };

        static List<string> allowedHeaders { get; } = new List<string>
        {
            "x-parse-application-id",
            "x-parse-installation-id",
            "content-type"
        };
        public UniversalWebClient() { } //: this(new BCLWebClient { }) { }

        //  public UniversalWebClient(BCLWebClient client) => Client = client;

        // BCLWebClient Client { get; set; }

        public async UniTask<Tuple<HttpStatusCode, string>> ExecuteAsync(WebRequest httpRequest) //, UploadHandler uploadProgress, DownloadHandler downloadProgress)
        {
            Tuple<HttpStatusCode, string> result = default;

            UnityWebRequest webRequest; // = new UnityWebRequest(httpRequest.Target, httpRequest.Method); //, downloadProgress, uploadProgress);
            
            switch (httpRequest.Method)
            {
                case "DELETE":
                    webRequest = UnityWebRequest.Delete(httpRequest.Target);
                    break;
                case "POST":
                    webRequest = CreatePostRequest(httpRequest);
                    break;
                case "PUT":
                    webRequest = CreatePutRequest(httpRequest);
                    break;
                default:
                    webRequest = UnityWebRequest.Get(httpRequest.Target);
                    break;
            }

            if (httpRequest.Headers != null)
            {
                foreach (KeyValuePair<string, string> header in httpRequest.Headers)
                {
                    //if (ContentHeaders.Contains(header.Key))
                    //{
                    //    webRequest.SetRequestHeader(header.Key, header.Value);
                    //}
                    //else
                    //{
                    if (!String.IsNullOrWhiteSpace(header.Value) && allowedHeaders.Contains(header.Key.ToLower()))
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                        Debug.Log($"Adding Header: {header.Key} value: {header.Value}");
                    }
                    //}
                }
            }

            //webRequest.SetRequestHeader("Cache-Control", "no-cache");
            //webRequest.SetRequestHeader("IfModifiedSince", DateTimeOffset.UtcNow.ToString());

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception exp)
            {
                Debug.LogError($"Error: {exp.Message}");
            }

            HttpStatusCode responseStatus = HttpStatusCode.BadRequest;
            string responseText = null;

            if (Enum.IsDefined(typeof(HttpStatusCode), (int)webRequest.responseCode))
            {
                responseStatus = (HttpStatusCode)Enum.ToObject(typeof(HttpStatusCode), webRequest.responseCode);
            }

            if (webRequest.isNetworkError)
            {
                Debug.Log("Error Getting Wallet Info: " + webRequest.error);
                responseText = webRequest.error;
            }
            else
            {
                responseText = webRequest.downloadHandler.text;
            }

            result = new Tuple<HttpStatusCode, string>(responseStatus, responseText);

            return result;
        }

        //private IEnumerator OnNetworkResponse(UnityWebRequest req)
        //{

        //    yield return req.SendWebRequest();

        //    if (UnityWebRequest.Result.ConnectionError.Equals(req.result) ||
        //        UnityWebRequest.Result.DataProcessingError.Equals(req.result) ||
        //        UnityWebRequest.Result.ProtocolError.Equals(req.result))
        //        Debug.Log("A web-request error has occured: " + req.GetResponseHeader(""));
        //    else
        //        Debug.Log("Success " + req.downloadHandler.text);

        //    byte[] results = req.downloadHandler.data;
        //    Debug.Log("Second Success");
        //    // Some code after success

        //}

        private UnityWebRequest CreatePostRequest(WebRequest httpRequest)
        {
            string requestData = null;
            var req = new UnityWebRequest(httpRequest.Target, "POST");
            Stream data = httpRequest.Data;

            if ((httpRequest.Data is null && httpRequest.Method.ToLower().Equals("post") ? new MemoryStream(new byte[0]) : httpRequest.Data) is Stream { } adjData)
            {
                data = adjData;
            }

            byte[] buffer = new byte[data.Length];
            data.Read(buffer, 0, buffer.Length);
            data.Position = 0;

            //requestData = Encoding.UTF8.GetString(buffer);
            req.uploadHandler = (UploadHandler)new UploadHandlerRaw(buffer);
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            return req; // UnityWebRequest.Post(httpRequest.Target, requestData);
        }

        private UnityWebRequest CreatePutRequest(WebRequest httpRequest)
        {
            string requestData = null;
            Stream data = httpRequest.Data;

            if ((httpRequest.Data is null && httpRequest.Method.ToLower().Equals("post") ? new MemoryStream(new byte[0]) : httpRequest.Data) is Stream { } adjData)
            {
                data = adjData;
            }

            byte[] buffer = new byte[data.Length];
            data.Read(buffer, 0, buffer.Length);
            data.Position = 0;

            requestData = Encoding.UTF8.GetString(buffer);

            return UnityWebRequest.Put(httpRequest.Target, requestData);
        }
    }
}
