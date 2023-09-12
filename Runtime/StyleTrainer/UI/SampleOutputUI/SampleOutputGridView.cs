using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputGridView : GridView
    {
        int m_CountPerRow = 2;
        const float k_DefaultThumbnailSize = 240;
        float m_ThumbnailSize = 1;
        bool m_CanModify;
        public Action<int> OnDeleteClickedCallback;

        public SampleOutputGridView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Unity.Muse.StyleTrainer/uss/SampleOutputView"));
            name = "SampleOutputGridView";

            makeItem = MakeGridItem;
            bindItem = BindItem;
            unbindItem = UnbindItem;
            columnCount = m_CountPerRow;
            selectionType = SelectionType.Single;
            itemHeight = (int)k_DefaultThumbnailSize;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scrollView.verticalScroller.style.opacity = 0;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void UnbindItem(VisualElement arg1, int arg2)
        {
            var ve = arg1 as SampleOutputGridItem;
            ve.OnDeleteClicked -= OnDeleteClicked;
            ve.OnPromptChanged -= OnPromptChanged;
        }

        void OnPromptChanged(int index, string prompt)
        {
            //TODO don't do this here
            ((SampleOutputData)itemsSource[index]).prompt = prompt;
        }

        void OnDeleteClicked(int obj)
        {
            OnDeleteClickedCallback?.Invoke(obj);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (panel == null || float.IsNaN(evt.newRect.width) || Mathf.Approximately(0, evt.newRect.width))
                return;

            RefreshThumbnailSize(m_ThumbnailSize);
        }

        void BindItem(VisualElement arg1, int arg2)
        {
            var ve = arg1 as SampleOutputGridItem;
            ve.SetEnabled(m_CanModify);
            if (itemsSource != null && arg2 < itemsSource.Count)
            {
                var data = itemsSource[arg2] as SampleOutputData;
                if (data != null)
                {
                    ve.prompt = data.prompt;
                    ve.OnDeleteClicked += OnDeleteClicked;
                    ve.OnPromptChanged += OnPromptChanged;
                    ve.itemIndex = arg2;
                    ve.SetArtifact(data.imageArtifact);
                }
            }
        }

        static VisualElement MakeGridItem()
        {
            return SampleOutputGridItem.CreateFromUxml();
        }

        public void RefreshThumbnailSize(float value)
        {
            m_ThumbnailSize = value;
            var size = value * k_DefaultThumbnailSize;

            var sizeAndMargin = size;

            var width = scrollView.contentContainer.resolvedStyle.width;
            var newCountPerRow = Mathf.FloorToInt(width / sizeAndMargin);

            newCountPerRow = Mathf.Max(1, newCountPerRow);

            if (newCountPerRow != m_CountPerRow)
            {
                m_CountPerRow = newCountPerRow;
                columnCount = m_CountPerRow;
            }

            var newItemHeight = Mathf.FloorToInt(width / m_CountPerRow);

            if (!Mathf.Approximately(itemHeight, newItemHeight))
                itemHeight = newItemHeight;
        }

        public void SetCanModify(bool canModify)
        {
            m_CanModify = canModify;
            var selections = selectedIds.ToArray();
            selectionType = canModify ? SelectionType.Single : SelectionType.None;
            EnableInClassList("styletrainer-sampleoutputview-gridview-disable", !canModify);
            Refresh();
            if (selectionType != SelectionType.None)
                SetSelectionWithoutNotify(selections);
        }
    }
}