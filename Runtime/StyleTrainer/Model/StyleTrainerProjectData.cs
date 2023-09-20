#if UNITY_EDITOR
using UnityEditor;
#else
using Unity.Muse.StyleTrainer.EditorMockClass;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace Unity.Muse.StyleTrainer
{
    [Serializable]
    [Preserve]
    [FilePath("ProjectSettings/StyleTrainerProjectData.asset", FilePathAttribute.Location.ProjectFolder)]
    class StyleTrainerProjectData : ScriptableSingleton<StyleTrainerProjectData>
    {
        [SerializeReference]
        StyleTrainerData m_StyleTrainerData;
        [FormerlySerializedAs("m_DefaultProjectData")]
        [SerializeReference]
        StyleTrainerData m_DefaultStyleData = new StyleTrainerData(EState.New);

        [SerializeField]
        ulong m_DefaultStyleVersion;
        [SerializeReference]
        List<StyleData> m_DefaultStyles;

        [SerializeField]
        [HideInInspector]
        string m_AssetPath;

        [SerializeField]
        List<string> m_PreviousProjectIDs = new();
        public string guid => m_StyleTrainerData?.guid;
        public event Action<StyleTrainerProjectData> onDataChanged = _ => { };

        public string assetPath
        {
            set => m_AssetPath = value;
        }

        public void Save()
        {
#if UNITY_EDITOR

            StyleTrainerDebug.Log("Saving Asset");
            Save(true);
#else
            StyleTrainerDebug.LogWarning("No save implemetation for non-editor builds");
#endif
        }

        public StyleTrainerData data => m_StyleTrainerData;


        public IReadOnlyList<StyleData> GetDefaultStyles(Action<IReadOnlyList<StyleData>> onDone, bool cache)
        {
            if (m_DefaultStyleData.state == EState.Loaded && cache)
            {
                var buildStyleList = GetDefaultStyles();
                onDone(buildStyleList);
            }
            else if(m_DefaultStyleData.state != EState.Loading)
            {
                var getDefaultStyle = new RetrieveDefaultStyleTask();
                getDefaultStyle.Execute(m_DefaultStyleData, () =>
                {
                    var buildStyleList = GetDefaultStyles();
                    onDone(buildStyleList);
                });
            }

            return builtInStyles;
        }

        IReadOnlyList<StyleData> GetDefaultStyles()
        {
            IReadOnlyList<StyleData> serverDefaultStyles = m_DefaultStyleData.styles.Where(s => s.state == EState.Loaded && s.visible && s.checkPoints != null && s.checkPoints.Any(c => c.state == EState.Loaded)).ToList();
            if (serverDefaultStyles.Count == 0)
                return builtInStyles;

            return serverDefaultStyles;
        }

        IReadOnlyList<StyleData> builtInStyles
        {
            get
            {
                if (m_DefaultStyleVersion != StyleTrainerConfig.config.defaultStyleVersion)
                {
                    m_DefaultStyleVersion = StyleTrainerConfig.config.defaultStyleVersion;
                    m_DefaultStyles = StyleTrainerConfig.config.defaultStyles.ToList();
                }
                return m_DefaultStyles;
            }
        }

        internal void Init()
        {
            m_StyleTrainerData.Init();
            m_DefaultStyles = StyleTrainerConfig.config.defaultStyles.ToList();
        }

        void OnEnable()
        {
            StyleTrainerDebug.Log("Asset enabled");
        }

        void OnDisable()
        {
            Save();
            StyleTrainerDebug.Log("Asset disabled");
            m_StyleTrainerData?.OnDispose();
        }

        void OnDestroy()
        {
            Save();
            StyleTrainerDebug.Log("Asset destroy");
        }

        public void Reset()
        {
            if (Utilities.ValidStringGUID(guid))
                m_PreviousProjectIDs.Add(guid);
            m_StyleTrainerData?.OnDispose();
            m_StyleTrainerData?.Delete();
            m_StyleTrainerData = new StyleTrainerData(EState.New);
            Save();
            onDataChanged.Invoke(this);
        }

        public void ClearProjectData()
        {
            var oldGuid = m_StyleTrainerData?.guid;
            m_StyleTrainerData?.OnDispose();
            m_StyleTrainerData?.Delete();
            m_StyleTrainerData = new StyleTrainerData(EState.New);
            m_StyleTrainerData.guid = oldGuid;
            m_StyleTrainerData.state = EState.Initial;
            onDataChanged.Invoke(this);
        }
    }
}