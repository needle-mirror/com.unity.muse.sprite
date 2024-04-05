using System;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using UnityEngine;
using UnityEngine.UIElements;
using TextField = Unity.Muse.AppUI.UI.TextField;

namespace Unity.Muse.StyleTrainer
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class StyleModelInfo : ExVisualElement
    {
        Text m_StatusLabel;
        Text m_DescriptionTextCount;
        TextField m_Name;
        TextArea m_Description;
        AppUI.UI.Button m_DuplicateButton;
        AppUI.UI.Button m_GenerateButton;
        EventBus m_EventBus;
        StyleData m_StyleData;
        CircularProgress m_TrainingIcon;
        Icon m_ErrorIcon;
        const float k_OriginalDescriptionTextAreaHeight = 75f;

        public StyleModelInfo()
        {
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            UnregisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);

            m_Name = this.Q<TextField>("StyleModelInfoDetailsName");
            m_Name.RegisterValueChangedCallback(OnNameChanged);
            m_Name.RegisterValueChangingCallback(OnNameChanging);
            m_Description = this.Q<TextArea>("StyleModelInfoDetailsDescription");
            m_Description.RegisterValueChangedCallback(OnDescriptionChanged);
            m_Description.RegisterValueChangingCallback(OnDescriptionChanging);
            m_Description.RegisterCallback((KeyDownEvent evt) =>
            {
                if ((evt.keyCode == KeyCode.Tab || (evt.keyCode == KeyCode.None && evt.character == '\t')) && !evt.shiftKey)
                {
                    evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                    evt.PreventDefault();
#endif
                    if (evt.character != '\t')
                        m_Description.focusController.FocusNextInDirectionEx(m_Description, VisualElementFocusChangeDirection.right);
                }
            }, TrickleDown.TrickleDown);

            m_DescriptionTextCount = this.Q<Text>("DescriptionTextCount");

            m_TrainingIcon = this.Q<CircularProgress>("TrainingIcon");
            m_ErrorIcon = this.Q<Icon>("ErrorIcon");
            m_StatusLabel = this.Q<Text>("StatusLabel");

            m_DuplicateButton = this.Q<AppUI.UI.Button>("StyleModelInfoDetailsDuplicateStyle");
            m_DuplicateButton.clicked += OnDuplicateButtonClicked;

            m_GenerateButton = this.Q<AppUI.UI.Button>("StyleModelInfoDetailsGenerateStyle");
            m_GenerateButton.clicked += OnGenerateButtonClicked;
        }

        void OnNameChanging(ChangingEvent<string> evt)
        {
            if (evt.newValue.Length > StyleData.maxNameLength) m_Name.SetValueWithoutNotify(evt.previousValue);
        }

        void OnDescriptionChanging(ChangingEvent<string> evt)
        {
            if (evt.newValue.Length > StyleData.maxDescriptionLength) m_Description.SetValueWithoutNotify(evt.previousValue);

            m_DescriptionTextCount.text = $"{m_Description.value.Length}/{StyleData.maxDescriptionLength}";
        }

        void UpdateInfoUI()
        {
            UpdateButtonState();
            UpdateStatusIcon();
            m_Name.SetEnabled(m_StyleData?.state == EState.New && !Utilities.ValidStringGUID(m_StyleData.guid));
            m_Description.SetEnabled(m_StyleData?.state == EState.New && !Utilities.ValidStringGUID(m_StyleData.guid));
            m_Name.SetValueWithoutNotify(m_StyleData?.title);
            m_Description.SetValueWithoutNotify(m_StyleData?.description);
            m_Description.tooltip = m_Description.value;
            m_DescriptionTextCount.text = $"{m_Description.value.Length}/{StyleData.maxDescriptionLength}";
            m_Description.style.height = k_OriginalDescriptionTextAreaHeight;
        }

        void UpdateStatusIcon()
        {
            m_TrainingIcon.style.display = DisplayStyle.None;
            m_ErrorIcon.style.display = DisplayStyle.None;
            if (m_StyleData.state == EState.Error)
            {
                m_StatusLabel.text = StringConstants.styleError;
                m_StatusLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                switch (m_StyleData.state)
                {
                    case EState.Loaded:
                        m_StatusLabel.style.display = DisplayStyle.None;
                        break;
                    case EState.Error:
                        m_StatusLabel.style.display = DisplayStyle.Flex;
                        m_StatusLabel.text = StringConstants.styleTrainedError;
                        m_ErrorIcon.style.display = DisplayStyle.Flex;
                        break;
                    case EState.Training:
                    case EState.Loading:
                        m_StatusLabel.style.display = DisplayStyle.Flex;
                        m_StatusLabel.text = StringConstants.styleTraining;
                        m_TrainingIcon.style.display = DisplayStyle.Flex;
                        break;
                    case EState.New:
                    case EState.Initial:
                        m_StatusLabel.style.display = DisplayStyle.None;
                        m_StatusLabel.text = StringConstants.styleNotTrained;
                        break;
                }
            }

        }

        void UpdateButtonState()
        {
            if (m_StyleData != null)
            {
                m_GenerateButton.SetEnabled(m_StyleData.state == EState.New);
                m_DuplicateButton.SetEnabled(m_StyleData.state != EState.New);
                m_GenerateButton.style.display = m_StyleData.state == EState.New ? DisplayStyle.Flex : DisplayStyle.None;
                m_DuplicateButton.style.display = m_StyleData.state != EState.New ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                m_GenerateButton.style.display = DisplayStyle.None;
                m_DuplicateButton.style.display = DisplayStyle.None;
            }
        }

        void OnNameChanged(ChangeEvent<string> evt)
        {
            if(!string.IsNullOrWhiteSpace(evt.newValue))
                m_StyleData.title = evt.newValue;
            else
                m_Name.SetValueWithoutNotify(m_StyleData.title);
        }

        void OnDescriptionChanged(ChangeEvent<string> evt)
        {
            if(!string.IsNullOrWhiteSpace(evt.newValue))
                m_StyleData.description = evt.newValue;
            else
                m_Description.SetValueWithoutNotify(m_StyleData.description);
            m_Description.tooltip = m_Description.value;
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            UnregisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            m_Name.UnregisterValueChangedCallback(OnNameChanged);
            m_Description.UnregisterValueChangedCallback(OnDescriptionChanged);
            m_DuplicateButton.clicked -= OnDuplicateButtonClicked;
            m_GenerateButton.clicked -= OnGenerateButtonClicked;
        }

        void OnGenerateButtonClicked()
        {
            m_EventBus.SendEvent(new GenerateButtonClickEvent());
        }

        void OnDuplicateButtonClicked()
        {
            m_EventBus.SendEvent(new DuplicateButtonClickEvent());
        }

#if ENABLE_UXML_TRAITS
        public new class UxmlFactory : UxmlFactory<StyleModelInfo, UxmlTraits> { }
#endif

        public void SetEventBus(EventBus eventBus)
        {
            m_EventBus = eventBus;
            m_EventBus.RegisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChanged);
            m_EventBus.RegisterEvent<GenerateButtonStateUpdateEvent>(OnGenerateButtonStateUpdate);
            m_EventBus.RegisterEvent<DuplicateButtonStateUpdateEvent>(OnDuplicateButtonStateUpdate);
            m_EventBus.RegisterEvent<StyleTrainingEvent>(OnStyleTrainingEvent);
        }

        void OnStyleTrainingEvent(StyleTrainingEvent arg0)
        {
            if (arg0.styleData.guid == m_StyleData.guid)
            {
                UpdateInfoUI();
            }
        }

        void OnDuplicateButtonStateUpdate(DuplicateButtonStateUpdateEvent arg0)
        {
            UpdateButtonState();
        }

        void OnGenerateButtonStateUpdate(GenerateButtonStateUpdateEvent arg0)
        {
            UpdateButtonState();
        }

        void OnStyleModelListSelectionChanged(StyleModelListSelectionChangedEvent arg0)
        {
            if (arg0.styleData is not null && m_StyleData != arg0.styleData)
            {
                if (m_StyleData != null)
                    m_StyleData.OnStateChanged -= OnStyleStateChanged;
                m_StyleData = arg0.styleData;
                m_StyleData.OnStateChanged += OnStyleStateChanged;
                LoadStyle();
            }
        }

        void OnStyleStateChanged(StyleData obj)
        {
            if (obj.state == EState.Initial)
                LoadStyle();
        }

        void LoadStyle()
        {
            m_EventBus.SendEvent(new ShowLoadingScreenEvent
            {
                description = "Loading Style...",
                show = true
            });
            m_StyleData.GetArtifact(OnGetArtifactDone, true);
        }

        void OnGetArtifactDone(StyleData obj)
        {
            if (obj == m_StyleData)
            {
                m_EventBus.SendEvent(new ShowLoadingScreenEvent
                {
                    show = false
                });
                UpdateInfoUI();
                m_EventBus.SendEvent(new SampleOutputDataSourceChangedEvent
                {
                    styleData = m_StyleData
                });
                m_EventBus.SendEvent(new TrainingSetDataSourceChangedEvent()
                {
                    styleData = m_StyleData
                });
            }
        }
    }
}