using System;
using System.Collections.Generic;
using System.Linq;
using StyleTrainer.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;

namespace Unity.Muse.StyleTrainer.Data
{
    interface IJob
    {
        public bool hasStarted { get; }
        public bool isCompleted { get; }
        public bool isSuccess { get; }

        event Action OnComplete;

        IList<IJob> successors { get; }

        void AddSuccessor(IJob successor);

        void Execute();
    }

    abstract class BaseJob : IJob
    {
        public bool hasStarted { get; private set; } = false;
        public bool isCompleted { get; private set; } = false;
        public bool isSuccess { get; private set; } = false;

        public event Action OnComplete;
        public IList<IJob> successors => m_Successors;
        List<IJob> m_Successors = new();

        public void Execute()
        {
            hasStarted = true;

            OnExecute();
        }

        public abstract void OnExecute();

        public void AddSuccessor(IJob successor)
        {
            m_Successors.Add(successor);
        }

        protected void Complete(bool success)
        {
            isCompleted = true;
            isSuccess = success;
            OnComplete?.Invoke();
        }
    }

    class GetStylesJob : BaseJob
    {
        readonly string m_Guid;

        int m_JobsToComplete;

        Dictionary<string, StyleData> m_StyleData;

        public Dictionary<string, StyleData> styleData => m_StyleData;

        int m_StyleCount;

        public GetStylesJob(string guid)
        {
            m_Guid = guid;
            m_StyleData = new Dictionary<string, StyleData>();
        }

        public override void OnExecute()
        {
            var getStylesRequest = new GetStylesRequest { guid = m_Guid };
            var getStylesRestCall = new GetStylesRestCall(ServerConfig.serverConfig, getStylesRequest);
            getStylesRestCall.RegisterOnSuccess(OnGetStylesSuccess);
            getStylesRestCall.RegisterOnFailure(OnGetStylesFailure);
            getStylesRestCall.SendRequest();
        }

        void OnGetStylesSuccess(GetStylesRestCall restCall, GetStylesResponse response)
        {
            m_StyleCount = response.styleIDs.Length;

            foreach (var styleId in response.styleIDs)
            {
                var getInfoJob = new GetStyleInfoJob(m_Guid, styleId);
                getInfoJob.OnComplete += () => OnCompleteGetInfo(getInfoJob);
                AddSuccessor(getInfoJob);
                getInfoJob.Execute();
            }
        }

        void OnCompleteGetInfo(GetStyleInfoJob getStyleInfoJob)
        {
            m_StyleData[getStyleInfoJob.styleId] = getStyleInfoJob.style;

            if (m_StyleCount == m_StyleData.Count)
                Complete(true);
        }

        void OnGetStylesFailure(GetStylesRestCall obj)
        {
            StyleTrainerDebug.Log($"GetStylesRestCall failed {obj.requestError}");

            Complete(false);
        }
    }

    class GetStyleInfoJob : BaseJob
    {
        public readonly string styleId;

        readonly string m_Guid;

        public StyleData style { get; private set; }

        int m_CheckpointCount;
        int m_TrainingSetCount;
        Dictionary<string, CheckPointData> m_CheckPoints = new();
        Dictionary<string, TrainingSetData> m_TrainingData = new();

        public GetStyleInfoJob(string projectGuid, string styleId)
        {
            m_Guid = projectGuid;
            this.styleId = styleId;
            style = new StyleData(EState.Loaded, styleId, Utilities.emptyGUID, projectGuid);
        }

        public override void OnExecute()
        {
            var getStyleRequest = new GetStyleRequest
            {
                guid = m_Guid,
                style_guid = styleId
            };
            var getStyleRestCall = new GetStyleRestCall(ServerConfig.serverConfig, getStyleRequest);
            getStyleRestCall.RegisterOnSuccess(OnGetStyleSuccess);
            getStyleRestCall.RegisterOnFailure(OnGetStyleFailure);
            getStyleRestCall.SendRequest();
        }

        void OnGetStyleFailure(GetStyleRestCall restCall)
        {
            StyleTrainerDebug.Log($"OnGetStyleFailure failed {restCall.requestError}");
            Complete(false);
        }

        void OnGetStyleSuccess(GetStyleRestCall restCall, GetStyleResponse response)
        {
            style.title = response.name;
            style.description = response.desc;
            if (response.checkpointIDs.Any(c => c == response.checkpoint))
                style.selectedCheckPointGUID = response.checkpoint;
            else if (response.checkpointIDs.Length > 0)
                style.selectedCheckPointGUID = response.checkpointIDs.Last();
            else
                StyleTrainerDebug.LogError($"style {style.title} id:({styleId}) doesn't have checkpoints.");

            Complete(true);

            //
            // m_CheckpointCount = response.checkpointIDs.Length;
            // foreach (var checkpointId in response.checkpointIDs)
            // {
            //     var getCheckPointInfoJob = new GetCheckPointInfoJob(m_Guid, checkpointId);
            //     getCheckPointInfoJob.OnComplete += OnSuccessorComplete;
            //     AddSuccessor(getCheckPointInfoJob);
            //     getCheckPointInfoJob.Execute();
            // }
            //
            // m_TrainingSetCount = response.trainingsetIDs.Length;
            // foreach (var trainingSetID in response.trainingsetIDs)
            // {
            //     var getTrainingSetDataInfoJob = new GetTrainingSetInfoJob(m_Guid, trainingSetID);
            //     getTrainingSetDataInfoJob.OnComplete += OnSuccessorComplete;
            //     AddSuccessor(getTrainingSetDataInfoJob);
            //     getTrainingSetDataInfoJob.Execute();
            // }
        }

        void OnSuccessorComplete()
        {
            if (m_CheckPoints.Count == m_CheckpointCount && m_TrainingData.Count == m_TrainingSetCount)
                Complete(true);
        }
    }

    //
    // internal class GetCheckPointInfoJob : BaseJob
    // {
    //     public readonly string checkpointId;
    //
    //     readonly string m_Guid;
    //
    //     public CheckPointData checkPointData;
    //
    //     public GetCheckPointInfoJob(string projectGuid, string checkpointId)
    //     {
    //         m_Guid = projectGuid;
    //         this.checkpointId = checkpointId;
    //     }
    //
    //     public override void OnExecute()
    //     {
    //         var getCheckPointRequest = new GetCheckPointRequest { guid = m_Guid, checkpoint_guid = checkpointId };
    //         var getCheckPointRestCall = new GetCheckPointRestCall(ServerConfig.serverConfig, getCheckPointRequest);
    //         getCheckPointRestCall.RegisterOnSuccess(OnGetCheckPointSuccess);
    //         getCheckPointRestCall.RegisterOnFailure(OnGetCheckPointFailure);
    //         getCheckPointRestCall.SendRequest();
    //     }
    //
    //     void OnGetCheckPointFailure(GetCheckPointRestCall restCall)
    //     {
    //         Debug.Log($"OnSetCheckPointFailure failed {restCall.requestError}");
    //         Complete(false);
    //     }
    //
    //     void OnGetCheckPointSuccess(GetCheckPointRestCall restCall, GetCheckPointResponse response)
    //     {
    //         if (!response.success)
    //         {
    //             Debug.Log($"OnSetCheckPointFailure failed {restCall.requestError}");
    //             Complete(false);
    //             return;
    //         }
    //
    //         checkPointData = new CheckPointData(EState.Loaded)
    //         {
    //             name =  response.name,
    //             guid = response.checkpointID,
    //             asset_id = response.asset_id,
    //             description =  response.description,
    //             parent_id = "",
    //         };
    //
    //         Complete(true);
    //     }
    // }
    //
    // internal class GetTrainingSetInfoJob : BaseJob
    // {
    //     public readonly string trainingSetId;
    //
    //     readonly string m_Guid;
    //
    //     public TrainingSetData trainingSetData { get; private set; }
    //
    //     public GetTrainingSetInfoJob(string projectGuid, string trainingSetId)
    //     {
    //         m_Guid = projectGuid;
    //         this.trainingSetId = trainingSetId;
    //     }
    //
    //     public override void OnExecute()
    //     {
    //         var getTrainingSetRequest = new GetTrainingSetRequest()
    //         {
    //             guid = m_Guid,
    //             training_set_guid = trainingSetId
    //         };
    //
    //         var getTrainingSetRestCall = new GetTrainingSetRestCall(ServerConfig.serverConfig, getTrainingSetRequest);
    //         getTrainingSetRestCall.RegisterOnSuccess(OnGetTrainingSetSuccess);
    //         getTrainingSetRestCall.RegisterOnFailure(OnGetTrainingSetFailure);
    //         getTrainingSetRestCall.SendRequest();
    //     }
    //
    //     void OnGetTrainingSetFailure(GetTrainingSetRestCall restCall)
    //     {
    //         Debug.Log($"OnGetTrainingSetFailure failed {restCall.requestError}");
    //         Complete(false);
    //     }
    //
    //     void OnGetTrainingSetSuccess(GetTrainingSetRestCall restCall, GetTrainingSetResponse response)
    //     {
    //         if (!response.success)
    //         {
    //             Debug.Log($"OnGetTrainingSetFailure failed {restCall.requestError}");
    //             return;
    //         }
    //
    //         Complete(true);
    //     }
    // }
}