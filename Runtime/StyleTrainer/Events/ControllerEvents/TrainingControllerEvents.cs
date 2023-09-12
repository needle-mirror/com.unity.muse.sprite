using Unity.Muse.Sprite.Common.Events;

namespace Unity.Muse.StyleTrainer.Events.TrainingControllerEvents
{
    class StyleTrainingEvent : BaseEvent<StyleTrainingEvent>
    {
        public EState state;
        public StyleData styleData;
        public ETrainingState trainingState;
    }
}