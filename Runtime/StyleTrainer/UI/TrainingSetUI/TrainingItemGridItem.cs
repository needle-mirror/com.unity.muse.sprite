using System;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class TrainingItemGridItem : ExVisualElement
    {
        ActionButton m_DeleteButton;
        int m_ItemIndex;
        public Action<int> OnDeleteClicked;
        PreviewImage m_PreviewImage;

        internal static TrainingItemGridItem CreateFromUxml()
        {
            var visualTree = ResourceManager.Load<VisualTreeAsset>(PackageResources.trainingItemGridItemTemplate);
            var ve = (TrainingItemGridItem)visualTree.CloneTree().Q("TrainingItemGridItem");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.trainingItemGridItemStyleSheet));
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

#if ENABLE_UXML_TRAITS
        public new class UxmlFactory : UxmlFactory<TrainingItemGridItem, UxmlTraits> { }
#endif

        public void CanModify(bool canModify)
        {
            m_DeleteButton.SetEnabled(canModify);
        }
    }
}