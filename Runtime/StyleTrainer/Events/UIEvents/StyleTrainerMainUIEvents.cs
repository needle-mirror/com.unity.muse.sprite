using System;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using UnityEngine;

namespace Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents
{
    class ShowDialogEvent : BaseEvent<ShowDialogEvent>
    {
        public string title;
        public string description;
        public Action confirmAction = () => { };
        public AlertSemantic semantic;
    }

    class ShowLoadingScreenEvent : BaseEvent<ShowLoadingScreenEvent>
    {
        public string description;
        public bool show;
    }
}
