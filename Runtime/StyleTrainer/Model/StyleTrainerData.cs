using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    [Serializable]
    class StyleTrainerData : Artifact<StyleTrainerData, StyleTrainerData>
    {
        // Unity version + XXXXX
        public const string k_Version = "202230001";

        [SerializeReference]
        List<StyleData> m_Styles = new();

        [SerializeField]
        string m_Version = k_Version;

        public string version => m_Version;

        public StyleTrainerData(EState state)
            : base(state)
        {
            if (!Utilities.ValidStringGUID(guid))
                guid = Guid.NewGuid().ToString();
        }

        public override void OnDispose()
        {
            for (var i = 0; i < m_Styles?.Count; ++i)
                m_Styles[i]?.OnDispose();
            base.OnDispose();
        }

        public override void GetArtifact(Action<StyleTrainerData> onDoneCallback, bool useCache)
        {
            onDoneCallback?.Invoke(this);
        }

        public IReadOnlyList<StyleData> styles => m_Styles;

        public void AddStyle(StyleData style)
        {
            m_Styles.Add(style);
            DataChanged(this);
        }

        public void RemoveStyle(StyleData style)
        {
            if (m_Styles.Remove(style))
            {
                style.Delete();
                style.OnDispose();
                DataChanged(this);
            }
        }

        public void ClearStyles()
        {
            m_Styles.Clear();
            DataChanged(this);
        }

        internal void Init()
        {
            if (!Utilities.ValidStringGUID(guid)) guid = Guid.NewGuid().ToString();
        }

        public void Delete()
        {
            foreach (var style in m_Styles) style?.Delete();

            m_Styles = null;
        }

        public void UpdateVersion()
        {
            m_Version = k_Version;
        }

        public bool HasTraining()
        {
            for (int i = 0; i < m_Styles?.Count; ++i)
            {
                if (m_Styles[i].HasTraining())
                    return true;
            }

            return false;
        }
    }
}