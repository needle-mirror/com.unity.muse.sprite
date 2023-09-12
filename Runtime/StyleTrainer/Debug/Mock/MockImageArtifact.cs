using System;
using Unity.Muse.StyleTrainer;
using UnityEngine;

namespace StyleTrainer.Runtime.Debug
{
    [Serializable]
    class MockImageArtifact : ImageArtifact
    {
        Texture2D m_Texture;
        [HideInInspector]
        public byte[] rawData;

        public Texture2D GetTexture2D()
        {
            if (m_Texture == null)
            {
                m_Texture = new Texture2D(1, 1);
                m_Texture.hideFlags = HideFlags.HideAndDontSave;
                m_Texture.name = $"MockImageArtifact-{guid}";
                m_Texture.LoadImage(rawData);
            }

            return m_Texture;
        }

        public override void OnDispose()
        {
            if (m_Texture != null)
                UnityEngine.Object.DestroyImmediate(m_Texture);
            m_Texture = null;
        }

        public MockImageArtifact(EState state)
            : base(state) { }
    }
}