using Unity.AppUI.UI;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputVersionFoldoutHeader : ExVisualElement
    {
        internal static SampleOutputVersionFoldoutHeader CreateFromUxml()
        {
            var visualTree = ResourceManager.Load<VisualTreeAsset>(PackageResources.sampleOutputVersionFoldoutHeaderTemplate);
            var ve = (SampleOutputVersionFoldoutHeader)visualTree.CloneTree().Q("SampleOutputVersionFoldoutHeader");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.sampleOutputPromptInputStyleSheet));
            return ve;
        }

        public new class UxmlFactory : UxmlFactory<SampleOutputVersionFoldoutHeader, UxmlTraits> { }
    }
}