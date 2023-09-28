using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.CheckPointModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using UnityEngine;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

namespace Unity.Muse.StyleTrainer
{
    class StyleModelInfo : ExVisualElement
    {
        Text m_StatusLabel;
        Text m_DescriptionTextCount;
        Icon m_ErrorIcon;
        CircularProgress m_TrainingIcon;
        TextField m_Name;
        TextArea m_Description;
        Dropdown m_StyleCheckPointDropdown;
        ActionButton m_DuplicateButton;
        ActionButton m_GenerateButton;
        EventBus m_EventBus;
        StyleData m_StyleData;
        Checkbox m_FavouriteToggle;
        CheckPointData m_CurrentCheckPoint;
        TouchSliderInt m_TrainingStepSlider;
        bool m_DuplicateButtonState = false;
        bool m_GenerateButtonState = false;

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
                    evt.PreventDefault();
                    if (evt.character != '\t')
                        m_Description.focusController.FocusNextInDirectionEx(m_Description, VisualElementFocusChangeDirection.right);
                }
            }, TrickleDown.TrickleDown);

            m_DescriptionTextCount = this.Q<Text>("DescriptionTextCount");
            m_StyleCheckPointDropdown = this.Q<Dropdown>("StyleModelInfoDetailsCheckpoint");
            m_StyleCheckPointDropdown.bindItem = BindDropDownItem;
            m_StyleCheckPointDropdown.RegisterValueChangedCallback(OnCheckPointDropDownChanged);

            m_StatusLabel = this.Q<Text>("StatusLabel");
            m_ErrorIcon = this.Q<Icon>("ErrorIcon");
            m_TrainingIcon = this.Q<CircularProgress>("TrainingIcon");

            m_DuplicateButton = this.Q<ActionButton>("StyleModelInfoDetailsDuplicateStyle");
            m_DuplicateButton.SetEnabled(m_DuplicateButtonState);
            m_DuplicateButton.clicked += OnDuplicateButtonClicked;

            m_GenerateButton = this.Q<ActionButton>("StyleModelInfoDetailsGenerateStyle");
            m_GenerateButton.clicked += OnGenerateButtonClicked;
            m_GenerateButton.SetEnabled(m_GenerateButtonState);

            m_FavouriteToggle = this.Q<Checkbox>("FavouriteToggle");
            m_FavouriteToggle.RegisterValueChangedCallback(OnFavoriteToggleChanged);
            var d = m_FavouriteToggle.Q("appui-checkbox__checkmark");
            d.AddToClassList("styletrainer--star-icon-selected");
            d = m_FavouriteToggle.Q("appui-checkbox__box");
            var icon = d.Q<Icon>("FavoriteToggleStarIcon");
            if (icon == null)
            {
                icon = new Icon
                {
                    name = "FavoriteToggleStarIcon"
                };
                icon.AddToClassList("styletrainer--star-icon-regular");
                icon.AddToClassList("styletrainer-stylemodelinfo__favouritetoggle-icon");
                d.Add(icon);
            }

            m_TrainingStepSlider = this.Q<TouchSliderInt>("TrainingStepSlider");
            m_TrainingStepSlider.incrementFactor = StyleTrainerConfig.config.trainingStepsIncrement;
            m_TrainingStepSlider.lowValue = StyleTrainerConfig.config.trainingStepRange.x;
            m_TrainingStepSlider.highValue = StyleTrainerConfig.config.trainingStepRange.y;
            m_TrainingStepSlider.RegisterValueChangedCallback(OnTrainingStepsChanged);
            m_TrainingStepSlider.RegisterValueChangingCallback(OnTrainingStepsChanging);
        }

        void OnTrainingStepsChanging(ChangingEvent<int> evt)
        {
            if (m_CurrentCheckPoint != null)
            {
                m_CurrentCheckPoint.trainingSteps = evt.newValue;
                m_TrainingStepSlider.SetValueWithoutNotify(m_CurrentCheckPoint.trainingSteps);
            }
        }

        void OnTrainingStepsChanged(ChangeEvent<int> evt)
        {
            if (m_CurrentCheckPoint != null)
            {
                m_CurrentCheckPoint.trainingSteps = evt.newValue;
                m_TrainingStepSlider.SetValueWithoutNotify(m_CurrentCheckPoint.trainingSteps);
            }
        }

        void OnNameChanging(ChangingEvent<string> evt)
        {
            if (evt.newValue.Length > CheckPointData.maxNameLength) m_Name.SetValueWithoutNotify(evt.previousValue);
        }

        void OnDescriptionChanging(ChangingEvent<string> evt)
        {
            if (evt.newValue.Length > CheckPointData.maxDescriptionLength) m_Description.SetValueWithoutNotify(evt.previousValue);

            m_DescriptionTextCount.text = $"{m_Description.value.Length}/{CheckPointData.maxDescriptionLength}";
        }

        void OnFavoriteToggleChanged(ChangeEvent<CheckboxState> evt)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            var checkPoint = m_StyleCheckPointDropdown.sourceItems[selection.Current] as CheckPointData;
            m_EventBus.SendEvent(new SetFavouriteCheckPointEvent
            {
                checkPointGUID = evt.newValue == CheckboxState.Checked ? checkPoint.guid : Guid.Empty.ToString(),
                styleData = m_StyleData
            });
        }

        void OnFavouriteCheckPointChanged(FavouriteCheckPointChangeEvent evt)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            m_StyleCheckPointDropdown.Refresh();
            UpdateFavouriteState(selection.Current);
        }

        void OnCheckPointDropDownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            using var selection = evt.newValue.GetEnumerator();
            if (!selection.MoveNext())
                return;

            UpdateInfoUI(selection.Current);
            m_EventBus.SendEvent(new CheckPointSelectionChangeEvent
            {
                styleData = m_StyleData,
                index = selection.Current
            });
        }

        void SetCurrentCheckPoint(CheckPointData checkPoint)
        {
            if (m_CurrentCheckPoint != checkPoint)
            {
                if (m_CurrentCheckPoint != null) m_CurrentCheckPoint.OnStateChanged -= OnCheckPointStateChanged;

                m_CurrentCheckPoint = checkPoint;
                if (m_CurrentCheckPoint != null) m_CurrentCheckPoint.OnStateChanged += OnCheckPointStateChanged;
            }
        }

        void OnCheckPointStateChanged(CheckPointData obj)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            UpdateInfoUI(selection.Current);
        }

        void UpdateInfoUI(int selected)
        {
            var checkPoint = m_StyleCheckPointDropdown.sourceItems[selected] as CheckPointData;
            if (checkPoint != null)
            {
                SetCurrentCheckPoint(checkPoint);
                UpdateFavouriteState(selected);
                UpdateButtonState(checkPoint.state, m_StyleCheckPointDropdown.sourceItems.Count);
                UpdateStatusIcon(checkPoint);
                m_Name.SetEnabled(checkPoint.state == EState.New);
                m_Name.SetValueWithoutNotify(checkPoint.name);
                m_Description.SetEnabled(checkPoint.state == EState.New);
                m_Description.SetValueWithoutNotify(checkPoint.description);
                m_DescriptionTextCount.text = $"{m_Description.value.Length}/{CheckPointData.maxDescriptionLength}";
                m_TrainingStepSlider.SetEnabled(checkPoint.state == EState.New);
                m_TrainingStepSlider.SetValueWithoutNotify(checkPoint.trainingSteps);
            }
        }

        void UpdateStatusIcon(CheckPointData checkPoint)
        {
            m_StatusLabel.style.display = DisplayStyle.None;
            m_ErrorIcon.style.display = DisplayStyle.None;
            m_TrainingIcon.style.display = DisplayStyle.None;
            if (m_StyleData.state == EState.Error)
            {
                m_StatusLabel.text = StringConstants.styleError;
                m_StatusLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                switch (checkPoint.state)
                {
                    case EState.Error:
                        m_StatusLabel.style.display = DisplayStyle.Flex;
                        m_StatusLabel.text = StringConstants.versionTrainedError;
                        m_ErrorIcon.style.display = DisplayStyle.Flex;
                        break;
                    case EState.Training:
                    case EState.Loading:
                        m_TrainingIcon.style.display = DisplayStyle.Flex;
                        break;
                    case EState.New:
                    case EState.Initial:
                        m_StatusLabel.text = StringConstants.versionNotTrained;
                        m_StatusLabel.style.display = DisplayStyle.Flex;
                        break;
                }
            }

        }

        void UpdateButtonState(EState state, int itemCount)
        {
            if (m_StyleData.state == EState.Error || m_StyleData.state == EState.Loading)
            {
                m_GenerateButton.SetEnabled(false);
                m_DuplicateButton.SetEnabled(false);
            }
            else
            {
             switch (state)
                {
                    case EState.Training:
                        m_GenerateButton.SetEnabled(false);
                        m_GenerateButton.icon = "stop";
                        m_GenerateButton.label = StringConstants.stopGenerate;
                        m_DuplicateButton.SetEnabled(false);
                        m_DuplicateButton.label = StringConstants.duplicateStyle;
                        m_DuplicateButton.icon = "duplicate";
                        break;
                    case EState.Loaded:
                        m_GenerateButton.SetEnabled(m_GenerateButtonState);
                        m_GenerateButton.icon = "review";
                        m_GenerateButton.label = StringConstants.newVersion;
                        m_DuplicateButton.SetEnabled(m_DuplicateButtonState);
                        m_DuplicateButton.label = StringConstants.duplicateStyle;
                        m_DuplicateButton.icon = "duplicate";
                        break;
                    case EState.Loading:
                        m_GenerateButton.SetEnabled(false);
                        m_GenerateButton.icon = "review";
                        m_GenerateButton.label = StringConstants.newVersion;
                        m_DuplicateButton.SetEnabled(false);
                        m_DuplicateButton.label = StringConstants.discardCheckPoint;
                        m_DuplicateButton.icon = "duplicate";
                        break;
                    case EState.New:
                        m_GenerateButton.SetEnabled(true);
                        m_GenerateButton.icon = "play";
                        m_GenerateButton.label = StringConstants.generateStyle;
                        m_DuplicateButton.SetEnabled(itemCount > 1);
                        m_DuplicateButton.label = StringConstants.discardCheckPoint;
                        m_DuplicateButton.icon = "delete";
                        break;
                    case EState.Error:
                        m_GenerateButton.SetEnabled(m_GenerateButtonState);
                        m_GenerateButton.icon = "review";
                        m_GenerateButton.label = StringConstants.newVersion;
                        m_DuplicateButton.SetEnabled(false);
                        m_DuplicateButton.label = StringConstants.discardCheckPoint;
                        m_DuplicateButton.icon = "delete";
                        break;
                    case EState.Initial:
                        m_GenerateButton.SetEnabled(m_GenerateButtonState);
                        m_GenerateButton.icon = "play";
                        m_GenerateButton.label = StringConstants.generateStyle;
                        m_DuplicateButton.SetEnabled(false);
                        m_DuplicateButton.label = StringConstants.duplicateStyle;
                        m_DuplicateButton.icon = "duplicate";
                        break;
                }
            }
        }

        void UpdateFavouriteState(int selected)
        {
            var checkPoint = m_StyleCheckPointDropdown.sourceItems[selected] as CheckPointData;
            if (checkPoint?.state == EState.Loaded)
            {
                m_FavouriteToggle.style.display = DisplayStyle.Flex;
                m_FavouriteToggle.SetValueWithoutNotify(checkPoint.guid == m_StyleData.favoriteCheckPoint ? CheckboxState.Checked : CheckboxState.Unchecked);
            }
            else
            {
                m_FavouriteToggle.style.display = DisplayStyle.None;
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
        }

        string GetCheckPointIcon(CheckPointData checkPoint)
        {
            var icon = "";
            switch (checkPoint.state)
            {
                case EState.Training:
                    icon = "training";
                    break;
                case EState.Loaded:
                    icon = "";
                    break;
                case EState.Loading:
                    icon = "training";
                    break;
                case EState.New:
                    icon = "prohibit";
                    break;
                case EState.Error:
                    icon = "warning";
                    break;
                case EState.Initial:
                    break;
            }

            if (checkPoint.guid == m_StyleData.favoriteCheckPoint && checkPoint.state != EState.New)
                icon = "star-selected";
            return icon;
        }

        void BindDropDownItem(DropdownItem arg1, int arg2)
        {
            var checkPoint = m_StyleCheckPointDropdown.sourceItems[arg2] as CheckPointData;
            arg1.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/Icons"));
            arg1.icon = GetCheckPointIcon(checkPoint);
            arg1.label = checkPoint?.dropDownLabelName;
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            UnregisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            m_Name.UnregisterValueChangedCallback(OnNameChanged);
            m_Description.UnregisterValueChangedCallback(OnDescriptionChanged);
            m_DuplicateButton.clicked -= OnDuplicateButtonClicked;
            m_GenerateButton.clicked -= OnGenerateButtonClicked;
            m_TrainingStepSlider.UnregisterValueChangedCallback(OnTrainingStepsChanged);
        }

        void OnGenerateButtonClicked()
        {
            m_EventBus.SendEvent(new GenerateButtonClickEvent());
        }

        void OnDuplicateButtonClicked()
        {
            m_EventBus.SendEvent(new DuplicateButtonClickEvent());
        }

        public new class UxmlFactory : UxmlFactory<StyleModelInfo, UxmlTraits> { }

        public void SetEventBus(EventBus eventBus)
        {
            m_EventBus = eventBus;
            m_EventBus.RegisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChanged);
            m_EventBus.RegisterEvent<GenerateButtonStateUpdateEvent>(OnGenerateButtonStateUpdate);
            m_EventBus.RegisterEvent<DuplicateButtonStateUpdateEvent>(OnDuplicateButtonStateUpdate);
            m_EventBus.RegisterEvent<CheckPointSourceDataChangedEvent>(OnCheckPointSourceDataChanged);
            m_EventBus.RegisterEvent<CheckPointDataChangedEvent>(OnCheckPointDataChanged);
            m_EventBus.RegisterEvent<FavouriteCheckPointChangeEvent>(OnFavouriteCheckPointChanged);
            m_EventBus.RegisterEvent<StyleTrainingEvent>(OnStyleTrainingEvent);
        }

        void OnStyleTrainingEvent(StyleTrainingEvent arg0)
        {
            if (arg0.styleData.guid == m_StyleData.guid && arg0.state != EState.Training)
            {
                UpdateInfoUI(m_StyleData.SelectedCheckPointIndex());
            }
        }

        void OnCheckPointDataChanged(CheckPointDataChangedEvent arg0)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            for (var i = 0; i < m_StyleCheckPointDropdown.sourceItems.Count; ++i)
            {
                var d = m_StyleCheckPointDropdown.sourceItems[i] as CheckPointData;
                if (d?.guid == arg0.checkPointData.guid)
                {
                    if (selection.Current == i)
                    {
                        UpdateInfoUI(i);
                        m_StyleCheckPointDropdown.Refresh();
                    }

                    break;
                }
            }
        }

        void OnCheckPointSourceDataChanged(CheckPointSourceDataChangedEvent arg0)
        {
            if (arg0.styleData.guid == m_StyleData.guid)
                UpdateCheckPointDropDown(arg0.styleData.checkPoints, arg0.styleData.SelectedCheckPointIndex());
        }

        void OnDuplicateButtonStateUpdate(DuplicateButtonStateUpdateEvent arg0)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            m_DuplicateButtonState = arg0.state;
            m_DuplicateButton.SetEnabled(arg0.state);
            if (m_StyleCheckPointDropdown.sourceItems?.Count >selection.Current)
            {
                var checkpoint = (CheckPointData)m_StyleCheckPointDropdown.sourceItems[selection.Current];
                UpdateButtonState(checkpoint.state, m_StyleCheckPointDropdown.sourceItems.Count);
            }
        }

        void OnGenerateButtonStateUpdate(GenerateButtonStateUpdateEvent arg0)
        {
            using var selection = m_StyleCheckPointDropdown.value.GetEnumerator();
            if (!selection.MoveNext())
                return;

            m_GenerateButtonState = arg0.state;
            m_GenerateButton.SetEnabled(arg0.state);
            if (m_StyleCheckPointDropdown.sourceItems?.Count > selection.Current)
            {
                var checkpoint = (CheckPointData)m_StyleCheckPointDropdown.sourceItems[selection.Current];
                UpdateButtonState(checkpoint.state, m_StyleCheckPointDropdown.sourceItems.Count);
            }
        }

        void OnStyleModelListSelectionChanged(StyleModelListSelectionChangedEvent arg0)
        {
            if (arg0.styleData is not null)
            {
                m_StyleData = arg0.styleData;
                m_EventBus.SendEvent(new ShowLoadingScreenEvent
                {
                    description = "Loading Style...",
                    show = true
                });
                arg0.styleData.GetArtifact(OnGetArtifactDone, true);
            }
        }

        void OnGetArtifactDone(StyleData obj)
        {
            if (obj == m_StyleData)
            {
                m_EventBus.SendEvent(new ShowLoadingScreenEvent
                {
                    show = false
                });
                m_Name.SetValueWithoutNotify(obj.title);
                m_Description.SetValueWithoutNotify(obj.description);
                m_DescriptionTextCount = this.Q<Text>("DescriptionTextCount");
                m_DescriptionTextCount.text = $"{m_Description.value.Length}/{CheckPointData.maxDescriptionLength}";
                UpdateCheckPointDropDown(obj.checkPoints, obj.SelectedCheckPointIndex());
            }
        }

        void UpdateCheckPointDropDown(IReadOnlyList<CheckPointData> checkPoints, int selectedCheckPoint)
        {
            if (checkPoints?.Count > 0)
            {
                // need to clear selection first in case the selected index is greater than the source items count
                m_StyleCheckPointDropdown.value = Array.Empty<int>();
                m_StyleCheckPointDropdown.sourceItems = (IList)checkPoints;
                m_StyleCheckPointDropdown.value = new []{selectedCheckPoint};
            }
            else
            {
                m_StyleCheckPointDropdown.sourceItems = new[]
                {
                    new CheckPointData(EState.New, Utilities.emptyGUID, m_StyleData.projectID)
                        { dropDownLabelName = StringConstants.newVersion }
                };
                m_StyleCheckPointDropdown.value = new[] {0};
            }

            m_StyleCheckPointDropdown.Refresh();
            UpdateInfoUI(selectedCheckPoint);
            m_StyleCheckPointDropdown.SetEnabled(checkPoints?.Count > 1);
        }
    }
}