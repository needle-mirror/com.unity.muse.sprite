using System;
using StyleTrainer.Backend;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.StyleTrainer.Debug;
using Unity.Muse.StyleTrainer.Events.CheckPointModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputModelEvents;
using Unity.Muse.StyleTrainer.Events.SampleOutputUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEditorUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleModelListUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerProjectEvents;
using Unity.Muse.StyleTrainer.Events.TrainingControllerEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetModelEvents;
using Unity.Muse.StyleTrainer.Events.TrainingSetUIEvents;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Muse.StyleTrainer
{
    [Serializable]
    class StyleModelController: IDisposable
    {
        StyleTrainerData m_StyleTrainerData;
        StyleTrainerMainUI m_StyleTrainerMainUI;
        StyleData m_CurrentSelectedStyle;
        TrainingController m_TrainingController;
        SignInController m_SignInController;
        SystemEvents m_ModifiedEvent = new()
        {
            state = SystemEvents.ESystemState.Modified
        };

        public EventBus eventBus = new();

        public void InitView(StyleTrainerMainUI styleTrainerMainUI)
        {
            eventBus.RegisterEvent<LoadStyleProjectEvent>(OnLoadStyleProject);
            eventBus.RegisterEvent<AddStyleButtonClickedEvent>(OnAddStyleClicked);
            eventBus.RegisterEvent<AddSampleOutputEvent>(OnAddSampleOutputEvent);
            eventBus.RegisterEvent<AddTrainingSetEvent>(OnAddTrainingSetEvent);
            eventBus.RegisterEvent<DeleteSampleOutputEvent>(OnDeleteSampleOutputEvent);
            eventBus.RegisterEvent<DeleteTrainingSetEvent>(OnDeleteTrainingSetEvent);
            eventBus.RegisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChangedEvent);
            eventBus.RegisterEvent<SetFavouriteCheckPointEvent>(OnSetFavouriteCheckPoint);
            eventBus.RegisterEvent<StyleVisibilityButtonClickedEvent>(OnStyleVisibilityChanged);
            eventBus.RegisterEvent<CheckPointSelectionChangeEvent>(ControlPointSelectionChanged);
            eventBus.RegisterEvent<DuplicateButtonClickEvent>(OnDuplicateClicked);
            eventBus.RegisterEvent<StyleDeleteButtonClickedEvent>(OnStyleDeleteButtonClicked);

            m_StyleTrainerMainUI = styleTrainerMainUI;
            m_StyleTrainerMainUI.SetEventBus(eventBus);
            m_TrainingController = new TrainingController(eventBus);
            m_TrainingController.SetStyleData(m_StyleTrainerData, m_CurrentSelectedStyle);
            m_SignInController = new SignInController();
            m_SignInController.Init(eventBus);
        }

        void OnLoadStyleProject(LoadStyleProjectEvent arg0)
        {
            eventBus.SendEvent(new ShowLoadingScreenEvent
            {
                description = "Loading Style Project Data...",
                show = true
            });
            m_StyleTrainerData.OnStateChanged += OnStyleTrainerProjectStateChanged;
            var loadingTask = new LoadingTask(m_StyleTrainerData, eventBus);
            loadingTask.Execute();
        }

        void OnStyleTrainerProjectStateChanged(StyleTrainerData obj)
        {
            if (m_StyleTrainerData.state == EState.Loaded || m_StyleTrainerData.state == EState.Error)
            {
                m_StyleTrainerData.OnStateChanged -= OnStyleTrainerProjectStateChanged;
                eventBus.SendEvent(new ShowLoadingScreenEvent
                {
                    show = false
                });
            }
        }

        void OnStyleDeleteButtonClicked(StyleDeleteButtonClickedEvent arg0)
        {
            var dialog = new AlertDialog
            {
                title = "Style Trainer",
                description = $"Are you sure you want to delete the \"{arg0.styleData.title}\" style?",
                variant = AlertSemantic.Destructive
            };
            dialog.SetPrimaryAction(730, "Delete", () =>
            {
                m_StyleTrainerData.RemoveStyle(arg0.styleData);
                eventBus.SendEvent(new StyleModelSourceChangedEvent
                {
                    styleModels = m_StyleTrainerData.styles
                }, true);
                SendModifiedEvent();
            });
            dialog.SetCancelAction(1, "Cancel");
            var modal = Modal.Build(m_StyleTrainerMainUI, dialog);
            modal.Show();
        }

        void OnDuplicateClicked(DuplicateButtonClickEvent arg0)
        {
            var index = m_CurrentSelectedStyle.SelectedCheckPointIndex();
            var checkPoint = m_CurrentSelectedStyle.checkPoints[index];
            if (checkPoint.state == EState.New && m_CurrentSelectedStyle.checkPoints.Count > 1)
            {
                var dialog = new AlertDialog
                {
                    title = "Style Trainer",
                    description = $"Are you sure you want to discard the new untrained version?",
                    variant = AlertSemantic.Destructive
                };
                dialog.SetPrimaryAction(730, "Discard", () =>
                {
                    m_CurrentSelectedStyle.RemoveCheckPointAt(index);
                    eventBus.SendEvent(new CheckPointSourceDataChangedEvent
                    {
                        styleData = m_CurrentSelectedStyle
                    });
                });
                dialog.SetCancelAction(1, "Cancel");
                var modal = Modal.Build(m_StyleTrainerMainUI, dialog);
                modal.Show();
            }
            else if (checkPoint.state == EState.Loaded)
            {
                DuplicateStyle(checkPoint);
            }
            else
            {
                StyleTrainerDebug.Log("Duplicate button clicked on a weird state");
            }

            SendModifiedEvent();
        }

        void DuplicateStyle(CheckPointData checkPointData)
        {
            eventBus.SendEvent(new ShowLoadingScreenEvent
            {
                description = "Duplicating Style...",
                show = true
            });
            checkPointData.trainingSetData.DuplicateNew(x => OnDuplicateStyleCheckPointDone(x, checkPointData));
        }

        void OnDuplicateStyleCheckPointDone(TrainingSetData obj, CheckPointData checkPointData)
        {
            var styleData = new StyleData(EState.New, Utilities.emptyGUID, checkPointData.guid, m_StyleTrainerData.guid)
            {
                title = $"{checkPointData.name} (duplicate)",
                description = checkPointData.description,
                visible = true
            };
            for (var i = 0; i < checkPointData.validationImagesData?.Count; ++i)
            {
                var sd = new SampleOutputData(EState.New, checkPointData.validationImagesData[i].prompt);
                styleData.AddSampleOutput(sd);
            }

            styleData.GetSelectedCheckPoint().trainingSetData = obj;
            AddStyle(styleData);
            eventBus.SendEvent(new ShowLoadingScreenEvent
            {
                description = "Duplicating Style...",
                show = false
            });
        }

        void ControlPointSelectionChanged(CheckPointSelectionChangeEvent arg0)
        {
            var checkpoint = arg0.styleData.checkPoints[arg0.index];
            arg0.styleData.selectedCheckPointGUID = checkpoint.guid;
            UpdateUIActionButtonState(arg0.styleData);
            eventBus.SendEvent(new SampleOutputDataSourceChangedEvent
            {
                sampleOutput = checkpoint.validationImagesData,
                styleData = arg0.styleData
            });
            eventBus.SendEvent(new TrainingSetDataSourceChangedEvent
            {
                trainingSetData = checkpoint.trainingSetData,
                styleData = arg0.styleData
            });
        }

        void OnStyleVisibilityChanged(StyleVisibilityButtonClickedEvent arg0)
        {
            arg0.styleData.visible = arg0.visible;
            var setStyleState = new SetStyleStateRestCall(ServerConfig.serverConfig, new SetStyleStateRequest
            {
                guid = m_StyleTrainerData.guid,
                style_guid = arg0.styleData.guid,
                state = arg0.visible ? SetStyleStateRestCall.activeState : SetStyleStateRestCall.inactiveState
            });
            setStyleState.RegisterOnFailure(OnSetStyleStateFailure);
            setStyleState.RegisterOnSuccess(OnSetStyleStateSuccess);
            setStyleState.SendRequest();
        }

        void OnSetStyleStateSuccess(SetStyleStateRestCall arg1, SetStyleStateResponse arg2)
        {
            SendModifiedEvent();
        }

        void OnSetStyleStateFailure(SetStyleStateRestCall obj)
        {
            StyleTrainerDebug.Log($"OnSetStyleStateFailure {obj.request.guid} {obj.request.state}");
        }

        void OnSetFavouriteCheckPoint(SetFavouriteCheckPointEvent arg0)
        {
            var checkPointGUID = arg0.checkPointGUID;
            if (!Utilities.ValidStringGUID(checkPointGUID))
            {
                // set it to the newest checkpoint
                if (arg0.styleData.checkPoints.Count <= 0)
                {
                    StyleTrainerDebug.LogWarning($"Unable to set favourite checkpoint, no checkpoints available {arg0.styleData.guid}");
                    return;
                }

                checkPointGUID = arg0.styleData.checkPoints[^1].guid;
            }

            arg0.styleData.favoriteCheckPoint = checkPointGUID;

            var setFavCheckPointRequest = new SetCheckPointFavouriteRequest
            {
                checkpoint_guid = checkPointGUID,
                style_guid = arg0.styleData.guid,
                guid = m_StyleTrainerData.guid
            };
            eventBus.SendEvent(new FavouriteCheckPointChangeEvent
            {
                styleData = arg0.styleData
            });
            var setFavCheckPointRestCall = new SetCheckPointFavouriteRestCall(ServerConfig.serverConfig, setFavCheckPointRequest);
            setFavCheckPointRestCall.RegisterOnFailure(OnSetFavouriteCheckPointFailure);
            setFavCheckPointRestCall.RegisterOnSuccess(OnSetFavouriteCheckPointSuccess);
            setFavCheckPointRestCall.SendRequest();

            SendModifiedEvent();
        }

        void OnSetFavouriteCheckPointSuccess(SetCheckPointFavouriteRestCall arg1, SetCheckPointFavouriteResponse arg2)
        {
            StyleTrainerDebug.Log($"OnSetFavouriteCheckPointSuccess {arg2.guid}");
        }

        void OnSetFavouriteCheckPointFailure(SetCheckPointFavouriteRestCall obj)
        {
            StyleTrainerDebug.Log($"OnSetFavouriteCheckPointFailure {obj.request.checkpoint_guid} {obj.errorMessage}");
        }

        void OnStyleModelListSelectionChangedEvent(StyleModelListSelectionChangedEvent arg0)
        {
            m_CurrentSelectedStyle = arg0.styleData;
            m_TrainingController.SetStyleData(m_StyleTrainerData, m_CurrentSelectedStyle);
            UpdateUIActionButtonState(arg0.styleData);
        }

        void OnDeleteTrainingSetEvent(DeleteTrainingSetEvent arg0)
        {
            var e = m_CurrentSelectedStyle.trainingSetData[arg0.indices[0]];
            e.Delete();
            e.OnDispose();
            m_CurrentSelectedStyle.trainingSetData.RemoveAt(arg0.indices[0]);
            UpdateUIActionButtonState(arg0.styleData);
            eventBus.SendEvent(new TrainingSetDataSourceChangedEvent
            {
                styleData = m_CurrentSelectedStyle,
                trainingSetData = m_CurrentSelectedStyle.trainingSetData
            });
            SendModifiedEvent();
        }

        void OnDeleteSampleOutputEvent(DeleteSampleOutputEvent arg0)
        {
            m_CurrentSelectedStyle.sampleOutputData.RemoveAt(arg0.deleteIndex);
            UpdateUIActionButtonState(arg0.styleData);
            eventBus.SendEvent(new SampleOutputDataSourceChangedEvent
            {
                styleData = m_CurrentSelectedStyle,
                sampleOutput = m_CurrentSelectedStyle.sampleOutputData
            });
            SendModifiedEvent();
        }

        void UpdateUIActionButtonState(StyleData styleData)
        {
            if (m_CurrentSelectedStyle?.guid != styleData?.guid)
                return;
            if (m_CurrentSelectedStyle is not null && m_CurrentSelectedStyle.state != EState.Loading)
            {
                var config = StyleTrainerConfig.config;
                if (m_CurrentSelectedStyle.trainingSetData?.Count < config.minTrainingSetSize ||
                    m_CurrentSelectedStyle.sampleOutputData?.Count < config.minSampleSetSize)
                {
                    eventBus.SendEvent(new DuplicateButtonStateUpdateEvent
                    {
                        state = false
                    });
                }
                else
                {
                    eventBus.SendEvent(new GenerateButtonStateUpdateEvent
                    {
                        state = true
                    });
                    eventBus.SendEvent(new DuplicateButtonStateUpdateEvent
                    {
                        state = m_CurrentSelectedStyle.checkPoints?.Count > 0
                    });
                }
            }
        }

        void OnAddTrainingSetEvent(AddTrainingSetEvent arg0)
        {
            var imageSizeWarning = false;
            for (var i = 0; i < arg0.textures.Count; ++i)
            {
                var ia = new ImageArtifact(EState.New);
                var tex = arg0.textures[i];

                var capWidth = Math.Min(tex.width, StyleTrainerConfig.config.maxTrainingImageSize.x);
                capWidth = Math.Max(StyleTrainerConfig.config.minTrainingImageSize.x, capWidth);
                var capHeight = Math.Min(tex.height, StyleTrainerConfig.config.maxTrainingImageSize.y);
                capHeight = Math.Max(StyleTrainerConfig.config.minTrainingImageSize.y, capHeight);
                if (imageSizeWarning == false && (tex.width != capWidth || tex.height != capHeight)) imageSizeWarning = true;

                // todo: rescale with aspect ratio
                tex = BackendUtilities.CreateTemporaryDuplicate(tex, capWidth, capHeight, TextureFormat.RGB24);
                ia.SetTexture(tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                var td = new TrainingData(EState.New, Utilities.emptyGUID);
                td.SetImageArtifact(ia);
                arg0.styleData.AddTrainingData(td);
            }

            eventBus.SendEvent(new TrainingSetDataSourceChangedEvent
            {
                styleData = arg0.styleData,
                trainingSetData = arg0.styleData.trainingSetData
            }, true);
            UpdateUIActionButtonState(arg0.styleData);
            if (imageSizeWarning)
                eventBus.SendEvent(new ShowDialogEvent
                {
                    title = "Add Training Set",
                    description = "One or more images were resized to fit the training image size requirement.",
                    semantic = AlertSemantic.Information,
                    confirmAction = () => { }
                });
            SendModifiedEvent();
        }

        void OnAddSampleOutputEvent(AddSampleOutputEvent arg0)
        {
            arg0.styleData.AddSampleOutput(new SampleOutputData(EState.New, ""));
            eventBus.SendEvent(new SampleOutputDataSourceChangedEvent
            {
                styleData = arg0.styleData,
                sampleOutput = arg0.styleData.sampleOutputData
            }, true);
            UpdateUIActionButtonState(arg0.styleData);
            SendModifiedEvent();
        }

        void SendModifiedEvent()
        {
            eventBus.SendEvent(m_ModifiedEvent);
        }

        void OnAddStyleClicked(AddStyleButtonClickedEvent evt)
        {
            var model = StyleData.CreateNewStyle(m_StyleTrainerData.guid);
            AddStyle(model);
            SendModifiedEvent();
        }

        void AddStyle(StyleData style)
        {
            m_StyleTrainerData.AddStyle(style);
            eventBus.SendEvent(new StyleModelSourceChangedEvent
            {
                selectedIndex = m_StyleTrainerData.styles.Count - 1,
                styleModels = m_StyleTrainerData.styles
            }, true);
        }

        public void SetModel(StyleTrainerData asset)
        {
            if (asset is not null)
            {
                m_StyleTrainerData = asset;
                eventBus.SendEvent(new StyleModelSourceChangedEvent
                {
                    selectedIndex = 0,
                    styleModels = asset.styles
                }, true);
                if (m_StyleTrainerData.version != StyleTrainerData.k_Version
                    || m_StyleTrainerData.state == EState.Initial)
                    OnLoadStyleProject(new LoadStyleProjectEvent());
            }
        }

        public void Dispose()
        {
            eventBus.UnregisterEvent<LoadStyleProjectEvent>(OnLoadStyleProject);
            eventBus.UnregisterEvent<AddStyleButtonClickedEvent>(OnAddStyleClicked);
            eventBus.UnregisterEvent<AddSampleOutputEvent>(OnAddSampleOutputEvent);
            eventBus.UnregisterEvent<AddTrainingSetEvent>(OnAddTrainingSetEvent);
            eventBus.UnregisterEvent<DeleteSampleOutputEvent>(OnDeleteSampleOutputEvent);
            eventBus.UnregisterEvent<DeleteTrainingSetEvent>(OnDeleteTrainingSetEvent);
            eventBus.UnregisterEvent<StyleModelListSelectionChangedEvent>(OnStyleModelListSelectionChangedEvent);
            eventBus.UnregisterEvent<SetFavouriteCheckPointEvent>(OnSetFavouriteCheckPoint);
            eventBus.UnregisterEvent<StyleVisibilityButtonClickedEvent>(OnStyleVisibilityChanged);
            eventBus.UnregisterEvent<CheckPointSelectionChangeEvent>(ControlPointSelectionChanged);
            eventBus.UnregisterEvent<DuplicateButtonClickEvent>(OnDuplicateClicked);
            eventBus.UnregisterEvent<StyleDeleteButtonClickedEvent>(OnStyleDeleteButtonClicked);
            m_TrainingController?.Dispose();
            m_SignInController?.Dispose();
            eventBus.Clear();
        }
    }
}