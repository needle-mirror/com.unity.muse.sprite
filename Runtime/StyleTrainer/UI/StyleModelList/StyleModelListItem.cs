using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class StyleModelListItem : ExVisualElement
    {
        Text m_StyleName;
        Text m_StyleDescription;
        Checkbox m_VisibilityToggle;
        StyleData m_StyleData;
        EventBus m_EventBus;

        CircularProgress m_Progress;
        ActionButton m_DeleteButton;
        VisualElement m_StyleLoadingContainer;
        VisualElement m_ListViewContainer;

        public StyleModelListItem()
        {
            name = "StyleModelListItem";
        }

        public void Init(StyleData o, EventBus evtBus)
        {
            m_StyleData = o;
            if (m_StyleData is not null)
            {
                m_StyleName.text = o.title;
                m_StyleDescription.text = o.description;
                m_StyleData.OnStateChanged += OnStyleStateChanged;
                OnStyleStateChanged(m_StyleData);

                //todo DISPOSE
                o.OnDataChanged += OnDataChanged;
                o.OnGUIDChanged += OnDataChanged;
                m_EventBus = evtBus;
                m_EventBus.RegisterEvent<StyleTrainingEvent>(OnStyleTrainingEvent);

            }
        }

        void OnStyleStateChanged(StyleData obj)
        {
            m_StyleLoadingContainer.style.display = obj.state == EState.Loading ? DisplayStyle.Flex : DisplayStyle.None;
            m_ListViewContainer.style.display = obj.state == EState.Loading ? DisplayStyle.None : DisplayStyle.Flex;
            if (obj.state != EState.Loading)
            {
                m_StyleName.text = obj.styleTitle;
                m_StyleDescription.text = obj.styleDescription;
                m_Progress.style.display = obj.state == EState.Training ? DisplayStyle.Flex : DisplayStyle.None;

                UpdateStatusIcon();
                m_VisibilityToggle.SetValueWithoutNotify(obj.visible ? CheckboxState.Unchecked : CheckboxState.Checked);
            }
        }

        public void UnbindItem()
        {
            if (m_StyleData is not null)
            {
                m_StyleData.OnStateChanged -= OnStyleStateChanged;
                m_StyleData.OnDataChanged -= OnDataChanged;
            }

            if (m_EventBus is not null) m_EventBus.UnregisterEvent<StyleTrainingEvent>(OnStyleTrainingEvent);
        }

        void OnDeleteButtonClicked()
        {
            m_EventBus.SendEvent(new StyleDeleteButtonClickedEvent
            {
                styleData = m_StyleData
            });
        }

        void OnStyleTrainingEvent(StyleTrainingEvent arg0)
        {
            if (arg0.styleData == m_StyleData) m_Progress.style.display = arg0.state == EState.Training ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDataChanged(StyleData obj)
        {
            if (obj == m_StyleData && m_StyleData.state != EState.Loading)
            {
                m_StyleName.text = obj.styleTitle;
                m_StyleDescription.text = obj.styleDescription;
                m_VisibilityToggle.style.display = Utilities.ValidStringGUID(obj.guid) ? DisplayStyle.Flex : DisplayStyle.None;
                m_VisibilityToggle.SetValueWithoutNotify(obj.visible ? CheckboxState.Unchecked : CheckboxState.Checked);
            }
        }

        void UpdateStatusIcon()
        {
            var hasTraining = m_StyleData.state == EState.Training;
            var notNew = m_StyleData.state != EState.New;
            for (var i = 0; i < m_StyleData.checkPoints.Count; ++i)
            {
                if (hasTraining == false && m_StyleData.checkPoints[i].state == EState.Training) hasTraining = true;

                if (notNew && hasTraining)
                    break;
            }

            m_VisibilityToggle.style.display = notNew ? DisplayStyle.Flex : DisplayStyle.None;
            m_Progress.style.display = hasTraining ? DisplayStyle.Flex : DisplayStyle.None;
            m_DeleteButton.SetEnabled(!notNew);
        }

        void BindElements()
        {
            m_StyleLoadingContainer = this.Q<VisualElement>("StyleLoadingContainer");
            m_StyleLoadingContainer.style.display = DisplayStyle.None;
            m_ListViewContainer = this.Q<VisualElement>("ListViewContainer");
            m_ListViewContainer.style.display = DisplayStyle.None;

            m_Progress = this.Q<CircularProgress>("Progress");
            m_StyleName = this.Q<Text>("StyleName");
            m_StyleDescription = this.Q<Text>("StyleDescription");
            m_VisibilityToggle = this.Q<Checkbox>("VisibilityToggle");
            m_VisibilityToggle.RegisterValueChangedCallback(OnVisibilityToggleValueChanged);
            m_DeleteButton = this.Q<ActionButton>("DeleteButton");
            m_DeleteButton.clicked += OnDeleteButtonClicked;
            var d = m_VisibilityToggle.Q("appui-checkbox__checkmark");
            d.AddToClassList("appui-icon--eye-slash--regular");
            d = m_VisibilityToggle.Q("appui-checkbox__box");
            var icon = new Icon
            {
                name = "VisibilityToggleEyeIcon",
                iconName = "eye--regular"
            };
            icon.AddToClassList("styletrainer-stylemodellistitem__visibilitytoggle-icon");
            d.Add(icon);
        }

        void OnVisibilityToggleValueChanged(ChangeEvent<CheckboxState> evt)
        {
            var e = new StyleVisibilityButtonClickedEvent
            {
                styleData = m_StyleData,
                visible = evt.newValue != CheckboxState.Checked
            };
            m_EventBus.SendEvent(e);
        }

        internal static StyleModelListItem CreateFromUxml()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("Unity.Muse.StyleTrainer/uxml/StyleModelListItem");
            var ve = (StyleModelListItem)visualTree.CloneTree().Q("StyleModelListItem");
            ve.styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/StyleModelListItem"));
            ve.BindElements();
            return ve;
        }

        public new class UxmlFactory : UxmlFactory<StyleModelListItem, UxmlTraits> { }
    }
}