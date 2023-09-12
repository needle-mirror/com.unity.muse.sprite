using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.Sprite.UIComponents;
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
    class SpriteGeneratorSettingsOperator : IOperator
    {
        public const string operatorName = "Unity.Muse.Sprite.Operators.SpriteGeneratorSettingsOperator";
        public string OperatorName => operatorName;
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Parameters";

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
            CheckPointID,
            SeedUserSpecified
        }

        const float k_DefaultStyleStrength = 0.85f;

        public SpriteGeneratorSettingsOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "Unity.Muse.Sprite","0.0.1",  new []
                { "", "", k_DefaultStyleStrength.ToString(), "512", "512", "1", "1", "", "0", "0", "0", "0" }, false);
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

            var text = new Text();
            text.text = Label;
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");

            UI.Add(text);

            //  var sizeField = new SizeIntField();
            //  sizeField.minValue = new Vector2Int(64, 64);
            //  sizeField.maxValue = new Vector2Int(512, 512);
            //  sizeField.name = "size-field";
            //  sizeField.AddToClassList("bottom-gap");
            //  sizeField.SetValueWithoutNotify(SizeFieldOperatorDataToVector(m_OperatorData.settings));
            //  sizeField.RegisterValueChangedCallback(SizeFieldChanged);
            // UI.Add(sizeField);

            var removeBgToggle = new Toggle(){name = "remove-bg-toggle"};
            removeBgToggle.RegisterValueChangedCallback(OnRemoveBGChanged);
            removeBgToggle.SetValueWithoutNotify(GetRemoveBgFromOperatorData());
            var removeBg = new InputLabel("Remove Background");
            removeBg.inputAlignment = Align.FlexEnd;
            removeBg.labelOverflow = TextOverflow.Ellipsis;
            removeBg.Add(removeBgToggle);
            removeBg.AddToClassList("bottom-gap");
            UI.Add(removeBg);

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



            // var useAsScribble = new InputLabel("Use as Scribble");
            // useAsScribble.inputAlignment = Align.FlexEnd;
            // var useAsScribbleToggle = new Toggle() {name = "use-as-scribble-toggle"};
            // useAsScribbleToggle.AddToClassList("bottom-gap");
            // useAsScribbleToggle.RegisterValueChangedCallback(OnScribbleChanged);
            // useAsScribbleToggle.SetValueWithoutNotify(GetUseAsScribbleFromOperatorData());
            // useAsScribble.Add(useAsScribbleToggle);
            // UI.Add(useAsScribble);

            m_SeedField = new SeedField(){name = "seed-field"};
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

        public void RandomSeed(bool userSet = false)
        {
            var value = UnityEngine.Random.Range(0, 65535);
            if (m_SeedField != null)
            {
                m_SeedField.SetValueWithoutNotify(value);
                m_SeedField.userSpecified = userSet;
            }

            SetSetting(ESettings.Seed, value.ToString());
            SetSetting(ESettings.SeedUserSpecified, userSet? "1" : "0");
        }

        void OnStyleStrengthChanging(ChangingEvent<float> evt)
        {
            m_StyleStrength.SetValueWithoutNotify((float)Math.Round(evt.newValue, 2));
        }

        int GetSeedFromOperatorData()
        {
            if(m_OperatorData.settings[(int)ESettings.Seed].Length == 0)
                m_OperatorData.settings[(int)ESettings.Seed] = UnityEngine.Random.Range(0, 65535).ToString();
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
            for(var i = 0; i < data.settings.Length && i < m_OperatorData.settings.Length; i++)
                m_OperatorData.settings[i] = data.settings[i];
        }

        public bool Enabled()
        {
            if ((ServerConfig.serverConfig.debugMode & ServerConfig.EDebugMode.OperatorDebug) > 0)
                return true;
            return m_Model.isRefineMode ? false : m_OperatorData.enabled;
        }

        public void ResetNodeList()
        {
            RandomSeed(true);
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public IOperator Clone()
        {
            var result = new SpriteGeneratorSettingsOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model)
        {
            m_Model = model;
        }

        public void UnregisterFromEvents(Model model)
        {
            if(m_Model = model)
                m_Model = null;
        }

        public bool IsSavable()
        {
            return true;
        }


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

        public string checkPointID
        {
            get => m_OperatorData.settings[(int)ESettings.CheckPointID];
            set => m_OperatorData.settings[(int)ESettings.CheckPointID] = value;
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
            SetSetting(ESettings.Scribble, jobInfoResponse.request.scribble ? "1" : "0");
            SetSetting(ESettings.RemoveBackground, jobInfoResponse.request.removeBackground ? "1" : "0");
            SetSetting(ESettings.StyleStrength, jobInfoResponse.request.styleStrength.ToString("N2"));
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var description = seedUserSpecified ? $"{TextContent.customSeed}: {seed:00000}." : $"{TextContent.randomSeed}.";
            return new Text { text = description };
        }
    }
}
