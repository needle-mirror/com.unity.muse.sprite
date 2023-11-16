using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using Unity.Muse.Common;
using Unity.Muse.Common.Baryon.UI.Manipulators;
using Unity.Muse.Sprite.Artifacts;
using Unity.Muse.Sprite.Common.Backend;
using Unity.Muse.Sprite.Operators;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Sprite.UIComponents
{
    internal class SpriteMuseArtifactResultView : ResultItemVisualElement
    {
        ActionButton m_EditButton;
        ActionButton m_ActionButton;
        VisualElement m_ButtonContainer;
        ActionButton m_BookmarkButton;
        ActionButton m_DislikeButton;
        VisualElement m_LeftVerticalContainer;

        const float k_LeftSideWidthVisible = 107f;
        const float k_EditIconWidthVisible = 70f;

        public SpriteMuseArtifactResultView(Artifact artifact)
            : base(artifact)
        {
            EnableInClassList("no-mouse", !Input.mousePresent);

            m_ButtonContainer = new VisualElement();
            m_ButtonContainer.AddToClassList("muse-asset-image__control-buttons-container");

            styleSheets.Add(ResourceManager.Load<StyleSheet>(Muse.Common.PackageResources.resultItemStyleSheet));

            m_EditButton = new ActionButton { name = "refine", icon = "pen", tooltip = Muse.Common.TextContent.refineTooltip };
            m_EditButton.AddToClassList("refine-button");
            m_EditButton.AddToClassList("refine-button-item");
            m_EditButton.clicked += OnRefineClicked;
            m_ActionButton = new ActionButton { name = "more", icon = "ellipsis", tooltip = "More options" };
            m_ActionButton.AddToClassList("refine-button");
            m_ActionButton.AddToClassList("refine-button-item");

            m_ActionButton.clicked += () => OnMenuTriggerClicked();
            m_PreviewImage.Add(m_ButtonContainer);
            m_ButtonContainer.Add(m_ActionButton);
            m_ButtonContainer.Add(m_EditButton);

            m_LeftVerticalContainer = new VisualElement();
            m_LeftVerticalContainer.AddToClassList("left-vertical-container");
            m_ButtonContainer.Add(m_LeftVerticalContainer);
            m_BookmarkButton = new ActionButton
            {
                tooltip = Muse.Common.TextContent.bookmarkButtonTooltip,
                icon = "star"
            };
            m_BookmarkButton.clicked += OnBookmarkClicked;
            m_BookmarkButton.AddToClassList("container-button");
            m_LeftVerticalContainer.Add(m_BookmarkButton);

            m_DislikeButton = new ActionButton
            {
                tooltip = Muse.Common.TextContent.dislikeTooltip,
                icon = "dislike"
            };
            m_DislikeButton.clicked += OnDislikeClicked;
            m_DislikeButton.AddToClassList("container-button");
            m_DislikeButton.AddToClassList("dislike-button");
            m_LeftVerticalContainer.Add(m_DislikeButton);

            m_PreviewImage.OnLoadedPreview += UpdateView;
            m_PreviewImage.OnDelete += DeleteCurrentModel;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            UpdateEditButton();
            UpdateLeftSideButtons();
            UpdateBookmark();
        }

        void OnMenuTriggerClicked()
        {
            m_ButtonContainer.AddToClassList("is-hovered");
            OnActionMenu(m_ActionButton);
        }

        protected override void MenuDismissed()
        {
            m_ButtonContainer.RemoveFromClassList("is-hovered");
        }

        public override void UpdateView()
        {
            m_ButtonContainer.style.display = canRefineBookmark ? DisplayStyle.Flex : DisplayStyle.None;
            m_ButtonContainer.visible = canRefineBookmark;
            m_EditButton.SetEnabled(m_PreviewImage.image != null);
            m_ActionButton.visible = canRefine;
            m_EditButton.visible = canRefine;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateFeedback();
            UpdateBookmark();
            UpdateView();
        }

        public override bool TryGoToRefineMode()
        {
            if (canRefine)
            {
                OnRefineClicked();
                return true;
            }

            return false;
        }

        void OnBookmarkClicked()
        {
            var bookmark = CurrentModel.GetData<BookmarkManager>();
            bookmark.Bookmark(m_Artifact, !bookmark.IsBookmarked(m_Artifact));

            UpdateBookmark();
        }

        void UpdateBookmark()
        {
            var isBookmarked = IsBookmarked();
            m_BookmarkButton.EnableInClassList("bookmarked", isBookmarked);
            m_BookmarkButton.icon = isBookmarked ? "star-filled" : "star";
        }

        void OnDislikeClicked()
        {
            var feedbackManager = CurrentModel.GetData<FeedbackManager>();
            feedbackManager.ToggleDislike(m_Artifact);

            UpdateFeedback();
        }

        void UpdateFeedback()
        {
            var feedbackManager = CurrentModel.GetData<FeedbackManager>();
            var isDisliked = feedbackManager.IsDisliked(m_Artifact);
            m_DislikeButton.icon = isDisliked ? "dislike-filled" : "dislike";
        }

        void UpdateEditButton()
        {
            m_EditButton.EnableInClassList("refine-button-hidden", !ShouldEditButtonBeVisible());
            m_EditButton.EnableInClassList("refine-button", ShouldEditButtonBeVisible());
        }

        void UpdateLeftSideButtons()
        {
            m_LeftVerticalContainer.EnableInClassList("container-hidden", !ShouldLeftSideButtonBeVisible());
        }

        internal bool ShouldLeftSideButtonBeVisible()
        {
            return resolvedStyle.width >= k_LeftSideWidthVisible && resolvedStyle.height >= k_LeftSideWidthVisible;
        }

        internal bool ShouldEditButtonBeVisible()
        {
            return resolvedStyle.width >= k_EditIconWidthVisible;
        }

        internal bool IsBookmarked()
        {
            return CurrentModel.GetData<BookmarkManager>().IsBookmarked(m_Artifact);
        }

        internal bool IsDisliked()
        {
            return CurrentModel.GetData<FeedbackManager>().IsDisliked(m_Artifact);
        }
    }

    class ResultItemVisualElement : ArtifactView
    {
        protected PreviewImage m_PreviewImage;

        public ResultItemVisualElement(Artifact artifact)
            : base(artifact)
        {
            AddToClassList("muse-asset-image");

            style.flexGrow = 1;
            m_PreviewImage = new PreviewImage();
            m_PreviewImage.SetAsset(artifact);
            m_PreviewImage.style.display = DisplayStyle.Flex;
            Add(m_PreviewImage);
        }

        public override UnityEngine.Texture Preview => m_PreviewImage.image;

        public override VisualElement PaintSurfaceElement => m_PreviewImage;
        protected bool isArtifactAvailable => m_PreviewImage.image != null;

        protected bool canRefine => isArtifactAvailable && CurrentModel != null && !CurrentModel.isRefineMode;
        protected bool canRefineBookmark => isArtifactAvailable && CurrentModel;

        public override IEnumerable<ContextMenuAction> GetAvailableActions(ActionContext context)
        {
            var actions = new List<ContextMenuAction>();

            if (Artifact is SpriteMuseArtifact spriteMuseArtifact)
            {
                actions.Add(new ContextMenuAction
                {
                    id = (int)Actions.GenerationSettings,
                    label = "Generation Data",
                    enabled = !context.isMultiSelect
                });

                if (CurrentModel.isRefineMode)
                {
                    actions.Add(new ContextMenuAction
                    {
                        id = (int)Actions.SetAsThumbnail,
                        label = "Set as Thumbnail",
                        enabled = !context.isMultiSelect && !CurrentModel.IsThumbnail(Artifact)
                    });

                    actions.Add(new ContextMenuAction
                    {
                        id = (int)Actions.Branch,
                        label = "Branch",
                        enabled = !context.isMultiSelect
                    });
                }

                actions.Add(new ContextMenuAction
                {
                    // Context menu Delete is available even if the generation is not ready yet, it's to have the option
                    // to delete the item when there is an error with the generation, otherwise, delete was only
                    // available with the keyboard shortcut
                    enabled = true,
                    id = (int)Actions.Delete,
                    label = context.isMultiSelect ? Unity.Muse.Common.TextContent.deleteMultiple : Unity.Muse.Common.TextContent.deleteSingle
                });

                if (context.selectedArtifacts.Any(view => ArtifactCache.IsInCache(view.Artifact)))
                {
                    actions.Add(new ContextMenuAction
                    {
                        enabled = true,
                        id = (int)Actions.Save,
                        label = context.isMultiSelect ? Unity.Muse.Common.TextContent.exportMultiple : Unity.Muse.Common.TextContent.exportSingle
                    });

                    if (context.isMultiSelect)
                    {
                        actions.Add(new ContextMenuAction
                        {
                            enabled = true,
                            id = (int)Actions.Star,
                            label = Unity.Muse.Common.TextContent.starMultiple
                        });
                        actions.Add(new ContextMenuAction
                        {
                            enabled = true,
                            id = (int)Actions.UnStar,
                            label = Unity.Muse.Common.TextContent.unStarMultiple
                        });
                    }
                    else
                    {
                        if (this is SpriteMuseArtifactResultView spriteResultView)
                        {
                            if (!spriteResultView.ShouldLeftSideButtonBeVisible())
                            {
                                if (spriteResultView.IsBookmarked())
                                {
                                    actions.Add(new ContextMenuAction
                                    {
                                        enabled = true,
                                        id = (int)Actions.UnStar,
                                        label = Unity.Muse.Common.TextContent.unStarSingle,
                                    });
                                }
                                else
                                {
                                    actions.Add(new ContextMenuAction
                                    {
                                        enabled = true,
                                        id = (int)Actions.Star,
                                        label = Unity.Muse.Common.TextContent.starSingle
                                    });
                                }

                                if (spriteResultView.IsDisliked())
                                {
                                    actions.Add(new ContextMenuAction
                                    {
                                        enabled = true,
                                        id = (int)Actions.Feedback,
                                        label = Unity.Muse.Common.TextContent.removeDislike
                                    });
                                }
                                else
                                {
                                    actions.Add(new ContextMenuAction
                                    {
                                        enabled = true,
                                        id = (int)Actions.Feedback,
                                        label = Unity.Muse.Common.TextContent.dislike
                                    });
                                }
                            }

                            if (!spriteResultView.ShouldEditButtonBeVisible() && canRefine)
                            {
                                actions.Add(new ContextMenuAction
                                {
                                    enabled = true,
                                    id = (int)Actions.Refine,
                                    label = Unity.Muse.Common.TextContent.refineSingle
                                });
                            }
                        }
                    }
                }
            }

            if ((ServerConfig.serverConfig.debugMode & ServerConfig.EDebugMode.ArtifactDebugInfo) > 0)
            {
                actions.Add(new ContextMenuAction
                {
                    id = (int)Actions.Debug,
                    label = "Debug Info",
                    icon = "debug",
                    enabled = !context.isMultiSelect
                });
            }

            return actions;
        }

        /// <summary>
        /// Perform action to perform on the selected artifact.
        /// </summary>
        /// <param name="actionId">The action to perform.</param>
        /// <param name="context">The action context.</param>
        /// <param name="pointerEvent">The pointer event at the source of the action.</param>
        public override void PerformAction(int actionId, ActionContext context, IPointerEvent pointerEvent)
        {
            if (Artifact != null && Artifact is Artifacts.SpriteMuseArtifact spriteMuseArtifact)
            {
                var id = (Actions)actionId;

                switch (id)
                {
                    case Actions.Feedback:
                        CurrentModel.GetData<FeedbackManager>().ToggleDislike(m_Artifact);
                        break;
                    case Actions.Download:
#if UNITY_WEBGL && !UNITY_EDITOR
                    spriteMuseArtifact.GetArtifact((Texture2D artifactInstance, byte[] rawData, string errorMessage) =>
                    {
                        if (artifactInstance == null)
                            return;

                        var bytes = artifactInstance.EncodeToPNG();
                        DllImport.DownloadFile(bytes, bytes.Length, spriteMuseArtifact.Guid + ".png");
                    }, true);
                        break;
#endif
                    case Actions.Debug:
#if UNITY_EDITOR
                        var session = spriteMuseArtifact.GetOperators().FirstOrDefault(x => x is SessionOperator) as SessionOperator;
                        var sgs = spriteMuseArtifact.GetOperators().FirstOrDefault(x => x is SpriteGeneratorSettingsOperator) as SpriteGeneratorSettingsOperator;
                        var log = $"JobID:{spriteMuseArtifact?.Guid}\n" +
                            $"Session:{session?.GetSessionID()}\n" +
                            $"ArtifactID:{sgs?.artifactID}\n" +
                            $"CheckPointID:{sgs?.checkPointGUID}\n" +
                            $"job Status:{spriteMuseArtifact.jobStatus}";
                        ShowDialog(log);
#endif
                        break;
                    default:
                        base.PerformAction(actionId, context, pointerEvent);
                        break;
                }
            }
        }

        void ShowDialog(string log)
        {
            var dialog = new AlertDialog
            {
                title = "Artifact Debug Info",
                variant = AlertSemantic.Destructive
            };
            var textField = new TextArea(log);
            dialog.contentContainer.Add(textField);
            dialog.SetPrimaryAction(99, "Copy", () => GUIUtility.systemCopyBuffer = log);
            dialog.SetCancelAction(1, "Ok");
            var modal = Modal.Build(this, dialog);
            modal.Show();
        }

        public override void DragEditor()
        {
            var artifactsAndType = GetArtifactsAndType();
            CurrentModel.EditorStartDrag(artifactsAndType.name, artifactsAndType.artifacts);
        }

        public override (string name, IList<Artifact> artifacts) GetArtifactsAndType()
        {
            return ("Sprite", new List<Artifact> { m_Artifact });
        }
    }
}