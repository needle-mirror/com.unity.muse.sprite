using System;
using System.Collections.Generic;
using StyleTrainer.Backend;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer.Debug;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    [Serializable]
    class StyleData : Artifact<StyleData, StyleData>
    {
        [SerializeReference]
        ImageArtifact m_Thumbnail;
        [SerializeField]
        bool m_Visible = true;
        [SerializeField]
        public string selectedCheckPointGUID = Utilities.emptyGUID;
        [SerializeField]
        public string m_FavoriteCheckPointGUID = Utilities.emptyGUID;
        [SerializeField]
        public string parentID = Utilities.emptyGUID;
        [SerializeField]
        string m_ProjectID = Utilities.emptyGUID;
        [SerializeField]
        List<CheckPointData> m_CheckPoints = new()
        {
            // new CheckPointData(EState.Loading)
            // {
            //     guid = "1",
            //     dropDownLabelName = EState.Loading.ToString(),
            // },
            // new CheckPointData(EState.Loaded)
            // {
            //     guid = "2",
            //     dropDownLabelName = EState.Loaded.ToString(),
            // },
            // new CheckPointData(EState.Loaded)
            // {
            //     guid = "3",
            //     dropDownLabelName = EState.Loaded.ToString(),
            // },
            // // new CheckPointData()
            // // {
            // //     guid = "4",
            // //     description = EState.New.ToString(),
            // //     state = EState.New
            // // },
            // new CheckPointData(EState.Error)
            // {
            //     guid = "5",
            //     dropDownLabelName = EState.Error.ToString(),
            // },
            // new CheckPointData(EState.Training)
            // {
            //     guid = "6",
            //     dropDownLabelName = EState.Training.ToString(),
            // },
            // new CheckPointData(EState.Initial)
            // {
            //     guid = "7",
            //     dropDownLabelName = EState.Initial.ToString(),
            // },
        };

        public string projectID => m_ProjectID;

        public override void OnDispose()
        {
            m_Thumbnail?.OnDispose();
            for (var i = 0; i < m_CheckPoints?.Count; ++i)
                m_CheckPoints[i]?.OnDispose();
            base.OnDispose();
        }

        public bool InTrainingState()
        {
            for (var i = 0; i < m_CheckPoints.Count; ++i)
                if (m_CheckPoints[i].state == EState.Training)
                    return true;

            return false;
        }

        public string favoriteCheckPoint
        {
            get => m_FavoriteCheckPointGUID;
            set
            {
                if (m_FavoriteCheckPointGUID != value)
                {
                    m_FavoriteCheckPointGUID = value;
                    DataChanged(this);
                }
            }
        }

        public StyleData(EState state, string guid, string parentID, string projectId)
            : base(state)
        {
            m_CheckPoints.Add(new CheckPointData(EState.New, Utilities.emptyGUID, projectId));
            m_CheckPoints[0].parent_id = parentID;
            title = "New Style";
            description = "Description of style";
            this.guid = guid;
            this.parentID = parentID;
            m_ProjectID = projectId;
        }

        public int SelectedCheckPointIndex()
        {
            var index = 0;
            for (; index < m_CheckPoints.Count; ++index)
                if (m_CheckPoints[index].guid == selectedCheckPointGUID)
                    return index;

            return 0;
        }

        public static StyleData CreateNewStyle(string projectID)
        {
            var styleData = new StyleData(EState.New, Utilities.emptyGUID, Utilities.emptyGUID, projectID);
            styleData.description = "Set a description here to help you remember what this style is for.";
            styleData.title = "New Style";
            styleData.guid = string.Empty;
            styleData.m_Thumbnail = new ImageArtifact(EState.New);
            return styleData;
        }

        public CheckPointData GetFavouriteOrLatestCheckPoint()
        {
            if (Utilities.ValidStringGUID(favoriteCheckPoint))
                for (var i = 0; i < m_CheckPoints.Count; ++i)
                    if (m_CheckPoints[i].guid == favoriteCheckPoint)
                        return m_CheckPoints[i];

            for (var i = m_CheckPoints.Count - 1; i >= 0; --i)
                if (m_CheckPoints[i].state == EState.Loaded)
                    return m_CheckPoints[i];

            return m_CheckPoints.Count > 0 ? m_CheckPoints[0] : null;
        }

        public CheckPointData GetSelectedCheckPoint()
        {
            var index = SelectedCheckPointIndex();
            if (index >= m_CheckPoints?.Count || index < 0)
                return null;
            return m_CheckPoints[SelectedCheckPointIndex()];
        }

        public string styleTitle => GetFavouriteOrLatestCheckPoint()?.name ?? "Style has no versions. Something is wrong";

        public string styleDescription => GetFavouriteOrLatestCheckPoint()?.description ?? "Style has no versions. Something is wrong";

        public string title
        {
            get => GetSelectedCheckPoint()?.name ?? "Style has no versions. Something is wrong";
            set
            {
                var selectedCheckPoint = GetSelectedCheckPoint();
                if (selectedCheckPoint != null)
                {
                    selectedCheckPoint.name = value;
                    DataChanged(this);
                }
            }
        }

        public string description
        {
            get => GetSelectedCheckPoint()?.description ?? "Style has no versions. Something is wrong";
            set
            {
                var selectedCheckPoint = GetSelectedCheckPoint();
                if (selectedCheckPoint != null)
                {
                    selectedCheckPoint.description = value;
                    DataChanged(this);
                }
            }
        }

        public bool visible
        {
            get => m_Visible;
            set
            {
                if (m_Visible != value)
                {
                    m_Visible = value;
                    DataChanged(this);
                }
            }
        }

        public ImageArtifact thumbnail => m_Thumbnail;

        public override void GetArtifact(Action<StyleData> onDoneCallback, bool useCache)
        {
            OnArtifactLoaded += onDoneCallback;
            if (state != EState.New)
            {
                if (state != EState.Loading)
                {
                    if (state != EState.Loaded && state != EState.Training)
                    {
                        state = EState.Loading;

                        LoadArtifact();
                    }
                    else
                    {
                        // ensure all checkpoints are loaded
                        for (var i = 0; i < m_CheckPoints?.Count; ++i)
                        {
                            m_CheckPoints[i].OnStateChanged += OnCheckPointStateChanged;
                            m_CheckPoints[i].GetArtifact(_ => { }, useCache);
                        }

                        OnCheckPointStateChanged(null);
                    }
                }
            }
            else
            {
                ArtifactLoaded(this);
            }
        }

        public IReadOnlyList<CheckPointData> checkPoints => m_CheckPoints;

        void LoadArtifact()
        {
            var getStyleRequest = new GetStyleRequest
            {
                style_guid = guid,
                guid = m_ProjectID
            };
            var getStyleRestCall = new GetStyleRestCall(ServerConfig.serverConfig, getStyleRequest);
            getStyleRestCall.RegisterOnSuccess(OnGetStyleSuccess);
            getStyleRestCall.RegisterOnFailure(OnGetStyleFailure);
            getStyleRestCall.SendRequest();
        }

        void OnGetStyleFailure(GetStyleRestCall obj)
        {
            if (obj.retriesFailed)
            {
                state = EState.Error;
                StyleTrainerDebug.Log($"Failed to load style {guid} {obj.errorMessage}");
                DataChanged(this);
                ArtifactLoaded(this);
            }
        }

        void OnGetStyleSuccess(GetStyleRestCall arg1, GetStyleResponse arg2)
        {
            if (arg2.success)
            {
                var oldCheckPoints = m_CheckPoints.ToArray();
                m_CheckPoints.Clear();
                title = arg2.name;
                description = arg2.desc;
                favoriteCheckPoint = SetStyleStateRestCall.activeState;
                visible = arg2.state == "active";
                if (arg2.checkpointIDs?.Length <= 0)
                {
                    StyleLoadSuccessNoCheckPoint(arg2.name, arg2.desc, arg2.prompts);
                }
                else
                {
                    for (var i = 0; i < arg2.checkpointIDs?.Length; ++i)
                    {
                        var checkPoint = new CheckPointData(EState.Initial, arg2.checkpointIDs[i], m_ProjectID);
                        var oldCheckPoint = Array.Find(oldCheckPoints, cp => cp.guid == checkPoint.guid);
                        if (oldCheckPoint != null)
                            checkPoint.trainingSteps = oldCheckPoint.trainingSteps;
                        m_CheckPoints.Add(checkPoint);
                        checkPoint.dropDownLabelName = string.Format(StringConstants.checkPointDropDownLabel, m_CheckPoints.Count);
                        checkPoint.OnStateChanged += OnCheckPointStateChanged;
                        checkPoint.GetArtifact(_ => { }, false);
                    }
                }
            }
            else
            {
                StyleTrainerDebug.LogWarning($"GetStyleResponse {guid} failed {arg2.success} {arg2.error}. Init to initial state");
                StyleLoadSuccessNoCheckPoint("New Style", "Set a description here to help you remember what this style is for.", null);
            }
        }

        void StyleLoadSuccessNoCheckPoint(string checkpointName, string checkPointDescription, string[] prompts)
        {
            state = EState.Loaded;

            m_CheckPoints.Clear();
            m_CheckPoints.Add(new CheckPointData(EState.New, Utilities.emptyGUID, m_ProjectID));
            m_CheckPoints[0].parent_id = parentID;
            m_CheckPoints[0].name = checkpointName?? "New Style";
            m_CheckPoints[0].description =  checkPointDescription ?? "Set a description here to you remember what this style is for.";
            m_CheckPoints[0].validationImagesData = new List<SampleOutputData>();
            for (int i = 0; i < prompts?.Length; ++i)
            {
                m_CheckPoints[0].validationImagesData.Add(new SampleOutputData(EState.New, prompts[i]));
            }
            ArtifactLoaded(this);
        }

        void OnCheckPointStateChanged(CheckPointData obj)
        {
            var hasTraining = false;
            for (var i = 0; i < m_CheckPoints.Count; ++i)
            {
                if (m_CheckPoints[i].state == EState.Initial || m_CheckPoints[i].state == EState.Loading)
                {
                    m_CheckPoints[i].OnStateChanged -= OnCheckPointStateChanged;
                    m_CheckPoints[i].OnStateChanged += OnCheckPointStateChanged;
                    m_CheckPoints[i].GetArtifact(_ => { }, false);
                    return;
                }
                m_CheckPoints[i].OnStateChanged -= OnCheckPointStateChanged;
                hasTraining |= m_CheckPoints[i].state == EState.Training;
            }

            state = hasTraining ? EState.Training : EState.Loaded;
            ArtifactLoaded(this);
            DataChanged(this);
        }

        void OnCheckPointLoaded(CheckPointData obj)
        {
            for (var i = 0; i < m_CheckPoints.Count; ++i)
                if (m_CheckPoints[i].state == EState.Initial || m_CheckPoints[i].state == EState.Loading)
                    return;

            state = EState.Loaded;
            ArtifactLoaded(this);
            DataChanged(this);
        }

        public void AddCheckPoint(CheckPointData checkPoint)
        {
            m_CheckPoints.Add(checkPoint);
            if (checkPoint.guid == Utilities.emptyGUID)
                checkPoint.OnGUIDChanged += OnCheckPointGUIDChanged;
        }

        void OnCheckPointGUIDChanged(CheckPointData obj)
        {
            if (selectedCheckPointGUID == Utilities.emptyGUID) selectedCheckPointGUID = obj.guid;

            obj.OnGUIDChanged -= OnCheckPointGUIDChanged;
        }

        void OnThumbnailChanged()
        {
            ArtifactLoaded(this);
            DataChanged(this);
        }

        public IList<SampleOutputData> sampleOutputData => GetSelectedCheckPoint()?.validationImagesData;
        public TrainingSetData trainingSetData => GetSelectedCheckPoint()?.trainingSetData;

        public void AddSampleOutput(SampleOutputData sampleOutput)
        {
            GetSelectedCheckPoint().validationImagesData.Add(sampleOutput);
            DataChanged(this);
        }

        public void AddTrainingData(TrainingData trainingData)
        {
            GetSelectedCheckPoint().trainingSetData.Add(trainingData);
            DataChanged(this);
        }

        public void RemoveCheckPointAt(int index)
        {
            if (m_CheckPoints.Count > index && index >= 0)
            {
                var checkPoint = m_CheckPoints[index];
                checkPoint.Delete();
                m_CheckPoints.RemoveAt(index);
                if (selectedCheckPointGUID == checkPoint.guid) selectedCheckPointGUID = checkPoint.parent_id;
                checkPoint.OnDispose();
            }
        }

        public void Delete()
        {
            for (var i = 0; i < m_CheckPoints?.Count; ++i)
                m_CheckPoints[i]?.Delete();
        }

        public bool HasTraining()
        {
            for (var i = 0; i < m_CheckPoints?.Count; ++i)
            {
                if (m_CheckPoints[i]?.state == EState.Training)
                    return true;
            }

            return false;
        }
    }
}