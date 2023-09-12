using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputView : ExVisualElement, IStyleModelInfoTabView
    {
        enum EViewType
        {
            Grid,
            List
        }

        Text m_ToolTipText;
        Icon m_ToolTipIcon;

        StyleData m_StyleData;
        Text m_HintText;
        SampleOutputGridView m_GridView;

        //SampleOutputListView m_ListView;
        IList<SampleOutputData> m_SampleOutputData;
        EventBus m_EventBus;
        float m_ThumbnailSize;
        public Action<int> OnDeleteClickedCallback;
        bool m_CanModify;

        public SampleOutputView()
        {
            name = "SampleSetView";
            AddToClassList("styletrainer-sampleoutputview");
            styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/SampleOutputView"));

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

            var ve = new ExVisualElement { name = "GridViewContainer" };
            ve.AddToClassList("styletrainer-sampleoutputview-gridview_container");
            Add(ve);
            m_GridView = new SampleOutputGridView();
            m_GridView.AddToClassList("styletrainer-sampleoutputview-gridview");
            m_GridView.OnDeleteClickedCallback += OnSampleOutputDeleteClicked;

            // m_ListView = new SampleOutputListView();
            // m_ListView.AddToClassList("styletrainer-sampleoutputview-listview");
            // m_ListView.OnDeleteClickedCallback += OnSampleOutputDeleteClicked;
            m_HintText = new Text
            {
                text = "Add some sample prompt to validate the model.",
                name = "SampleOutputViewHintText"
            };
            m_HintText.AddToClassList("styletrainer-sampleoutputview__hintext");
            ve.Add(m_GridView);

            //Add(m_ListView);
            ve.Add(m_HintText);
            ShowView(EViewType.Grid);
        }

        void OnSampleOutputDeleteClicked(int obj)
        {
            m_EventBus.SendEvent(new DeleteSampleOutputEvent
            {
                styleData = m_StyleData,
                deleteIndex = obj
            });
        }

        void ShowView(EViewType viewType)
        {
            var itemCount = m_SampleOutputData?.Count;
            if (itemCount == 0)
            {
                m_HintText.style.display = DisplayStyle.Flex;
                m_GridView.style.display = DisplayStyle.None;

                //m_ListView.style.display = DisplayStyle.None;
            }
            else
            {
                m_HintText.style.display = DisplayStyle.None;
                m_GridView.style.display = viewType == EViewType.Grid ? DisplayStyle.Flex : DisplayStyle.None;

                //m_ListView.style.display= viewType == EViewType.List? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateToolTip();
        }

        void UpdateToolTip()
        {
            if (m_CanModify)
            {
                var itemCount = m_SampleOutputData?.Count;
                if (itemCount >= StyleTrainerConfig.config.minSampleSetSize &&
                    itemCount <= StyleTrainerConfig.config.maxSampleSetSize)
                {
                    m_ToolTipIcon.iconName = "check";
                    m_ToolTipText.text = $"Sample output good to go!";
                }
                else
                {
                    if (itemCount > StyleTrainerConfig.config.maxSampleSetSize)
                    {
                        m_ToolTipText.text = $"There are too many prompts. Remove {m_SampleOutputData?.Count - StyleTrainerConfig.config.maxSampleSetSize} prompt(s).";
                        m_ToolTipIcon.iconName = "warning";
                    }
                    else
                    {
                        m_ToolTipText.text = $"Sample output requires {StyleTrainerConfig.config.minSampleSetSize - m_SampleOutputData?.Count} more prompt(s).";
                        m_ToolTipIcon.iconName = "info";
                    }
                }
            }
            else
            {
                m_ToolTipIcon.iconName = "info";
                m_ToolTipText.text = "Style is trained. Sample set is locked";
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
        }

        public void CanModify(bool canModify)
        {
            m_CanModify = canModify;
            UpdateToolTip();

            //m_GridView.SetEnabled(canModify);
            m_GridView.SetCanModify(m_CanModify);
        }

        void OnSampleOutputSourceChanged(SampleOutputDataSourceChangedEvent arg0)
        {
            if (m_StyleData != null)
                m_StyleData.OnDataChanged -= OnStyleDataChanged;
            m_StyleData = arg0.styleData;

            // Todo unregister on clean up
            m_StyleData.OnDataChanged += OnStyleDataChanged;
            m_SampleOutputData = arg0.sampleOutput;
            m_GridView.itemsSource = (IList)m_SampleOutputData;

            //m_ListView.itemsSource = (IList)m_SampleOutputData;
            // if (!Utilities.ValidStringGUID(m_StyleData.guid))
            // {
            //     ShowView(EViewType.List);
            //     m_ListView.RefreshItems();
            // }
            // else
            {
                ShowView(EViewType.Grid);
                m_GridView.Refresh();
            }
        }

        void OnStyleDataChanged(StyleData styleData)
        {
            // if (!Utilities.ValidStringGUID(m_StyleData.guid))
            // {
            //     ShowView(EViewType.List);
            //     m_ListView.RefreshItems();
            // }
            // else
            {
                ShowView(EViewType.Grid);
                m_GridView.Refresh();

                //m_GridView.RefreshThumbnailSize(m_ThumbnailSize);
            }
        }

        void OnThumbnailSizeChanged(ThumbnailSizeChangedEvent arg0)
        {
            m_ThumbnailSize = arg0.thumbnailSize;
            if (m_GridView.style.display != DisplayStyle.None)
                m_GridView.RefreshThumbnailSize(arg0.thumbnailSize);
        }

        public void OnViewActivated(float thumbNailSize)
        {
            m_ThumbnailSize = thumbNailSize;
            if (m_GridView.style.display == DisplayStyle.Flex)
                m_GridView.RefreshThumbnailSize(m_ThumbnailSize);
        }

        public void SelectItems(IList<int> indices)
        {
            m_GridView.SetSelectionWithoutNotify(indices);
            m_GridView.ScrollToItem(indices[0]);
        }
    }
}