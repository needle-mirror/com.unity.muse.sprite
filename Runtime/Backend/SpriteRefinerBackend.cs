using System;
using Unity.Muse.Sprite.Common.Backend;
using UnityEngine;

namespace Unity.Muse.Sprite.Backend
{
    internal class SpriteRefineRestCall : SpriteGeneratorRestCall<SpriteRefinerRequest, GenerateResponse, SpriteRefineRestCall>
    {
        public SpriteRefineRestCall(ServerConfig asset, SpriteRefinerRequest request, string generatorProfile)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.asset_id = generatorProfile;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/refine";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;

        protected override string RequestLog()
        {
            var logRequest = request;
            logRequest.base64Image = $"Image data removed for logging size:{request.base64Image?.Length}";
            logRequest.mask64Image = $"Image data removed for logging size:{request.mask64Image?.Length}";
            return $"Request:{MakeEndPoint(this)} Payload:{JsonUtility.ToJson(logRequest)}";
        }
    }

    internal class GetSpriteRefinerJobListRestCall : SpriteGeneratorRestCall<ServerRequest<EmptyPayload>, JobListResponse, GetSpriteRefinerJobListRestCall>
    {
        string m_GeneratorProfile;

        public GetSpriteRefinerJobListRestCall(ServerConfig asset, ServerRequest<EmptyPayload> request, string generatorProfile)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            request.guid = generatorProfile;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/refine/jobs";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;
    }

    [Serializable]
    internal struct SpriteRefinerRequestSettings
    {
        public int width;
        public int height;
        public int seed;
        public bool seamless;
        public string negative_prompt;
        public float strength;
    }

    [Serializable]
    internal struct SpriteRefinerRequest
    {
        public string access_token;
        public string asset_id;
        public string prompt;
        public string base64Image;
        public string mask64Image;
        public int image_count;
        public float maskStrength;
        public bool simulate;
        public SpriteRefinerRequestSettings settings;
        public int scribble;
        public int removeBackground;
        public string inputGuid;
        public string mask0Guid;
        public string checkpoint_id;
    }

}