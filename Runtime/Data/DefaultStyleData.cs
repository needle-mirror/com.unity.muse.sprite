using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.Common;
using Unity.Muse.StyleTrainer;

namespace Unity.Muse.Sprite.Data
{
    class DefaultStyleData : IModelData
    {
        public event Action OnModified;
        IReadOnlyList<StyleData> m_DefaultStyles;
        bool m_Loading = false;

        public IReadOnlyList<StyleData> GetBuiltInStyle()
        {
            if (!m_Loading && m_DefaultStyles == null)
            {
                m_DefaultStyles = StyleTrainerProjectData.instance.GetDefaultStyles(OnGetDefaultStyleDone, false);
                m_Loading = true;
            }

            return m_DefaultStyles;
        }

        public bool loading => m_Loading;

        void OnGetDefaultStyleDone(IReadOnlyList<StyleData> obj)
        {
            var newDefaultStyles = new List<StyleData>(obj.Where(s => s.state == EState.Loaded && s.visible && s.checkPoints != null && s.checkPoints.Any(c => c.state == EState.Loaded)));
            m_DefaultStyles = newDefaultStyles.Count == 0 ? m_DefaultStyles : newDefaultStyles;
            m_Loading = false;
            OnModified?.Invoke();
        }

        public void Reset()
        {
            m_DefaultStyles = null;
            m_Loading = false;
        }
    }
}