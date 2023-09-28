using System;
using System.Collections.Generic;
using StyleTrainer.Backend;
using Unity.Muse.Sprite.Common;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    [Serializable]
    class CheckPointData : Artifact<CheckPointData, CheckPointData>
    {
        [SerializeField]
        string m_ProjectID = Utilities.emptyGUID;
        public string name = StringConstants.newVersion;
        public string description = StringConstants.newVersion;
        public string parent_id = Utilities.emptyGUID;
        public string dropDownLabelName = StringConstants.newVersion;
        public TrainingSetData trainingSetData;
        public List<SampleOutputData> validationImagesData = new();
        [SerializeField]
        int m_TrainingSteps;
        GetCheckPointRestCall m_GetCheckPointRestCall;
        GetCheckPointStatusRestCall m_GetCheckPointStatusRestCall;

        // Arbitrary. Server limits to 256.
        public const int maxNameLength = 150;
        public const int maxDescriptionLength = 256;

        public CheckPointData(EState state, string guid, string projectID)
            : base(state)
        {
            m_ProjectID = projectID;
            this.guid = guid;
            trainingSetData = new TrainingSetData(state, Utilities.emptyGUID, projectID);
            m_TrainingSteps = StyleTrainerConfig.config.trainingStepRange.x;
        }

        public void SetName(string newName)
        {
            name = newName.Substring(0, Math.Min(newName.Length, maxNameLength));
        }

        public int trainingSteps
        {
            get
            {
                // cap training steps
                trainingSteps = m_TrainingSteps;
                return m_TrainingSteps;
            }
            set
            {
                value = (int)((float)value/StyleTrainerConfig.config.trainingStepsIncrement) * StyleTrainerConfig.config.trainingStepsIncrement;
                if (value > StyleTrainerConfig.config.trainingStepRange.y)
                    value = StyleTrainerConfig.config.trainingStepRange.y;
                if (value < StyleTrainerConfig.config.trainingStepRange.x)
                    value = StyleTrainerConfig.config.trainingStepRange.x;
                if (value != m_TrainingSteps)
                {
                    m_TrainingSteps = value;
                    DataChanged(this);
                }
            }
        }

        public override void OnDispose()
        {
            m_GetCheckPointRestCall?.Dispose();
            m_GetCheckPointRestCall = null;
            m_GetCheckPointStatusRestCall?.Dispose();
            m_GetCheckPointStatusRestCall = null;
            trainingSetData?.OnDispose();
            for (var i = 0; i < validationImagesData?.Count; ++i)
                validationImagesData[i]?.OnDispose();
            base.OnDispose();
        }

        public override void GetArtifact(Action<CheckPointData> onDoneCallback, bool useCache)
        {
            if (Utilities.ValidStringGUID(m_ProjectID) && Utilities.ValidStringGUID(guid))
            {
                OnArtifactLoaded += onDoneCallback;
                if (state == EState.Initial)
                {
                    state = EState.Loading;
                    if (m_GetCheckPointRestCall == null)
                    {
                        var checkPointRequest = new GetCheckPointRequest
                        {
                            checkpoint_guid = guid,
                            guid = m_ProjectID
                        };
                        m_GetCheckPointRestCall = new GetCheckPointRestCall(ServerConfig.serverConfig, checkPointRequest);
                        m_GetCheckPointRestCall.RegisterOnSuccess(OnGetCheckPointSuccess);
                        m_GetCheckPointRestCall.RegisterOnFailure(OnGetCheckPointFailure);
                    }

                    if (m_GetCheckPointStatusRestCall == null)
                    {
                        var request = new GetCheckPointStatusRequest
                        {
                            guid = m_ProjectID,
                            guids = new[] { guid }
                        };
                        m_GetCheckPointStatusRestCall = new GetCheckPointStatusRestCall(ServerConfig.serverConfig, request);
                        m_GetCheckPointStatusRestCall.RegisterOnSuccess(OnGetCheckPointStatusSuccess);
                        m_GetCheckPointStatusRestCall.RegisterOnFailure(OnGetCheckPointStatusFailure);
                    }

                    LoadCheckPoint();
                }
                else if (state != EState.Loading && state != EState.Training)
                {
                    ArtifactLoaded(this);
                }
            }
            else if (state != EState.New && state != EState.Training)
            {
                state = EState.Loaded;
                StyleTrainerDebug.Log($"Check point data incomplete. Unable to load. guid:{guid} asset_id:{m_ProjectID}");
                onDoneCallback?.Invoke(this);
            }
        }

        void LoadCheckPoint()
        {
            m_GetCheckPointRestCall?.SendRequest();
        }

        void OnGetCheckPointFailure(GetCheckPointRestCall obj)
        {
            if (obj.retriesFailed)
            {
                StyleTrainerDebug.LogError($"OnGetCheckPointFailure: Failed to create style. {obj.requestError} {obj.errorMessage}");
                state = EState.Error;
                ArtifactLoaded(this);
            }
        }

        void OnGetCheckPointSuccess(GetCheckPointRestCall arg1, GetCheckPointResponse arg2)
        {
            if (arg2.success)
            {
                StyleTrainerDebug.Log($"Loading checkpoint status {arg2.status} {arg2.checkpointID}");
                ProcessGetCheckPointResponse(arg2);
            }
            else
            {
                StyleTrainerDebug.LogError($"OnGetCheckPointSuccess: Request call success but response failed. {arg2.success}");
                state = EState.Error;
                ArtifactLoaded(this);
            }
        }

        void ProcessGetCheckPointResponse(GetCheckPointResponse getCheckPointResponse)
        {
            name = getCheckPointResponse.name;
            description = getCheckPointResponse.description;

            if (trainingSetData == null)
            {
                trainingSetData = new TrainingSetData(EState.Initial, getCheckPointResponse.trainingsetID, m_ProjectID);
            }
            else if (trainingSetData.guid != getCheckPointResponse.trainingsetID)
            {
                trainingSetData.guid = getCheckPointResponse.trainingsetID;
                trainingSetData.state = EState.Initial;
            }

            if (validationImagesData.Count != getCheckPointResponse.validation_image_prompts.Length)
            {
                validationImagesData.Clear();
                for (var i = 0; i < getCheckPointResponse.validation_image_prompts.Length; i++)
                    validationImagesData.Add(new SampleOutputData(EState.Initial, getCheckPointResponse.validation_image_prompts[i]));
            }

            if (getCheckPointResponse.status == "done")
            {
                if (getCheckPointResponse.validation_image_guids?.Length != getCheckPointResponse.validation_image_prompts?.Length)
                {
                    StyleTrainerDebug.Log($"Waiting for validation image {getCheckPointResponse.validation_image_guids?.Length} {getCheckPointResponse.validation_image_prompts?.Length}");
                    Scheduler.ScheduleCallback(ServerConfig.serverConfig.webRequestPollRate, LoadCheckPoint);
                }
                else
                {
                    state = EState.Loaded;
                    StoreSampleOutput(getCheckPointResponse);
                    ArtifactLoaded(this);
                }
            }
            else if (getCheckPointResponse.status == "working")
            {
                if (state != EState.Training)
                {
                    state = EState.Training;
                    DataChanged(this);
                }

                Scheduler.ScheduleCallback(ServerConfig.serverConfig.webRequestPollRate, GetCheckPointStatus);
            }
            else if (getCheckPointResponse.status == "failed")
            {
                state = EState.Error;
                StyleTrainerDebug.LogError($"Checkpoint training failed: assetid:{getCheckPointResponse.asset_id} styleid:{getCheckPointResponse.styleID} checkpointid:{getCheckPointResponse.checkpointID} error:{getCheckPointResponse.error}");
                StoreSampleOutput(getCheckPointResponse);
                ArtifactLoaded(this);
            }
        }

        void GetCheckPointStatus()
        {
            m_GetCheckPointStatusRestCall?.SendRequest();
        }

        void OnGetCheckPointStatusFailure(GetCheckPointStatusRestCall obj)
        {
            if (obj.retriesFailed)
            {
                StyleTrainerDebug.LogError($"OnGetCheckPointStatusFailure: Failed to get style status. {obj.requestError} {obj.errorMessage}");
                state = EState.Error;
                ArtifactLoaded(this);
            }
        }

        void OnGetCheckPointStatusSuccess(GetCheckPointStatusRestCall arg1, GetCheckPointStatusResponse arg2)
        {
            if (arg2.success)
            {
                for (var i = 0; i < arg2.results.Length; ++i)
                    if (arg2.results[i].guid == guid)
                    {
                        StyleTrainerDebug.Log($"Loading checkpoint status {arg2.results[i].guid} {arg2.results[i].status}");
                        if (arg2.results[i].status != "working")
                            Scheduler.ScheduleCallback(ServerConfig.serverConfig.webRequestPollRate, LoadCheckPoint);
                        else
                            Scheduler.ScheduleCallback(ServerConfig.serverConfig.webRequestPollRate, GetCheckPointStatus);
                        break;
                    }
            }
            else
            {
                StyleTrainerDebug.LogError($"OnGetCheckPointStatusSuccess: Request call success but response failed. {arg2.error} {guid}");
                Scheduler.ScheduleCallback(ServerConfig.serverConfig.webRequestPollRate, LoadCheckPoint);
            }
        }

        void StoreSampleOutput(GetCheckPointResponse p0)
        {
            for (var i = 0; i < p0.validation_image_prompts.Length && i < p0.validation_image_guids.Length; ++i)
            {
                var serverPrompt = p0.validation_image_prompts[i];
                var serverGUID = p0.validation_image_guids[i];

                if (Utilities.ValidStringGUID(serverGUID))
                {
                    // check if this guid is already assigned
                    int j;
                    for (j = 0; j < validationImagesData.Count; ++j)
                        if (validationImagesData[j].guid == serverGUID)
                            break;

                    if (j >= validationImagesData.Count)
                    {
                        // not found. Assign to a prompt
                        for (j = 0; j < validationImagesData.Count; ++j)
                            if (!Utilities.ValidStringGUID(validationImagesData[j].guid) &&
                                validationImagesData[j].prompt == serverPrompt)
                            {
                                validationImagesData[j].guid = serverGUID;
                                break;
                            }

                        if (j >= validationImagesData.Count) StyleTrainerDebug.LogWarning($"Prompt on server not found in local data. prompt:{serverPrompt} guid:{serverGUID}");
                    }
                }
            }
        }

        public void DuplicateNew(Action<CheckPointData> onDuplicateDone)
        {
            trainingSetData.DuplicateNew(x => DuplicateCheckPoint(x, onDuplicateDone));
        }

        void DuplicateCheckPoint(TrainingSetData trainingSetData, Action<CheckPointData> onDuplicateDone)
        {
            var checkPoint = new CheckPointData(EState.New, Utilities.emptyGUID, m_ProjectID)
            {
                name = $"{name}",
                description = description,
                parent_id = guid,
                dropDownLabelName = StringConstants.newVersion,
                trainingSetData = trainingSetData,
                validationImagesData = DuplicateNewValidationData()
            };

            onDuplicateDone?.Invoke(checkPoint);
        }

        List<SampleOutputData> DuplicateNewValidationData()
        {
            var list = new List<SampleOutputData>();
            for (var i = 0; i < validationImagesData.Count; ++i) list.Add(validationImagesData[i].Duplicate());

            return list;
        }

        public void Delete()
        {
            trainingSetData?.Delete();
            for (var i = 0; i < validationImagesData?.Count; ++i) validationImagesData[i]?.Delete();
        }
    }
}