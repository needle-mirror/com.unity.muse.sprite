using System;
using Unity.Muse.Sprite.Common;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;
using UnityEngine.Networking;

namespace StyleTrainer.Backend
{
    abstract class StyleTrainerRestCall<T1, T2, T3> : QuarkRestCall<T1, T2, T3> where T3 : QuarkRestCall
    {
        readonly ServerConfig m_ServerConfig;
        readonly MockData m_MockData;

        protected StyleTrainerRestCall(ServerConfig serverConfig, T1 request)
        {
            m_ServerConfig = serverConfig;
            this.request = request;
            maxRetries = serverConfig.maxRetries;
            retryDelay = serverConfig.webRequestPollRate;

            var config = StyleTrainerConfig.config;
            if (config.useMockData)
            {
                m_MockData = MockData.instance;
                m_MockData.Init();
            }
            else
            {
                m_MockData = null;
            }
        }

        protected MockData mockData => m_MockData;

        public override string server => m_ServerConfig.server;
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override void MakeServerRequest()
        {
            if (m_MockData is not null)
                Scheduler.ScheduleCallback(3, CallMockResponse);
            else
                base.MakeServerRequest();
        }

        void CallMockResponse()
        {
            try
            {
                OnMockResponse();
            }
            finally
            {
                SignalRequestCompleted(restCallState);
            }
        }

        protected virtual void OnMockResponse() { }
    }

    class CreateStyleRestCall : StyleTrainerRestCall<CreateStyleRequest, CreateStyleResponse, CreateStyleRestCall>
    {
        public CreateStyleRestCall(ServerConfig asset, CreateStyleRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/create";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.CreateStyleRestCallMock(request));
        }
    }

    class GetStylesRestCall : StyleTrainerRestCall<GetStylesRequest, GetStylesResponse, GetStylesRestCall>
    {
        public GetStylesRestCall(ServerConfig asset, GetStylesRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/getlist";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetStylesRestCallMock(request));
        }
    }

    class SetStyleStateRestCall : StyleTrainerRestCall<SetStyleStateRequest, SetStyleStateResponse, SetStyleStateRestCall>
    {
        public const string activeState = "active";
        public const string inactiveState = "inactive";

        public SetStyleStateRestCall(ServerConfig asset, SetStyleStateRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/setstate";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.SetStyleStateRestCallMock(request));
        }
    }

    class GetStyleRestCall : StyleTrainerRestCall<GetStyleRequest, GetStyleResponse, GetStyleRestCall>
    {
        public GetStyleRestCall(ServerConfig asset, GetStyleRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/getinfo";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetStyleRestCallMock(request));
        }
    }

    class CreateTrainingSetRestCall : StyleTrainerRestCall<CreateTrainingSetRequest, CreateTrainingSetResponse, CreateTrainingSetRestCall>
    {
        public CreateTrainingSetRestCall(ServerConfig asset, CreateTrainingSetRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/trainingset";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.CreateTrainingSetRestCallMock(request));
        }

        protected override string RequestLog()
        {
            var logRequest = new CreateTrainingSetRequest
            {
                access_token = request.access_token,
                asset_id = request.asset_id,
                guid = request.guid,
                images = new string[request.images?.Length ?? 0]
            };
            for (int i = 0; i < request.images?.Length; ++i)
            {
                logRequest.images[i] = $"Image data removed for logging size:{request.images[i].Length}";
            }
            return $"Request:{MakeEndPoint(this)} Payload:{JsonUtility.ToJson(logRequest)}";
        }
    }

    class GetTrainingSetRestCall : StyleTrainerRestCall<GetTrainingSetRequest, GetTrainingSetResponse, GetTrainingSetRestCall>
    {
        public GetTrainingSetRestCall(ServerConfig asset, GetTrainingSetRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/trainingsetinfo";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetTrainingSetRestCallMock(request));
        }
    }

    class CreateCheckPointRestCall : StyleTrainerRestCall<CreateCheckPointRequest, CreateCheckPointResponse, CreateCheckPointRestCall>
    {
        public CreateCheckPointRestCall(ServerConfig asset, CreateCheckPointRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.training_steps = Math.Max(request.training_steps, StyleTrainerConfig.config.trainingStepRange.x);
            request.training_steps = Math.Min(request.training_steps, StyleTrainerConfig.config.trainingStepRange.y);
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/checkpoint";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.CreateCheckPointRestCallMock(request));
        }
    }

    class GetCheckPointRestCall : StyleTrainerRestCall<GetCheckPointRequest, GetCheckPointResponse, GetCheckPointRestCall>
    {
        public GetCheckPointRestCall(ServerConfig asset, GetCheckPointRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/checkpointinfo";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetCheckPointRestCallMock(request));
        }
    }

    class SetCheckPointFavouriteRestCall : StyleTrainerRestCall<SetCheckPointFavouriteRequest, SetCheckPointFavouriteResponse, SetCheckPointFavouriteRestCall>
    {
        public SetCheckPointFavouriteRestCall(ServerConfig asset, SetCheckPointFavouriteRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/style/setcheckpoint";

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.SetCheckPointFavouriteRestCallMock(request));
        }
    }

    class GetImageFromURLRestCall : StyleTrainerRestCall<GetImageRequest, byte[], GetImageFromURLRestCall>
    {
        string m_ImageDownloadURL = string.Empty;
        readonly  GetImageURLRestCall m_GetImageURL;

        public GetImageFromURLRestCall(ServerConfig serverConfig, GetImageRequest request)
            : base(serverConfig, request)
        {
            m_GetImageURL = new GetImageURLRestCall(serverConfig, request);
            DependOn(m_GetImageURL);
            m_GetImageURL.RegisterOnSuccess(OnGetImageURLSuccess);
            m_GetImageURL.RegisterOnFailure(OnGetImageURLFailed);
        }

        void OnGetImageURLFailed(GetImageURLRestCall obj)
        {
            // forbidden
            if (obj.responseCode == 403)
            {
                maxRetries = 0;
                StyleTrainerDebug.LogError($"Forbidden access URL for image {request.guid}");
                SignalRequestCompleted(EState.Forbidden);
                OnForbidden();
            }
            else if (obj.retriesFailed)
            {
                maxRetries = 0;
                StyleTrainerDebug.LogError($"Failed to get URL for image {request.guid}");
                SignalRequestCompleted(EState.Error);
                OnError();
            }
        }

        void OnGetImageURLSuccess(GetImageURLRestCall arg1, GetImageURLResponse arg2)
        {
            m_ImageDownloadURL = arg2.url;
        }

        public override string server => m_ImageDownloadURL;
        public override string endPoint => "";

        protected override byte[] ParseResponse(UnityWebRequest response)
        {
            return response.downloadHandler.data;
        }

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.GET;

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetImage(request));
        }
    }

    class GetImageURLRestCall : StyleTrainerRestCall<GetImageRequest, GetImageURLResponse, GetImageURLRestCall>
    {
        public GetImageURLRestCall(ServerConfig asset, GetImageRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/download_url";

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetImageURL(request));
        }
    }

    class GetImageRestCall : StyleTrainerRestCall<GetImageRequest, byte[], GetImageRestCall>
    {
        public GetImageRestCall(ServerConfig asset, GetImageRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/download_image";

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override byte[] ParseResponse(UnityWebRequest response)
        {
            return response.downloadHandler.data;
        }

        protected override string RequestLog()
        {
            var log = base.RequestLog();
            log += $"\n Artifact:{request.guid}";
            return log;
        }

        protected override string ResponseLog()
        {
            var log = base.RequestLog();
            log += $"\n Artifact:{request.guid} {requestResult} {requestError}";
            return log;
        }

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetImage(request));
        }
    }

    class GetCheckPointStatusRestCall : StyleTrainerRestCall<GetCheckPointStatusRequest, GetCheckPointStatusResponse, GetCheckPointStatusRestCall>
    {
        public GetCheckPointStatusRestCall(ServerConfig asset, GetCheckPointStatusRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/style/checkpointstatus";

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override void OnMockResponse()
        {
            throw new NotImplementedException();
        }
    }

    class GetDefaultStyleProjectRestCall : StyleTrainerRestCall<GetDefaultStyleProjectRequest, GetDefaultStyleProjectResponse, GetDefaultStyleProjectRestCall>
    {
        public GetDefaultStyleProjectRestCall(ServerConfig asset, GetDefaultStyleProjectRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => "/api/v1/sprite/default_project";

        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetDefaultStyleProject(request));
        }
    }

    [Serializable]
    record GetDefaultStyleProjectResponse
    {
        public bool success;
        public string guid;
    }

    [Serializable]
    record GetDefaultStyleProjectRequest
    {
        public string access_token;
    }


    [Serializable]
    record GetCheckPointStatusResponse
    {
        [Serializable]
        public record CheckPointStatus
        {
            public string guid;
            public string status;
        }

        public bool success;
        public string error;
        public CheckPointStatus[] results;
    }

    [Serializable]
    record GetCheckPointStatusRequest
    {
        public string access_token;
        public string guid; // asset guid
        public string[] guids; // checkpoint guids
    }

    [Serializable]
    record GetImageRequest
    {
        public string access_token;
        public string guid;
    }

    [Serializable]
    record GetImageURLResponse
    {
        public string url;
        public bool success;
    }

    [Serializable]
    record SetStyleStateResponse
    {
        public bool success;
    }

    [Serializable]
    record SetStyleStateRequest
    {
        public string access_token;
        public string guid;
        public string style_guid;
        public string state;
    }

    [Serializable]
    struct SetCheckPointFavouriteResponse
    {
        public bool success;
        public string guid;
    }

    [Serializable]
    record SetCheckPointFavouriteRequest
    {
        public string access_token;
        public string guid;
        public string style_guid;
        public string checkpoint_guid;
    }

    [Serializable]
    record GetCheckPointResponse
    {
        public bool success;
        public string asset_id;
        public string styleID;
        public string trainingsetID;
        public string checkpointID;
        public string name;
        public string description;
        public string[] validation_image_prompts;
        public string[] validation_image_guids;
        public string status;
        public string error;
    }

    [Serializable]
    record GetCheckPointRequest
    {
        public string access_token;
        public string guid;
        public string checkpoint_guid;
    }

    [Serializable]
    record CreateCheckPointResponse
    {
        public string guid;
        public bool success;
        public string error;
    }

    [Serializable]
    record CreateCheckPointRequest
    {
        public string access_token;
        public string asset_id;
        public string guid;
        public string training_guid;
        public string name;
        public string description;
        public string resume_guid;
        public int training_steps;
    }

    [Serializable]
    record GetTrainingSetResponse
    {
        public bool success;
        public string asset_id;
        public string styleID;
        public string trainingsetID;
        public string[] training_image_guids;
        public string error;
    }

    [Serializable]
    record GetTrainingSetRequest
    {
        public string access_token;
        public string guid;
        public string training_set_guid;
    }

    [Serializable]
    record GetStyleResponse
    {
        public bool success;
        public string name;
        public string desc;
        public string[] prompts;
        public string[] trainingsetIDs;
        public string[] checkpointIDs;
        public string error;
        public string checkpoint;
        public string state;
    }

    [Serializable]
    record GetStyleRequest
    {
        public string access_token;
        public string guid;
        public string style_guid;
    }

    [Serializable]
    record CreateTrainingSetResponse
    {
        public string guid;
        public bool success;
        public string error;
    }

    [Serializable]
    record CreateTrainingSetRequest
    {
        public string access_token;
        public string asset_id;
        public string guid;
        public string[] images;
    }

    [Serializable]
    record GetStylesRequest
    {
        public string access_token;
        public string guid;
    }

    [Serializable]
    record GetStylesResponse
    {
        public bool success;
        public string[] styleIDs;
        public string error;
    }

    [Serializable]
    record CreateStyleResponse
    {
        public string guid;
        public bool success;
        public string error;
    }

    [Serializable]
    record CreateStyleRequest
    {
        public string access_token;
        public string asset_id;
        public string name;
        public string desc;
        public string[] prompts;
        public string parent_id;
    }
}