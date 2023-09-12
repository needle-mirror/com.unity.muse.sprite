using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Unity.Muse.StyleTrainer
{
    class GridView : BindableElement, ISerializationCallbackReceiver
    {
        const float k_PageSizeFactor = 0.25f;

        /// <summary>
        /// Available Operations.
        /// </summary>
        [Flags]
        public enum GridOperation
        {
            None = 0,
            SelectAll = 1,
            Cancel = 2,
            Left = 3,
            Right = 4,
            Up = 5,
            Down = 6,
            Begin = 7,
            End = 8,
            Choose =9
        }

        const int k_ExtraVisibleRows = 2;

        /// <summary>
        /// The USS class name for GridView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the GridView element. Any styling applied to
        /// this class affects every GridView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        const string k_UssClassName = "appui-grid-view";

        /// <summary>
        /// The USS class name for GridView elements with a border.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the GridView element if the instance's
        /// <see cref="GridView.showBorder"/> property is set to true. Any styling applied to this class
        /// affects every such GridView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        const string k_BorderUssClassName = k_UssClassName + "--with-border";

        /// <summary>
        /// The USS class name of item elements in GridView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element the GridView contains. Any styling applied to
        /// this class affects every item element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        const string k_ItemUssClassName = k_UssClassName + "__item";

        /// <summary>
        /// The USS class name of selected item elements in the GridView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every selected element in the GridView. The <see cref="GridView.selectionType"/>
        /// property decides if zero, one, or more elements can be selected. Any styling applied to
        /// this class affects every GridView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        internal const string itemSelectedVariantUssClassName = Styles.selectedUssClassName;

        /// <summary>
        /// The USS class name of rows in the GridView.
        /// </summary>
        const string k_RowUssClassName = k_UssClassName + "__row";

        const int k_DefaultItemHeight = 30;

        const int k_MaxNumberOfGridColumns = 4;

        static CustomStyleProperty<int> s_ItemHeightProperty = new("--unity-item-height");

        internal readonly ScrollView scrollView;

        readonly List<int> m_SelectedIds = new();

        readonly List<int> m_SelectedIndices = new();

        readonly List<object> m_SelectedItems = new();

        List<int> m_OriginalSelection;

        float m_OriginalScrollOffset;

        int m_SoftSelectIndex = -1;

        Action<VisualElement, int> m_BindItem;

        int m_ColumnCount = 1;

        int m_FirstVisibleIndex;

        Func<int, int> m_GetItemId;

        int m_ItemHeight = k_DefaultItemHeight;

        bool m_ItemHeightIsInline;

        IList m_ItemsSource;

        float m_LastHeight;

        Func<VisualElement> m_MakeItem;

        int m_RangeSelectionOrigin = -1;

        bool m_IsRangeSelectionDirectionUp;

        List<RecycledRow> m_RowPool = new();

        // we keep this list in order to minimize temporary gc allocs
        List<RecycledRow> m_ScrollInsertionList = new();

        // Persisted.
        float m_ScrollOffset;

        SelectionType m_SelectionType;

        int m_VisibleRowCount;

        bool m_IsList;

        NavigationMoveEvent m_NavigationMoveAdapter;

        NavigationCancelEvent m_NavigationCancelAdapter;

        bool m_HasPointerMoved;
        readonly GridViewDragger m_Dragger;

        /// <summary>
        /// Creates a <see cref="GridView"/> with all default properties. The <see cref="GridView.itemsSource"/>,
        /// <see cref="GridView.itemHeight"/>, <see cref="GridView.makeItem"/> and <see cref="GridView.bindItem"/> properties
        /// must all be set for the GridView to function properly.
        /// </summary>
        public GridView()
        {
            AddToClassList(k_UssClassName);

            selectionType = SelectionType.Single;
            m_ScrollOffset = 0.0f;

            scrollView = new ScrollView { viewDataKey = "grid-view__scroll-view" };
            scrollView.StretchToParentSize();
            scrollView.verticalScroller.valueChanged += OnScroll;

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            scrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            scrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(scrollView);

            scrollView.contentContainer.focusable = true;
            scrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            focusable = true;

            m_Dragger = new GridViewDragger(OnDraggerStarted, OnDraggerMoved, OnDraggerEnded, OnDraggerCanceled);
            this.AddManipulator(m_Dragger);
        }

        /// <summary>
        /// Constructs a <see cref="GridView"/>, with all required properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        public GridView(IList itemsSource, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
            : this()
        {
            m_ItemsSource = itemsSource;
            m_ItemHeightIsInline = true;

            m_MakeItem = makeItem;
            m_BindItem = bindItem;

            operationMask = ~GridOperation.None;
        }

        bool Apply(GridOperation operation, bool shiftKey)
        {
            void HandleSelectionAndScroll(int index)
            {
                if (selectionType == SelectionType.Multiple && shiftKey && m_SelectedIndices.Count != 0)
                    DoRangeSelection(index);
                else
                    selectedIndex = index;

                ScrollToItem(index);
            }

            switch (operation)
            {
                case GridOperation.None:
                    break;
                case GridOperation.SelectAll:
                    SelectAll();
                    return true;
                case GridOperation.Cancel:
                    ClearSelection();
                    return true;
                case GridOperation.Left:
                    {
                        var newIndex = Mathf.Max(selectedIndex - 1, 0);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case GridOperation.Right:
                    {
                        var newIndex = Mathf.Min(selectedIndex + 1, itemsSource.Count);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case GridOperation.Up:
                    {
                        var newIndex = Mathf.Max(selectedIndex - columnCount, 0);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case GridOperation.Down:
                    {
                        var newIndex = Mathf.Min(selectedIndex + columnCount, itemsSource.Count);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case GridOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case GridOperation.End:
                    HandleSelectionAndScroll(itemsSource.Count - 1);
                    return true;
                case GridOperation.Choose:
                    onItemsChosen?.Invoke(selectedItems);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }

            return false;
        }

        void Apply(GridOperation operation, EventBase sourceEvent)
        {
            if ((operation & operationMask) != 0 && Apply(operation, (sourceEvent as IKeyboardEvent)?.shiftKey ?? false))
            {
                sourceEvent?.StopPropagation();
                sourceEvent?.PreventDefault();
            }
        }

        /// <summary>
        /// Internal use only.
        /// </summary>
        /// <param name="operation"></param>
        internal void Apply(GridOperation operation)
        {
            Apply(operation, null);
        }

        void OnDraggerStarted(PointerMoveEvent evt)
        {
            dragStarted?.Invoke(evt);
        }

        void OnDraggerMoved(PointerMoveEvent evt)
        {
            dragUpdated?.Invoke(evt);
        }

        void OnDraggerEnded(PointerUpEvent evt)
        {
            dragFinished?.Invoke(evt);
        }

        void OnDraggerCanceled()
        {
            dragCanceled?.Invoke();

            CancelSoftSelect();
        }

        /// <summary>
        /// Cancel drag operation.
        /// </summary>
        public void CancelDrag()
        {
            m_Dragger.Cancel();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            var operation = evt.keyCode switch
            {
                KeyCode.A when evt.actionKey => GridOperation.SelectAll,
                KeyCode.Escape => GridOperation.Cancel,
                KeyCode.Home => GridOperation.Begin,
                KeyCode.End => GridOperation.End,
                KeyCode.UpArrow => GridOperation.Up,
                KeyCode.DownArrow => GridOperation.Down,
                KeyCode.LeftArrow => GridOperation.Left,
                KeyCode.RightArrow => GridOperation.Right,
                KeyCode.KeypadEnter or KeyCode.Return => GridOperation.Choose,
                _ => GridOperation.None
            };

            Apply(operation, evt);
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            evt.StopPropagation();
            evt.PreventDefault();
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            evt.StopPropagation();
            evt.PreventDefault();
        }

        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to bind, and the index of the
        /// element to bind it to.
        /// </remarks>
        public Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                m_BindItem = value;
                Refresh();
            }
        }

        /// <summary>
        /// The number of columns for this grid.
        /// </summary>
        public int columnCount
        {
            get => m_ColumnCount;

            set
            {
                if (m_ColumnCount != value && value > 0)
                {
                    m_ScrollOffset = 0;
                    m_ColumnCount = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// A mask describing available operations in this <see cref="GridView"/> when the user interacts with it.
        /// </summary>
        public GridOperation operationMask { get; set; } =
            GridOperation.SelectAll | GridOperation.Cancel |
            GridOperation.Begin | GridOperation.End |
            GridOperation.Left | GridOperation.Right |
            GridOperation.Up | GridOperation.Down;

        /// <summary>
        /// Returns the content container for the <see cref="GridView"/>. Because the GridView control automatically manages
        /// its content, this always returns null.
        /// </summary>
        public override VisualElement contentContainer => null;

        /// <summary>
        /// The height of a single item in the list, in pixels.
        /// </summary>
        /// <remarks>
        /// GridView requires that all visual elements have the same height so that it can calculate the
        /// scroller size.
        ///
        /// This property must be set for the list view to function.
        /// </remarks>
        public int itemHeight
        {
            get => m_ItemHeight;
            set
            {
                if (m_ItemHeight != value && value > 0)
                {
                    m_ItemHeightIsInline = true;
                    m_ItemHeight = value;
                    scrollView.verticalPageSize = m_ItemHeight * k_PageSizeFactor;
                    Refresh();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public float itemWidth => scrollView.contentViewport.layout.width / columnCount;

        /// <summary>
        /// The data source for list items.
        /// </summary>
        /// <remarks>
        /// This list contains the items that the <see cref="GridView"/> displays.
        ///
        /// This property must be set for the list view to function.
        /// </remarks>
        public IList itemsSource
        {
            get => m_ItemsSource;
            set
            {
                if (m_ItemsSource is INotifyCollectionChanged oldCollection) oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;

                m_ItemsSource = value;
                if (m_ItemsSource is INotifyCollectionChanged newCollection) newCollection.CollectionChanged += OnItemsSourceCollectionChanged;

                Refresh();
            }
        }

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        /// <remarks>
        /// This callback needs to call a function that constructs a blank <see cref="VisualElement"/> that is
        /// bound to an element from the list.
        ///
        /// The GridView automatically creates enough elements to fill the visible area, and adds more if the area
        /// is expanded. As the user scrolls, the GridView cycles elements in and out as they appear or disappear.
        ///
        ///  This property must be set for the list view to function.
        /// </remarks>
        public Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                if (m_MakeItem == value)
                    return;
                m_MakeItem = value;
                Refresh();
            }
        }

        /// <summary>
        /// The computed pixel-aligned height for the list elements.
        /// </summary>
        /// <remarks>
        /// This value changes depending on the current panel's DPI scaling.
        /// </remarks>
        /// <seealso cref="GridView.itemHeight"/>
        public float resolvedItemHeight
        {
            get
            {
                var dpiScaling = 1f;
                return Mathf.Round(itemHeight * dpiScaling) / dpiScaling;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public float resolvedItemWidth
        {
            get
            {
                var dpiScaling = 1f;
                return Mathf.Round(itemWidth * dpiScaling) / dpiScaling;
            }
        }

        /// <summary>
        /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
        /// first selected item's index. If multiple items are provided, sets them all as selected.
        /// </summary>
        public int selectedIndex
        {
            get => m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First();
            set => SetSelection(value);
        }

        /// <summary>
        /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        public IEnumerable<int> selectedIndices => m_SelectedIndices;

        /// <summary>
        /// Returns the selected item from the data source. If multiple items are selected, returns the first selected item.
        /// </summary>
        public object selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();

        /// <summary>
        /// Returns the selected items from the data source. Always returns an enumerable, even if no item is selected, or a single
        /// item is selected.
        /// </summary>
        public IEnumerable<object> selectedItems => m_SelectedItems;

        /// <summary>
        /// Returns the IDs of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        public IEnumerable<int> selectedIds => m_SelectedIds;

        /// <summary>
        /// Controls the selection type.
        /// </summary>
        /// <remarks>
        /// You can set the GridView to make one item selectable at a time, make multiple items selectable, or disable selections completely.
        ///
        /// When you set the GridView to disable selections, any current selection is cleared.
        /// </remarks>
        public SelectionType selectionType
        {
            get => m_SelectionType;
            set
            {
                m_SelectionType = value;
                if (m_SelectionType == SelectionType.None || (m_SelectionType == SelectionType.Single && m_SelectedIndices.Count > 1)) ClearSelection();
            }
        }

        /// <summary>
        /// Returns true if the soft-selection is in progress.
        /// </summary>
        internal bool isSelecting => m_SoftSelectIndex != -1;

        /// <summary>
        /// Enable this property to display a border around the GridView.
        /// </summary>
        /// <remarks>
        /// If set to true, a border appears around the ScrollView.
        /// </remarks>
        public bool showBorder
        {
            get => ClassListContains(k_BorderUssClassName);
            set => EnableInClassList(k_BorderUssClassName, value);
        }

        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to unbind, and the index of the
        /// element to unbind it from.
        /// </remarks>
        public Action<VisualElement, int> unbindItem { get; set; }

        internal Func<int, int> getItemId
        {
            get => m_GetItemId;
            set
            {
                m_GetItemId = value;
                Refresh();
            }
        }

        internal List<RecycledRow> rowPool => m_RowPool;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Refresh();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        /// <summary>
        /// Callback triggered when the user acts on a selection of one or more items, for example by double-clicking or pressing Enter.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// </remarks>
        public event Action<IEnumerable<object>> onItemsChosen;

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        public event Action<IEnumerable<object>> onSelectionChange;

        /// <summary>
        /// Callback triggered when the user right-clicks on an item.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        public event Action<PointerDownEvent> onContextClick;

        /// <summary>
        /// Callback triggered when drag has started.
        /// </summary>
        public event Action<PointerMoveEvent> dragStarted;

        /// <summary>
        /// Callback triggered when items are dragged.
        /// </summary>
        public event Action<PointerMoveEvent> dragUpdated;

        /// <summary>
        /// Callback triggered when drag has finished.
        /// </summary>
        public event Action<PointerUpEvent> dragFinished;

        /// <summary>
        /// Callback triggered when drag has been canceled.
        /// </summary>
        public event Action dragCanceled;

        /// <summary>
        /// Adds an item to the collection of selected items.
        /// </summary>
        /// <param name="index">Item index.</param>
        public void AddToSelection(int index)
        {
            AddToSelection(new[] { index });
        }

        internal void AddToSelection(int index, bool notify)
        {
            AddToSelection(new[] { index }, notify);
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            ClearSelectionWithoutNotify();
            NotifyOfSelectionChange();
        }

        public void ClearSelectionWithoutNotify()
        {
            if (!HasValidDataAndBindings() || m_SelectedIds.Count == 0)
                return;

            ClearSelectionWithoutValidation();
            m_RangeSelectionOrigin = -1;
        }

        /// <summary>
        /// Clears the GridView, recreates all visible visual elements, and rebinds all items.
        /// </summary>
        /// <remarks>
        /// Call this method whenever the data source changes.
        /// </remarks>
        public void Refresh()
        {
            foreach (var recycledRow in m_RowPool) recycledRow.Clear();

            m_RowPool.Clear();
            scrollView.Clear();
            m_VisibleRowCount = 0;

            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            // O(n)
            if (m_SelectedIds.Count > 0)

                // Add selected objects to working lists.
                for (var index = 0; index < m_ItemsSource.Count; ++index)
                {
                    if (!m_SelectedIds.Contains(GetIdFromIndex(index))) continue;

                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(m_ItemsSource[index]);
                }

            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = scrollView.layout.height;

            if (float.IsNaN(m_LastHeight))
                return;

            m_FirstVisibleIndex = Math.Min((int)(m_ScrollOffset / resolvedItemHeight) * columnCount, m_ItemsSource.Count - 1);
            ResizeHeight(m_LastHeight);
        }

        /// <summary>
        /// Removes an item from the collection of selected items.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RemoveFromSelection(int index)
        {
            RemoveFromSelectionInternal(index);
        }

        internal void RemoveFromSelectionInternal(int index, bool notify = true)
        {
            if (!HasValidDataAndBindings())
                return;

            RemoveFromSelectionWithoutValidation(index);
            if (notify)
                NotifyOfSelectionChange();
        }

        /// <summary>
        /// Scrolls to a specific item index and makes it visible.
        /// </summary>
        /// <param name="index">Item index to scroll to. Specify -1 to make the last item visible.</param>
        public void ScrollToItem(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            if (m_VisibleRowCount == 0 || index < -1)
                return;

            var pixelAlignedItemHeight = resolvedItemHeight;
            var lastRowIndex = Mathf.FloorToInt((itemsSource.Count - 1) / (float)columnCount);
            var maxOffset = Mathf.Max(0, lastRowIndex * pixelAlignedItemHeight - m_LastHeight + pixelAlignedItemHeight);
            var targetRowIndex = Mathf.FloorToInt(index / (float)columnCount);
            var targetOffset = targetRowIndex * pixelAlignedItemHeight;
            var currentOffset = scrollView.scrollOffset.y;
            var d = targetOffset - currentOffset;

            if (index == -1)
            {
                scrollView.scrollOffset = Vector2.up * maxOffset;
            }
            else if (d < 0)
            {
                scrollView.scrollOffset = Vector2.up * targetOffset;
            }
            else if (d > m_LastHeight - pixelAlignedItemHeight)
            {
                // need to scroll up so the item should be visible in last row
                targetOffset += pixelAlignedItemHeight - m_LastHeight;
                scrollView.scrollOffset = Vector2.up * Mathf.Min(maxOffset, targetOffset);
            }

            // else do nothing because the item is already entirely visible

            schedule.Execute(() => ResizeHeight(m_LastHeight)).ExecuteLater(2L);
        }

        /// <summary>
        /// Sets the currently selected item.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void SetSelection(int index)
        {
            if (index < 0 || itemsSource == null || index >= itemsSource.Count)
            {
                ClearSelection();
                return;
            }

            SetSelection(new[] { index });
        }

        /// <summary>
        /// Sets a collection of selected items.
        /// </summary>
        /// <param name="indices">The collection of the indices of the items to be selected.</param>
        public void SetSelection(IEnumerable<int> indices)
        {
            switch (selectionType)
            {
                case SelectionType.None:
                    return;
                case SelectionType.Single:
                    if (indices != null)
                        indices = new[] { indices.Last() };
                    break;
                case SelectionType.Multiple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetSelectionInternal(indices, true);
        }

        /// <summary>
        /// Sets a collection of selected items without triggering a selection change callback.
        /// </summary>
        /// <param name="indices">The collection of items to be selected.</param>
        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        internal void AddToSelection(IList<int> indexes, bool notify = true)
        {
            if (!HasValidDataAndBindings() || indexes == null || indexes.Count == 0)
                return;

            foreach (var index in indexes) AddToSelectionWithoutValidation(index);

            if (notify)
                NotifyOfSelectionChange();

            //SaveViewData();
        }

        internal void SelectAll()
        {
            if (!HasValidDataAndBindings())
                return;

            if (selectionType != SelectionType.Multiple) return;

            for (var index = 0; index < itemsSource.Count; index++)
            {
                var id = GetIdFromIndex(index);
                var item = m_ItemsSource[index];

                foreach (var recycledRow in m_RowPool)
                    if (recycledRow.ContainsId(id, out var indexInRow))
                        recycledRow.SetSelected(indexInRow, true);

                if (!m_SelectedIds.Contains(id))
                {
                    m_SelectedIds.Add(id);
                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(item);
                }
            }

            NotifyOfSelectionChange();

            //SaveViewData();
        }

        internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
        {
            if (!HasValidDataAndBindings() || indices == null)
                return;

            ClearSelectionWithoutValidation();
            foreach (var index in indices) AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();

            //SaveViewData();
        }

        void AddToSelectionWithoutValidation(int index)
        {
            if (index < 0 || index >= m_ItemsSource.Count || m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledRow in m_RowPool)
                if (recycledRow.ContainsId(id, out var indexInRow))
                    recycledRow.SetSelected(indexInRow, true);

            m_SelectedIds.Add(id);
            m_SelectedIndices.Add(index);
            m_SelectedItems.Add(item);
        }

        internal VisualElement GetVisualElement(int index)
        {
            if (index < 0 || index >= m_ItemsSource.Count || !m_SelectedIndices.Contains(index))
                return null;

            return GetVisualElementInternal(index);
        }

        internal VisualElement GetVisualElementWithoutSelection(int index)
        {
            if (index < 0 || index >= m_ItemsSource.Count)
                return null;

            return GetVisualElementInternal(index);
        }

        VisualElement GetVisualElementInternal(int index)
        {
            var id = GetIdFromIndex(index);

            foreach (var recycledRow in m_RowPool)
                if (recycledRow.ContainsId(id, out var indexInRow))
                    return recycledRow.ElementAt(indexInRow);

            return null;
        }

        void ClearSelectionWithoutValidation()
        {
            foreach (var recycledRow in m_RowPool) recycledRow.ClearSelection();

            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();
        }

        VisualElement CreateDummyItemElement()
        {
            var item = new VisualElement();
            SetupItemElement(item);
            return item;
        }

        void DoRangeSelection(int rangeSelectionFinalIndex, bool notify = true)
        {
            m_RangeSelectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Max() : m_SelectedIndices.Min();

            ClearSelectionWithoutValidation();

            // Add range
            var range = new List<int>();
            m_IsRangeSelectionDirectionUp = rangeSelectionFinalIndex < m_RangeSelectionOrigin;
            if (m_IsRangeSelectionDirectionUp)
                for (var i = rangeSelectionFinalIndex; i <= m_RangeSelectionOrigin; i++)
                    range.Add(i);
            else
                for (var i = rangeSelectionFinalIndex; i >= m_RangeSelectionOrigin; i--)
                    range.Add(i);

            AddToSelection(range, notify);
        }

        void DoContextClickAfterSelect(PointerDownEvent evt)
        {
            onContextClick?.Invoke(evt);
        }

        void DoSoftSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = GetIndexByPosition(localPosition);
            if (clickedIndex > m_ItemsSource.Count - 1)
                return;

            m_SoftSelectIndex = clickedIndex;

            var clickedItemId = GetIdFromIndex(clickedIndex);
            switch (clickCount)
            {
                case 2:
                    break;
                default:
                    if (selectionType == SelectionType.None)
                        return;

                    if (selectionType == SelectionType.Multiple && actionKey)
                    {
                        m_RangeSelectionOrigin = clickedIndex;

                        // Add/remove single clicked element
                        if (m_SelectedIds.Contains(clickedItemId))
                            RemoveFromSelectionInternal(clickedIndex, false);
                        else
                            AddToSelection(clickedIndex, false);
                    }
                    else if (selectionType == SelectionType.Multiple && shiftKey)
                    {
                        if (m_RangeSelectionOrigin == -1 || !selectedItems.Any())
                        {
                            m_RangeSelectionOrigin = clickedIndex;
                            SetSelectionInternal(new[] { clickedIndex }, false);
                        }
                        else
                        {
                            DoRangeSelection(clickedIndex, false);
                        }
                    }
                    else if (selectionType == SelectionType.Multiple && m_SelectedIndices.Contains(clickedIndex))
                    {
                        // Do noting, selection will be processed OnPointerUp.
                        // If drag and drop will be started GridViewDragger will capture the mouse and GridView will not receive the mouse up event.
                    }
                    else // single
                    {
                        m_RangeSelectionOrigin = clickedIndex;
                        if (!(m_SelectedIndices.Count == 1 && m_SelectedIndices[0] == clickedIndex)) SetSelectionInternal(new[] { clickedIndex }, false);
                    }

                    break;
            }

            ScrollToItem(clickedIndex);
        }

        int GetIdFromIndex(int index)
        {
            if (m_GetItemId == null)
                return index;
            return m_GetItemId(index);
        }

        bool HasValidDataAndBindings()
        {
            return itemsSource != null && makeItem != null && bindItem != null;
        }

        void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            onSelectionChange?.Invoke(m_SelectedItems);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            scrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            scrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            scrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            scrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);
            scrollView.contentContainer.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            scrollView.contentContainer.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int height;
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out height))
                if (m_ItemHeight != height)
                {
                    m_ItemHeight = height;
                    Refresh();
                }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            scrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            scrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            scrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            scrollView.contentContainer.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            scrollView.contentContainer.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            scrollView.contentContainer.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            m_HasPointerMoved = true;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            evt.StopImmediatePropagation();
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            var capturingElement = panel?.GetCapturingElement(evt.pointerId);

            if (capturingElement is VisualElement ve &&
                ve != scrollView.contentContainer &&
                ve.FindCommonAncestor(scrollView.contentContainer) != null)
                return;

            m_OriginalSelection = new List<int>(m_SelectedIndices);
            m_OriginalScrollOffset = m_ScrollOffset;
            m_SoftSelectIndex = -1;

            var clickCount = m_HasPointerMoved ? 1 : evt.clickCount;
            m_HasPointerMoved = false;

            DoSoftSelect(evt.localPosition, clickCount, evt.actionKey, evt.shiftKey);

            if (evt.button == (int)MouseButton.RightMouse)
                DoContextClickAfterSelect(evt);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            var clickedIndex = GetIndexByPosition(evt.localPosition);
            if (m_SoftSelectIndex != -1 && worldBound.Contains(evt.position) && clickedIndex == m_SoftSelectIndex)
                NotifyOfSelectionChange();
            else
                CancelSoftSelect();

            m_SoftSelectIndex = -1;
        }

        void CancelSoftSelect()
        {
            if (m_SoftSelectIndex != -1)
            {
                SetSelectionInternal(m_OriginalSelection, false);
                scrollView.verticalScroller.value = m_OriginalScrollOffset;
            }

            m_SoftSelectIndex = -1;
        }

        public int GetIndexByPosition(Vector2 localPosition)
        {
            return Mathf.FloorToInt(localPosition.y / resolvedItemHeight) * columnCount + Mathf.FloorToInt(localPosition.x / resolvedItemWidth);
        }

        void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Refresh();
        }

        void OnScroll(float offset)
        {
            if (!HasValidDataAndBindings())
                return;

            m_ScrollOffset = offset;
            var pixelAlignedItemHeight = resolvedItemHeight;
            var firstVisibleIndex = Mathf.FloorToInt(offset / pixelAlignedItemHeight) * columnCount;

            scrollView.contentContainer.style.paddingTop = Mathf.FloorToInt(firstVisibleIndex / (float)columnCount) * pixelAlignedItemHeight;
            scrollView.contentContainer.style.height = Mathf.CeilToInt(itemsSource.Count / (float)columnCount) * pixelAlignedItemHeight;

            if (firstVisibleIndex != m_FirstVisibleIndex)
            {
                m_FirstVisibleIndex = firstVisibleIndex;

                if (m_RowPool.Count > 0)
                {
                    // we try to avoid rebinding a few items
                    if (m_FirstVisibleIndex < m_RowPool[0].firstIndex) //we're scrolling up
                    {
                        //How many do we have to swap back
                        var count = m_RowPool[0].firstIndex - m_FirstVisibleIndex;

                        var inserting = m_ScrollInsertionList;

                        for (var i = 0; i < count && m_RowPool.Count > 0; ++i)
                        {
                            var last = m_RowPool[m_RowPool.Count - 1];
                            inserting.Add(last);
                            m_RowPool.RemoveAt(m_RowPool.Count - 1); //we remove from the end

                            last.SendToBack(); //We send the element to the top of the list (back in z-order)
                        }

                        inserting.Reverse();

                        m_ScrollInsertionList = m_RowPool;
                        m_RowPool = inserting;
                        m_RowPool.AddRange(m_ScrollInsertionList);
                        m_ScrollInsertionList.Clear();
                    }
                    else if (m_FirstVisibleIndex > m_RowPool[0].firstIndex) //down
                    {
                        var inserting = m_ScrollInsertionList;

                        var checkIndex = 0;
                        while (checkIndex < m_RowPool.Count && m_FirstVisibleIndex > m_RowPool[checkIndex].firstIndex)
                        {
                            var first = m_RowPool[checkIndex];
                            inserting.Add(first);
                            first.BringToFront(); //We send the element to the bottom of the list (front in z-order)
                            checkIndex++;
                        }

                        m_RowPool.RemoveRange(0, checkIndex); //we remove them all at once
                        m_RowPool.AddRange(inserting); // add them back to the end
                        inserting.Clear();
                    }

                    //Let's rebind everything
                    for (var rowIndex = 0; rowIndex < m_RowPool.Count; rowIndex++)
                        for (var colIndex = 0; colIndex < columnCount; colIndex++)
                        {
                            var index = rowIndex * columnCount + colIndex + m_FirstVisibleIndex;

                            if (index < itemsSource.Count)
                            {
                                var item = m_RowPool[rowIndex].ElementAt(colIndex);
                                if (m_RowPool[rowIndex].indices[colIndex] == RecycledRow.kUndefinedIndex)
                                {
                                    var newItem = makeItem != null ? makeItem.Invoke() : CreateDummyItemElement();
                                    SetupItemElement(newItem);
                                    m_RowPool[rowIndex].RemoveAt(colIndex);
                                    m_RowPool[rowIndex].Insert(colIndex, newItem);
                                    item = newItem;
                                }

                                Setup(item, index);
                            }
                            else
                            {
                                var remainingOldItems = columnCount - colIndex;

                                while (remainingOldItems > 0)
                                {
                                    m_RowPool[rowIndex].RemoveAt(colIndex);
                                    m_RowPool[rowIndex].Insert(colIndex, CreateDummyItemElement());
                                    m_RowPool[rowIndex].ids.RemoveAt(colIndex);
                                    m_RowPool[rowIndex].ids.Insert(colIndex, RecycledRow.kUndefinedIndex);
                                    m_RowPool[rowIndex].indices.RemoveAt(colIndex);
                                    m_RowPool[rowIndex].indices.Insert(colIndex, RecycledRow.kUndefinedIndex);
                                    remainingOldItems--;
                                }
                            }
                        }
                }
            }
        }

        void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            ResizeHeight(evt.newRect.height);
        }

        void ProcessSingleClick(int clickedIndex)
        {
            m_RangeSelectionOrigin = clickedIndex;
            SetSelection(clickedIndex);
        }

        void RemoveFromSelectionWithoutValidation(int index)
        {
            if (!m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledRow in m_RowPool)
                if (recycledRow.ContainsId(id, out var indexInRow))
                    recycledRow.SetSelected(indexInRow, false);

            m_SelectedIds.Remove(id);
            m_SelectedIndices.Remove(index);
            m_SelectedItems.Remove(item);
        }

        void ResizeHeight(float height)
        {
            if (!HasValidDataAndBindings())
                return;

            var pixelAlignedItemHeight = resolvedItemHeight;
            var rowCountForSource = Mathf.CeilToInt(itemsSource.Count / (float)columnCount);
            var contentHeight = rowCountForSource * pixelAlignedItemHeight;
            scrollView.contentContainer.style.height = contentHeight;

            var scrollableHeight = Mathf.Max(0, contentHeight - scrollView.contentViewport.layout.height);
            scrollView.verticalScroller.highValue = scrollableHeight;
            scrollView.verticalScroller.value = Mathf.Min(m_ScrollOffset, scrollView.verticalScroller.highValue);

            var rowCountForHeight = Mathf.FloorToInt(height / pixelAlignedItemHeight) + k_ExtraVisibleRows;
            var rowCount = Math.Min(rowCountForHeight, rowCountForSource);

            if (m_VisibleRowCount != rowCount)
            {
                if (m_VisibleRowCount > rowCount)
                {
                    // Shrink
                    var removeCount = m_VisibleRowCount - rowCount;
                    for (var i = 0; i < removeCount; i++)
                    {
                        var lastIndex = m_RowPool.Count - 1;
                        m_RowPool[lastIndex].Clear();
                        scrollView.Remove(m_RowPool[lastIndex]);
                        m_RowPool.RemoveAt(lastIndex);
                    }
                }
                else
                {
                    // Grow
                    var addCount = rowCount - m_VisibleRowCount;
                    for (var i = 0; i < addCount; i++)
                    {
                        var recycledRow = new RecycledRow(resolvedItemHeight);

                        for (var indexInRow = 0; indexInRow < columnCount; indexInRow++)
                        {
                            var index = m_RowPool.Count * columnCount + indexInRow + m_FirstVisibleIndex;
                            var item = makeItem != null && index < itemsSource.Count ? makeItem.Invoke() : CreateDummyItemElement();
                            SetupItemElement(item);

                            recycledRow.Add(item);

                            if (index < itemsSource.Count)
                            {
                                Setup(item, index);
                            }
                            else
                            {
                                recycledRow.ids.Add(RecycledRow.kUndefinedIndex);
                                recycledRow.indices.Add(RecycledRow.kUndefinedIndex);
                            }
                        }

                        m_RowPool.Add(recycledRow);
                        recycledRow.style.height = pixelAlignedItemHeight;

                        scrollView.Add(recycledRow);
                    }
                }

                m_VisibleRowCount = rowCount;
            }

            m_LastHeight = height;
        }

        void Setup(VisualElement item, int newIndex)
        {
            var newId = GetIdFromIndex(newIndex);

            if (!(item.parent is RecycledRow recycledRow))
                throw new Exception("The item to setup can't be orphan");

            var indexInRow = recycledRow.IndexOf(item);

            if (recycledRow.indices.Count <= indexInRow)
            {
                recycledRow.indices.Add(RecycledRow.kUndefinedIndex);
                recycledRow.ids.Add(RecycledRow.kUndefinedIndex);
            }

            if (recycledRow.indices[indexInRow] == newIndex)
                return;

            if (recycledRow.indices[indexInRow] != RecycledRow.kUndefinedIndex)
                unbindItem?.Invoke(item, recycledRow.indices[indexInRow]);

            recycledRow.indices[indexInRow] = newIndex;
            recycledRow.ids[indexInRow] = newId;

            bindItem.Invoke(item, recycledRow.indices[indexInRow]);

            recycledRow.SetSelected(indexInRow, m_SelectedIds.Contains(recycledRow.ids[indexInRow]));
        }

        void SetupItemElement(VisualElement item)
        {
            item.AddToClassList(k_ItemUssClassName);
            item.style.position = Position.Relative;
            item.style.flexBasis = 0;
            item.style.flexGrow = 1f;
            item.style.flexShrink = 1f;
        }

        /// <summary>
        /// Instantiates a <see cref="GridView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<GridView, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="GridView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the GridView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            readonly UxmlIntAttributeDescription m_ItemHeight = new() { name = "item-height", obsoleteNames = new[] { "itemHeight" }, defaultValue = k_DefaultItemHeight };

            readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new() { name = "selection-type", defaultValue = SelectionType.Single };

            readonly UxmlBoolAttributeDescription m_ShowBorder = new() { name = "show-border", defaultValue = false };

            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initializes <see cref="GridView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var view = (GridView)ve;

                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (m_ItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight)) view.itemHeight = itemHeight;

                view.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                view.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
            }
        }

        internal class RecycledRow : VisualElement
        {
            public const int kUndefinedIndex = -1;

            public readonly List<int> ids;

            public readonly List<int> indices;

            public RecycledRow(float height)
            {
                AddToClassList(k_RowUssClassName);
                style.height = height;

                indices = new List<int>();
                ids = new List<int>();
            }

            public int firstIndex => indices.Count > 0 ? indices[0] : kUndefinedIndex;
            public int lastIndex => indices.Count > 0 ? indices[indices.Count - 1] : kUndefinedIndex;

            public void ClearSelection()
            {
                for (var i = 0; i < childCount; i++) SetSelected(i, false);
            }

            public bool ContainsId(int id, out int indexInRow)
            {
                indexInRow = ids.IndexOf(id);
                return indexInRow >= 0;
            }

            public bool ContainsIndex(int index, out int indexInRow)
            {
                indexInRow = indices.IndexOf(index);
                return indexInRow >= 0;
            }

            public void SetSelected(int indexInRow, bool selected)
            {
                if (childCount > indexInRow && indexInRow >= 0)
                {
                    if (selected)
                        ElementAt(indexInRow).AddToClassList(itemSelectedVariantUssClassName);
                    else
                        ElementAt(indexInRow).RemoveFromClassList(itemSelectedVariantUssClassName);
                }
            }
        }

        /// <summary>
        /// Manipulator that allows dragging from a <see cref="GridView"/> component.
        /// </summary>
        internal class GridViewDragger : PointerManipulator
        {
            /// <summary>
            /// The threshold in pixels after which a drag will start.
            /// </summary>
            public float dragThreshold { get; set; } = 8f;

            Action<PointerMoveEvent> m_DragStarted;

            Action<PointerMoveEvent> m_Dragging;

            Action<PointerUpEvent> m_DragEnded;

            Action m_DragCanceled;

            Vector3 m_StartPosition;

            GridView gridView => target as GridView;

            /// <summary>
            /// Whether the drag is currently active.
            /// </summary>
            public bool isActive { get; private set; }

            int m_PointerId = -1;

            /// <summary>
            /// Creates a new <see cref="GridViewDragger"/> instance.
            /// </summary>
            /// <param name="dragStarted"> The event that will be fired when a drag starts. </param>
            /// <param name="dragging"> The event that will be fired when a drag is in progress. </param>
            /// <param name="dragEnded"> The event that will be fired when a drag ends. </param>
            /// <param name="dragCanceled"> The event that will be fired when a drag is cancelled. </param>
            public GridViewDragger(Action<PointerMoveEvent> dragStarted, Action<PointerMoveEvent> dragging,
                Action<PointerUpEvent> dragEnded, Action dragCanceled)
            {
                m_DragStarted = dragStarted;
                m_Dragging = dragging;
                m_DragEnded = dragEnded;
                m_DragCanceled = dragCanceled;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                gridView.scrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
                gridView.scrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                gridView.scrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
                gridView.scrollView.contentContainer.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
                gridView.scrollView.contentContainer.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                gridView.scrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                activators.Clear();
                gridView.scrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                gridView.scrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                gridView.scrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                gridView.scrollView.contentContainer.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
                gridView.scrollView.contentContainer.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                gridView.scrollView.contentContainer.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }

            void OnPointerDown(PointerDownEvent evt)
            {
                isActive = false;
                if (CanStartManipulation(evt))
                {
                    if (!gridView.scrollView.contentContainer.HasPointerCapture(evt.pointerId))
                        gridView.scrollView.contentContainer.CapturePointer(evt.pointerId);
                    m_StartPosition = evt.position;
                    m_PointerId = evt.pointerId;
                }
            }

            void OnPointerMove(PointerMoveEvent evt)
            {
                if (evt.pointerId != m_PointerId)
                    return;

                if (gridView.scrollView.contentContainer.HasPointerCapture(evt.pointerId))
                {
                    if (!isActive)
                    {
                        var delta = evt.position - m_StartPosition;
                        if (delta.sqrMagnitude > dragThreshold * dragThreshold && gridView.m_SelectedIndices.Count > 0)
                        {
                            isActive = true;
                            m_DragStarted?.Invoke(evt);
                        }
                    }
                    else
                    {
                        m_Dragging?.Invoke(evt);
                    }
                }
            }

            void OnPointerUp(PointerUpEvent evt)
            {
                if (evt.pointerId != m_PointerId)
                    return;

                if (gridView.scrollView.contentContainer.HasPointerCapture(evt.pointerId))
                    gridView.scrollView.contentContainer.ReleasePointer(evt.pointerId);
                if (isActive) m_DragEnded?.Invoke(evt);

                isActive = false;
                m_PointerId = -1;
            }

            void OnPointerCancel(PointerCancelEvent evt)
            {
                if (evt.pointerId == m_PointerId)
                    Cancel();
            }

            void OnPointerCaptureOut(PointerCaptureOutEvent evt)
            {
                if (evt.pointerId == m_PointerId)
                    Cancel();
            }

            void OnKeyDown(KeyDownEvent evt)
            {
                if (m_PointerId == -1)
                    return;

                if (evt.keyCode == KeyCode.Escape)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    Cancel();
                }
            }

            /// <summary>
            /// Cancels the drag.
            /// </summary>
            public void Cancel()
            {
                if (gridView.scrollView.contentContainer.HasPointerCapture(m_PointerId))
                    gridView.scrollView.contentContainer.ReleasePointer(m_PointerId);

                if (isActive)
                {
                    isActive = false;
                    m_DragCanceled?.Invoke();
                }

                m_PointerId = -1;
            }
        }
    }
}