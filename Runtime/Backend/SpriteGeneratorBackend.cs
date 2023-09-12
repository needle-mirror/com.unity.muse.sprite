using System;
using Unity.Muse.Sprite.Common.Backend;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Muse.Sprite.Backend
{
    internal abstract class SpriteGeneratorRestCall<T1, T2, T3> : QuarkRestCall<T1, T2, T3> where T3 : QuarkRestCall
    {
        ServerConfig m_ServerConfig;
        public SpriteGeneratorRestCall(ServerConfig serverConfig, T1 request)
        {
            m_ServerConfig = serverConfig;
            this.request = request;
            maxRetries = serverConfig.maxRetries;
            retryDelay = serverConfig.webRequestPollRate;
        }

        public override string server => m_ServerConfig.server;
    }
    internal class SpriteGenerateRestCall : SpriteGeneratorRestCall<GeneratorRequest, GenerateResponse, SpriteGenerateRestCall>
    {
        public static class Status
        {
            public const string failed = "failed";
            public const string completed = "done";
            public const string waiting = "waiting";
            public const string working = "working";
        }

        public SpriteGenerateRestCall(ServerConfig asset, GeneratorRequest request, string generatorProfile)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.asset_id = generatorProfile;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/generate";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override string RequestLog()
        {
            return $"Request:{MakeEndPoint(this)} Payload:{request.GetRequestLog()}";
        }
    }

    internal class SpriteVariantRestCall : SpriteGenerateRestCall
    {
        public SpriteVariantRestCall(ServerConfig asset, GeneratorRequest request, string generatorProfile)
            : base(asset, request, generatorProfile)
        {
        }

        public override string endPoint => $"/api/v1/sprite/variation";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override string RequestLog()
        {
            return $"Request:{MakeEndPoint(this)} Payload:{request.GetRequestLog()}";
        }
    }

    internal class SpriteScribbleRestCall : SpriteGenerateRestCall
    {
        public SpriteScribbleRestCall(ServerConfig asset, GeneratorRequest request, string generatorProfile)
            : base(asset, request, generatorProfile)
        {
        }

        public override string endPoint => $"/api/v1/sprite/scribble";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override string RequestLog()
        {
            return $"Request:{MakeEndPoint(this)} Payload:{request.GetRequestLog()}";
        }
    }

    internal class GetSpriteGeneratorJobListRestCall : SpriteGeneratorRestCall<ServerRequest<EmptyPayload>, JobListResponse, GetSpriteGeneratorJobListRestCall>
    {
        string m_GeneratorProfile;

        public GetSpriteGeneratorJobListRestCall(ServerConfig asset, ServerRequest<EmptyPayload> request, string generatorProfile)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.guid = generatorProfile;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/jobs";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;
    }

    internal class GetJobRestCall : SpriteGeneratorRestCall<ServerRequest<JobInfoRequest>, JobInfoResponse, GetJobRestCall>
    {
        public GetJobRestCall(ServerConfig asset, ServerRequest<JobInfoRequest> request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public string jobID => request.data.jobID;
        public override string endPoint => $"/api/v1/sprite/job";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override string ResponseLog()
        {
            var log = base.RequestLog();
            log += $"\n JobID:{jobID} {webRequest.downloadHandler.text}";
            return log;
        }
    }

    internal class GetArtifactUrlRestCall : SpriteGeneratorRestCall<ServerRequest<EmptyPayload>, GetArtifactUrlResponse, GetArtifactUrlRestCall>
    {
        public GetArtifactUrlRestCall(ServerConfig asset, string guid)
            : base(asset, new ServerRequest<EmptyPayload>())
        {
            var r = request;
            r.access_token = asset.accessToken;
            r.guid = guid;
            request = r;
        }

        public override string endPoint => $"/api/v1/sprite/download_url";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;
    }

    class GetArtifactRestCall : SpriteGeneratorRestCall<ServerRequest<EmptyPayload>, byte[], GetArtifactRestCall>
    {
        string m_ImageDownloadURL = string.Empty;
        GetArtifactUrlRestCall m_GetImageURL;

        public GetArtifactRestCall(ServerConfig serverConfig, string guid)
            : base(serverConfig, new ServerRequest<EmptyPayload>())
        {
            var r = request;
            r.access_token = serverConfig.accessToken;
            r.guid = guid;
            request = r;

            m_GetImageURL = new GetArtifactUrlRestCall(serverConfig, request.guid);
            DependOn(m_GetImageURL);
            m_GetImageURL.RegisterOnSuccess(OnGetImageURLSuccess);
            m_GetImageURL.RegisterOnFailure(OnGetImageURLFailed);
        }

        void OnGetImageURLFailed(GetArtifactUrlRestCall obj)
        {
            if (obj.retriesFailed)
            {
                maxRetries = 0;
                Debug.LogError($"Failed to get URL for image {request.guid}");
                SignalRequestCompleted(EState.Error);
                OnError();
            }
        }

        void OnGetImageURLSuccess(GetArtifactUrlRestCall arg1, GetArtifactUrlResponse response)
        {
            m_ImageDownloadURL = response.url;
        }

        public override string server => m_ImageDownloadURL;
        public override string endPoint => "";

        protected override byte[] ParseResponse(UnityWebRequest response)
        {
            return response.downloadHandler.data;
        }

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.GET;
    }

    // enum EArtifactStatus
    // {
    //     None,
    //     Queued,
    //     Processing,
    //     Completed,
    //     Failed
    // }

    // internal class GetArtifactStatusRestCall : SpriteGeneratorRestCall<EmptyPayload, EArtifactStatus, GetArtifactRestCall>
    // {
    //     string m_ArticaftUrl;
    //     string m_JobID;
    //     public string artifactUrl => m_ArticaftUrl;
    //
    //     public GetArtifactStatusRestCall(ServerConfig asset, string jobID, string artifact)
    //         : base(asset, new EmptyPayload())
    //     {
    //         m_ArticaftUrl = artifact;
    //         m_JobID = jobID;
    //     }
    //     public override string endPoint => $"/v1/artifacts/{m_JobID}/status/{m_ArticaftUrl}";
    //     public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.GET;
    //
    //     protected override EArtifactStatus ParseResponse(UnityWebRequest response)
    //     {
    //         var status = response.downloadHandler.text.TrimEnd().TrimStart();
    //         switch (status)
    //         {
    //             case "\"completed\"":
    //                 return EArtifactStatus.Completed;
    //             case "\"in queue\"":
    //                 return EArtifactStatus.Queued;
    //             case "\"in progress\"":
    //                 return EArtifactStatus.Processing;
    //         }
    //
    //         return EArtifactStatus.Failed;
    //     }
    // }
    //
    // internal class GetSourceImageRestCall : SpriteGeneratorRestCall<EmptyPayload, byte[], GetSourceImageRestCall>
    // {
    //     string m_JobID;
    //
    //     public GetSourceImageRestCall(ServerConfig asset, string jobID)
    //         : base(asset, new EmptyPayload())
    //     {
    //         m_JobID = jobID;
    //     }
    //
    //     public override string endPoint => $"/v1/artifacts/{m_JobID}/source/";
    //     public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.GET;
    //     protected override byte[] ParseResponse(UnityWebRequest response)
    //     {
    //         return response.downloadHandler.data;
    //     }
    // }

    [Serializable]
    struct Settings
    {
        public int width;
        public int height;
        public int seed;
        public int model;
        public bool seamless;
        public string negative_prompt;
        public float strength;
    }

    [Serializable]
    struct GeneratorRequest
    {
        public string access_token;
        public string asset_id;
        public string prompt;
        public string base64Image;
        public string mask64Image;
        public int image_count;
        public float styleStrength;
        public float maskStrength;
        public bool simulate;
        public Settings settings;
        public bool removeBackground;
        public bool scribble;
        public string inputGuid;
        public string mask0Guid;
        public string checkpoint_id;

        public string GetRequestLog()
        {
            var logRequest = this;
            logRequest.base64Image = $"Image data removed for logging size:{logRequest.base64Image?.Length}";
            logRequest.mask64Image = $"Image data removed for logging size:{logRequest.mask64Image?.Length}";
            return JsonUtility.ToJson(logRequest);
        }
    }

    [Serializable]
    struct GenerateResponse
    {
        [SerializeField]
        private string guid;

        public string jobID => guid;
    }

    [Serializable]
    struct JobListResponse
    {
        public string[] jobIDs;
    }

    [Serializable]
    struct JobInfoRequest
    {
        public string jobID;
        public string[] fields;
    }

    [Serializable]
    struct JobInfoResponse
    {
        public string guid;
        public string status;
        public GeneratorRequest request;
    }

    [Serializable]
    struct EmptyPayload { }

    [Serializable]
    class ServerRequest<T>
    {
        public string access_token;
        public string guid;
        public T data;
    }

    [Serializable]
    struct GetArtifactUrlResponse
    {
        public string url;
        public bool success;
    }

    [Serializable]
    struct DatasetModel
    {
        public int id;
        public string name;
    }

    [Serializable]
    struct DatasetModels
    {
        public DatasetModel[] models;
    }
}