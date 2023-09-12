using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputListView : ListView
    {
        public Action<int> OnDeleteClickedCallback;

        public SampleOutputListView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/SampleOutputView"));
            name = "SampleOutputGridView";
            makeItem = MakeListItem;
            bindItem = BindItem;
            unbindItem = UnbindItem;
            selectionType = SelectionType.Multiple;
            fixedItemHeight = 80;
            itemsSource = new SampleOutputData[0];
        }

        void UnbindItem(VisualElement arg1, int arg2)
        {
            var ve = arg1 as SampleOutputListItem;
            ve.OnDeleteClicked -= OnDeleteClicked;
            ve.OnPromptChanged -= OnPromptChanged;
        }

        void BindItem(VisualElement arg1, int arg2)
        {
            var ve = arg1 as SampleOutputListItem;
            ve.OnDeleteClicked += OnDeleteClicked;
            ve.OnPromptChanged += OnPromptChanged;
            ve.itemIndex = arg2;
            ve.prompt = this[arg2].prompt;
        }

        VisualElement MakeListItem()
        {
            return SampleOutputListItem.CreateFromUxml();
        }

        void OnDeleteClicked(int index)
        {
            OnDeleteClickedCallback?.Invoke(index);
        }

        void OnPromptChanged(int index, string prompt)
        {
            //TODO don't do this here
            ((SampleOutputData)itemsSource[index]).prompt = prompt;
        }

        new SampleOutputData this[int index] => (SampleOutputData)itemsSource[index];
    }
}