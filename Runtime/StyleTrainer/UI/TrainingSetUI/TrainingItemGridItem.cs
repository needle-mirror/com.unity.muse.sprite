using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class TrainingItemGridItem : ExVisualElement
    {
        ActionButton m_DeleteButton;
        int m_ItemIndex;
        public Action<int> OnDeleteClicked;
        PreviewImage m_PreviewImage;

        internal static TrainingItemGridItem CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/TrainingItemGridItem");
            var ve = (TrainingItemGridItem)visualTree.CloneTree().Q("TrainingItemGridItem");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/TrainingItemGridItem"));
            ve.BindElements();
            return ve;
        }

        void BindElements()
        {
            m_DeleteButton = this.Q<ActionButton>("DeleteButton");
            m_DeleteButton.clicked += OnDeleteButtonClicked;
            m_PreviewImage = this.Q<PreviewImage>("PreviewImage");
            m_PreviewImage.image = Utilities.placeHolderTexture;
#if UNITY_WEBGL && !UNITY_EDITOR
            m_DeleteButton.AddToClassList("delete-button-webgl");
#endif
        }

        public int itemIndex
        {
            set => m_ItemIndex = value;
        }

        void OnDeleteButtonClicked()
        {
            OnDeleteClicked?.Invoke(m_ItemIndex);
        }

        public void SetPreviewImage(ImageArtifact ai)
        {
            if (ai is not null) m_PreviewImage.SetArtifact(ai);
        }

        public new class UxmlFactory : UxmlFactory<TrainingItemGridItem, UxmlTraits> { }

        public void CanModify(bool canModify)
        {
            m_DeleteButton.SetEnabled(canModify);
        }
    }
}