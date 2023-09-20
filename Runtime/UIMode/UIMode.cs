using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Data;
using Unity.Muse.Sprite.Operators;
using UnityEngine;

namespace Unity.Muse.Sprite.UIMode
{
    class UIMode : IUIMode
    {
        public const string modeKey = "TextToSprite";

        MainUI m_MainUI;
        Model m_Model;
        public void Activate(MainUI mainUI)
        {
            m_MainUI = mainUI;
            m_Model = m_MainUI.model;
            m_Model.OnGenerateButtonClicked += OnGenerateButtonClicked;
            m_Model.OnSetOperatorDefaults += OnSetOperatorDefault;
            m_Model.GetData<DefaultStyleData>().Reset();
        }

        public void Deactivate()
        {
            m_Model.OnGenerateButtonClicked -= OnGenerateButtonClicked;
            m_Model.OnSetOperatorDefaults -= OnSetOperatorDefault;
            m_Model.GetData<DefaultStyleData>().Reset();
        }

        void OnGenerateButtonClicked()
        {
            var countData = m_Model.GetData<GenerateCountData>();
            countData.ResetCounter();
        }

        IEnumerable<IOperator> OnSetOperatorDefault(IEnumerable<IOperator> currentOperators)
        {
            if (m_Model.isRefineMode)
            {
                // Keep these operators from the original artifact as they are not displayed but still needed for refinement requests.
                currentOperators = currentOperators.Select(op =>
                {
                    var newOp = op switch
                    {
                        SpriteGeneratorSettingsOperator => m_Model.SelectedArtifact.GetOperator<SpriteGeneratorSettingsOperator>().Clone() ?? op,
                        SessionOperator => m_Model.SelectedArtifact.GetOperator<SessionOperator>().Clone() ?? op,
                        _ => op
                    };

                    return newOp;
                }).Where(op => op is not KeyImageOperator);
            }

            return currentOperators;
        }
    }
}