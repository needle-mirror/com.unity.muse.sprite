using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.Sprite.Data;
using Unity.Muse.Sprite.UIComponents;
using Unity.Muse.StyleTrainer;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;
using TextContent = Unity.Muse.Sprite.UIComponents.TextContent;
using TextOverflow = Unity.AppUI.UI.TextOverflow;
using Toggle = Unity.AppUI.UI.Toggle;

namespace Unity.Muse.Sprite.Operators
{
    [Preserve]
    [Serializable]
    class SpriteGeneratorSettingsOperator : IOperator, ISerializationCallbackReceiver
    {
        public const string operatorName = "Unity.Muse.Sprite.Operators.SpriteGeneratorSettingsOperator";
        public string OperatorName => operatorName;

        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Style and Parameters";

        SizeIntField m_SizeField;
        TouchSliderFloat m_StyleStrength;
        SeedField m_SeedField;

        Model m_Model;
        [SerializeField]
        OperatorData m_OperatorData;

        enum ESettings
        {
            Subject = 0,
            NegPrompt,
            StyleStrength,
            ImageWidth,
            ImageHeight,
            Scribble,
            RemoveBackground,
            Seed,
            JobID,
            ArtifactID,
            SelectedStyleGuid,
            SeedUserSpecified,
            CheckPointUsed,
            ServerURL,
        }

        const float k_DefaultStyleStrength = 0.85f;

        bool m_Hidden;
        Dropdown m_StyleSelection;

        CircularProgress m_StyleLoading;

        static List<StyleData> s_Styles;

        public SpriteGeneratorSettingsOperator()
        {
            m_OperatorData = CreateDefaultOperatorData();
        }

        static OperatorData CreateDefaultOperatorData()
        {
            return new OperatorData(operatorName, "Unity.Muse.Sprite", "0.0.1", new[]
                { "", "", k_DefaultStyleStrength.ToString(), "512", "512", "1", "1", "", "0", "0", "0", "0", "0", "" }, false);
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("SpriteGeneratorSettings.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.name = "prompt-node";

            var titleContainer = new ExVisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    flexShrink = 1,
                }
            };
            titleContainer.AddToClassList("muse-node__title");
            titleContainer.AddToClassList("bottom-gap");
            UI.Add(titleContainer);
            var titleText = new Text { text = Label };
            titleContainer.Add(titleText);
            m_StyleLoading = new CircularProgress()
            {
                style =
                {
                    width = 16,
                    height = 16,
                    marginLeft = 5,
                    display = DisplayStyle.None
                },
                tooltip = "Loading latest default styles from server...",
                pickingMode = PickingMode.Position
            };
            titleContainer.Add(m_StyleLoading);

            m_StyleSelection = new Dropdown();
            var styleSelectionTitle = m_StyleSelection.Q<LocalizedTextElement>(className: Dropdown.titleUssClassName);
            styleSelectionTitle.style.overflow = Overflow.Hidden;
            styleSelectionTitle.style.textOverflow = UnityEngine.UIElements.TextOverflow.Ellipsis;
            styleSelectionTitle.style.whiteSpace = WhiteSpace.NoWrap;
            m_StyleSelection.bindItem += OnBindStyle;
            m_StyleSelection.RegisterValueChangedCallback(OnStyleSelected);
            m_StyleSelection.AddToClassList("bottom-gap");
            UpdateWithStyleData(GetStylesFromProject());
            UI.Add(m_StyleSelection);

            m_StyleStrength = new TouchSliderFloat();
            m_StyleStrength.name = "style-strength-slider";
            m_StyleStrength.AddToClassList("bottom-gap");
            m_StyleStrength.label = "Style Strength";
            m_StyleStrength.lowValue = 0;
            m_StyleStrength.highValue = 1;
            m_StyleStrength.RegisterValueChangedCallback(OnStyleStrengthChanged);
            m_StyleStrength.RegisterCallback<ChangingEvent<float>>(OnStyleStrengthChanging);
            m_StyleStrength.SetValueWithoutNotify(GetStyleStrengthFromOperatorData());
            UI.Add(m_StyleStrength);

            var removeBgToggle = new Toggle() { name = "remove-bg-toggle" };
            removeBgToggle.RegisterValueChangedCallback(OnRemoveBGChanged);
            removeBgToggle.SetValueWithoutNotify(GetRemoveBgFromOperatorData());
            var removeBg = new InputLabel("Remove Background");
            removeBg.inputAlignment = Align.FlexEnd;
            removeBg.labelOverflow = TextOverflow.Ellipsis;
            removeBg.Add(removeBgToggle);
            removeBg.AddToClassList("bottom-gap");
            UI.Add(removeBg);

            m_SeedField = new SeedField() { name = "seed-field" };
            m_SeedField.RegisterValueChangedCallback(OnSeedChanged);
            m_SeedField.SetValueWithoutNotify(GetSeedFromOperatorData());
            m_SeedField.userSpecified = seedUserSpecified;
            UI.Add(m_SeedField);

            if ((ServerConfig.serverConfig.debugMode & ServerConfig.EDebugMode.OperatorDebug) > 0)
            {
                var textField = new Text()
                {
                    text = $"JobID:{jobID}"
                };
                UI.Add(textField);
            }

            return UI;
        }

        void OnGetDefaultStyleModified()
        {
            UpdateWithStyleData(GetStylesFromProject());
        }

        public string GetSelectedStyleCheckpointGuid() => GetStarredCheckpointGuid(selectedStyle);

        public string GetStarredCheckpointGuid(string styleId)
        {
            var checkpointGuid = Guid.Empty.ToString();
            s_Styles ??= GetStylesFromProject();
            var style = s_Styles.FirstOrDefault(s => s.guid == styleId);
            if (style != null)
            {
                var guid = style.selectedCheckPointGUID;
                var selectedCheckPointData = style.checkPoints.FirstOrDefault(c => c.guid == guid);
                if (string.IsNullOrEmpty(guid) || guid == Guid.Empty.ToString() || guid == style.guid || selectedCheckPointData is not { state: EState.Loaded })
                    guid = style.checkPoints.Last(c => c.state == EState.Loaded).guid;

                if (!string.IsNullOrEmpty(guid))
                    checkpointGuid = guid;
            }

            return checkpointGuid;
        }

        List<StyleData> GetStylesFromProject()
        {
            var styles = new List<StyleData>(StyleTrainerProjectData.instance.data.styles
                .Where(styleData => styleData.visible && styleData.checkPoints != null && styleData.checkPoints.Any(c => c.state == EState.Loaded)));
            if (m_Model != null)
            {
                var defaultStyleData = m_Model.GetData<DefaultStyleData>();
                var defaultStyles = defaultStyleData.GetBuiltInStyle();
                if (m_StyleLoading != null)
                    m_StyleLoading.style.display = defaultStyleData.loading ? DisplayStyle.Flex : DisplayStyle.None;
                for (var i = 0; i < defaultStyles?.Count; ++i)
                    styles.Insert(i, defaultStyles[i]);
            }

            return styles;
        }

        void OnBindStyle(MenuItem visualElement, int i)
        {
            if (i >= 0 && i < s_Styles.Count)
            {
                var style = s_Styles[i];
                visualElement.label = style.styleTitle;
                visualElement.tooltip = style.styleDescription;
                visualElement.style.maxWidth = 600;
            }
        }

        internal void UpdateWithStyleData(List<StyleData> styles)
        {
            if (m_StyleSelection != null && styles?.Count > 0)
            {
                s_Styles = styles;

                var titles = new List<string>(s_Styles.Select(s => s.styleTitle));
                m_StyleSelection.sourceItems = titles;

                for (var i = 0; i < s_Styles.Count; i++)
                {
                    var style = s_Styles[i];
                    if (style.guid == selectedStyle)
                    {
                        m_StyleSelection.value = i;
                        break;
                    }
                }

                if (m_StyleSelection.value >= s_Styles.Count || m_StyleSelection.value == -1 && s_Styles.Count > 0)
                {
                    m_StyleSelection.value = 0;
                    SetSetting(ESettings.SelectedStyleGuid, s_Styles[0].guid);
                }

                var selectedStyleValue = s_Styles[m_StyleSelection.value];
                m_StyleSelection.tooltip = selectedStyleValue.styleDescription;
                SetSetting(ESettings.SelectedStyleGuid, selectedStyleValue.guid);

                m_StyleSelection.Refresh();
            }
        }

        void OnStyleSelected(ChangeEvent<int> changeEvent)
        {
            var selectedIndex = m_StyleSelection.value;
            if (selectedIndex < 0 || selectedIndex >= s_Styles.Count)
                return;

            var selectedStyleValue = s_Styles[selectedIndex];

            m_StyleSelection.tooltip = selectedStyleValue.styleDescription;
            SetSetting(ESettings.SelectedStyleGuid, selectedStyleValue.guid);
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

        void OnStyleStateChanged(StyleData obj)
        {
            UpdateWithStyleData(GetStylesFromProject());
        }

        public void RandomSeed(bool userSet = false)
        {
            var value = UnityEngine.Random.Range(0, 65535);
            if (m_SeedField != null)
            {
                m_SeedField.SetValueWithoutNotify(value);
                m_SeedField.userSpecified = userSet;
            }

            SetSetting(ESettings.Seed, value.ToString());
            SetSetting(ESettings.SeedUserSpecified, userSet ? "1" : "0");
        }

        void OnStyleStrengthChanging(ChangingEvent<float> evt)
        {
            m_StyleStrength.SetValueWithoutNotify((float)Math.Round(evt.newValue, 2));
        }

        int GetSeedFromOperatorData()
        {
            if (m_OperatorData.settings[(int)ESettings.Seed].Length == 0)
                SetSetting(ESettings.Seed, UnityEngine.Random.Range(0, 65535).ToString());
            return int.Parse(m_OperatorData.settings[(int)ESettings.Seed]);
        }

        void OnSeedChanged(ChangeEvent<int> evt)
        {
            SetSetting(ESettings.Seed, evt.newValue.ToString());
            SetSetting(ESettings.SeedUserSpecified, m_SeedField.userSpecified ? "1" : "0");
        }

        bool GetUseAsScribbleFromOperatorData()
        {
            return int.Parse(m_OperatorData.settings[(int)ESettings.Scribble]) == 1;
        }

        bool GetRemoveBgFromOperatorData()
        {
            return int.Parse(m_OperatorData.settings[(int)ESettings.RemoveBackground]) == 1;
        }

        float GetStyleStrengthFromOperatorData()
        {
            return float.Parse(m_OperatorData.settings[(int)ESettings.StyleStrength], new CultureInfo("en-US"));
        }

        void SetSetting(ESettings setting, string value)
        {
            m_OperatorData.settings[(int)setting] = value;
        }

        void OnStyleStrengthChanged(ChangeEvent<float> evt)
        {
            SetSetting(ESettings.StyleStrength, evt.newValue.ToString("N2", new CultureInfo("en-US")));
            m_StyleStrength.SetValueWithoutNotify(styleStrength);
        }

        void OnRemoveBGChanged(ChangeEvent<bool> evt)
        {
            SetSetting(ESettings.RemoveBackground, evt.newValue ? "1" : "0");
        }

        void OnScribbleChanged(ChangeEvent<bool> evt)
        {
            SetSetting(ESettings.Scribble, evt.newValue ? "1" : "0");
        }

        void SizeFieldChanged(ChangeEvent<Vector2Int> evt)
        {
            SizeFieldToOperatorData(evt.newValue, ref m_OperatorData);
        }

        static Vector2Int SizeFieldOperatorDataToVector(IReadOnlyList<string> data)
        {
            return new Vector2Int(int.Parse(data[(int)ESettings.ImageWidth]), int.Parse(data[(int)ESettings.ImageHeight]));
        }

        static void SizeFieldToOperatorData(Vector2 vector, ref OperatorData data)
        {
            data.settings[(int)ESettings.ImageWidth] = vector.x.ToString();
            data.settings[(int)ESettings.ImageHeight] = vector.y.ToString();
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
            return m_OperatorData.enabled;
        }

        public void ResetNodeList()
        {
            RandomSeed(true);
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public bool Hidden
        {
            get => m_Model.isRefineMode || m_Hidden;
            set => m_Hidden = value;
        }

        public IOperator Clone()
        {
            var result = new SpriteGeneratorSettingsOperator();
            result.UpdateWithStyleData(s_Styles);
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model)
        {
            if (s_Styles != null)
                UpdateWithStyleData(s_Styles);
            m_Model = model;

            StyleTrainerProjectData.instance.data.OnDataChanged += OnStyleProjectDataChanged;
            for (int i = 0; i < StyleTrainerProjectData.instance.data.styles?.Count; i++)
            {
                StyleTrainerProjectData.instance.data.styles[i].OnStateChanged += OnStyleStateChanged;
                StyleTrainerProjectData.instance.data.styles[i].OnDataChanged += OnStyleStateChanged;
            }

            model.GetData<DefaultStyleData>().OnModified -= OnGetDefaultStyleModified;
            model.GetData<DefaultStyleData>().OnModified += OnGetDefaultStyleModified;
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

            model.GetData<DefaultStyleData>().OnModified -= OnGetDefaultStyleModified;
        }

        public bool IsSavable()
        {
            return true;
        }

        public string selectedStyle => m_OperatorData.settings[(int)ESettings.SelectedStyleGuid];
        public string subject => m_OperatorData.settings[(int)ESettings.Subject];
        public Vector2Int imageSize => new Vector2Int(int.Parse(m_OperatorData.settings[(int)ESettings.ImageWidth]), int.Parse(m_OperatorData.settings[(int)ESettings.ImageHeight]));
        public bool scribble => m_OperatorData.settings[(int)ESettings.Scribble] == "1";
        public bool removeBackground => m_OperatorData.settings[(int)ESettings.RemoveBackground] == "1";
        public float styleStrength => float.Parse(m_OperatorData.settings[(int)ESettings.StyleStrength], new CultureInfo("en-US"));
        public int seed => m_OperatorData.settings[(int)ESettings.Seed] == "" ? 0 : int.Parse(m_OperatorData.settings[(int)ESettings.Seed]);

        public bool seedUserSpecified
        {
            get => m_OperatorData.settings[(int)ESettings.SeedUserSpecified] == "1";
            set => SetSetting(ESettings.SeedUserSpecified, value ? "1" : "0");
        }

        public Texture2D image => null;

        public string jobID
        {
            get => m_OperatorData.settings[(int)ESettings.JobID];
            set => m_OperatorData.settings[(int)ESettings.JobID] = value;
        }

        public string artifactID
        {
            get => m_OperatorData.settings[(int)ESettings.ArtifactID];
            set => m_OperatorData.settings[(int)ESettings.ArtifactID] = value;
        }

        public string checkPointUsed
        {
            get => m_OperatorData.settings[(int)ESettings.CheckPointUsed];
            set => m_OperatorData.settings[(int)ESettings.CheckPointUsed] = value;
        }

        public void SetSeed(int seed)
        {
            SetSetting(ESettings.Seed, seed.ToString());
        }

        public void InitFromJobInfo(JobInfoResponse jobInfoResponse)
        {
            jobID = jobInfoResponse.guid;
            SetSeed(jobInfoResponse.request.settings.seed);
            artifactID = jobInfoResponse.request.inputGuid;
            SetStyleFromCheckpointId(jobInfoResponse.request.checkpoint_id);
            SetSetting(ESettings.Scribble, jobInfoResponse.request.scribble ? "1" : "0");
            SetSetting(ESettings.RemoveBackground, jobInfoResponse.request.removeBackground ? "1" : "0");
            SetSetting(ESettings.StyleStrength, jobInfoResponse.request.styleStrength.ToString("N2"));
        }

        void SetStyleFromCheckpointId(string checkPointId)
        {
            UpdateWithStyleData(s_Styles);

            foreach (var style in s_Styles)
            {
                foreach (var checkPoint in style.checkPoints)
                {
                    if (checkPoint.guid == checkPointId)
                    {
                        SetSetting(ESettings.SelectedStyleGuid, style.guid);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var result = new VisualElement();
            var styleDescription = $"{TextContent.style}: {s_Styles.FirstOrDefault(s => s.guid == selectedStyle)?.title ?? TextContent.styleUndefined}";
            var styleStrengthDescription = $"{TextContent.styleStrength}: {styleStrength}";
            var removeBackgroundDescription = $"{TextContent.removeBackground}: {removeBackground}";
            var seedDescription = seedUserSpecified ? $"{TextContent.customSeed}: {seed:00000}." : $"{TextContent.randomSeed}";
            result.Add(new Text { text = styleDescription });
            result.Add(new Text { text = styleStrengthDescription });
            result.Add(new Text { text = removeBackgroundDescription });
            result.Add(new Text { text = seedDescription });
            return result;
        }

        public void OnBeforeSerialize()
        {
            // not needed
        }

        public void OnAfterDeserialize()
        {
            var newOperator = CreateDefaultOperatorData();
            if (newOperator.settings.Length != m_OperatorData.settings.Length)
            {
                for (var i = 0; i < newOperator.settings.Length && i < m_OperatorData.settings.Length; i++)
                    newOperator.settings[i] = m_OperatorData.settings[i];
                m_OperatorData = newOperator;
            }
        }
    }
}