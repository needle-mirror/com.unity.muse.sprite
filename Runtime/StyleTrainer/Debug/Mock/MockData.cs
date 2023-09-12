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

        public StyleTrainerData project;
        public List<TrainingSetData> trainingSetData = new();
        public List<MockImageArtifact> imageArtifacts = new();
        public List<StylePrompt> stylePrompts = new();

        public List<Texture2D> mockTextures = new();

        public void Reset()
        {
            project = new StyleTrainerData(EState.Initial);
            project.Init();
            trainingSetData = new List<TrainingSetData>();
            imageArtifacts = new List<MockImageArtifact>();
            stylePrompts = new List<StylePrompt>();
        }

        public void Init()
        {
            if (project is null)
            {
                project = new StyleTrainerData(EState.Initial);
                project.Init();
            }
        }

        public CreateStyleResponse CreateStyleRestCallMock(CreateStyleRequest req)
        {
            var style = new StyleData(EState.New, Guid.NewGuid().ToString(), req.parent_id, Utilities.emptyGUID);
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

        public GetStylesResponse GetStylesRestCallMock(GetStylesRequest _)
        {
            var res = new GetStylesResponse
            {
                success = true,
                styleIDs = project.styles.Select(style => style.guid).ToArray()
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
            var d = project.styles.FirstOrDefault(x => x.guid == req.guid);
            if (d != null)
            {
                var res = new GetStyleResponse
                {
                    checkpoint = d.favoriteCheckPoint,
                    checkpointIDs = d.checkPoints.Select(checkPoint => checkPoint.guid).ToArray(),
                    desc = d.description,
                    name = d.title,
                    prompts = d.sampleOutputData.Select(sampleOutput => sampleOutput.prompt).ToArray(),
                    success = true,
                    trainingsetIDs = trainingSetData.ConvertAll(x => x.guid).ToArray()
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

        public CreateCheckPointResponse CreateCheckPointRestCallMock(CreateCheckPointRequest req)
        {
            var style = project.styles.FirstOrDefault(x => x.guid == req.guid);
            if (style != null)
            {
                var prompts = stylePrompts.FirstOrDefault(x => x.guid == req.guid);
                var checkPoint = new CheckPointData(EState.New, Guid.NewGuid().ToString(), req.asset_id)
                {
                    name = req.name,
                    description = req.description,
                    parent_id = req.resume_guid,
                    trainingSetData = trainingSetData.FirstOrDefault(x => x.guid == req.training_guid),
                    validationImagesData = prompts.prompts.Select(x => new SampleOutputData(EState.New, x)).ToList()
                };

                style.AddCheckPoint(checkPoint);
                var res = new CreateCheckPointResponse
                {
                    guid = checkPoint.guid,
                    success = true
                };
                return res;
            }

            return new CreateCheckPointResponse
            {
                success = false,
                error =$"{req.guid} not found"
            };
        }

        public GetCheckPointResponse GetCheckPointRestCallMock(GetCheckPointRequest req)
        {
            for (var i = 0; i < project.styles.Count; ++i)
            {
                var checkpoint = project.styles[i].checkPoints.FirstOrDefault(x => x.guid == req.checkpoint_guid);
                var styleId = project.styles[i].guid;
                if (checkpoint != null)
                {
                    var res = new GetCheckPointResponse
                    {
                        success = true,
                        asset_id = project.guid,
                        styleID = styleId,
                        trainingsetID = checkpoint.trainingSetData.guid,
                        checkpointID = checkpoint.guid,
                        name = checkpoint.name,
                        description = checkpoint.description,
                        validation_image_prompts = checkpoint.validationImagesData.ConvertAll(x => x.prompt).ToArray(),
                        validation_image_guids = checkpoint.validationImagesData.Where(x => Utilities.ValidStringGUID(x.imageArtifact.guid)).Select(x => x.imageArtifact.guid).ToArray(),
                        status = "done"
                    };

                    //simulate image generation from each call
                    for (var j = 0; j < checkpoint.validationImagesData.Count; ++j)
                        if (!Utilities.ValidStringGUID(checkpoint.validationImagesData[j].imageArtifact.guid))
                        {
                            checkpoint.validationImagesData[i].imageArtifact.guid = Guid.NewGuid().ToString();
                            var mockImage = new MockImageArtifact(EState.New);
                            mockImage.guid = checkpoint.validationImagesData[j].imageArtifact.guid;
                            imageArtifacts.Add(mockImage);
                            break;
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
    }
}