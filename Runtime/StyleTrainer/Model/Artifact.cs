using System;
using UnityEngine;

namespace Unity.Muse.StyleTrainer
{
    enum EState
    {
        // Data is in training
        Training,

        // Data is loaded
        Loaded,

        // Data is loading
        Loading,

        // Data is newly created
        New,

        // Data encountered error when loading
        Error,

        // Initial data state and not loaded yet
        Initial,

        // Data is disposed
        Dispose
    }

    abstract class Artifact
    {
        [SerializeField]
        string m_Guid = Utilities.emptyGUID;
        [SerializeField]
        EState m_State = EState.New;

        protected abstract void GUIDChanged();

        protected abstract void StateChanged();

        protected Artifact(EState state)
        {
            m_State = state;
        }

        public EState state
        {
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    StateChanged();
                }
            }
            get => m_State;
        }

        public string guid
        {
            set
            {
                if (m_Guid != value)
                {
                    m_Guid = value;
                    GUIDChanged();
                }
            }
            get => m_Guid;
        }
    }

    [Serializable]
    abstract class Artifact<T, T1> : Artifact where T1 : Artifact
    {
        protected event Action<T> OnArtifactLoaded;
        public event Action<T1> OnDataChanged;
        public event Action<T1> OnStateChanged;
        public event Action<T1> OnGUIDChanged;

        protected Artifact(EState state):base(state)
        { }

        protected override void GUIDChanged()
        {
            OnGUIDChanged?.Invoke(this as T1);
        }

        protected override void StateChanged()
        {
            OnStateChanged?.Invoke(this as T1);
        }

        public virtual void OnDispose()
        {
            if (state != EState.New && state != EState.Loaded)
                state = EState.Initial;

            // Set dispose state for call back and remove all callbacks
            OnArtifactLoaded = null;
            OnDataChanged = null;
            OnStateChanged = null;
            OnGUIDChanged = null;
        }

        protected void DataChanged(T1 data)
        {
            OnDataChanged?.Invoke(data);
        }

        protected void ArtifactLoaded(T artifact, bool clearCallback = true)
        {
            OnArtifactLoaded?.Invoke(artifact);
            if (clearCallback)
                OnArtifactLoaded = null;
        }

        public abstract void GetArtifact(Action<T> onDoneCallback, bool useCache);
    }
}