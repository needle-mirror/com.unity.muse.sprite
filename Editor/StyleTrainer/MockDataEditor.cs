using System;
using StyleTrainer.Backend;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer.Editor
{
    [CustomEditor(typeof(MockData))]
    class MockDataEditor : UnityEditor.Editor, IDisposable
    {
        bool m_Foldout;

        public override void OnInspectorGUI()
        {
            m_Foldout = EditorGUILayout.Foldout(m_Foldout, "Mock Data");
            if (m_Foldout)
            {
                base.OnInspectorGUI();
                if (GUILayout.Button("Reset"))
                {
                    ((MockData)target).Reset();
                    serializedObject.Update();
                }
            }
        }

        public void CreateInspectorUI(ScrollView newScrollView)
        {
            newScrollView.Add(new IMGUIContainer(OnInspectorGUI));
        }

        public void Dispose()
        {
            // Not used for now.
        }
    }
}