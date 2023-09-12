using Unity.Muse.Sprite.Analytics;
using UnityEditor;

namespace Unity.Muse.Sprite.Editor.Analytics
{
    static class SpriteAnalyticsManager
    {
        const int maxEventsPerHour = 500;
        static string vendorKey = "unity.muse";

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorAnalytics.RegisterEventWithLimit(SaveSpriteData.eventName, maxEventsPerHour, 6, vendorKey);
            EditorAnalytics.RegisterEventWithLimit(GenerateAnalyticsData.eventName, maxEventsPerHour, 6, vendorKey);
        }
    }
}
