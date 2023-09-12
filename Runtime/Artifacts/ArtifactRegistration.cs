using Unity.Muse.Common;
using UnityEngine;

namespace Unity.Muse.Sprite.Artifacts
{
    internal static class ArtifactRegistration
    {
        #if !UNITY_EDITOR
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        #endif
        public static void RegisterArtifact()
        {
            ArtifactFactory.SetArtifactTypeForMode<SpriteMuseArtifact>(UIMode.UIMode.modeKey);
        }
    }
}
