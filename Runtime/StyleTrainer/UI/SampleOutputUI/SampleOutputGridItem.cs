using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputGridItem : ExVisualElement
    {
        PreviewImage m_PreviewImage;
        TextField m_Prompt;
        ActionButton m_DeleteButton;
        int m_ItemIndex;
        public Action<int> OnDeleteClicked;
        public Action<int, string> OnPromptChanged;

        internal static SampleOutputGridItem CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/SampleOutputGridItem");
            var ve = (SampleOutputGridItem)visualTree.CloneTree().Q("SampleOutputGridItem");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/SampleOutputGridItem"));
            ve.BindElements();
            return ve;
        }

        void BindElements()
        {
            m_Prompt = this.Q<TextField>("Prompt");

            //m_Prompt.SetEnabled(false);
            m_Prompt.RegisterValueChangedCallback(OnPromptValueChanged);
            m_PreviewImage = this.Q<PreviewImage>("PreviewImage");
            m_PreviewImage.image = Utilities.placeHolderTexture;
            m_DeleteButton = this.Q<ActionButton>("DeleteButton");

            // TODO unregister event
            m_DeleteButton.clicked += OnDeleteButtonClicked;
        }

        void OnPromptValueChanged(ChangeEvent<string> evt)
        {
            OnPromptChanged?.Invoke(m_ItemIndex, evt.newValue);
        }

        public int itemIndex
        {
            set => m_ItemIndex = value;
        }

        void OnDeleteButtonClicked()
        {
            OnDeleteClicked?.Invoke(m_ItemIndex);
        }

        public void SetArtifact(ImageArtifact artifact)
        {
            m_PreviewImage.SetArtifact(artifact);
        }

        public string prompt
        {
            set => m_Prompt.SetValueWithoutNotify(value);
        }

        public new class UxmlFactory : UxmlFactory<SampleOutputGridItem, UxmlTraits> { }
    }
}