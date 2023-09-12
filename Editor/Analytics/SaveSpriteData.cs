using System;
using Unity.Muse.Common.Analytics;

namespace Unity.Muse.Sprite.Editor.Analytics
{
    [Serializable]
    class SaveSpriteData : IAnalyticsData
    {
        public const string eventName = "muse_spriteTool_save";
        public string EventName => eventName;
        public int Version => 1;

        public SpriteSaveDestination drag_destination;
        public bool is_drag;
        public string material_hash;
    }
}
