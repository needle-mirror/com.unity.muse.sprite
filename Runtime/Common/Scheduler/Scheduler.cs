using System;
using System.Collections.Generic;
using UnityEditor;
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
                if(s_Schedules.Count == 0)
                    EditorApplication.update += ScheduleTick;
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
            s_Schedules.Add(new ScheduleCallbackObject()
            {
                timer = timer,
                callback = callback,
                startTime = DateTime.Now
            });
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
                EditorApplication.update -= ScheduleTick;
#endif
        }
    }
}