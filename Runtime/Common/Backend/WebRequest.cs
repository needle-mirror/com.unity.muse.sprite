using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Muse.Sprite.Common.Backend
{
    static class WebRequestFactory
    {
        static Func<string, string, IWebRequest> s_Factory = DefaultFactory;

        static public IWebRequest CreateWebRequest(string url, string method)
        {
            return s_Factory(url, method);
        }

        public static void SetFactory(Func<string, string, IWebRequest> factory)
        {
            if(factory == null)
                s_Factory = DefaultFactory;
            else
                s_Factory = factory;
        }

        static IWebRequest DefaultFactory(string url, string method)
        {
            return new WebRequest(url, method);
        }
    }

    interface IWebRequest
    {
        void SetRequestHeader(string name, string value);
        void SetPayload(byte[] payload, string payloadType);
        void SendWebRequest(Action<IWebRequest> onComplete);
        void Dispose();

        UnityWebRequest.Result result { get; }
        long responseCode { get; }
        string error { get; }
        string errorMessage { get; }
        string responseText { get; }
        byte[] responseByte { get; }
        string info { get; }

        IWebRequest Recreate()
        {
            return null;
        }
    }

    class WebRequest : IWebRequest
    {
        UnityWebRequest m_WebRequest;
        AsyncOperation m_WebOperation;
        Action<IWebRequest> m_OnComplete;
        Dictionary<string, string> m_Header = new();

        public string info => m_WebRequest.url;

        public WebRequest(string url, string method)
        {
            m_WebRequest = new UnityWebRequest(url, method);
            m_WebRequest.downloadHandler = new DownloadHandlerBuffer();
        }

        public void SetRequestHeader(string name, string value)
        {
            m_Header.Add(name, value);
            m_WebRequest.SetRequestHeader(name, value);
        }

        public void SetPayload(byte[] payload, string payloadType)
        {
            m_WebRequest.uploadHandler = new UploadHandlerRaw(payload);
            m_WebRequest.uploadHandler.contentType = payloadType;
        }

        public void SendWebRequest(Action<IWebRequest> onComplete)
        {
            m_WebOperation = m_WebRequest.SendWebRequest();
            m_WebOperation.completed += x => onComplete?.Invoke(this);
        }

        public IWebRequest Recreate()
        {
            var webRequest = new WebRequest(m_WebRequest.url, m_WebRequest.method);
            foreach(var header in m_Header)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            if(m_WebRequest.uploadHandler != null)
                webRequest.SetPayload(m_WebRequest.uploadHandler.data, m_WebRequest.uploadHandler.contentType);
            return webRequest;
        }

        public void Dispose()
        {
            m_WebRequest?.uploadHandler?.Dispose();
            m_WebRequest?.downloadHandler?.Dispose();
            m_WebRequest?.Dispose();
        }

        public UnityWebRequest.Result result => m_WebRequest.result;
        public long responseCode => m_WebRequest.responseCode;
        public string error => m_WebRequest.error;
        public string errorMessage => m_WebRequest.downloadHandler?.text;
        public string responseText => m_WebRequest.downloadHandler?.text;
        public byte[] responseByte => m_WebRequest.downloadHandler?.data;
    }
}