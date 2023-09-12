using System;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.StyleTrainer
{
    class StyleTrainerMainUI : ExVisualElement, IDisposable
    {
        StyleModelInfoEditor m_StyleModelInfoEditor;
        StyleModelList m_StyleModelList;
        EventBus m_EventBus;
        SplitView m_SplitView;
        Button m_LoginButton;
        VisualElement m_NoAssetContainer;
        VisualElement m_LoginScreen;
        VisualElement m_LoadingScreen;
        Text m_LoadingScreenText;

        public StyleTrainerMainUI()
        {
            name = "StyleTrainerMainUI";
        }

        public void SetEventBus(EventBus eventBus)
        {
            m_EventBus = eventBus;
            m_EventBus.RegisterEvent<SignInEvent>(OnSignInEvent);
            m_EventBus.RegisterEvent<ShowDialogEvent>(OnShowDialogEvent);
            m_EventBus.RegisterEvent<ShowLoadingScreenEvent>(OnShowLoadingScreenEvent);
            m_StyleModelInfoEditor.SetEventBus(eventBus);
            m_StyleModelList.SetEventBus(eventBus);
        }

        void OnShowDialogEvent(ShowDialogEvent dialogData)
        {
            var dialog = new AlertDialog
            {
                title = dialogData.title,
                description = dialogData.description,
                variant = AlertSemantic.Destructive
            };
            dialog.SetPrimaryAction(1, "Ok", dialogData.confirmAction);
            var modal = Modal.Build(this, dialog);
            modal.Show();
        }

        void OnShowLoadingScreenEvent(ShowLoadingScreenEvent dialogData)
        {
            if (dialogData.show)
            {
                m_LoadingScreen.style.display = DisplayStyle.Flex;
                m_LoadingScreenText.text = dialogData.description;
            }
            else
            {
                m_LoadingScreen.style.display = DisplayStyle.None;
            }
        }

        void OnSignInEvent(SignInEvent evt)
        {
            RefreshLoggedIn();
        }

        internal static StyleTrainerMainUI CreateFromUxml(StyleModelController controller, VisualElement cloneToElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/StyleTrainerMainUI");
            visualTree.CloneTree(cloneToElement);
            var styleTrainerMainUI = cloneToElement.Q<StyleTrainerMainUI>();
            styleTrainerMainUI.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/StyleTrainerMainUI"));
            styleTrainerMainUI.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/Icons"));
            styleTrainerMainUI.BindElements();
            controller.InitView(styleTrainerMainUI);
            return styleTrainerMainUI;
        }

        void BindElements()
        {
            m_LoadingScreen = this.Q<VisualElement>("LoadingScreen");
            m_LoadingScreenText = m_LoadingScreen.Q<Text>("LoadingText");
            m_LoadingScreenText.text = "Loading...";
            m_LoadingScreen.style.display = DisplayStyle.None;

            m_SplitView = this.Q<SplitView>("StyleTrainerUISplitView");
            m_SplitView.fixedPaneIndex = 0;
            m_SplitView.fixedPaneInitialDimension = 300;
            m_SplitView.orientation = TwoPaneSplitViewOrientation.Horizontal;

            m_StyleModelInfoEditor = StyleModelInfoEditor.CreateFromUxml();
            m_StyleModelList = StyleModelList.CreateFromUxml();
            m_SplitView.Add(m_StyleModelList);
            m_SplitView.Add(m_StyleModelInfoEditor);

            m_LoginScreen = this.Q<VisualElement>("LoginScreen");
            m_LoginButton = m_LoginScreen.Q<Button>("LoginButton");
            m_LoginButton.clicked += LogInClicked;

            m_NoAssetContainer = this.Q<VisualElement>("NoAssetContainer");
            m_NoAssetContainer.style.display = DisplayStyle.None;

            RefreshLoggedIn();
        }

        void RefreshLoggedIn()
        {
#if UNITY_EDITOR
            var isLoggedIn = UnityConnectUtils.GetIsLoggedIn();
#else
            var isLoggedIn = true;
#endif
            m_LoginScreen.style.display = isLoggedIn ? DisplayStyle.None : DisplayStyle.Flex;
            m_SplitView.style.display = isLoggedIn ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static void LogInClicked()
        {
#if UNITY_EDITOR
            UnityConnectProxy.instance.ShowLogin();
#endif
        }

        public new class UxmlFactory : UxmlFactory<StyleTrainerMainUI, UxmlTraits> { }

        public void Dispose()
        {
            // not used for now
        }
    }
}