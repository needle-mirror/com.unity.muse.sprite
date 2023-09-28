using System;
using Unity.Muse.Sprite.Common.Backend;

namespace Unity.Muse.Sprite.Backend
{
    [Serializable]
    struct FeedbackRequest
    {
        public string access_token;
        public string guid;
        public int feedback_flags;
        public string feedback_comment;
    }

    [Serializable]
    struct FeedbackResponse
    {
        public bool success;
        public string guid;
    }

    internal class SubmitFeedbackRestCall : SpriteGeneratorRestCall<FeedbackRequest, FeedbackResponse, SubmitFeedbackRestCall>
    {
        public SubmitFeedbackRestCall(ServerConfig asset, FeedbackRequest request)
            : base(asset, request)
        {
            request.access_token = asset.accessToken;
            this.request = request;
        }

        public override string endPoint => $"/api/v1/sprite/feedback";
        public override IQuarkEndpoint.EMethod method => IQuarkEndpoint.EMethod.POST;
    }
}
