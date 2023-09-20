using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.DebugConfig;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerProjectEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class StyleModelList : ExVisualElement
    {
        ListView m_ListView;
        ActionButton m_AddStyleButton;
        EventBus m_EventBus;
        ContextualMenuManipulator m_DebugMenu;

        void BindElements()
        {
            m_AddStyleButton = this.Q<ActionButton>("AddStyleButton");
            m_AddStyleButton.clicked += OnAddStyleClicked;
            if (DebugConfig.developerMode) m_DebugMenu = new ContextualMenuManipulator(DebugMenuBuilder);
            this.AddManipulator(m_DebugMenu);
            SetupListView();
        }

        void DebugMenuBuilder(ContextualMenuPopulateEvent obj)
        {
            obj.menu.AppendAction("DEVELOPER: Load Styles", LoadStyles, DropdownMenuAction.AlwaysEnabled);
        }

        void LoadStyles(DropdownMenuAction obj)
        {
            m_EventBus.SendEvent(new LoadStyleProjectEvent());
        }

        void SetupListView()
        {
            m_ListView = this.Q<ListView>("StyleModelListView");
            m_ListView.reorderable = false;
            m_ListView.makeItem = MakeItem;
            m_ListView.itemsSource = new List<StyleModelInfo>();
            m_ListView.bindItem = (element, i) =>
            {
                var item = (StyleModelListItem)element;
                item.Init(m_ListView.itemsSource[i] as StyleData, m_EventBus);
            };
            m_ListView.unbindItem = (element, _) =>
            {
                var item = (StyleModelListItem)element;
                item.UnbindItem();
            };
            m_ListView.selectionChanged += OnSelectionChanged;
        }

        void OnSelectionChanged(IEnumerable<object> obj)
        {
            var evt = new StyleModelListSelectionChangedEvent();
            evt.styleData = m_ListView.selectedItem as StyleData;
            m_EventBus.SendEvent(evt);
        }

        static VisualElement MakeItem()
        {
            return StyleModelListItem.CreateFromUxml();
        }

        void OnAddStyleClicked()
        {
            m_EventBus.SendEvent(new AddStyleButtonClickedEvent());
        }

        public void SetEventBus(EventBus eventBus)
        {
            m_EventBus = eventBus;
            m_EventBus.RegisterEvent<StyleModelSourceChangedEvent>(ModelSourcedChanged);
        }

        void ModelSourcedChanged(StyleModelSourceChangedEvent arg0)
        {
            m_ListView.itemsSource = (IList)arg0.styleModels;
            m_ListView.RefreshItems();
            if (arg0.selectedIndex < arg0.styleModels.Count && arg0.selectedIndex >= 0)
            {
                m_ListView.SetSelectionWithoutNotify(new[] { arg0.selectedIndex });
                m_ListView.ScrollToItem(arg0.selectedIndex);
                m_ListView.Focus();
            }

            var evt = new StyleModelListSelectionChangedEvent();
            evt.styleData = m_ListView.selectedItem as StyleData;
            m_EventBus.SendEvent(evt);
        }

        internal static StyleModelList CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/StyleModelList");
            var ve = (StyleModelList)visualTree.CloneTree().Q("StyleModelList");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/StyleModelList"));
            ve.BindElements();
            return ve;
        }

        public new class UxmlFactory : UxmlFactory<StyleModelList, UxmlTraits> { }
    }
}