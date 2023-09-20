using System;
using Unity.Muse.Common;
using Unity.Muse.Sprite.Backend;
using Unity.Muse.Sprite.Common.Backend;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Muse.Sprite.Operators
{
    [Preserve]
    [Serializable]
    internal class SpriteRefiningMaskOperator : IOperator
    {
        enum ESettings
        {
            Mask,
            SourceJobID,
            SourceArtifactID,
            Refined,
        }

        public SpriteRefiningMaskOperator()
        {
            m_OperatorData = new OperatorData( OperatorName, "Unity.Muse.Sprite","0.0.1",  new [] { "", "", "", "", "" }, false);
        }

        OperatorData m_OperatorData;
        public const string baseUssClassName = "appui-sprite-refining-mask";
        public string OperatorName => "Unity.Muse.Sprite.Operators.SpriteRefiningMaskOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Sprite Refining Mask";

        public bool Enabled() => m_OperatorData.enabled;

        public void Enable(bool enable) => m_OperatorData.enabled = enable;
        public bool Hidden { get; set; }

        public VisualElement GetCanvasView() => new VisualElement();

        public VisualElement GetOperatorView(Model model) => new VisualElement();

        public OperatorData GetOperatorData() => m_OperatorData;

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null || data.settings.Length == 0)
                return;
            for(int i = 0; i < m_OperatorData.settings.Length && i < data.settings.Length; i++)
                m_OperatorData.settings[i] = data.settings[i];
        }

        public IOperator Clone()
        {
            var result = new SpriteRefiningMaskOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model)
        {
            if (!model.CurrentOperators.Contains(this))
                return;         // Only register to paint event for the current operator and not the selected artifact's operator

            if(Enabled())
                model.OnMaskPaintDone += OnMaskPaintDone;
        }

        public void UnregisterFromEvents(Model model)
        {
            if(Enabled())
                model.OnMaskPaintDone -= OnMaskPaintDone;
        }

        public bool IsSavable() => true;

        void OnMaskPaintDone(Texture2D texture)
        {
            m_OperatorData.settings[(int)ESettings.Mask] = Convert.ToBase64String(texture.EncodeToPNG());
        }

        public string GetMask()
        {
            return m_OperatorData.settings[(int)ESettings.Mask];
        }

        public string SetMask(string mask)
        {
            return m_OperatorData.settings[(int)ESettings.Mask] = mask;
        }

        void ValidateSettings()
        {

        }

        public string sourceJobID
        {
            get => m_OperatorData.settings[(int)ESettings.SourceJobID];
            set => m_OperatorData.settings[(int)ESettings.SourceJobID] = value;
        }

        public string sourceArtifactID
        {
            get => m_OperatorData.settings[(int)ESettings.SourceArtifactID];
            set => m_OperatorData.settings[(int)ESettings.SourceArtifactID] = value;
        }

        public void InitFromJobInfo(JobInfoResponse jobInfoResponse)
        {
            Guid guid;
            if (System.Guid.TryParse(jobInfoResponse.request.mask0Guid, out guid) && guid != Guid.Empty)
            {
                var getArtifact = new GetArtifactRestCall(ServerConfig.serverConfig, jobInfoResponse.request.mask0Guid);
                getArtifact.RegisterOnSuccess(OnGetMaskArtifactSuccess);
                getArtifact.RegisterOnFailure(OnGetMaskArtifactFailed);
                getArtifact.SendRequest();
            }
        }

        public bool refined
        {
            get => m_OperatorData.settings[(int)ESettings.Refined] == "1";
            set => m_OperatorData.settings[(int)ESettings.Refined] = value ? "1" : "0";
        }

        void OnGetMaskArtifactSuccess(GetArtifactRestCall request, byte[] data)
        {
            m_OperatorData.settings[(int)ESettings.Mask] = Convert.ToBase64String(data);
        }

        void OnGetMaskArtifactFailed(GetArtifactRestCall request)
        {
            Debug.Log($"Failed to get refine image mask artifact: {request.requestError} {request.requestResult}");
        }

        Texture2D GetTexture()
        {
            Texture2D texture = null;

            var raw = GetMask();
            if (!string.IsNullOrEmpty(raw))
            {
                texture = new Texture2D(2, 2);
                texture.LoadImage(Convert.FromBase64String(raw));
                texture.Apply();
            }

            return texture;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var mask = GetTexture();
            if (mask is null)
                return null;

            return new Image { image = mask };
        }
    }
}
