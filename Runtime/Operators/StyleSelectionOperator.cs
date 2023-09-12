using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using TextContent = Unity.Muse.Sprite.UIComponents.TextContent;

namespace Unity.Muse.Sprite.Operators
{
    [Preserve]
    [Serializable]
    class StyleSelectionOperator : IOperator
    {
        public const string operatorName = "Unity.Muse.Sprite.Operators.StyleSelectionOperator";
        public string OperatorName => operatorName;
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Style";

        Dropdown m_StyleSelection;

        Model m_Model;
        [SerializeField]
        OperatorData m_OperatorData;

        List<StyleData> m_Styles;

        string selectedStyle => m_OperatorData.settings[0];

        public StyleSelectionOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "Unity.Muse.Sprite", "0.0.1", new[]
                { Guid.Empty.ToString() }, false);
        }

        public string GetSelectedStyleCheckpointGuid()
        {
            var checkpointGuid = Guid.Empty.ToString();
            m_Styles ??= GetStylesFromProject();
            var style = m_Styles.FirstOrDefault(s => s.guid == selectedStyle);
            if (style != null)
            {
                var guid = style.selectedCheckPointGUID;
                if (string.IsNullOrEmpty(guid) || guid == Guid.Empty.ToString() || guid == style.guid)
                    guid = style.checkPoints.Last(c => c.state == EState.Loaded).guid;

                if (!string.IsNullOrEmpty(guid))
                    checkpointGuid = guid;
            }

            return checkpointGuid;
        }

        List<StyleData> GetStylesFromProject()
        {
            var styles = new List<StyleData>(StyleTrainerProjectData.instance.data.styles.Where(s => s.state == EState.Loaded && s.visible && s.checkPoints != null && s.checkPoints.Any(c => c.state == EState.Loaded)));
            var defaultStyles = StyleTrainerProjectData.instance.defaultStyles;
            for (int i = 0; i < defaultStyles.Count; ++i)
            {
                styles.Insert(i, defaultStyles[i]);
            }

            return styles;
        }

        public VisualElement GetCanvasView() => new();

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.name = "style-selection";
            UI.tooltip = TextContent.styleSelectionTooltip;

            var titleText = new Text();
            titleText.text = Label;
            titleText.AddToClassList("muse-node__title");
            titleText.AddToClassList("bottom-gap");
            UI.Add(titleText);

            m_StyleSelection = new Dropdown();
            var styleSelectionTitle = m_StyleSelection.Q<LocalizedTextElement>(className: Dropdown.titleUssClassName);
            styleSelectionTitle.style.overflow = Overflow.Hidden;
            styleSelectionTitle.style.textOverflow = UnityEngine.UIElements.TextOverflow.Ellipsis;
            styleSelectionTitle.style.whiteSpace = WhiteSpace.NoWrap;
            m_StyleSelection.bindItem += OnBind;
            m_StyleSelection.RegisterValueChangedCallback(OnStyleSelected);
            UI.Add(m_StyleSelection);

            UpdateWithStyleData(GetStylesFromProject());

            return UI;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            if (!Enabled())
                return null;

            var style = m_Styles?.FirstOrDefault(s => s.guid == selectedStyle);
            if (style is null)
                return null;

            return new Text {text = style.styleTitle};
        }

        void OnStyleStateChanged(StyleData obj)
        {
            UpdateWithStyleData(GetStylesFromProject());
        }

        void OnStyleProjectDataChanged(StyleTrainerData styleTrainerData)
        {
            for (int i = 0; i < StyleTrainerProjectData.instance.data.styles?.Count; i++)
            {
                StyleTrainerProjectData.instance.data.styles[i].OnStateChanged -= OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnDataChanged -= OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnStateChanged += OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnDataChanged += OnStyleStateChanged;
            }

            UpdateWithStyleData(GetStylesFromProject());
        }

        void OnBind(MenuItem item, int i)
        {
            if (i >= 0 && i < m_Styles.Count)
            {
                var style = m_Styles[i];
                item.label = style.styleTitle;
                item.tooltip = style.styleDescription;
                item.style.maxWidth = 600;
            }
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null)
                return;
            for (var i = 0; i < data.settings.Length && i < m_OperatorData.settings.Length; i++)
                m_OperatorData.settings[i] = data.settings[i];
        }

        public bool Enabled()
        {
            if ((ServerConfig.serverConfig.debugMode & ServerConfig.EDebugMode.OperatorDebug) > 0)
                return true;
            return !m_Model.isRefineMode && m_OperatorData.enabled;
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public IOperator Clone()
        {
            var result = new StyleSelectionOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model)
        {
            m_Model = model;

            StyleTrainerProjectData.instance.data.OnDataChanged += OnStyleProjectDataChanged;
            for (int i = 0; i < StyleTrainerProjectData.instance.data.styles?.Count; i++)
            {
                StyleTrainerProjectData.instance.data.styles[i].OnStateChanged += OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnDataChanged += OnStyleStateChanged;
            }
        }

        public void UnregisterFromEvents(Model model)
        {
            if (m_Model == model)
                m_Model = null;

            StyleTrainerProjectData.instance.data.OnDataChanged -= OnStyleProjectDataChanged;
            for (int i = 0; i < StyleTrainerProjectData.instance.data.styles?.Count; i++)
            {
                StyleTrainerProjectData.instance.data.styles[i].OnStateChanged -= OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnDataChanged -= OnStyleStateChanged;
            }
        }

        public bool IsSavable()
        {
            return true;
        }

        internal void UpdateWithStyleData(List<StyleData> styles)
        {
            if (m_StyleSelection != null)
            {
                m_Styles = styles;

                var titles = new List<string>(m_Styles.Select(s => s.styleTitle));
                m_StyleSelection.sourceItems = titles;

                for (var i = 0; i < m_Styles.Count; i++)
                {
                    var style = m_Styles[i];
                    if (style.guid == selectedStyle)
                    {
                        m_StyleSelection.value = i;
                        break;
                    }
                }

                if (m_StyleSelection.value >= m_Styles.Count || m_StyleSelection.value == -1 && m_Styles.Count > 0)
                    m_StyleSelection.value = 0;

                var selectedStyleValue = m_Styles[m_StyleSelection.value];
                m_StyleSelection.tooltip = selectedStyleValue.styleDescription;
                m_OperatorData.settings[0] = selectedStyleValue.guid;

                m_StyleSelection.Refresh();
            }
        }

        void OnStyleSelected(ChangeEvent<int> changeEvent)
        {
            var selectedIndex = m_StyleSelection.value;
            if (selectedIndex < 0 || selectedIndex >= m_Styles.Count)
                return;

            var selectedStyleValue = m_Styles[selectedIndex];

            m_StyleSelection.tooltip = selectedStyleValue.styleDescription;
            m_OperatorData.settings[0] = selectedStyleValue.guid;
        }
    }
}