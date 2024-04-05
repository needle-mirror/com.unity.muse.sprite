using System;
using System.Collections.Generic;
using Unity.Muse.AppUI.UI;
using Unity.Muse.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.StyleTrainer
{
    class SampleOutputPromptRow : ExVisualElement, ISampleOutputRow
    {
        struct RowItemData
        {
            public SampleOutputPromptInput promptInput;
            public SampleOutputListRowItem rowItem;
        }
        StyleData m_StyleData;
        List<RowItemData> m_RowItems = new ();
        public const float k_PromptDimensionRatio = 2f;
        public Action<int> OnSampleOutputPromptDeleteClicked;
        const float k_RowMaxHeight = 100f;
        float m_PromptDimensionRatio = k_PromptDimensionRatio;
        protected SampleOutputPromptRow() { }

        public void BindElements(StyleData styleData)
        {
            ClearContent();
            m_StyleData = styleData;

            for(int i = 0; i < m_StyleData.sampleOutputPrompts.Count; i++)
            {
                var input = SampleOutputPromptInput.CreateFromUxml();
                input.itemIndex = i;
                input.prompt = m_StyleData.sampleOutputPrompts[i];
                input.OnPromptChanged += OnPromptChanged;
                input.OnDeleteClicked += OnDeleteClicked;
                var rowItem = CreateRowItem(input, m_PromptDimensionRatio);
                m_RowItems.Add(new RowItemData
                {
                    promptInput = input,
                    rowItem = rowItem
                });
                Add(rowItem);
            }
        }

        public void ClearContent()
        {
            for(int i = 0; i < m_RowItems.Count; i++)
            {
                m_RowItems[i].promptInput.OnPromptChanged -= OnPromptChanged;
                m_RowItems[i].promptInput.OnDeleteClicked -= OnDeleteClicked;
            }

            this.Clear();
            m_RowItems.Clear();
        }

        void OnDeleteClicked(int obj)
        {
            OnSampleOutputPromptDeleteClicked?.Invoke(obj);
        }

        void OnPromptChanged(int arg1, string arg2)
        {
            m_RowItems[arg1].promptInput.prompt = m_StyleData.UpdateSamplePrompt(arg1, arg2);
        }

        internal static SampleOutputPromptRow CreateFromUxml(StyleData styleData, float height)
        {
            var ve = new SampleOutputPromptRow();
            ve.AddToClassList("styletrainer-sampleoutputpromptrow");
            ve.styleSheets.Add(ResourceManager.Load<StyleSheet>(PackageResources.sampleOutputPromptRowStyleSheet));
            ve.UpdateRowHeight(height);
            ve.BindElements(styleData);
            return ve;
        }

        SampleOutputListRowItem CreateRowItem(VisualElement child, float ratio)
        {
            var item = new SampleOutputListRowItem(child, ratio);
            item.AddToClassList("sampleoutputv2-listview-rowitem");
            return item;
        }

        static public float PromptItemWidth(float height)
        {
            var h = Mathf.Min(height, k_RowMaxHeight);
            return PromptItemRatio(height) * h;
        }

        static float PromptItemRatio(float height)
        {
            var ratio = k_PromptDimensionRatio;
            if(height > k_RowMaxHeight)
            {
                ratio = k_PromptDimensionRatio * height / k_RowMaxHeight;
            }
            return ratio;
        }
        public void UpdateRowHeight(float height)
        {
            var h = Mathf.Min(height, k_RowMaxHeight);
            m_PromptDimensionRatio = PromptItemRatio(height);
            for(int i = 0; i < m_RowItems.Count; i++)
            {
                m_RowItems[i].rowItem.ratio = m_PromptDimensionRatio;
            }
            style.height = h;
            MarkDirtyRepaint();
        }

        public void Unbind()
        {
            ClearContent();
        }

        public void CanModify(bool canModify)
        {
            for(int i = 0; i < m_RowItems.Count; i++)
            {
                m_RowItems[i].promptInput.CanModify(canModify);
            }
        }

        public bool UpdateCheckPointData(CheckPointData checkPointData)
        {
            return false;
        }

        public bool SetFavouriteCheckpoint(string checkpoint)
        {
            return false;
        }

        public void SelectItems(IList<int> indices)
        {
            if (indices.Count > 0 && indices[0] < m_RowItems.Count)
            {
                m_RowItems[indices[0]].promptInput.FocusItem();
            }
        }

        public RowStateBase GetRowState()
        {
            return new RowStateBase()
            {
                guid = "PromptRow-State"
            };
        }
    }
}