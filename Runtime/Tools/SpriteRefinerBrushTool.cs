using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Operators;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = Unity.AppUI.UI.Toggle;
using TextContent = Unity.Muse.Sprite.UIComponents.TextContent;

namespace Unity.Muse.Sprite.Tools
{
    class SpriteRefinerBrushTool : ICanvasTool
    {
        Model m_CurrentModel;
        PaintCanvasToolManipulator m_CurrentManipulator;
        PaintingManipulatorSettings m_Settings;

        public CanvasManipulator GetToolManipulator()
        {
            m_CurrentManipulator = new PaintCanvasToolManipulator(m_CurrentModel, new Vector2Int(2, 2));
            m_Settings = new PaintingManipulatorSettings(this, m_CurrentManipulator);
            return m_CurrentManipulator;
        }

        public void SetModel(Model model)
        {
            m_CurrentModel = model;
        }

        public bool EvaluateEnableState(Artifact artifact)
        {
            return m_CurrentModel.isRefineMode;
        }

        public void ActivateOperators()
        {
            if (m_CurrentModel == null) return;

            var opMask = m_CurrentModel.CurrentOperators.Find(x => x.GetType() == typeof(SpriteRefiningMaskOperator)) ??
                m_CurrentModel.AddOperator<SpriteRefiningMaskOperator>();

            if (opMask != null && !opMask.Enabled())
            {
                opMask.Enable(true);
                m_CurrentModel.UpdateOperators(opMask);
            }
        }

        public ICanvasTool.ToolButtonData GetToolData()
        {
            return new ICanvasTool.ToolButtonData()
            {
                Name = "muse-brush-tool-button",
                Label = "",
                Icon = "paint-brush",
                Tooltip = TextContent.controlMaskToolTooltip
            };
        }

        public VisualElement GetSettings()
        {
            return m_Settings?.GetSettings();
        }

        public float radius
        {
            get => m_CurrentManipulator.radius;
            set => m_CurrentManipulator.radius = value;
        }

        public void SetEraserMode(bool isEraser)
        {
            m_CurrentManipulator?.SetEraserMode(isEraser);
        }

        public void Clear()
        {
            m_CurrentManipulator?.ClearPainting();
        }
    }

    internal class PaintingManipulatorSettings
    {
        VisualElement m_Root;
        SpriteRefinerBrushTool m_BrushTool;
        PaintCanvasToolManipulator m_ToolManipulator;
        bool m_IsInitialized;
        List<MuseShortcut> m_Shortcuts;
        TouchSliderFloat m_RadiusSlider;
        Toggle m_ToggleErase;

        private PaintingManipulatorSettings() { }

        public PaintingManipulatorSettings(SpriteRefinerBrushTool brushTool, PaintCanvasToolManipulator paintManipulator)
        {
            m_BrushTool = brushTool;
            m_ToolManipulator = paintManipulator;
            Init();
        }

        void Init()
        {
            if (m_IsInitialized)
                return;

            m_Root = new VisualElement();
            m_Root.style.flexDirection = FlexDirection.Row;
            m_RadiusSlider = new TouchSliderFloat();
            m_RadiusSlider.label = "Radius";
            m_RadiusSlider.tooltip = TextContent.controlMaskBrushSizeTooltip;
            m_RadiusSlider.incrementFactor = 0.1f;
            m_RadiusSlider.formatString = "F1";
            m_RadiusSlider.lowValue = 3.0f;
            m_RadiusSlider.highValue = 50.0f;
            m_RadiusSlider.value = m_BrushTool.radius;
            m_RadiusSlider.style.width = 150.0f;

            m_RadiusSlider.RegisterValueChangedCallback(evt =>
            {
                m_BrushTool.radius = evt.newValue;
            });

            m_ToggleErase = new Toggle { label = "Eraser", tooltip = TextContent.controlMaskEraserToolTooltip };

            m_ToggleErase.RegisterValueChangedCallback(evt =>
            {
                m_BrushTool.SetEraserMode(evt.newValue);
            });
            m_ToggleErase.style.width = 100.0f;

            var clearButton = new ActionButton
            {
                name = "refiner-clear-button",
                label = "",
                tooltip = TextContent.controlMaskClearToolTooltip,
                icon = "delete",
                quiet = true
            };

            clearButton.AddToClassList("muse-controltoolbar__actionbutton");
            clearButton.clicked += () =>
            {
                m_BrushTool.Clear();
            };
            m_Root.Add(m_ToggleErase);
            m_Root.Add(m_RadiusSlider);
            m_Root.Add(clearButton);

            m_Root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            m_Root.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            m_IsInitialized = true;
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_Shortcuts = new List<MuseShortcut>
            {
                new("Increase Brush Size", OnIncreaseBrushSize, KeyCode.RightBracket, source: m_Root),
                new("Decrease Brush Size", OnDecreaseBrushSize, KeyCode.LeftBracket, source: m_Root),
                new("Toggle Brush", ToggleBrush, KeyCode.B, source: m_Root),
                new("Toggle Eraser", ToggleEraser, KeyCode.E, source: m_Root)
            };
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                m_Shortcuts.Add(new MuseShortcut("Clear", ClearDoodle, KeyCode.Backspace, KeyModifier.Action, source: m_Root));
            else
                m_Shortcuts.Add(new MuseShortcut("Clear", ClearDoodle, KeyCode.Delete, source: m_Root));

            foreach (var shortcut in m_Shortcuts)
                MuseShortcuts.AddShortcut(shortcut);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            foreach (var shortcut in m_Shortcuts)
                MuseShortcuts.RemoveShortcut(shortcut);
        }

        public VisualElement GetSettings()
        {
            return m_Root;
        }

        void OnIncreaseBrushSize()
        {
            if (!isFocused)
                return;

            m_RadiusSlider.value += k_RadiusStep;
        }

        const float k_RadiusStep = 3f;

        void OnDecreaseBrushSize()
        {
            if (!isFocused)
                return;

            m_RadiusSlider.value -= k_RadiusStep;
        }

        void ToggleBrush()
        {
            if (!isFocused)
                return;

            if (m_ToggleErase.value)
                m_ToggleErase.value = false;
        }

        void ToggleEraser()
        {
            if (!isFocused)
                return;

            if (!m_ToggleErase.value)
                m_ToggleErase.value = true;
        }

        void ClearDoodle()
        {
            if (!isFocused)
                return;

            m_BrushTool.Clear();
        }

        bool isFocused
        {
            get
            {
                var focusedElement = m_ToolManipulator.target.panel.focusController.focusedElement;
                return focusedElement == m_ToolManipulator.target;
            }
        }
    }
}
