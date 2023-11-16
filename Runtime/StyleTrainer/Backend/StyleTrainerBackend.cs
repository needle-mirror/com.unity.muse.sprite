using System;
using System.Text;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;

namespace StyleTrainer.Backend
{
    abstract class StyleTrainerRestCall<T1, T2, T3> : QuarkRestCall<T1, T2, T3>
        where T1 : BaseRequest
        where T3 : QuarkRestCall
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

        protected override void OnError()
        {
            if (responseCode == 401)
            {
#if UNITY_EDITOR
                // when access token failed, we try to refresh it, then signal the original error so the caller will send the request again.
                StyleTrainerDebug.LogError($"Are you logged in? Attempt to refresh token.\nerror:{errorMessage}\nIsLoggedIn:{UnityConnectUtils.GetIsLoggedIn()}");
                UnityEditor.CloudProjectSettings.RefreshAccessToken(refreshed =>
                {
                    if (!refreshed)
                    {
                        var task = UnityConnectUtils.GetUserAccessTokenAsync();
                        StyleTrainerDebug.LogWarning($"Refreshing token...");
                        if (!task.IsCompleted)
                        {
                            task.Start();
                            task.Wait();
                        }

                        SignInController.ForceSignInTokenRefresh(false);
                    }
                    StyleTrainerDebug.LogWarning($"Token refreshed. If error continues, try signing out and back in again.");
                    base.OnError();
                });

                StyleTrainerDebug.LogError($"Attempt to refresh token. If error continues, try signing out and back in again.");
                var task = UnityConnectUtils.GetUserAccessTokenAsync();
                if (!task.IsCompleted)
                {
                    task.Start();
                    task.Wait();
                }
                base.OnError();
#endif
            }
            else
            {
                base.OnError();
            }
        }
    }

    class CreateStyleRestCall : StyleTrainerRestCall<CreateStyleRequest, CreateStyleResponse, CreateStyleRestCall>
    {
        public CreateStyleRestCall(ServerConfig asset, CreateStyleRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/create",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.asset_id}/style/create",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.POST,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/getlist",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/list",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/setstate",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/{request.style_guid}/state",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.PUT,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/getinfo",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/{request.style_guid}/info",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/trainingset",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.asset_id}/style/{request.guid}/trainingset",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.POST,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/trainingsetinfo",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/trainingset/{request.training_set_guid}",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetTrainingSetRestCallMock(request));
        }
    }

    class CreateCheckPointV2RestCall : StyleTrainerRestCall<CreateCheckPointV2Request, CreateCheckPointV2Response, CreateCheckPointV2RestCall>
    {
        public CreateCheckPointV2RestCall(ServerConfig asset, CreateCheckPointV2Request request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/v2/style/checkpoint",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.asset_id}/style/{request.guid}/checkpoint",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.POST,
        };

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.CreateCheckPointV2RestCallMock(request));
        }
    }

    class GetCheckPointRestCall : StyleTrainerRestCall<GetCheckPointRequest, GetCheckPointResponse, GetCheckPointRestCall>
    {
        public GetCheckPointRestCall(ServerConfig asset, GetCheckPointRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/checkpointinfo",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/checkpoint/{request.checkpoint_guid}/info",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/style/setcheckpoint",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/{request.style_guid}/checkpoint/{request.checkpoint_guid}/set",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.PUT,
        };

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
            this.request = new GetImageRequest();
            this.request.access_token = String.Empty;
            this.request.guid = request.guid;

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
        protected override string[] endPoints
        {
            get
            {
                return new [] { "" };
            }
        }

        protected override byte[] ParseResponse(IWebRequest response)
        {
            return response.responseByte;
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.GET,
        };

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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[]
                {
                    $"/api/v1/sprite/download_url",
                    $"/api/v2/assets/images/sprites/organizations/{request.organization_id}/assets/{request.guid}",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetImageURL(request));
        }
    }

    // TODO: Deprecated call in v2
    class GetImageRestCall : StyleTrainerRestCall<GetImageRequest, byte[], GetImageRestCall>
    {
        public GetImageRestCall(ServerConfig asset, GetImageRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                return new[] {$"/api/v1/sprite/download_image" };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST,
        };

        protected override byte[] ParseResponse(IWebRequest response)
        {
            return response.responseByte;
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
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                var guids = String.Empty;
                if (request.guids.Length > 0)
                {
                    var sb = new StringBuilder("?", 42*8);
                    for (var i = 0; i < request.guids.Length; ++i)
                    {
                        sb.Append($"guids={request.guids[i]}");
                        if (i < request.guids.Length - 1)
                            sb.Append("&");
                    }
                    guids = sb.ToString();
                }
                return new[]
                {
                    $"/api/v1/sprite/style/checkpointstatus",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/projects/{request.guid}/style/checkpointstatus{guids}"
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

        protected override void OnMockResponse()
        {
            OnSuccess(mockData.GetCheckPointStatus(request));
        }
    }

    class GetDefaultStyleProjectRestCall : StyleTrainerRestCall<GetDefaultStyleProjectRequest, GetDefaultStyleProjectResponse, GetDefaultStyleProjectRestCall>
    {
        public GetDefaultStyleProjectRestCall(ServerConfig asset, GetDefaultStyleProjectRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.organization_id = asset.organizationId;
            this.request = request;
        }

        protected override string[] endPoints
        {
            get
            {
                if (ServerConfig.serverConfig.apiVersion == -1)
                    return new[] { $"/api/v2/images/sprites/organizations/{request.organization_id}/default_project" };

                return new[]
                {
                    $"/api/v1/sprite/default_project",
                    $"/api/v2/images/sprites/organizations/{request.organization_id}/default_project",
                };
            }
        }

        protected override IQuarkEndpoint.EMethod[] methods => new [] {
            IQuarkEndpoint.EMethod.POST, IQuarkEndpoint.EMethod.GET,
        };

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
    record GetDefaultStyleProjectRequest : BaseRequest
    {
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
    record GetCheckPointStatusRequest : BaseRequest
    {
        public string guid; // asset guid
        public string[] guids; // checkpoint guids
    }

    [Serializable]
    record GetImageRequest : BaseRequest
    {
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
    record SetStyleStateRequest : BaseRequest
    {
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
    record SetCheckPointFavouriteRequest : BaseRequest
    {
        public string guid;
        public string style_guid;
        public string checkpoint_guid;
    }

    [Serializable]
    record GetCheckPointResponse
    {
        public record Status
        {
            public const string failed = "failed";
            public const string done = "done";
            public const string working = "working";
        }
        public bool success;
        public string asset_id;
        public string styleID;
        public string trainingsetID;
        public string checkpointID;
        public string name;
        public string description;
        public string[] validation_image_prompts;
        public string[] validation_image_guids;
        public int train_steps;
        public string status;
        public string error;
    }

    [Serializable]
    record GetCheckPointRequest : BaseRequest
    {
        public string guid;
        public string checkpoint_guid;
    }

    [Serializable]
    record CreateCheckPointV2Response
    {
        public string[] guids;
        public bool success;
        public string error;
    }

    [Serializable]
    record CreateCheckPointV2Request : BaseRequest
    {
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
    record GetTrainingSetRequest : BaseRequest
    {
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
    record GetStyleRequest : BaseRequest
    {
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
    record CreateTrainingSetRequest : BaseRequest
    {
        public string asset_id;
        public string guid;
        public string[] images;
    }

    [Serializable]
    record GetStylesRequest : BaseRequest
    {
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
    record CreateStyleRequest : BaseRequest
    {
        public string asset_id;
        public string name;
        public string desc;
        public string[] prompts;
        public string parent_id;
    }
}