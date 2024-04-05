using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Muse.Sprite.Common
{
    internal static class Scheduler
    {
        struct ScheduleCallbackObject
        {
            public float timer;
            public DateTime startTime;
            public Action callback;
        }
        static List<ScheduleCallbackObject> s_Schedules = new List<ScheduleCallbackObject>();
        static SchedulerGameObject.SchedulerGO m_SchedulerGO;

        static public bool IsCallScheduled(Action callback)
        {
            for (int i = 0; i < s_Schedules.Count; ++i)
            {
                if (s_Schedules[i].callback == callback)
                    return true;
            }

            return false;
        }

        static public void ScheduleCallback(float timer, Action callback)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                if(m_SchedulerGO == null)
                {
                    var go = new GameObject("Scheduler");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    m_SchedulerGO = go.AddComponent<SchedulerGameObject.SchedulerGO>();
                    m_SchedulerGO.onDestroyingComponent += OnHelperDestroyed;
                }
            }
            else
            {
                UnityEditor.EditorApplication.update -= ScheduleTick;
                UnityEditor.EditorApplication.update += ScheduleTick;
            }
#else
            if (m_SchedulerGO == null)
            {
                var go = new GameObject("Scheduler");
                go.hideFlags = HideFlags.HideAndDontSave;
                m_SchedulerGO = go.AddComponent<SchedulerGameObject.SchedulerGO>();
                m_SchedulerGO.onDestroyingComponent += OnHelperDestroyed;
            }
#endif
            if (timer <= 0)
            {
                callback?.Invoke();
            }
            else
            {
                s_Schedules.Add(new ScheduleCallbackObject()
                {
                    timer = timer,
                    callback = callback,
                    startTime = DateTime.Now
                });
            }
        }

        static void OnHelperDestroyed(GameObject obj)
        {
            m_SchedulerGO = null;
        }

        static internal void ScheduleTick()
        {
            var now = DateTime.Now;
            for (int i = 0; i < s_Schedules.Count; ++i)
            {
                if (s_Schedules[i].timer <= (now - s_Schedules[i].startTime).TotalSeconds)
                {
                    var callback = s_Schedules[i];
                    s_Schedules.RemoveAt(i);
                    callback.callback?.Invoke();
                    --i;
                }
            }
#if UNITY_EDITOR
            if(s_Schedules.Count == 0)
                UnityEditor.EditorApplication.update -= ScheduleTick;
#endif
        }

        internal static void Flush()
        {
            s_Schedules.Clear();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= ScheduleTick;
#endif
            if (m_SchedulerGO)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                    UnityEngine.Object.Destroy(m_SchedulerGO.gameObject);
                else 
                    UnityEngine.Object.DestroyImmediate(m_SchedulerGO.gameObject);
#else
                UnityEngine.Object.Destroy(m_SchedulerGO.gameObject);
#endif
            }
        }
    }
}