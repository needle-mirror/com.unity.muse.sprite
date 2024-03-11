using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetUIEvents;
using Unity.Muse.StyleTrainer.Manipulator;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class TrainingSetView : ExVisualElement, IStyleModelInfoTabView
    {
        GridView m_GridView;
        StyleData m_StyleData;
        EventBus m_EventBus;
        Text m_HintText;
        int m_CountPerRow = 2;
        const float k_DefaultThumbnailSize = 240;
        VisualElement m_DragAndDropContainer;
        SpriteTextureDropManipulator m_SpriteTextureDropManipulator;
        public Action<IList<Texture2D>> OnDragAndDrop;
        float m_ThumbnailSize = 1;
        public Action<int> OnDeleteClickedCallback;
        bool m_CanModify;
        ExVisualElement m_ToolTipContainer;
        Icon m_ToolTipIcon;
        Text m_ToolTipText;
        VisualElement m_LoadingScreen;
        VisualElement m_DataContent;
        string m_CurrentContext;

        void BindItem(VisualElement arg1, int arg2)
        {
            if (arg1 is TrainingItemGridItem tigi)
            {
                tigi.CanModify(m_CanModify);
                tigi.itemIndex = arg2;
                tigi.SetPreviewImage(this[arg2].imageArtifact);
                tigi.OnDeleteClicked += OnDeleteClicked;
            }
        }

        void UnbindItem(VisualElement arg1, int arg2)
        {
            if (arg1 is TrainingItemGridItem ve)
            {
                ve.OnDeleteClicked -= OnDeleteClicked;
            }
        }

        void OnDeleteClicked(int obj)
        {
            m_EventBus.SendEvent(new DeleteTrainingSetEvent
            {
                styleData = m_StyleData,
                indices = new[] { obj }
            });
        }

        new TrainingData this[int index] => (TrainingData)m_GridView.itemsSource[index];

        static VisualElement MakeGridItem()
        {
            return TrainingItemGridItem.CreateFromUxml();
        }

        public VisualElement GetView()
        {
            return this;
        }

        public void SetEventBus(EventBus evtBus)
        {
            m_EventBus = evtBus;
            m_EventBus.RegisterEvent<ThumbnailSizeChangedEvent>(OnThumbnailSizeChanged);
            m_EventBus.RegisterEvent<TrainingSetDataSourceChangedEvent>(OnTrainingSetDataSourceChanged);
            m_SpriteTextureDropManipulator = new SpriteTextureDropManipulator();
            this.AddManipulator(m_SpriteTextureDropManipulator);
            m_SpriteTextureDropManipulator.onDragStart += OnDragStart;
            m_SpriteTextureDropManipulator.onDragEnd += OnDragEnd;
            m_SpriteTextureDropManipulator.onDrop += OnDrop;
        }

        void OnTrainingSetDataSourceChanged(TrainingSetDataSourceChangedEvent arg0)
        {
            m_LoadingScreen.style.display = DisplayStyle.Flex;
            m_DataContent.style.display = DisplayStyle.None;
            m_StyleData = arg0.styleData;
            if (m_StyleData.state == EState.Loading)
            {
                m_StyleData.OnStateChanged += OnStyleStateChange;
            }
            else
            {
                if (m_StyleData.trainingSetData != null)
                {
                    var context = m_StyleData.trainingSetData[0].guid;
                    m_CurrentContext = context;
                    m_StyleData.trainingSetData[0].GetArtifact(x =>
                        OnGetTrainingSetDone(context, x), true);
                }
            }
        }

        void OnStyleStateChange(StyleData obj)
        {
            obj.OnStateChanged -= OnStyleStateChange;
            if (obj == m_StyleData && m_StyleData.trainingSetData != null)
            {
                m_CurrentContext = m_StyleData.trainingSetData[0].guid;
                m_StyleData.trainingSetData[0].GetArtifact(x =>
                    OnGetTrainingSetDone(m_StyleData.trainingSetData[0].guid, x), true);
            }
        }

        void OnGetTrainingSetDone(string context, IList<TrainingData> obj)
        {
            if (m_CurrentContext == context)
            {
                m_LoadingScreen.style.display = DisplayStyle.None;
                m_DataContent.style.display = DisplayStyle.Flex;
                // dispose off old ones
                for(int i = 0; i < m_GridView.itemsSource?.Count; ++i)
                {
                    if (m_GridView.itemsSource[i] is TrainingData td)
                    {
                        td.imageArtifact?.OnDispose();
                    }
                }

                m_GridView.itemsSource = (IList)obj;
                UpdateHintView();
                m_GridView.Refresh();
            }
        }

        public void OnViewActivated(float thumbNailSize)
        {
            m_ThumbnailSize = thumbNailSize;
            m_GridView.Refresh();
        }

        void OnDrop(IList<Texture2D> textures)
        {
            if (m_CanModify)
            {
                m_DragAndDropContainer.RemoveFromClassList("styletrainer-trainingsetview__draganddropcontainer_dragging");
                OnDragAndDrop?.Invoke(textures);
            }
        }

        void OnDragEnd()
        {
            if (m_CanModify) m_DragAndDropContainer.RemoveFromClassList("styletrainer-trainingsetview__draganddropcontainer_dragging");
        }

        void OnDragStart()
        {
            if (m_CanModify)
                m_DragAndDropContainer.AddToClassList("styletrainer-trainingsetview__draganddropcontainer_dragging");
        }

        void OnThumbnailSizeChanged(ThumbnailSizeChangedEvent arg0)
        {
            m_ThumbnailSize = arg0.thumbnailSize;
            RefreshThumbnailSize(arg0.thumbnailSize);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (panel == null || float.IsNaN(evt.newRect.width) || Mathf.Approximately(0, evt.newRect.width))
                return;

            RefreshThumbnailSize(m_ThumbnailSize);
        }

        void RefreshThumbnailSize(float value)
        {
            var size = value * k_DefaultThumbnailSize;

            var sizeAndMargin = size;

            var width = m_DataContent.resolvedStyle.width;
            var newCountPerRow = Mathf.FloorToInt(width / sizeAndMargin);

            newCountPerRow = Mathf.Max(1, newCountPerRow);

            if (newCountPerRow != m_CountPerRow)
            {
                m_CountPerRow = newCountPerRow;
                m_GridView.columnCount = m_CountPerRow;
            }

            var itemHeight = Mathf.FloorToInt(width / m_CountPerRow);

            if (!Mathf.Approximately(itemHeight, m_GridView.itemHeight))
                m_GridView.itemHeight = itemHeight;
        }

        internal static TrainingSetView CreateFromUxml()
        {
            var visualTree = ResourceManager.Load<VisualTreeAsset>(PackageResources.trainingSetViewTemplate);
            var ve = (TrainingSetView)visualTree.CloneTree().Q("TrainingSetView");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.trainingSetViewStyleSheet));
            ve.BindElements();
            return ve;
        }

        void BindElements()
        {
            styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.trainingSetViewStyleSheet));
            m_GridView = this.Q<GridView>("TrainingSetViewGridView");
            m_GridView.makeItem = MakeGridItem;
            m_GridView.bindItem = BindItem;
            m_GridView.unbindItem = UnbindItem;
            m_GridView.columnCount = m_CountPerRow;
            m_GridView.selectionType = SelectionType.Multiple;
            m_GridView.itemHeight = (int)k_DefaultThumbnailSize;
            m_GridView.scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_GridView.scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            m_GridView.scrollView.verticalScroller.style.opacity = 0;
            m_DragAndDropContainer = this.Q<VisualElement>("DragAndDropContainer");
            m_DragAndDropContainer.pickingMode = PickingMode.Ignore;

            m_LoadingScreen = this.Q<VisualElement>("LoadingScreen");
            m_DataContent = this.Q<VisualElement>("DataContent");
            m_DataContent.style.display = DisplayStyle.None;

            m_HintText = this.Q<Text>("HintText");
            m_ToolTipContainer = this.Q<ExVisualElement>("ToolTipContainer");
            m_ToolTipIcon = m_ToolTipContainer.Q<Icon>("ToolTipIcon");
            m_ToolTipText = m_ToolTipContainer.Q<Text>("ToolTipText");
            m_ToolTipText.text = $"Training set requires at least {StyleTrainerConfig.config.minTrainingSetSize} and no more than {StyleTrainerConfig.config.maxTrainingSetSize} images";

            UpdateHintView();
            m_DataContent.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void UpdateToolTip()
        {
            if (m_CanModify)
            {
                var itemCount = m_GridView.itemsSource?.Count;
                if (itemCount >= StyleTrainerConfig.config.minTrainingSetSize &&
                    itemCount <= StyleTrainerConfig.config.maxTrainingSetSize)
                {
                    m_ToolTipIcon.iconName = "check";
                    m_ToolTipText.text = $"Training set good to go!";
                }
                else
                {
                    if (itemCount > StyleTrainerConfig.config.maxTrainingSetSize)
                    {
                        m_ToolTipText.text = $"There are too many training images. Remove {m_GridView.itemsSource?.Count - StyleTrainerConfig.config.maxTrainingSetSize} image(s).";
                        m_ToolTipIcon.iconName = "warning";
                    }
                    else
                    {
                        m_ToolTipText.text = $"Training set requires at least {StyleTrainerConfig.config.minTrainingSetSize - m_GridView.itemsSource?.Count} more image(s).";
                        m_ToolTipIcon.iconName = "info";
                    }
                }
            }
            else
            {
                m_ToolTipIcon.iconName = "info";
                m_ToolTipText.text = "Training set has been created. Training set is locked.";
            }
        }

        void UpdateHintView()
        {
            var itemCount = m_GridView.itemsSource?.Count;
            if (itemCount == 0)
            {
                m_GridView.style.display = DisplayStyle.None;
                m_HintText.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_GridView.style.display = DisplayStyle.Flex;
                m_HintText.style.display = DisplayStyle.None;
            }

            UpdateToolTip();
        }

        public void CanModify(bool canModify)
        {
            m_CanModify = canModify;
            UpdateToolTip();

            m_GridView.EnableInClassList("styletrainer-trainingsetview-gridview-disable", !canModify);
            m_GridView.selectionType = canModify ? SelectionType.Multiple : SelectionType.None;
            m_GridView.Refresh();
        }

        public void SelectItems(IList<int> indices)
        {
            m_GridView.SetSelectionWithoutNotify(indices);
            m_GridView.ScrollToItem(indices[0]);
        }

#if ENABLE_UXML_TRAITS
        public new class UxmlFactory : UxmlFactory<TrainingSetView, UxmlTraits> { }
#endif
    }
}