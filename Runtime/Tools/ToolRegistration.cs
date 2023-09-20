using Unity.Muse.Common;
using UnityEngine;

namespace Unity.Muse.Sprite.Tools
{
    static class ToolRegistration
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterTools()
        {
            AvailableToolsFactory.RegisterTool<SpriteRefinerBrushTool>(UIMode.UIMode.modeKey);
        }
    }
}