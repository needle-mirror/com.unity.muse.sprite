using Unity.Muse.Sprite.Common.Events;

namespace Unity.Muse.StyleTrainer.Events.TrainingControllerEvents
{
    class SystemEvents : BaseEvent<StyleTrainingEvent>
    {
        public enum ESystemState
        {
            Dispose,
            Modified,
            RequestSave
        }

        public ESystemState state;
    }
}
