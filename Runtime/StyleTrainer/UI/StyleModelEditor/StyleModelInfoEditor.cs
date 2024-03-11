using System;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
#if ENABLE_UXML_SERIALIZED_DATA
    [UxmlElement]
#endif
    partial class StyleModelInfoEditor : ExVisualElement
    {
        public const int k_SampleOutputTab = 0;
        public const int k_TrainingSetTab = 1;
        public const float k_InitialThumbnailSliderValue = 0.4f;

        ExVisualElement m_StyleModelEditorContainer;
        ExVisualElement m_StyleModelInfoEditorContent;
        FloatingActionButton m_AddButton;
        SliderFloat m_ThumbnailSizeSlider;
        EventBus m_EventBus;
        VisualElement m_SplashScreen;
        VisualElement m_StyleLoadingScreen;
        IStyleModelEditorContent m_EditorContent;
        StyleData m_StyleData;

        internal static StyleModelInfoEditor CreateFromUxml()
        {
            var visualTree = ResourceManager.Load<VisualTreeAsset>(PackageResources.styleModelInfoEditorTemplate);
            var ve = (StyleModelInfoEditor)visualTree.CloneTree().Q("StyleModelInfoEditor");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.styleModelInfoEditorStyleSheet));
            ve.BindElements();
            return ve;
        }

        void BindElements()
        {
            name = "StyleModelInfoEditor";
            AddToClassList("appui-elevation-8");

            m_StyleModelEditorContainer = this.Q<ExVisualElement>("StyleModelEditorContainer");
            m_StyleModelInfoEditorContent = m_StyleModelEditorContainer.Q<ExVisualElement>("StyleModelInfoEditorContent");
            //var contentEditor = StyleModelInfoEditorContent.CreateFromUxml();
            var contentEditor = StyleModelInfoEditorContent.CreateFromUxml();
            m_EditorContent = contentEditor;
            m_EditorContent.NotifyCanAddChanged(CanAddChanged);
            m_StyleModelInfoEditorContent.Add(contentEditor);

            m_AddButton = this.Q<FloatingActionButton>("AddButton");
            m_AddButton.clicked += OnAddClicked;

            m_ThumbnailSizeSlider = this.Q<SliderFloat>("ThumbnailSizeSlider");
            m_ThumbnailSizeSlider.RegisterValueChangingCallback(OnThumbnailSizeSliderChanged);
            m_ThumbnailSizeSlider.value = k_InitialThumbnailSliderValue;

            m_SplashScreen = this.Q<VisualElement>("StyleModelEditorSplashScreen");
            m_StyleLoadingScreen = this.Q<VisualElement>("StyleModelEditorStyleLoadingScreen");
            UpdateMainScreen();
        }

        void CanAddChanged(bool obj)
        {
            m_AddButton.style.display = obj ? DisplayStyle.Flex : DisplayStyle.None;
            m_AddButton.SetEnabled(obj);
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
                    sampleOutput = m_StyleData.sampleOutputPrompts
                });
            }

            UpdateMainScreen();
        }

        void OnStyleDataStateChange(StyleData styleData)
        {
            UpdateMainScreen();
        }

        void UpdateMainScreen()
        {
            if (m_StyleData is not null)
            {
                m_SplashScreen.style.display = DisplayStyle.None;
                m_StyleLoadingScreen.style.display = m_StyleData.state == EState.Loading ? DisplayStyle.Flex : DisplayStyle.None;
                m_StyleModelEditorContainer.style.display = m_StyleData.state == EState.Loading ? DisplayStyle.None : DisplayStyle.Flex;
            }
            else
            {
                m_SplashScreen.style.display = DisplayStyle.Flex;
                m_StyleLoadingScreen.style.display = DisplayStyle.None;
                m_StyleModelEditorContainer.style.display = DisplayStyle.None;
            }

            m_EditorContent.UpdateView();
        }

        void OnAddClicked()
        {
            m_EditorContent.OnAddClicked();
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
            m_EditorContent.SetEventBus(eventBus);
            m_EventBus.RegisterEvent<GenerateButtonClickEvent>(OnGenerateButtonClicked);
            m_EventBus.RegisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChanged);
        }

        void OnGenerateButtonClicked(GenerateButtonClickEvent arg0)
        {
            m_AddButton.SetEnabled(false);
        }

#if ENABLE_UXML_TRAITS
        public new class UxmlFactory : UxmlFactory<StyleModelInfoEditor, UxmlTraits> { }
#endif
    }

    interface IStyleModelEditorContent
    {
        void SetEventBus(EventBus eventBus);
        void OnAddClicked();
        void UpdateView();
        void NotifyCanAddChanged(Action<bool> callback);
    }
}