using System;
using Unity.Muse.Common.Analytics;

namespace Unity.Muse.Sprite.Analytics
{
    [Serializable]
    class GenerateAnalyticsData : IAnalyticsData
    {
        public const string eventName = "muse_spriteTool_generate";
        public string EventName => eventName;
        public int Version => 1;

        public string prompt;
        public string prompt_negative;
        public bool inpainting_used;
        public int images_generated_nr;
        public bool reference_image_used;
        public bool is_doodle;
    }
}
