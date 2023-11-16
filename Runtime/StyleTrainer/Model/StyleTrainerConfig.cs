using System;
using System.IO;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.DebugConfig;
using UnityEngine;
using MuseArtifact = Unity.Muse.Common.Artifact;

namespace Unity.Muse.StyleTrainer
{
    //[CreateAssetMenu(fileName = "StyleTrainerConfig", menuName = "Muse/StyleTrainerConfig")]
    class StyleTrainerConfig : ScriptableObject
    {
        public int trainingSteps = 2000;
        public int minTrainingSetSize = 3;
        public int maxTrainingSetSize = 10;
        public int minSampleSetSize = 1;
        public int maxSampleSetSize = 5;
        public Vector2Int minTrainingImageSize = new(128, 128);
        public Vector2Int maxTrainingImageSize = new(512, 512);
        public bool debugLog;
        public bool logToFile;
        public ulong defaultStyleVersion = 1;
        public StyleData[] defaultStyles;
#if UNITY_EDITOR
        [SerializeField]
        bool m_UseMockData = false;
#endif

        public bool useMockData =>
#if UNITY_EDITOR
            DebugConfig.developerMode && m_UseMockData;
#else
            false;
#endif
        public static StyleTrainerConfig config => ResourceManager.Load<StyleTrainerConfig>(PackageResources.styleTrainerConfig);

        StyleTrainerArtifactCache m_ArtifactCache;
        public string artifactCachePath =>
#if UNITY_EDITOR
            "Library/Muse/StyleTrainer/StyleTrainerCache.db";
#else
            $"{Application.persistentDataPath}/StyleTrainerCache.db";
#endif
        public StyleTrainerArtifactCache artifactCache
        {
            get
            {
                if (m_ArtifactCache == null)
                {
                    m_ArtifactCache = new StyleTrainerArtifactCache(artifactCachePath);
                }
                return m_ArtifactCache;
            }
        }
    }
}