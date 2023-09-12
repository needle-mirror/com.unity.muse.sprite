using System;
using System.Collections.Generic;
using System.IO;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetUIEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class StyleModelInfoEditor : ExVisualElement
    {
        public const int k_SampleOutputTab = 0;
        public const int k_TrainingSetTab = 1;

        struct TabData
        {
            public string tabName;
            public IStyleModelInfoTabView view;
            public Action<EventBus, StyleData> sendAddEvent;
            public Func<bool> CanModify;
        }

        readonly TabData[] m_TabData;
        StyleData m_StyleData;
        VisualElement m_SplashScreen;
        VisualElement m_StyleEditorContainer;
        VisualElement m_TabContainer;
        Tabs m_Tabs;
        StyleModelInfo m_StyleModelInfo;
        SliderFloat m_ThumbnailSizeSlider;
        EventBus m_EventBus;
        ActionButton m_AddButton;
        VisualElement m_StyleLoadingScreen;

        public StyleModelInfoEditor()
        {
            m_TabData = new[]
            {
                new TabData
                {
                    tabName = StringConstants.sampleOutputTab,
                    view = new SampleOutputView(),
                    CanModify = () => !Utilities.ValidStringGUID(m_StyleData?.guid) && m_StyleData?.state == EState.New,
                    sendAddEvent = (evtBus, styleData) =>
                    {
                        evtBus.SendEvent(new AddSampleOutputEvent
                        {
                            styleData = styleData
                        });
                    }
                },
                new TabData
                {
                    tabName = StringConstants.trainingSetTab,
                    view = CreateTrainingSetView(),
                    CanModify = () => !Utilities.ValidStringGUID(m_StyleData?.GetSelectedCheckPoint()?.trainingSetData?.guid) && m_StyleData?.state != EState.Training,
                    sendAddEvent = (evtBus, styleData) =>
                    {
#if UNITY_EDITOR
                        var file = EditorUtilities.OpenFile("Add Training Set", "Assets", "png");
                        if (!string.IsNullOrEmpty(file))
                        {
                            var texture = new Texture2D(2, 2);
                            ImageConversion.LoadImage(texture, File.ReadAllBytes(file));
                            texture.hideFlags = HideFlags.HideAndDontSave;
                            texture.name = $"AddFileTexture-{file}";
                            evtBus.SendEvent(new AddTrainingSetEvent()
                            {
                                styleData = styleData,
                                textures = new Texture2D[] { texture }
                            });
                        }
#endif
                    }
                }
            };
        }

        IStyleModelInfoTabView CreateTrainingSetView()
        {
            var t = TrainingSetView.CreateFromUxml();
            t.OnDragAndDrop = TrainingDataDragAndDrop;
            return t;
        }

        void OnAddClicked()
        {
            m_TabData[m_Tabs.value].sendAddEvent(m_EventBus, m_StyleData);
        }

        void OnTabValueChange(ChangeEvent<int> evt)
        {
            UpdateTabComponent();
        }

        void UpdateTabComponent()
        {
            for (var i = 0; i < m_TabData.Length; i++)
                if (m_Tabs.value == i)
                {
                    m_TabData[i].view.GetView().style.display = DisplayStyle.Flex;
                    m_AddButton?.SetEnabled(m_TabData[i].CanModify());
                    m_TabData[i].view.CanModify(m_TabData[i].CanModify());
                    m_TabData[i].view.OnViewActivated(m_ThumbnailSizeSlider.value);
                }
                else
                {
                    m_TabData[i].view.GetView().style.display = DisplayStyle.None;
                }
        }

        void OnThumbnailSizeSliderChanged(ChangingEvent<float> evt)
        {
            m_EventBus.SendEvent(new ThumbnailSizeChangedEvent
            {
                thumbnailSize = evt.newValue
            });
        }

        public void SetEventBus(EventBus eventBus)
        {
            m_EventBus = eventBus;
            for (var i = 0; i < m_TabData.Length; ++i) m_TabData[i].view.SetEventBus(eventBus);
            m_EventBus.RegisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChanged);
            m_EventBus.RegisterEvent<CheckPointSelectionChangeEvent>(OnCheckPointSelectionChanged);
            m_EventBus.RegisterEvent<GenerateButtonClickEvent>(OnGenerateButtonClicked);
            m_EventBus.RegisterEvent<StyleTrainingEvent>(OnStyleTrainingEvent);
            m_EventBus.RegisterEvent<RequestChangeTabEvent>(OnRequestChangeTabEvent);

            m_StyleModelInfo.SetEventBus(eventBus);
        }

        void OnRequestChangeTabEvent(RequestChangeTabEvent arg0)
        {
            if (arg0.tabIndex >= 0 && arg0.tabIndex < m_TabData.Length)
            {
                m_Tabs.value = arg0.tabIndex;
                if (arg0.highlightIndices != null)
                    m_TabData[arg0.tabIndex].view.SelectItems(arg0.highlightIndices);
            }
        }

        void OnStyleTrainingEvent(StyleTrainingEvent arg0)
        {
            UpdateTabComponent();
        }

        void OnGenerateButtonClicked(GenerateButtonClickEvent arg0)
        {
            m_AddButton.SetEnabled(false);
            for (var i = 0; i < m_TabData.Length; i++) m_TabData[i].view.CanModify(false);
        }

        void OnCheckPointSelectionChanged(CheckPointSelectionChangeEvent arg0)
        {
            UpdateTabComponent();
        }

        void OnStyleModelListSelectionChanged(StyleModelListSelectionChangedEvent arg0)
        {
            if (m_StyleData != null)
                m_StyleData.OnStateChanged -= OnStyleDataStateChange;
            m_StyleData = arg0.styleData;
            if (m_StyleData != null)
                m_StyleData.OnStateChanged += OnStyleDataStateChange;
            if (m_StyleData is not null)
            {
                m_EventBus.SendEvent(new TrainingSetDataSourceChangedEvent
                {
                    styleData = m_StyleData,
                    trainingSetData = m_StyleData.trainingSetData
                });
                m_EventBus.SendEvent(new SampleOutputDataSourceChangedEvent
                {
                    styleData = m_StyleData,
                    sampleOutput = m_StyleData.sampleOutputData
                });
            }

            UpdateMainScreen();
        }

        void OnStyleDataStateChange(StyleData styleData)
        {
            UpdateMainScreen();
        }

        internal static StyleModelInfoEditor CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/StyleModelInfoEditor");
            var ve = (StyleModelInfoEditor)visualTree.CloneTree().Q("StyleModelInfoEditor");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/StyleModelInfoEditor"));
            ve.BindElements();
            return ve;
        }

        void UpdateMainScreen()
        {
            m_SplashScreen.style.display = m_StyleData is not null ? DisplayStyle.None : DisplayStyle.Flex;
            if (m_StyleData is not null)
            {
                m_StyleLoadingScreen.style.display = m_StyleData.state == EState.Loading ? DisplayStyle.Flex : DisplayStyle.None;
                m_StyleEditorContainer.style.display = m_StyleData.state == EState.Loading ? DisplayStyle.None : DisplayStyle.Flex;
            }
            else
            {
                m_StyleLoadingScreen.style.display = DisplayStyle.None;
                m_StyleEditorContainer.style.display = DisplayStyle.None;
            }

            UpdateTabComponent();
        }

        void TrainingDataDragAndDrop(IList<Texture2D> textures)
        {
            var textureArray = new Texture2D[textures.Count];
            textures.CopyTo(textureArray, 0);
            m_EventBus.SendEvent(new AddTrainingSetEvent
            {
                styleData = m_StyleData,
                textures = textureArray
            });
        }

        void BindElements()
        {
            name = "StyleModelEditor";

            AddToClassList("appui-elevation-8");

            m_StyleModelInfo = this.Q<StyleModelInfo>();
            m_TabContainer = this.Q<VisualElement>("StyleTabContainer");
            m_Tabs = m_TabContainer.Q<Tabs>("DataTabs");
            m_Tabs.Add(new TabItem { name = "StyleTab", label = "Sample Output" });
            m_Tabs.Add(new TabItem { name = "StyleTab", label = "Training Set" });
            m_Tabs.RegisterValueChangedCallback(OnTabValueChange);
            var tabContent = m_TabContainer.Q<VisualElement>("TabContent");
            tabContent.Add(m_TabData[0].view.GetView());
            tabContent.Add(m_TabData[1].view.GetView());

            m_AddButton = this.Q<ActionButton>("AddButton");
            m_AddButton.clicked += OnAddClicked;

            m_ThumbnailSizeSlider = this.Q<SliderFloat>("ThumbnailSizeSlider");
            m_ThumbnailSizeSlider.RegisterValueChangingCallback(OnThumbnailSizeSliderChanged);
            m_ThumbnailSizeSlider.value = 1;

            m_SplashScreen = this.Q<VisualElement>("StyleModelEditorSplashScreen");
            m_StyleEditorContainer = this.Q<VisualElement>("StyleModelEditorContainer");
            m_StyleLoadingScreen = this.Q<VisualElement>("StyleModelEditorStyleLoadingScreen");
            UpdateMainScreen();
        }

        public new class UxmlFactory : UxmlFactory<StyleModelInfoEditor, UxmlTraits> { }
    }

    interface IStyleModelInfoTabView
    {
        VisualElement GetView();
        void SetEventBus(EventBus evtBus);
        void CanModify(bool canModify);
        void OnViewActivated(float thumbNailSize);
        void SelectItems(IList<int> indices);
    }
}