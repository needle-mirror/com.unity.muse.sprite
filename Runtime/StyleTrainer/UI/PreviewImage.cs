using System;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class PreviewImage : LoadableImage
    {
        ImageArtifact m_ImageArtifact;

        public PreviewImage()
            : base(false) { }

        public void SetArtifact(ImageArtifact artifact)
        {
            m_ImageArtifact = artifact;
            OnLoading();
            artifact.GetArtifact(OnDoneCallback, true);
            if (!Utilities.ValidStringGUID(m_ImageArtifact.guid))
                m_ImageArtifact.OnDataChanged += OnImageArtifactDataChanged;
        }

        void OnImageArtifactDataChanged(ImageArtifact obj)
        {
            if (Utilities.ValidStringGUID(m_ImageArtifact.guid))
            {
                OnLoading();
                m_ImageArtifact.GetArtifact(OnDoneCallback, true);
            }
        }

        void OnDoneCallback(Texture2D obj)
        {
            OnLoaded(obj);
        }

        public new class UxmlFactory : UxmlFactory<PreviewImage, UxmlTraits> { }
    }
}
