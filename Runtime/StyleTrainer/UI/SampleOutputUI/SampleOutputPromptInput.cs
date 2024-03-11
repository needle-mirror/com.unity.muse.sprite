using System;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    internal partial class SampleOutputPromptInput : ExVisualElement
    {
        TextArea m_Prompt;
        ActionButton m_DeleteButton;
        int m_ItemIndex;
        public Action<int> OnDeleteClicked;
        public Action<int, string> OnPromptChanged;

        void OnPromptValueChanged(ChangeEvent<string> evt)
        {
            OnPromptChanged?.Invoke(m_ItemIndex, evt.newValue);
            m_Prompt.tooltip = m_Prompt.value;
        }

        public int itemIndex
        {
            set => m_ItemIndex = value;
        }

        void OnDeleteButtonClicked()
        {
            OnDeleteClicked?.Invoke(m_ItemIndex);
        }

        void BindElements()
        {
            m_Prompt = this.Q<TextArea>("Prompt");
            m_Prompt.RegisterCallback((KeyDownEvent evt) =>
            {
                if ((evt.keyCode == KeyCode.Tab || (evt.keyCode == KeyCode.None && evt.character == '\t')) && !evt.shiftKey)
                {
                    evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                    evt.PreventDefault();
#endif

                    if (evt.character != '\t')
                        m_Prompt.focusController.FocusNextInDirectionEx(m_Prompt, VisualElementFocusChangeDirection.right);
                }
            }, TrickleDown.TrickleDown);
            m_Prompt.hierarchy.Remove(this.Q(TextArea.resizeHandleUssClassName));
            m_Prompt.RegisterValueChangedCallback(OnPromptValueChanged);
            //m_Prompt.RegisterCallback<FocusInEvent>(OnFocusIn);
            m_DeleteButton = this.Q<ActionButton>("DeleteButton");
            m_DeleteButton.clicked += OnDeleteButtonClicked;
            this.RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        void OnFocusIn(FocusInEvent evt)
        {
            ScrollToItem();
        }

        public string prompt
        {
            set
            {
                m_Prompt.SetValueWithoutNotify(value);
                m_Prompt.tooltip = m_Prompt.value;
            }
        }

        public void CanModify(bool canModify)
        {
            m_Prompt.SetEnabled(canModify);
            EnableInClassList("styletrainer-sampleoutputview-gridview-disable", !canModify);
        }

        ScrollView GetScrollView()
        {
            var parent = this.parent;
            while(parent != null)
            {
                if (parent is ScrollView scrollView)
                {
                    return scrollView;
                }
                parent = parent.parent;
            }

            return null;
        }

        public void FocusItem()
        {
            schedule.Execute(() =>
            {
                ScrollToItem();
                m_Prompt.contentContainer.Focus();
            });
        }


        void ScrollToItem()
        {
            var scrollView = GetScrollView();
            scrollView?.ScrollTo(this);
        }


        internal static SampleOutputPromptInput CreateFromUxml()
        {
            var visualTree = ResourceManager.Load<VisualTreeAsset>(PackageResources.sampleOutputPromptInputTemplate);
            var ve = (SampleOutputPromptInput)visualTree.CloneTree().Q("SampleOutputPromptInput");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.sampleOutputPromptInputStyleSheet));
            ve.BindElements();
            return ve;
        }

#if ENABLE_UXML_TRAITS
        public new class UxmlFactory : UxmlFactory<SampleOutputPromptInput, UxmlTraits> { }
#endif
    }
}