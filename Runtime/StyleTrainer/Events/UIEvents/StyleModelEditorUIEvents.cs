using Unity.Muse.Sprite.Common.Events;

namespace Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents
{
    class ThumbnailSizeChangedEvent : BaseEvent<ThumbnailSizeChangedEvent>
    {
        public float thumbnailSize;
    }

    class CheckPointSelectionChangeEvent : BaseEvent<CheckPointSelectionChangeEvent>
    {
        public StyleData styleData;
        public int index;
    }

    class GenerateButtonClickEvent : BaseEvent<GenerateButtonClickEvent> { }

    class DuplicateButtonClickEvent : BaseEvent<DuplicateButtonClickEvent> { }

    class SetFavouriteCheckPointEvent : BaseEvent<SetFavouriteCheckPointEvent>
    {
        public StyleData styleData;
        public string checkPointGUID;
    }
}
