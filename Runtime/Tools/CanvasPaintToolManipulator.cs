using System;
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
        readonly DoodlePadManipulator m_PaintingManipulator;

        PaintCanvasToolManipulatorUndo m_Undo;

        int m_Version = 0;

        public PaintCanvasToolManipulator(Model model, Vector2Int size)
            : base(model)
        {
            m_PaintingManipulator = new DoodlePadManipulator(size, 0.8f);
            m_PaintingManipulator.onValueChanged += OnPaintingDone;
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

            m_CurrentModel.OnDispose += OnDispose;
            m_Undo = PaintCanvasToolManipulatorUndo.Get();
            m_Undo.onUndoRedo += OnUndoRedo;

            var size = m_CurrentModel.CurrentOperators.GetOperator<SpriteGeneratorSettingsOperator>().imageSize;
            m_PaintingManipulator.Resize(size);
            node.PaintSurfaceElement.AddManipulator(m_PaintingManipulator);

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
            m_PaintingManipulator?.target.RemoveManipulator(m_PaintingManipulator);
            OnDispose();
        }

        void OnDispose()
        {
            m_Undo.Dispose();
            m_CurrentModel.OnDispose -= OnDispose;
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