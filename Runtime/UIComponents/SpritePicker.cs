using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Sprite.UIComponents
{
#if UNITY_EDITOR
    internal class SpritePicker : VisualElement
    {
        public event Action onPickStart;
        public event Action onPickEnd;

        public event Action<UnityEngine.Sprite> onSelectedObject;

        public bool isPicking => m_StartedPicking;

        bool m_StartedPicking;

        public SpritePicker()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDownHandler);
        }

        public void StartPicking()
        {
            if (isPicking)
                return;

            m_StartedPicking = true;

            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGui;
            Selection.selectionChanged += OnSelectionChanged;
            if (this.HasMouseCapture())
                return;

            this.CaptureMouse();
            RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);

            onPickStart?.Invoke();
        }

        public void EndPicking()
        {
            if (!isPicking)
                return;

            UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);

            m_StartedPicking = false;

            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGui;
            Selection.selectionChanged -= OnSelectionChanged;
            if (this.HasMouseCapture())
                this.ReleaseMouse();

            onPickEnd?.Invoke();
        }

        void OnEditorUpdate()
        {
            if (!isPicking)
                return;

            switch (EditorWindow.focusedWindow.titleContent.text)
            {
                case "Hierarchy":
                case "Project":
                    schedule.Execute(OnSelectionChanged);
                    break;
            }
        }

        void OnSelectionChanged()
        {
            if (!isPicking)
                return;

            switch (Selection.activeObject)
            {
                case GameObject go:
                {
                    var spriteRenderer = go.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                        onSelectedObject?.Invoke(spriteRenderer.sprite);
                    break;
                }
                case UnityEngine.Sprite sr:
                    onSelectedObject?.Invoke(sr);
                    break;
                default:
                {
                    var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    var sprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(assetPath);
                    if (sprite != null)
                        onSelectedObject?.Invoke(sprite);
                    break;
                }
            }

            EndPicking();
        }

        void OnMouseCaptureOut(MouseCaptureOutEvent mouseCaptureOutEvent)
        {
            EndPicking();
        }

        void OnMouseDownHandler(MouseDownEvent evt)
        {
            if (!isPicking)
                StartPicking();
            else
                EndPicking();
        }

        void OnSceneGui(SceneView sceneView)
        {
            if (!isPicking)
                return;

            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                var obj = FindObject();
                if (obj != null)
                    onSelectedObject?.Invoke(obj);

                EndPicking();
            }
        }

        static UnityEngine.Sprite FindObject()
        {
            var go = HandleUtility.PickGameObject(Event.current.mousePosition, true);
            if (go != null)
            {
                var selection = go.GetComponentsInChildren<SpriteRenderer>();
                if (selection.Length > 0)
                    return selection[0].sprite;
            }

            return null;
        }
    }
#endif
}
