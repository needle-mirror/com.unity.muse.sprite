using StyleTrainer.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Debug;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerProjectEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    class LoadStyleProjectEvent : BaseEvent<LoadStyleProjectEvent> { }

    class LoadingTask
    {
        StyleTrainerData m_Project;
        EventBus m_EventBus;

        public LoadingTask(StyleTrainerData styleData, EventBus eventBus)
        {
            m_Project = styleData;
            m_EventBus = eventBus;
        }

        public void Execute()
        {
            if (m_Project == null || !Utilities.ValidStringGUID(m_Project.guid))
            {
                StyleTrainerDebug.LogWarning("Unable to load project. Missing Project GUID");
                return;
            }

            m_Project.ClearStyles();
            m_EventBus.SendEvent(new StyleModelSourceChangedEvent
            {
                styleModels = m_Project.styles
            }, true);
            m_Project.state = EState.Loading;

            var getStylesRequest = new GetStylesRequest
            {
                guid = m_Project.guid
            };
            var getStylesRestCall = new GetStylesRestCall(ServerConfig.serverConfig, getStylesRequest);
            getStylesRestCall.RegisterOnSuccess(OnGetStylesSuccess);
            getStylesRestCall.RegisterOnFailure(OnGetStylesFailure);
            getStylesRestCall.SendRequest();
        }

        void OnGetStylesFailure(GetStylesRestCall obj)
        {
            m_Project.state = EState.Error;
            StyleTrainerDebug.LogError($"Unable to load style trainer project {m_Project.guid} {obj.requestError} {obj.errorMessage}");
        }

        void OnGetStylesSuccess(GetStylesRestCall arg1, GetStylesResponse arg2)
        {
            if (arg2.success)
            {
                m_Project.state = EState.Loaded;
                m_Project.UpdateVersion();
                m_EventBus.SendEvent(new SystemEvents
                {
                    state = SystemEvents.ESystemState.RequestSave
                });
                foreach (var id in arg2.styleIDs)
                {
                    var styleData = new StyleData(EState.Initial, id, Utilities.emptyGUID, m_Project.guid);
                    styleData.GetArtifact(OnGetStyleDone, false);
                    m_Project.AddStyle(styleData);
                }

                m_EventBus.SendEvent(new StyleModelSourceChangedEvent
                {
                    styleModels = m_Project.styles
                }, true);
            }
            else
            {
                StyleTrainerDebug.Log($"OnGetStylesSuccess but call failed. {m_Project.guid} {arg1.errorMessage}");
            }
        }

        void OnGetStyleDone(StyleData obj)
        {
            if (obj.state == EState.Loaded)
            {
                m_EventBus.SendEvent(new TrainingSetDataSourceChangedEvent
                {
                    styleData = obj,
                    trainingSetData = obj.trainingSetData
                });
                m_EventBus.SendEvent(new SampleOutputDataSourceChangedEvent
                {
                    styleData = obj,
                    sampleOutput = obj.sampleOutputData
                });
            }
        }
    }
}