using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Muse.Sprite.Common.Events;
using Unity.Muse.StyleTrainer.Events.StyleModelEvents;
using Unity.Muse.StyleTrainer.Events.StyleTrainerMainUIEvents;

namespace Unity.Muse.StyleTrainer
{
    class ValidateTrainingParametersTask
    {
        readonly StyleData m_StyleData;
        readonly EventBus m_EventBus;
        Action<bool> m_OnDoneCallback;
        int m_TrainingImagesLoaded = 0;

        public ValidateTrainingParametersTask(StyleData styleData, EventBus eventBus)
        {
            m_StyleData = styleData;
            m_EventBus = eventBus;
        }

        public void Execute(Action<bool> onDoneCallback)
        {
            m_OnDoneCallback = onDoneCallback;
            var showDialogEvent = new ShowDialogEvent
            {
                title = "Error",
                description = "Cannot generate style",
                semantic = AlertSemantic.Error
            };
            var config = StyleTrainerConfig.config;

            // Validate style name and description
            if (string.IsNullOrWhiteSpace(m_StyleData.title) || string.IsNullOrWhiteSpace(m_StyleData.description))
            {
                showDialogEvent.description = $"Version's name and description cannot be empty.";
                m_EventBus.SendEvent(showDialogEvent);
                m_OnDoneCallback.Invoke(false);
                return;
            }

            // validate sample output
            if (m_StyleData.sampleOutputPrompts?.Count < config.minSampleSetSize)
            {
                showDialogEvent.description = $"Sample output must have at least {config.minSampleSetSize} samples";
                showDialogEvent.confirmAction = () =>
                {
                    m_EventBus.SendEvent(new RequestChangeTabEvent { tabIndex = StyleModelInfoEditor.k_SampleOutputTab });
                };
                m_EventBus.SendEvent(showDialogEvent);
                m_OnDoneCallback.Invoke(false);
                return;
            }

            if (m_StyleData.sampleOutputPrompts?.Count > config.maxSampleSetSize)
            {
                showDialogEvent.description = $"Sample output must have at most {config.maxSampleSetSize} samples";
                showDialogEvent.confirmAction = () =>
                {
                    m_EventBus.SendEvent(new RequestChangeTabEvent { tabIndex = StyleModelInfoEditor.k_SampleOutputTab });
                };
                m_EventBus.SendEvent(showDialogEvent);
                m_OnDoneCallback.Invoke(false);
                return;
            }

            if (m_StyleData.trainingSetData?.Count < 1 || m_StyleData.trainingSetData[0]?.Count < config.minTrainingSetSize)
            {
                showDialogEvent.description = $"Training set must have at least {config.minTrainingSetSize} samples";
                showDialogEvent.confirmAction = () =>
                {
                    m_EventBus.SendEvent(new RequestChangeTabEvent { tabIndex = StyleModelInfoEditor.k_TrainingSetTab });
                };
                m_EventBus.SendEvent(showDialogEvent);
                m_OnDoneCallback.Invoke(false);
                return;
            }

            if (m_StyleData.trainingSetData?.Count < 1 || m_StyleData.trainingSetData[0]?.Count > config.maxTrainingSetSize)
            {
                showDialogEvent.description = $"Training set must have at most {config.maxTrainingSetSize} samples";
                showDialogEvent.confirmAction = () =>
                {
                    m_EventBus.SendEvent(new RequestChangeTabEvent { tabIndex = StyleModelInfoEditor.k_TrainingSetTab });
                };
                m_EventBus.SendEvent(showDialogEvent);
                m_OnDoneCallback.Invoke(false);
                return;
            }

            var duplicatedItem = new List<int>();

            // check if any of the samples are empty
            for (var i = 0; i < m_StyleData.sampleOutputPrompts?.Count; ++i)
            {
                var prompt1 = m_StyleData.sampleOutputPrompts[i];
                if (string.IsNullOrWhiteSpace(prompt1))
                {
                    showDialogEvent.description = $"Sample output cannot have empty prompts.";
                    showDialogEvent.confirmAction = () =>
                    {
                        m_EventBus.SendEvent(new RequestChangeTabEvent { tabIndex = StyleModelInfoEditor.k_SampleOutputTab });
                    };
                    m_EventBus.SendEvent(showDialogEvent);
                    m_OnDoneCallback.Invoke(false);
                    return;
                }

                duplicatedItem.Clear();
                for (var j = i + 1; j < m_StyleData.sampleOutputPrompts?.Count; ++j)
                    if (prompt1 == m_StyleData.sampleOutputPrompts[j])
                        duplicatedItem.Add(j);

                if (duplicatedItem.Count > 0)
                {
                    duplicatedItem.Add(i);
                    showDialogEvent.description = "One of the sample prompt is a duplicate. Please review your sample prompts.";
                    showDialogEvent.confirmAction = () =>
                    {
                        m_EventBus.SendEvent(new RequestChangeTabEvent
                        {
                            tabIndex = StyleModelInfoEditor.k_SampleOutputTab,
                            highlightIndices = duplicatedItem.AsReadOnly()
                        });
                    };
                    m_EventBus.SendEvent(showDialogEvent);
                    m_OnDoneCallback.Invoke(false);
                    return;
                }
            }

            //validate training are all unique
            m_TrainingImagesLoaded = 0;
            for (var i = 0; i < m_StyleData.trainingSetData[0]?.Count; ++i) m_StyleData.trainingSetData[0][i].imageArtifact.GetArtifact(_ => ValidateTrainingSetImages(), true);
        }

        void ValidateTrainingSetImages()
        {
            ++m_TrainingImagesLoaded;
            var trainingSetData = m_StyleData.trainingSetData[0];
            if (trainingSetData == null || m_TrainingImagesLoaded < trainingSetData.Count)
                return;

            var duplicatedItem = new List<int>();
            var showDialogEvent = new ShowDialogEvent
            {
                title = "Error",
                description = "Cannot generate style",
                semantic = AlertSemantic.Error
            };

            for (var i = 0; i < trainingSetData.Count; ++i)
            {
                var data = trainingSetData[i].imageArtifact.GetRawData();
                duplicatedItem.Clear();
                for (var j = i + 1; j < trainingSetData.Count; ++j)
                {
                    var data1 = trainingSetData[j].imageArtifact.GetRawData();
                    if (data?.Length == data1?.Length &&
                        Utilities.ByteArraysEqual(data, data1))
                        duplicatedItem.Add(j);
                }

                if (duplicatedItem.Count > 0)
                {
                    duplicatedItem.Add(i);
                    showDialogEvent.description = "One of the training samples is a duplicate. Please review your training set.";
                    showDialogEvent.confirmAction = () =>
                    {
                        m_EventBus.SendEvent(new RequestChangeTabEvent
                        {
                            tabIndex = StyleModelInfoEditor.k_TrainingSetTab,
                            highlightIndices = duplicatedItem.AsReadOnly()
                        });
                    };
                    m_EventBus.SendEvent(showDialogEvent);
                    m_OnDoneCallback.Invoke(false);
                    return;
                }
            }

            // Any more validation should be done here next
            m_OnDoneCallback.Invoke(true);
        }
    }
}