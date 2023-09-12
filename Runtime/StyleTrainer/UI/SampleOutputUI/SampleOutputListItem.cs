using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputListItem : ExVisualElement
    {
        TextField m_Prompt;
        ActionButton m_DeleteButton;
        int m_ItemIndex;
        public Action<int> OnDeleteClicked;
        public Action<int, string> OnPromptChanged;

        internal static SampleOutputListItem CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/SampleOutputListItem");
            var ve = (SampleOutputListItem)visualTree.CloneTree().Q("SampleOutputListItem");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/SampleOutputListItem"));
            ve.BindElements();
            return ve;
        }

        void BindElements()
        {
            m_DeleteButton = this.Q<ActionButton>("DeleteButton");

            // TODO unregister event
            m_DeleteButton.clicked += OnDeleteButtonClicked;
            m_Prompt = this.Q<TextField>("SamplePrompt");
            m_Prompt.RegisterValueChangedCallback(OnPromptValueChanged);
        }

        public int itemIndex
        {
            set => m_ItemIndex = value;
        }

        void OnPromptValueChanged(ChangeEvent<string> evt)
        {
            OnPromptChanged?.Invoke(m_ItemIndex, evt.newValue);
        }

        void OnDeleteButtonClicked()
        {
            OnDeleteClicked?.Invoke(m_ItemIndex);
        }

        public string prompt
        {
            set => m_Prompt.SetValueWithoutNotify(value);
        }

        public new class UxmlFactory : UxmlFactory<SampleOutputListItem, UxmlTraits> { }
    }
}