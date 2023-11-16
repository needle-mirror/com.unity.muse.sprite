using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.CheckPointModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputView : ExVisualElement, IStyleModelInfoTabView
    {
        Text m_ToolTipText;
        Icon m_ToolTipIcon;

        StyleData m_StyleData;
        Text m_HintText;

        SampleOutputListView m_ListView;
        EventBus m_EventBus;
        float m_ThumbnailSize = StyleModelInfoEditor.k_InitialThumbnailSliderValue;
        public Action<int> OnDeleteClickedCallback;
        bool m_CanModify;

        public SampleOutputView()
        {
            name = "SampleSetViewV2";
            AddToClassList("styletrainer-sampleoutputview");
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.sampleOutputViewStyleSheet));

            var toolTipContainer = new ExVisualElement { name = "ToolTipContainer" };
            Add(toolTipContainer);
            toolTipContainer.AddToClassList("styletrainer-sampleoutputview-tooltip_container");
            m_ToolTipIcon = new Icon
            {
                name = "ToolTipIcon",
                iconName = "info"
            };
            m_ToolTipIcon.AddToClassList("styletrainer-sampleoutputview-tooltip_icon");
            toolTipContainer.Add(m_ToolTipIcon);
            m_ToolTipText = new Text
            {
                name = "ToolTipText",
                text = $"Sample output requires at least {StyleTrainerConfig.config.minSampleSetSize} and no more than {StyleTrainerConfig.config.maxSampleSetSize} prompts."
            };
            toolTipContainer.Add(m_ToolTipText);
            m_ToolTipText.AddToClassList("styletrainer-sampleoutputview-tooltip_text");

            m_ListView = new SampleOutputListView();
            // m_ListView.AddToClassList("styletrainer-sampleoutputview-listview");
            m_HintText = new Text
            {
                text = "Add some sample prompt to validate the model.",
                name = "SampleOutputViewHintText"
            };
            m_HintText.AddToClassList("styletrainer-sampleoutputview__hintext");

            Add(m_ListView);
            Add(m_HintText);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            m_ListView.OnDeleteClickedCallback -= OnSampleOutputDeleteClicked;
            m_ListView.OnFavoriteToggleChangedCallback -= OnFavouriteToggleChanged;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_ListView.OnDeleteClickedCallback += OnSampleOutputDeleteClicked;
            m_ListView.OnFavoriteToggleChangedCallback += OnFavouriteToggleChanged;
        }

        void OnFavouriteToggleChanged(bool favourite, CheckPointData checkPoint)
        {
            m_EventBus.SendEvent(new SetFavouriteCheckPointEvent
            {
                checkPointGUID = favourite ? checkPoint.guid : Guid.Empty.ToString(),
                styleData = m_StyleData
            });
        }

        void OnSampleOutputDeleteClicked(int obj)
        {
            m_EventBus.SendEvent(new DeleteSampleOutputEvent
            {
                styleData = m_StyleData,
                deleteIndex = obj
            });
        }

        void ShowView()
        {
            var itemCount = m_StyleData?.sampleOutputPrompts?.Count;
            if (itemCount == 0)
            {
                m_HintText.style.display = DisplayStyle.Flex;
                m_ListView.style.display = DisplayStyle.None;
            }
            else
            {
                m_HintText.style.display = DisplayStyle.None;
                m_ListView.style.display= DisplayStyle.Flex;
                m_ListView.SetStyleData(m_StyleData);
            }

            UpdateToolTip();
        }

        void UpdateToolTip()
        {
            if (m_CanModify)
            {
                var itemCount = m_StyleData?.sampleOutputPrompts?.Count;
                if (itemCount >= StyleTrainerConfig.config.minSampleSetSize &&
                    itemCount <= StyleTrainerConfig.config.maxSampleSetSize)
                {
                    var emptyPrompt = 0;
                    for (int i = 0; i < itemCount; ++i)
                    {
                        if (string.IsNullOrWhiteSpace(m_StyleData.sampleOutputPrompts[i]))
                            ++emptyPrompt;
                    }

                    if (emptyPrompt == 0)
                    {
                        m_ToolTipIcon.iconName = "check";
                        m_ToolTipText.text = $"Sample output good to go!";
                    }
                    else
                    {
                        m_ToolTipIcon.iconName = "info";
                        m_ToolTipText.text = $"There are empty prompts. Please fill them up.";
                    }
                }
                else
                {
                    if (itemCount > StyleTrainerConfig.config.maxSampleSetSize)
                    {
                        m_ToolTipText.text = $"There are too many prompts. Remove {itemCount - StyleTrainerConfig.config.maxSampleSetSize} prompt(s).";
                        m_ToolTipIcon.iconName = "warning";
                    }
                    else
                    {
                        m_ToolTipText.text = $"Sample output requires {StyleTrainerConfig.config.minSampleSetSize - itemCount} more prompt(s).";
                        m_ToolTipIcon.iconName = "info";
                    }
                }
            }
            else
            {
                m_ToolTipIcon.iconName = "info";
                m_ToolTipText.text = "Style created. Sample set is locked";
            }
        }

        public VisualElement GetView()
        {
            return this;
        }

        public void SetEventBus(EventBus evtBus)
        {
            m_EventBus = evtBus;
            m_EventBus.RegisterEvent<ThumbnailSizeChangedEvent>(OnThumbnailSizeChanged);
            m_EventBus.RegisterEvent<SampleOutputDataSourceChangedEvent>(OnSampleOutputSourceChanged);
            m_EventBus.RegisterEvent<CheckPointSourceDataChangedEvent>(OnCheckPointSourceDataChanged);
            m_EventBus.RegisterEvent<CheckPointDataChangedEvent>(OnCheckPointDataChanged);
            m_EventBus.RegisterEvent<FavouriteCheckPointChangeEvent>(OnFavouriteToggleChanged);
        }

        void OnFavouriteToggleChanged(FavouriteCheckPointChangeEvent arg0)
        {
            m_ListView.UpdateFavouriteCheckpoint();
        }

        void OnCheckPointDataChanged(CheckPointDataChangedEvent arg0)
        {
            if (m_StyleData?.guid == arg0.styleData.guid)
            {
                m_ListView.CheckPointDataChanged(arg0.checkPointData);
            }
        }

        void OnCheckPointSourceDataChanged(CheckPointSourceDataChangedEvent arg0)
        {
            if (m_StyleData?.guid == arg0.styleData.guid)
            {
                m_ListView.CheckPointSourceDataChanged();
            }
        }

        public void CanModify(bool canModify)
        {
            m_CanModify = canModify;
            UpdateToolTip();
            m_ListView.CanModify(canModify);
        }

        void OnSampleOutputSourceChanged(SampleOutputDataSourceChangedEvent arg0)
        {
            if (m_StyleData != null)
                m_StyleData.OnDataChanged -= OnStyleDataChanged;
            m_StyleData = arg0.styleData;

            // Todo unregister on clean up
            m_StyleData.OnDataChanged += OnStyleDataChanged;
            ShowView();
        }

        void OnStyleDataChanged(StyleData styleData)
        {
            if (m_StyleData != null)
                m_StyleData.OnDataChanged -= OnStyleDataChanged;
            m_StyleData = styleData;

            // Todo unregister on clean up
            m_StyleData.OnDataChanged += OnStyleDataChanged;
            ShowView();
        }

        void OnThumbnailSizeChanged(ThumbnailSizeChangedEvent arg0)
        {
            m_ThumbnailSize = arg0.thumbnailSize;
            m_ListView.SetRowSize(m_ThumbnailSize);
        }

        public void OnViewActivated(float thumbNailSize)
        {
            m_ThumbnailSize = thumbNailSize;
            m_ListView.SetRowSize(m_ThumbnailSize);
        }

        public void SelectItems(IList<int> indices)
        {
            m_ListView.SelectItems(indices);
        }
    }
}