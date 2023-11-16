using System;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class PreviewImage : LoadableImage
    {
        ImageArtifact m_ImageArtifact;
        static int s_ImageCount = 0;
        const int k_MaxRequest = 3;
        int m_Tries = 0;
        const int k_MaxTries = 5;
        readonly Vector2Int k_DelayLoadMS = new(20,200) ;
        public PreviewImage()
            : base(false) { }

        public void SetArtifact(ImageArtifact artifact)
        {
            m_ImageArtifact = artifact;
            OnImageArtifactDataChanged(artifact);
            if (!Utilities.ValidStringGUID(m_ImageArtifact.guid))
                m_ImageArtifact.OnGUIDChanged += OnImageArtifactDataChanged;
        }

        public void ShowLoading()
        {
            OnLoading();
        }

        public void ShowImage()
        {
            OnImageArtifactDataChanged(m_ImageArtifact);
        }

        void OnImageArtifactDataChanged(ImageArtifact obj)
        {
            OnLoading();
            var result = m_ImageArtifact.GetLoaded();
            if (result.cached)
            {
                OnDoneCallback(result.texture);
            }
            else
            {
                if(s_ImageCount < k_MaxRequest)
                {
                    ++s_ImageCount;
                    m_ImageArtifact.GetArtifact(OnDoneCallback, true);
                }
                else
                {
                    schedule.Execute(DelayLoad).StartingIn(UnityEngine.Random.Range(k_DelayLoadMS.x, k_DelayLoadMS.y));
                }
            }
        }

        void DelayLoad()
        {
            if (m_Tries > k_MaxTries && s_ImageCount >= k_MaxRequest)
            {
                s_ImageCount = 0;
            }

            if(s_ImageCount < k_MaxRequest || m_Tries > k_MaxTries)
            {
                ++s_ImageCount;
                m_ImageArtifact.GetArtifact(OnDoneCallback, true);
                m_Tries = 0;
            }
            else
            {
                ++m_Tries;
                schedule.Execute(DelayLoad).StartingIn(UnityEngine.Random.Range(k_DelayLoadMS.x, k_DelayLoadMS.y));
            }
        }

        void OnDoneCallback(Texture2D obj)
        {
            OnLoaded(obj);
        }

        public new class UxmlFactory : UxmlFactory<PreviewImage, UxmlTraits> { }
    }
}