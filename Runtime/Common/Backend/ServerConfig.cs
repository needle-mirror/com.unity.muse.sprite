using System;
using Unity.Muse.Common;
using Unity.Muse.Common.Account;
using UnityEditor;
using UnityEngine;

namespace Unity.Muse.Sprite.Common.Backend
{
    //[CreateAssetMenu(fileName = "ServerConfig.asset", menuName = "Muse/Sprite/ServerConfig")]
    internal class ServerConfig : ScriptableObject
    {
        [Flags]
        public enum EDebugMode
        {
            ArtifactDebugInfo = 1,
            SessionDebug = 1 << 2,
            OperatorDebug = 1 << 3,
            ForceUseSecretKey = 1 << 4
        }

        public string[] serverList;
        public int serverIndex;
        [SerializeField]
        string secretToken;
        public float webRequestPollRate = 1.0f;
        public int maxRetries = 3;
        public string server => serverList[serverIndex];

        [HideInInspector]
        public int model;
        [HideInInspector]
        public bool simulate;

        [NonSerialized] public bool callApiVersion = false;
        public int apiVersion = -1;
        [NonSerialized] public string lastApiServer = null;

        [SerializeField]
        EDebugMode m_DebugMode;
        public EDebugMode debugMode =>
#if UNITY_EDITOR
            UnityEditor.Unsupported.IsDeveloperMode() ? m_DebugMode : 0;
#else
            0;
#endif


        public string accessToken =>
#if UNITY_EDITOR
            (debugMode & EDebugMode.ForceUseSecretKey) > 0 ? secretToken : CloudProjectSettings.accessToken;
#else
            secretToken;
#endif

        public string organizationId
        {
            get
            {
#if UNITY_EDITOR
                return AccountInfo.Instance.Organization?.Id;
#else
                return secretToken;
#endif
            }
        }
        public static ServerConfig serverConfig =>
#if UNITY_EDITOR
            GetServerConfigEditor();
#else
            ResourceManager.Load<ServerConfig>(PackageResources.spriteGeneratorServerConfig);
#endif

#if UNITY_EDITOR
        static ServerConfig GetServerConfigEditor()
        {
            var objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget("ProjectSettings/SpriteMuseServerConfig.asset");
            ServerConfig config = (objs.Length > 0 ? objs[0] : null) as ServerConfig;
            return config != null ? config : ResourceManager.Load<ServerConfig>(PackageResources.spriteGeneratorServerConfig);
        }
#endif
    }
}