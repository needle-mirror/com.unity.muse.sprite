#if UNITY_EDITOR
using UnityEditor;
#else
using Unity.Muse.StyleTrainer.EditorMockClass;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using StyleTrainer.Runtime.Debug;
using Unity.Muse.StyleTrainer;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StyleTrainer.Backend
{
    [Serializable]
    [FilePath("ProjectSettings/StyleTrainerMockData.asset", FilePathAttribute.Location.ProjectFolder)]
    class MockData : ScriptableSingleton<MockData>
    {
        [Serializable]
        public struct StylePrompt
        {
            public string guid;
            public string[] prompts;
        }

        // simulate training time
        [Serializable]
        public class VersionTraining
        {
            public string guid;
            public int ticks;
        }

        // Mock data config section
        // how much training time to reduce per call
        public int trainingTickSimulation = 1;
        // validation mock images
        public List<Texture2D> mockTextures = new();

        // Mock data section
        public StyleTrainerData project;
        public StyleTrainerData defaultStyleProject;
        public List<TrainingSetData> trainingSetData = new();
        public List<MockImageArtifact> imageArtifacts = new();
        public List<StylePrompt> stylePrompts = new();
        public List<VersionTraining> versionTrainings = new();


        public void Reset()
        {
            project = new StyleTrainerData(EState.Initial);
            project.Init();
            trainingSetData = new List<TrainingSetData>();
            imageArtifacts = new List<MockImageArtifact>();
            stylePrompts = new List<StylePrompt>();
            InitDefaultStyleProjectMockData();
        }

        void InitDefaultStyleProjectMockData()
        {
            defaultStyleProject = new StyleTrainerData(EState.Loaded);
            defaultStyleProject.guid = "MOCK-DEFAULT-PROJECT-GUID";
            var s = new StyleData(EState.Loaded, "MOCK-DEFAULT-PROJECT-GUID-Style-1",  defaultStyleProject.guid)
            {
                title = "Mock Style 1",
                description = "Mock Style 1 Description",
            };
            s.visible = true;
            while(s.checkPoints.Count > 0)
            {
                s.RemoveCheckPointAt(0);
            }

            var c = new CheckPointData(EState.Loaded, "MOCK-DEFAULT-PROJECT-GUID-Style-1-CheckPoint-1", defaultStyleProject.guid);
            c.SetName("Mock Checkpoint 1");
            c.SetDescription("Mock Checkpoint 1 Description");
            s.AddCheckPoint(c);

            c = new CheckPointData(EState.Loaded, "MOCK-DEFAULT-PROJECT-GUID-Style-1-CheckPoint-2", defaultStyleProject.guid);
            c.SetName("Mock Checkpoint 2");
            c.SetDescription("Mock Checkpoint 2 Description");
            s.AddCheckPoint(c);
            defaultStyleProject.AddStyle(s);
        }

        public void Init()
        {
            if (project is null)
            {
                project = new StyleTrainerData(EState.Initial);
                project.Init();
            }
            InitDefaultStyleProjectMockData();
        }

        public CreateStyleResponse CreateStyleRestCallMock(CreateStyleRequest req)
        {
            var style = new StyleData(EState.New, Guid.NewGuid().ToString(), Utilities.emptyGUID);
            project.AddStyle(style);
            stylePrompts.Add(new StylePrompt
            {
                guid = style.guid,
                prompts = req.prompts
            });
            var res = new CreateStyleResponse
            {
                success = true,
                guid = style.guid
            };

            return res;
        }

        void OnDisable()
        {
            instance.Save(false);
        }

        public GetStylesResponse GetStylesRestCallMock(GetStylesRequest req)
        {
            var p = req.guid == defaultStyleProject.guid ? defaultStyleProject : project;
            var res = new GetStylesResponse
            {
                success = true,
                styleIDs = p.styles.Select(style => style.guid).ToArray()
            };
            return res;
        }

        public SetStyleStateResponse SetStyleStateRestCallMock(SetStyleStateRequest req)
        {
            var d = project.styles.FirstOrDefault(x => x.guid == req.guid);
            if (d != null) d.visible = req.state == SetStyleStateRestCall.activeState;

            var res = new SetStyleStateResponse
            {
                success = true
            };
            return res;
        }

        public GetStyleResponse GetStyleRestCallMock(GetStyleRequest req)
        {
            var p = req.guid == defaultStyleProject.guid ? defaultStyleProject : project;
            var d = p.styles.FirstOrDefault(x => x.guid == req.style_guid);
            if (d != null)
            {
                var res = new GetStyleResponse
                {
                    checkpoint = d.favoriteCheckPoint,
                    checkpointIDs = d.checkPoints.Select(checkPoint => checkPoint.guid).ToArray(),
                    desc = d.description,
                    name = d.title,
                    prompts = d.sampleOutputPrompts.ToArray(),
                    success = true,
                    trainingsetIDs = trainingSetData.ConvertAll(x => x.guid).ToArray(),
                    state = d.visible ? SetStyleStateRestCall.activeState : SetStyleStateRestCall.inactiveState
                };
                return res;
            }

            return new GetStyleResponse();
        }

        public CreateTrainingSetResponse CreateTrainingSetRestCallMock(CreateTrainingSetRequest req)
        {
            var tds = new TrainingSetData(EState.New, Guid.NewGuid().ToString(), req.asset_id);
            trainingSetData.Add(tds);
            for (var i = 0; i < req.images.Length; i++)
            {
                var td = new TrainingData(EState.New, Guid.NewGuid().ToString());

                var imageArtifact = new MockImageArtifact(EState.New);
                td.SetImageArtifact(imageArtifact);
                imageArtifact.guid = Guid.NewGuid().ToString();
                var data = Convert.FromBase64String(req.images[i]);

                // if(!Directory.Exists("Assets/MockData"))
                //     Directory.CreateDirectory("Assets/MockData");
                // BackendUtilities.SaveBytesToFile($"Assets/MockData/{td.imageArtifact.guid}.png", data);
                imageArtifact.rawData = data;
                imageArtifacts.Add(imageArtifact);
                tds.Add(td);
            }

            var res = new CreateTrainingSetResponse()
            {
                success = true,
                guid = tds.guid
            };
            return res;
        }

        public GetTrainingSetResponse GetTrainingSetRestCallMock(GetTrainingSetRequest req)
        {
            var td = trainingSetData.FirstOrDefault(x => x.guid == req.training_set_guid);
            if (td != null)
            {
                var imageGuids = new string[td.Count];
                for (var i = 0; i < td.Count; ++i) imageGuids[i] = td[i].imageArtifact.guid;
                var res = new GetTrainingSetResponse()
                {
                    success = true,
                    asset_id = project.guid,
                    styleID = req.guid,
                    trainingsetID = req.training_set_guid,
                    training_image_guids = imageGuids
                };
                return res;
            }

            return new GetTrainingSetResponse()
            {
                success = false,
                error =$"{req.training_set_guid} not found"
            };
        }

        public GetCheckPointResponse GetCheckPointRestCallMock(GetCheckPointRequest req)
        {
            var p = req.guid == defaultStyleProject.guid ? defaultStyleProject : project;
            for (var i = 0; i < p.styles.Count; ++i)
            {
                var checkpoint = p.styles[i].checkPoints.FirstOrDefault(x => x.guid == req.checkpoint_guid);
                var styleId = p.styles[i].guid;
                if (checkpoint != null)
                {
                    var res = new GetCheckPointResponse
                    {
                        success = true,
                        asset_id = p.guid,
                        styleID = styleId,
                        trainingsetID = checkpoint.trainingSetData.guid,
                        checkpointID = checkpoint.guid,
                        name = checkpoint.name,
                        description = checkpoint.description,
                        validation_image_prompts = checkpoint.validationImageData.Select(x => x.prompt).ToArray(),
                        validation_image_guids = checkpoint.validationImageData.Where(x => Utilities.ValidStringGUID(x.imageArtifact.guid)).Select(x => x.imageArtifact.guid).ToArray(),
                        status = GetCheckPointStatusAsResponseString(checkpoint),
                        train_steps = checkpoint.trainingSteps
                    };

                    //simulate image generation from each call
                    if (checkpoint.state == EState.Loaded)
                    {
                        for (var j = 0; j < checkpoint.validationImageData.Count; ++j)
                        {
                            if (!Utilities.ValidStringGUID(checkpoint.validationImageData[j].imageArtifact.guid))
                            {
                                checkpoint.validationImageData[j].imageArtifact.guid = Guid.NewGuid().ToString();
                                var mockImage = new MockImageArtifact(EState.New);
                                mockImage.guid = checkpoint.validationImageData[j].imageArtifact.guid;
                                imageArtifacts.Add(mockImage);
                                break;
                            }
                        }
                    }

                    return res;
                }
            }

            return new GetCheckPointResponse()
            {
                success = false,
                error = $"{req.checkpoint_guid} not found"
            };
        }

        public SetCheckPointFavouriteResponse SetCheckPointFavouriteRestCallMock(SetCheckPointFavouriteRequest req)
        {
            var style = project.styles.FirstOrDefault(x => x.guid == req.style_guid);
            if (style != null)
            {
                style.favoriteCheckPoint = req.checkpoint_guid;
                var res = new SetCheckPointFavouriteResponse()
                {
                    success = true,
                    guid = req.checkpoint_guid
                };
                return res;
            }

            return new SetCheckPointFavouriteResponse()
            {
                success = false
            };
        }

        public byte[] GetImage(GetImageRequest request)
        {
            var image = imageArtifacts.FirstOrDefault(x => x.guid == request.guid);
            if (image != null)
            {
                var data = image.rawData;
                if (data == null || data.Length == 0)
                {
                    var r = Random.Range(0, mockTextures.Count - 1);
                    image.rawData = mockTextures[r].EncodeToPNG();
                }

                return image.rawData;
            }

            return Utilities.errorTexture.EncodeToPNG();
        }

        public GetImageURLResponse GetImageURL(GetImageRequest request)
        {
            return new GetImageURLResponse
            {
                url = request.guid,
                success = true
            };
        }

        public GetDefaultStyleProjectResponse GetDefaultStyleProject(GetDefaultStyleProjectRequest request)
        {
            var res = new GetDefaultStyleProjectResponse
            {
                success = true,
                guid = defaultStyleProject.guid
            };
            return res;
        }

        public CreateCheckPointV2Response CreateCheckPointV2RestCallMock(CreateCheckPointV2Request request)
        {
            const int k_TrainingSteps = 100;
            var style = project.styles.FirstOrDefault(x => x.guid == request.guid);
            if (style != null)
            {
                Debug.Log("=====Mock need to set training set and validation images=====");
                var prompts = stylePrompts.FirstOrDefault(x => x.guid == request.guid);
                var checkPointCounts = request.training_steps/ k_TrainingSteps;
                if (checkPointCounts == 0)
                    checkPointCounts = 1;
                string[] guids = new string[checkPointCounts];
                for (int i = 0; i < checkPointCounts; ++i)
                {
                    var checkPoint = new CheckPointData(EState.Training, Guid.NewGuid().ToString(), request.asset_id)
                    {
                        parent_id = request.resume_guid,
                        //m_TrainingSetData = trainingSetData.FirstOrDefault(x => x.guid == req.training_guid),
                        //m_ValidationImagesData = prompts.prompts.Select(x => new SampleOutputData(EState.New, x)).ToList()
                    };
                    versionTrainings.Add(new VersionTraining
                    {
                        guid = checkPoint.guid,
                        ticks = (i+1) * k_TrainingSteps
                    });
                    checkPoint.SetMockData(new TrainingSetData(EState.New, request.training_guid, request.asset_id),
                        prompts.prompts.Select(x => new SampleOutputData(EState.New, x)).ToList(),
                        (i +1) * k_TrainingSteps);
                    checkPoint.SetName(request.name);
                    checkPoint.SetDescription(request.description);
                    style.AddCheckPoint(checkPoint);
                    guids[i] = checkPoint.guid;
                }
                var res = new CreateCheckPointV2Response
                {
                    guids = guids,
                    success = true
                };
                return res;
            }

            return new CreateCheckPointV2Response
            {
                success = false,
                error =$"{request.guid} not found"
            };
        }

        string GetCheckPointStatusAsResponseString(CheckPointData cp)
        {
            var status = GetCheckPointResponse.Status.done;
            switch (cp.state)
            {
                case EState.Loaded:
                    status = GetCheckPointResponse.Status.done;
                    break;
                case EState.Error:
                    status = GetCheckPointResponse.Status.failed;
                    break;
                case EState.Training:
                    status = GetCheckPointResponse.Status.working;
                    break;
            }

            return status;
        }

        public GetCheckPointStatusResponse GetCheckPointStatus(GetCheckPointStatusRequest request)
        {
            var p = request.guid == defaultStyleProject.guid ? defaultStyleProject : project;

            if (p != null)
            {
                List<GetCheckPointStatusResponse.CheckPointStatus> checkPointStatuses = new List<GetCheckPointStatusResponse.CheckPointStatus>();
                for (int j = 0; j < p.styles.Count; ++j)
                {
                    var style = p.styles[j];
                    for (int i = 0; i < style.checkPoints.Count; ++i)
                    {
                        var cp = style.checkPoints[i];
                        if (request.guids.Contains(cp.guid))
                        {
                            if (cp.state == EState.Training)
                            {
                                var vt = versionTrainings.FirstOrDefault(x => x.guid == cp.guid);
                                if (vt == null)
                                {
                                    cp.state = EState.Loaded;
                                }
                                else
                                {
                                    vt.ticks -= trainingTickSimulation;
                                    if (vt.ticks <= 0)
                                    {
                                        cp.state = EState.Loaded;
                                        versionTrainings.Remove(vt);
                                    }
                                }
                            }

                            checkPointStatuses.Add(new GetCheckPointStatusResponse.CheckPointStatus()
                            {
                                guid = cp.guid,
                                status = GetCheckPointStatusAsResponseString(cp)
                            });
                        }
                    }
                }


                return new GetCheckPointStatusResponse()
                {
                    success = true,
                    results = checkPointStatuses.ToArray()
                };
            }
            return new GetCheckPointStatusResponse
            {
                success = false,
                error =$"{request.guid} not found"
            };
        }
    }
}