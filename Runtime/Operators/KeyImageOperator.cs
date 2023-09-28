using System;
using System.Globalization;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.Sprite.UIComponents;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
using TextContent = Unity.Muse.Sprite.UIComponents.TextContent;

namespace Unity.Muse.Sprite.Operators
{
    [Preserve]
    [Serializable]
    class KeyImageOperator : IOperator
    {
        [SerializeField]
        OperatorData m_OperatorData;

        Model m_Model;
        Image m_ReferenceImage;
        ActionButton m_BrushButton;
        ActionButton m_EraserButton;
        Button m_DeleteButton;
#if UNITY_EDITOR
        SpritePicker m_ScenePicker;
        ActionButton m_PickerButton;
#endif
        SpriteTextureDropManipulator m_SpriteTextureDropManipulator;
        DoodlePadManipulator m_DoodlePadManipulator;

        VisualElement m_ImageContainer;
        TouchSliderFloat m_MaskTightness;
        Text m_HintLabel;

        readonly Vector2Int k_DefaultDoodleSize = new Vector2Int(512, 512);
        MuseShortcut[] m_Shortcuts;

        const int k_BrushSizeStep = 2;

        /// <summary>
        /// Base USS class name.
        /// </summary>
        public const string baseUssClassName = "appui-sprite-keyimage";
        /// <summary>
        /// Container of the image class name.
        /// </summary>
        public const string imageContainerClassName = baseUssClassName + "__image-container";
        /// <summary>
        /// Hint label class name.
        /// </summary>
        public const string hintUssClassName = baseUssClassName + "__hint";
        /// <summary>
        /// Main container for control buttons class name.
        /// </summary>
        public const string controlButtonsContainerClassName = baseUssClassName + "__control-buttons-container";
        /// <summary>
        /// Left container for control buttons class name.
        /// </summary>
        public const string controlButtonsContainerLeftClassName = baseUssClassName + "__control-buttons-container-left";
        /// <summary>
        /// Right container for control buttons class name.
        /// </summary>
        public const string controlButtonsContainerRightClassName = baseUssClassName + "__control-buttons-container-right";
        /// <summary>
        /// Reference image class name.
        /// </summary>
        public const string referenceImageClassName = baseUssClassName + "__reference-image";
        /// <summary>
        /// Accept dragged elements class name.
        /// </summary>
        public const string acceptDragClassName = baseUssClassName + "__accept-drag";

        enum ESettings
        {
            Doodle,
            ReferenceImage,
            IsClear,
            DoodleSizeX,
            DoodleSizeY,
            MaskStrength
        }

        public KeyImageOperator()
        {
            var type = GetType();
            m_OperatorData = new OperatorData(type.ToString(), type.Assembly.GetName().Name, "0.0.1",
                new[] { "", "", true.ToString(), k_DefaultDoodleSize.x.ToString(), k_DefaultDoodleSize.y.ToString(), "0.5" }, true);
        }

        public const string operatorName = "Unity.Muse.Sprite.Operators.KeyImageOperator";
        public string OperatorName => operatorName;

        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Input Image";

        public bool Enabled() => m_OperatorData.enabled;

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        bool m_Hidden;

        public bool Hidden
        {
            get => m_Model.isRefineMode || m_Hidden;
            set => m_Hidden = value;
        }

        public VisualElement GetCanvasView()
        {
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList(baseUssClassName);
            UI.AddToClassList("muse-node");
            UI.AddToClassList("appui-elevation-8");
            UI.styleSheets.Add(Resources.Load<StyleSheet>("UI/KeyImage"));
            UI.name = "key-image-node";

            var titleText = new Text();
            titleText.text = "Input Image";
            titleText.AddToClassList("muse-node__title");
            titleText.AddToClassList("bottom-gap");
            UI.Add(titleText);

            var controlButtons = new VisualElement();
            controlButtons.AddToClassList(controlButtonsContainerClassName);
            controlButtons.AddToClassList("bottom-gap");
            UI.Add(controlButtons);

            var controlButtonsLeft = new ActionGroup { compact = true };
            controlButtonsLeft.AddToClassList(controlButtonsContainerLeftClassName);
            controlButtons.Add(controlButtonsLeft);

            m_BrushButton = new ActionButton(ToggleBrush) { icon = "paint-brush", accent = true, tooltip = TextContent.doodleBrushTooltip };
            m_EraserButton = new ActionButton(ToggleEraser) { icon = "eraser", accent = true, tooltip = TextContent.doodleEraserTooltip };
            controlButtonsLeft.Add(m_BrushButton);
            controlButtonsLeft.Add(m_EraserButton);

#if UNITY_EDITOR
            m_ScenePicker = new SpritePicker();
            m_ScenePicker.onSelectedObject += OnScenePickerSelectedObject;
            m_ScenePicker.onPickStart += UpdateVisibility;
            m_ScenePicker.onPickEnd += UpdateVisibility;
            m_PickerButton = new ActionButton(ToggleEditorSelection) { icon = "color-picker", accent = true, tooltip = TextContent.doodleSelectorTooltip };
            controlButtonsLeft.Add(m_PickerButton);
#endif
            var controlButtonsRight = new VisualElement();
            controlButtonsRight.AddToClassList(controlButtonsContainerRightClassName);
            controlButtons.Add(controlButtonsRight);

            m_DeleteButton = new Button(OnDeleteClicked) { leadingIcon = "x", tooltip = TextContent.doodleClearTooltip };
            controlButtonsRight.Add(m_DeleteButton);

            m_ImageContainer = new VisualElement();
            m_ImageContainer.AddToClassList(imageContainerClassName);
            m_ImageContainer.RegisterCallback<GeometryChangedEvent>(evt => m_ImageContainer.style.height = evt.newRect.width);
            m_ImageContainer.AddToClassList("bottom-gap");
            UI.Add(m_ImageContainer);

            m_HintLabel = new Text(TextContent.doodleStartTooltip);
            m_HintLabel.AddToClassList(hintUssClassName);
            m_HintLabel.size = TextSize.XS;
            m_ImageContainer.Add(m_HintLabel);

            m_ReferenceImage = new Image { pickingMode = PickingMode.Ignore };
            m_ReferenceImage.AddToClassList(referenceImageClassName);
            m_ImageContainer.Add(m_ReferenceImage);
            m_ReferenceImage.StretchToParentSize();
            var refImageRaw = GetReferenceImage();
            if (!string.IsNullOrEmpty(refImageRaw))
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(Convert.FromBase64String(refImageRaw));
                t.Apply();

                // var s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect);
                m_ReferenceImage.image = t;
            }

            m_DoodlePadManipulator = new DoodlePadManipulator(doodleSize);
            m_DoodlePadManipulator.onModifierStateChanged += OnDoodleModifierChanged;
            m_DoodlePadManipulator.onDoodleUpdate += UpdateVisibility;
            m_ImageContainer.AddManipulator(m_DoodlePadManipulator);
            m_DoodlePadManipulator.SetValueWithoutNotify(Convert.FromBase64String(GetDoodle()));
            m_DoodlePadManipulator.isClear = bool.Parse(m_OperatorData.settings[(int)ESettings.IsClear]);
            m_DoodlePadManipulator.onValueChanged += OnDoodleChanged;
            UI.focusable = true;

            m_SpriteTextureDropManipulator = new SpriteTextureDropManipulator(model);
            m_SpriteTextureDropManipulator.onDrop += SetReferenceImage;
            m_SpriteTextureDropManipulator.onDragStart += () => UI.EnableInClassList(acceptDragClassName, true);
            m_SpriteTextureDropManipulator.onDragEnd += () => UI.EnableInClassList(acceptDragClassName, false);
            m_ImageContainer.AddManipulator(m_SpriteTextureDropManipulator);

            m_MaskTightness = new TouchSliderFloat { tooltip = TextContent.operatorTightnessTooltip };
            m_MaskTightness.name = "mask-tightness-slider";
            m_MaskTightness.AddToClassList("bottom-gap");
            m_MaskTightness.label = "Tightness";
            m_MaskTightness.lowValue = 0;
            m_MaskTightness.highValue = 1;
            m_MaskTightness.SetValueWithoutNotify(GetMaskStrengthFromOperatorData());
            m_MaskTightness.RegisterCallback<ChangingEvent<float>>(OnMaskTightnessChanging);
            m_MaskTightness.RegisterValueChangedCallback(OnMaskTightnessChanged);
            UI.Add(m_MaskTightness);

            UI.RegisterCallback<AttachToPanelEvent>(OnAttach);
            UI.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            UpdateVisibility();
            return UI;
        }

        internal bool HasReference() => string.IsNullOrEmpty(GetReferenceImage());

        internal bool HasDoodle() => m_DoodlePadManipulator != null && !m_DoodlePadManipulator.isClear;

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var result = new VisualElement();

            var referenceImage = GetTexture(ESettings.ReferenceImage);
            if (referenceImage != null)
                result.Add(new Image {image = referenceImage});

            var doodleImage = GetTexture(ESettings.Doodle);
            if (doodleImage != null)
                result.Add(new Image {image = doodleImage});

            if (result.childCount == 0)
                return null;

            var tightness = GetMaskStrengthFromOperatorData();
            result.Add(new Text($"Tightness: {tightness}"));

            return result;
        }

        void OnMaskTightnessChanging(ChangingEvent<float> evt)
        {
            m_MaskTightness.SetValueWithoutNotify((float)Math.Round(evt.newValue, 2));
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_Shortcuts = new[]
            {
                new MuseShortcut("Increase Brush Size", OnIncreaseBrushSize, KeyCode.RightBracket, source: (VisualElement)evt.target),
                new MuseShortcut("Decrease Brush Size", OnDecreaseBrushSize, KeyCode.LeftBracket, source: (VisualElement)evt.target),
                new MuseShortcut("Toggle Brush", ToggleBrush, KeyCode.B, source: (VisualElement)evt.target),
                new MuseShortcut("Toggle Eraser", ToggleEraser, KeyCode.E, source: (VisualElement)evt.target),
                new MuseShortcut("Clear", ClearDoodle, KeyCode.Delete, source: (VisualElement)evt.target)
            };
            foreach (var shortcut in m_Shortcuts)
                MuseShortcuts.AddShortcut(shortcut);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            foreach (var shortcut in m_Shortcuts)
                MuseShortcuts.RemoveShortcut(shortcut);
        }

        float GetMaskStrengthFromOperatorData()
        {
            return float.Parse(m_OperatorData.settings[(int)ESettings.MaskStrength], new CultureInfo("en-US"));
        }

        void OnMaskTightnessChanged(ChangeEvent<float> evt)
        {
            m_OperatorData.settings[(int)ESettings.MaskStrength] = evt.newValue.ToString("N2", new CultureInfo("en-US"));
            m_MaskTightness.SetValueWithoutNotify(maskStrength);
        }

        public float maskStrength => float.Parse(m_OperatorData.settings[(int)ESettings.MaskStrength], new CultureInfo("en-US"));

        void OnScenePickerSelectedObject(UnityEngine.Sprite sprite)
        {
            if (sprite == null)
                return;

            if (sprite != null)
            {
                SetReferenceImage(BackendUtilities.SpriteAsTexture(sprite));
            }
        }

#if UNITY_EDITOR
        void ToggleEditorSelection()
        {
            if (m_ScenePicker.isPicking)
                m_ScenePicker.EndPicking();
            else
                m_ScenePicker.StartPicking();
        }
#endif
        public string GetDoodle()
        {
            return GetImage(ESettings.Doodle);
        }

        public Vector2Int doodleSize => new Vector2Int(int.Parse(m_OperatorData.settings[(int)ESettings.DoodleSizeX]), int.Parse(m_OperatorData.settings[(int)ESettings.DoodleSizeY]));

        public string GetReferenceImage()
        {
            return GetImage(ESettings.ReferenceImage);
        }

        string GetImage(ESettings setting)
        {
            return m_OperatorData.settings[(int)setting];
        }

        public bool isClear => bool.Parse(m_OperatorData.settings[(int)ESettings.IsClear]);

        void OnDoodleChanged(byte[] doodleValue)
        {
            m_OperatorData.settings[(int)ESettings.Doodle] = Convert.ToBase64String(doodleValue);
            m_OperatorData.settings[(int)ESettings.IsClear] = m_DoodlePadManipulator.isClear.ToString();

            RecordUndo(doodleValue, isClear, null);

            UpdateVisibility();
        }

        void OnDoodleModifierChanged(DoodleModifierState newModifierState)
        {
            m_BrushButton.selected = newModifierState == DoodleModifierState.Brush;
            m_EraserButton.selected = newModifierState == DoodleModifierState.Erase;

            UpdateVisibility();
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null || data.settings.Length == 0)
                return;
            m_OperatorData.FromJson(data.ToJson());
        }

        public IOperator Clone()
        {
            var result = new KeyImageOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model)
        {
            m_Model = model;

            m_Model.OnDispose += OnDispose;
            AddUndo();
        }

        public void UnregisterFromEvents(Model model)
        {
            model.OnDispose -= OnDispose;
            RemoveUndo();
            if (m_Model == model)
                m_Model = null;
        }

        void OnDispose()
        {
            RemoveUndo();
        }

        public bool IsSavable()
        {
            return true;
        }

        void ToggleBrush()
        {
            m_DoodlePadManipulator.ToggleBrush();
        }

        void ToggleEraser()
        {
            m_DoodlePadManipulator.ToggleEraser();
        }

        void OnIncreaseBrushSize()
        {
            m_DoodlePadManipulator.IncreaseBrushSize();
        }

        void OnDecreaseBrushSize()
        {
            m_DoodlePadManipulator.DecreaseBrushSize();
        }

        void ClearDoodle()
        {
            m_DoodlePadManipulator.ClearDoodle();
        }

        void OnDeleteClicked()
        {
            m_DoodlePadManipulator.Resize(k_DefaultDoodleSize);
            m_DoodlePadManipulator.ClearDoodle();
            m_ReferenceImage.image = null;
            m_ReferenceImage.sprite = null;
            m_OperatorData.settings[(int)ESettings.ReferenceImage] = string.Empty;
            m_DoodlePadManipulator.SetBrush();

            RecordUndo(null, true, null);

            UpdateVisibility();
        }

        void SetReferenceImage(Texture2D referenceImage)
        {
            m_ReferenceImage.image = referenceImage;

            m_OperatorData.settings[(int)ESettings.ReferenceImage] = Convert.ToBase64String(referenceImage.EncodeToPNG());

            m_DoodlePadManipulator.Resize(new Vector2Int(referenceImage.width, referenceImage.height));
            m_DoodlePadManipulator.ClearDoodle();

            RecordUndo(null, isClear, referenceImage);

            UpdateVisibility();
        }

        void UpdateVisibility()
        {
            var isScenePicking = false;
#if UNITY_EDITOR
            isScenePicking = m_ScenePicker.isPicking;
            m_PickerButton.selected = isScenePicking;
#endif
            var isReferenceClear = m_ReferenceImage.image == null && m_ReferenceImage.sprite == null;
            var isDoodling = !isScenePicking && isReferenceClear;

            m_BrushButton.SetEnabled(isDoodling);
            m_EraserButton.SetEnabled(isDoodling);
            m_BrushButton.tooltip = isDoodling ? TextContent.doodleBrushTooltip : TextContent.doodleTooltipDisabled;
            m_EraserButton.tooltip = isDoodling ? TextContent.doodleEraserTooltip : TextContent.doodleTooltipDisabled;

            var showHint = isDoodling && m_DoodlePadManipulator.isClear;
            m_HintLabel.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isDoodling && m_DoodlePadManipulator.currentState != DoodleModifierState.None)
                m_DoodlePadManipulator.SetNone();
        }

        public void InitFromJobInfo(JobInfoResponse jobInfoResponse)
        {
            Guid guid;
            if (System.Guid.TryParse(jobInfoResponse.request.mask0Guid, out guid) && guid != Guid.Empty)
            {
                var getArtifact = new GetArtifactRestCall(ServerConfig.serverConfig, jobInfoResponse.request.mask0Guid);
                getArtifact.RegisterOnSuccess(OnGetMaskArtifactSuccess);
                getArtifact.RegisterOnFailure(OnGetMaskArtifactFailed);
                getArtifact.SendRequest();
            }

            if (System.Guid.TryParse(jobInfoResponse.request.inputGuid, out guid) && guid != Guid.Empty)
            {
                var getArtifact = new GetArtifactRestCall(ServerConfig.serverConfig, jobInfoResponse.request.inputGuid);
                getArtifact.RegisterOnSuccess(OnGetReferenceImageArtifactSuccess);
                getArtifact.RegisterOnFailure(OnGetReferenceImageArtifactFailed);
                getArtifact.SendRequest();
            }

            m_OperatorData.settings[(int)ESettings.MaskStrength] = jobInfoResponse.request.maskStrength.ToString("N2");
        }

        void OnGetReferenceImageArtifactFailed(GetArtifactRestCall request)
        {
            Debug.Log($"Failed to get reference image artifact: {request.requestError} {request.requestResult}");
        }

        void OnGetReferenceImageArtifactSuccess(GetArtifactRestCall request, byte[] data)
        {
            m_OperatorData.settings[(int)ESettings.ReferenceImage] = Convert.ToBase64String(data);
            m_OperatorData.settings[(int)ESettings.IsClear] = true.ToString();
        }

        void OnGetMaskArtifactSuccess(GetArtifactRestCall request, byte[] data)
        {
            m_OperatorData.settings[(int)ESettings.Doodle] = Convert.ToBase64String(data);
            m_OperatorData.settings[(int)ESettings.IsClear] = false.ToString();
        }

        void OnGetMaskArtifactFailed(GetArtifactRestCall request)
        {
            Debug.Log($"Failed to get mask image artifact: {request.requestError} {request.requestResult}");
        }

        Texture2D GetTexture(ESettings setting)
        {
            Texture2D texture = null;

            var refImageRaw = GetImage(setting);
            if (!string.IsNullOrEmpty(refImageRaw))
            {
                texture = new Texture2D(2, 2);
                texture.LoadImage(Convert.FromBase64String(refImageRaw));
                texture.Apply();
            }

            return texture;
        }

        #region Undo

        void RecordUndo(byte[] doodle, bool clear, Texture2D refImage)
        {
#if UNITY_EDITOR
            m_Undo.SetData(doodle, clear, refImage, ++m_UndoVersion);
#endif
        }

#if UNITY_EDITOR
        KeyImageUndo m_Undo;
        int m_UndoVersion;
#endif

        void AddUndo()
        {
#if UNITY_EDITOR
            m_Undo = KeyImageUndo.Get();
            m_Undo.SetData(Convert.FromBase64String(GetDoodle()), isClear, (Texture2D)m_ReferenceImage?.image, 0);
            m_Undo.onUndoRedo += OnUndoRedo;
#endif
        }

        void RemoveUndo()
        {
#if UNITY_EDITOR
            if (m_Undo == null)
                return;
            m_Undo.onUndoRedo -= OnUndoRedo;
            m_Undo.Dispose();
#endif
        }

        void OnUndoRedo()
        {
#if UNITY_EDITOR
            if (m_DoodlePadManipulator == null)
                return;

            if (m_UndoVersion == m_Undo.version)
                return;

            m_UndoVersion = m_Undo.version;

            var doodleData = m_Undo.rawTextureData ?? Array.Empty<byte>();
            var imageData = m_Undo.referenceImage != null ? m_Undo.referenceImage.EncodeToPNG() : Array.Empty<byte>();

            m_DoodlePadManipulator.SetValueWithoutNotify(m_Undo.rawTextureData);
            m_DoodlePadManipulator.isClear = m_Undo.isClear;
            m_ReferenceImage.image = m_Undo.referenceImage;
            m_ReferenceImage.sprite = null;

            m_OperatorData.settings[(int)ESettings.Doodle] = Convert.ToBase64String(doodleData);
            m_OperatorData.settings[(int)ESettings.IsClear] = m_Undo.isClear.ToString();
            m_OperatorData.settings[(int)ESettings.ReferenceImage] = Convert.ToBase64String(imageData);

            UpdateVisibility();
#endif
        }

        #endregion
    }
}