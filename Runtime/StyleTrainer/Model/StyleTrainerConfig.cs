using System;
using Unity.Muse.Sprite.Common.DebugConfig;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    //[CreateAssetMenu(fileName = "StyleTrainerConfig", menuName = "Muse/StyleTrainerConfig")]
    class StyleTrainerConfig : ScriptableObject
    {
        public int minTrainingSetSize = 3;
        public int maxTrainingSetSize = 10;
        public int minSampleSetSize = 1;
        public int maxSampleSetSize = 5;
        public Vector2Int minTrainingImageSize = new(128, 128);
        public Vector2Int maxTrainingImageSize = new(512, 512);
        public bool debugLog;
        public bool logToFile;
        public Vector2Int trainingStepRange = new(100, 1000);
        public int trainingStepsIncrement = 50;
        public ulong defaultStyleVersion = 1;
        public StyleData[] defaultStyles;
        [SerializeField]
        bool m_UseMockData = false;

        public bool useMockData =>
#if UNITY_EDITOR
            DebugConfig.developerMode && m_UseMockData;
#else
            false;
#endif
        public static StyleTrainerConfig config => Resources.Load<StyleTrainerConfig>("Unity.Muse.StyleTrainer/StyleTrainerConfig");
    }
}