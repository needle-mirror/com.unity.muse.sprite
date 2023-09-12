using Unity.Muse.Common;
using UnityEngine;

namespace Unity.Muse.Sprite.UIMode
{
    static class UIModeRegistration
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterUIMode()
        {
            UIModeFactory.RegisterUIMode<UIMode>(UIMode.modeKey);
        }
    }
}