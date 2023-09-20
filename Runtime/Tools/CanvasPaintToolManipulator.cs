using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Operators;
using Unity.Muse.Sprite.UIComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Sprite.Tools
{
    class PaintCanvasToolManipulator : CanvasManipulator
    {
        DoodlePadManipulator m_PaintingManipulator;
        PaintCanvasToolManipulatorUndo m_Undo;

        int m_Version = 0;
        Vector2Int m_Size;

        public PaintCanvasToolManipulator(Model model, Vector2Int size)
            : base(model)
        {
            m_Size = size;
            m_PaintingManipulator = new DoodlePadManipulator(m_Size, 0.8f);
        }

        void Refresh(IEnumerable<IOperator> operators, bool set)
        {
            if (!m_CurrentModel.isRefineMode)
                return;
            if (m_PaintingManipulator is null)
                return;
            if (set)
                return;

            RefreshMask();
        }

        void OnPaintingDone(byte[] data)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            tex.Apply();

            m_Undo.SetData(data, ++m_Version);

            m_CurrentModel.MaskPaintDone(tex);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var nodeQuery = target.Query<ArtifactView>().Where((element) => element.Artifact.Guid == m_CurrentModel.SelectedArtifact.Guid);
            var node = nodeQuery.First();

            if (node == null)
            {
                Debug.LogError("Could not find Node");
                return;
            }

            UnregisterPaintingManipulator();
            m_PaintingManipulator = new DoodlePadManipulator(m_Size, 0.8f);

            m_CurrentModel.OnDispose += OnDispose;
            m_Undo = PaintCanvasToolManipulatorUndo.Get();
            m_Undo.onUndoRedo += OnUndoRedo;
            m_CurrentModel.OnOperatorUpdated += Refresh;
            m_PaintingManipulator.onValueChanged += OnPaintingDone;

            var size = m_CurrentModel.CurrentOperators.GetOperator<SpriteGeneratorSettingsOperator>().imageSize;
            m_PaintingManipulator.Resize(size);
            node.PaintSurfaceElement.AddManipulator(m_PaintingManipulator);
            RefreshMask();
        }

        void RefreshMask()
        {
            var maskOperator = m_CurrentModel.CurrentOperators.GetOperator<SpriteRefiningMaskOperator>();
            if (maskOperator != null)
            {
                var mask = maskOperator.GetMask();
                if(!string.IsNullOrEmpty(mask))
                    m_PaintingManipulator.SetValueWithoutNotify(Convert.FromBase64String(mask));
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            UnregisterPaintingManipulator();
            OnDispose();
        }

        void UnregisterPaintingManipulator()
        {
            if (m_PaintingManipulator is null)
                return;
            m_PaintingManipulator.onValueChanged -= OnPaintingDone;
            m_PaintingManipulator?.target.RemoveManipulator(m_PaintingManipulator);
        }

        void OnDispose()
        {
            m_Undo.Dispose();
            m_CurrentModel.OnDispose -= OnDispose;
            m_CurrentModel.OnOperatorUpdated -= Refresh;
        }

        void OnUndoRedo()
        {
            if(m_PaintingManipulator == null)
                return;

            if(m_Version == m_Undo.version)
                return;

            m_Version = m_Undo.version;

            m_PaintingManipulator.SetValueWithoutNotify(m_Undo.rawTextureData);
        }

        public float radius
        {
            get => m_PaintingManipulator.GetBrushSize();
            set => m_PaintingManipulator.SetBrushSize(value);
        }

        public void SetEraserMode(bool isEraser)
        {
            if (isEraser)
                m_PaintingManipulator.SetEraser();
            else
                m_PaintingManipulator.SetBrush();
        }

        public void ClearPainting()
        {
            m_PaintingManipulator.ClearDoodle();
        }
    }
}