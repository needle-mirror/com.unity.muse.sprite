using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Artifacts;
using Unity.Muse.Sprite.Backend;
using Unity.Muse.Sprite.Common.Backend;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Sprite.UIComponents
{
    class SpriteTextureDropManipulator : Manipulator
    {
        public event Action onDragStart;
        public event Action onDragEnd;
        public event Action<Texture2D> onDrop;

        readonly Model m_Model;

        Texture2D m_Texture;

        bool isDragging => m_Texture != null;

        public SpriteTextureDropManipulator(Model model)
        {
            m_Model = model;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            #if UNITY_EDITOR
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            target.RegisterCallback<DragUpdatedEvent>(OnSpriteDragUpdate);
            target.RegisterCallback<DragExitedEvent>(OnSpriteDragExit);
            #endif
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            #if UNITY_EDITOR
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            target.UnregisterCallback<DragUpdatedEvent>(OnSpriteDragUpdate);
            target.UnregisterCallback<DragExitedEvent>(OnSpriteDragExit);
            #endif
        }

        void OnArtifactsDrop(IEnumerable<Artifact> artifacts, Vector3 pos)
        {
            if(!isDragging)
                return;

            var artifact = artifacts.FirstOrDefault();

            if (artifact is SpriteMuseArtifact spriteMuseArtifact && ArtifactCache.IsInCache(spriteMuseArtifact))
            {
                var texture = (Texture2D)ArtifactCache.Read(spriteMuseArtifact);
                if (texture != null)
                {
                    m_Texture = texture;
                    onDrop?.Invoke(m_Texture);

                    OnExit();
                }
            }
        }

        // Sprite Muse Alpha - WebGL
        void OnPreviewDrop(IEnumerable<Texture2D> previews, Vector3 pos)
        {
            if(!isDragging)
                return;

            var preview = previews.FirstOrDefault();
            if (preview != null)
            {
                m_Texture = preview;
                onDrop?.Invoke(m_Texture);

                OnExit();
            }
        }
        // Sprite Muse Alpha - WebGL

        void OnPointerEnter(PointerEnterEvent evt) => OnEnter();
        void OnPointerLeave(PointerLeaveEvent evt) => OnExit();
        void OnPointerCancel(PointerCancelEvent evt) => OnExit();
        #if UNITY_EDITOR
        void OnDragPerform(DragPerformEvent evt) => OnPerform();
        void OnSpriteDragUpdate(DragUpdatedEvent evt) => OnMove();
        void OnSpriteDragExit(DragExitedEvent evt) => OnExit();
        #endif

        void OnEnter()
        {
            var texture = GetTexture();
            if(texture != null)
            {
                m_Texture = texture;
                onDragStart?.Invoke();

                m_Model.OnItemsDropped += OnArtifactsDrop;
            }
        }

        void OnExit()
        {
            if(!isDragging)
                return;

            m_Texture = null;
            onDragEnd?.Invoke();

            m_Model.OnItemsDropped -= OnArtifactsDrop;
        }

        void OnPerform()
        {
            if(!isDragging)
                return;

            var texture = GetTexture();
            if (texture != null)
            {
                m_Texture = texture;
#if UNITY_EDITOR
                UnityEditor.DragAndDrop.AcceptDrag();
#endif
                onDrop?.Invoke(texture);
            }

            OnExit();
        }

        void OnMove()
        {
            if(!isDragging)
                return;

            var texture = GetTexture();
            if(texture == null)
            {
                m_Texture = null;
                onDragEnd?.Invoke();
            }
            else
            {
                m_Texture = texture;
#if UNITY_EDITOR
                UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
#endif
            }
        }

        Texture2D GetTexture()
        {
            var artifact = m_Model.DraggedArtifacts?.FirstOrDefault();
            if (artifact is SpriteMuseArtifact spriteMuseArtifact && ArtifactCache.IsInCache(spriteMuseArtifact))
                return (Texture2D) ArtifactCache.Read(spriteMuseArtifact);


            #if UNITY_EDITOR
            var spriteRef = UnityEditor.DragAndDrop.objectReferences.FirstOrDefault(obj => obj is UnityEngine.Sprite);
            if(spriteRef != null)
                return BackendUtilities.SpriteAsTexture((UnityEngine.Sprite)spriteRef);

            var go = UnityEditor.DragAndDrop.objectReferences.FirstOrDefault(obj => obj is GameObject go && go.GetComponentInChildren<SpriteRenderer>()?.sprite != null);
            if(go != null)
                return BackendUtilities.SpriteAsTexture(((GameObject)go).GetComponent<SpriteRenderer>().sprite);

            var textureRef = UnityEditor.DragAndDrop.objectReferences.FirstOrDefault(obj => obj is UnityEngine.Texture2D);
            if (textureRef != null)
            {
                var texture = (Texture2D)textureRef;
                return BackendUtilities.CreateTemporaryDuplicate(texture, texture.width, texture.height);
            }

            #endif

            return null;
        }
    }
}