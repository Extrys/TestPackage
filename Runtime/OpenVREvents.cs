﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.XR.OpenVR
{
    public class OpenVREvent : UnityEvent<API.VREvent_t> { }
    public class OpenVREvents
    {
        private static OpenVREvents instance;

        //dictionaries are slow/allocate in mono for some reason. So we just allocate a bunch at the beginning.
        private OpenVREvent[] events;
        private int[] eventIndicies;
        private API.VREvent_t vrEvent;
        private uint vrEventSize;

        private bool preloadedEvents = false;

        private const int maxEventsPerUpdate = 64;

        public static void Initialize(bool lazyLoadEvents = false)
        {
            instance = new OpenVREvents(lazyLoadEvents);
        }

        public bool IsInitialized()
        {
            return instance != null;
        }

        public OpenVREvents(bool lazyLoadEvents = false)
        {
            instance = this;
            events = new OpenVREvent[(int)API.EVREventType.VREvent_VendorSpecific_Reserved_End];

            vrEvent = new API.VREvent_t();
            vrEventSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(API.VREvent_t));

            if (lazyLoadEvents == false)
            {
                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    events[eventIndex] = new OpenVREvent();
                }
            }
            else
            {
                preloadedEvents = true;
            }

            RegisterDefaultEvents();
        }

        public void RegisterDefaultEvents()
        {
            AddListener(API.EVREventType.VREvent_Quit, On_VREvent_Quit);
        }

        public static void AddListener(API.EVREventType eventType, UnityAction<API.VREvent_t> action, bool removeOtherListeners = false)
        {
            instance.Add(eventType, action, removeOtherListeners);
        }
        public void Add(API.EVREventType eventType, UnityAction<API.VREvent_t> action, bool removeOtherListeners = false)
        {
            int eventIndex = (int)eventType;
            if (preloadedEvents == false && events[eventIndex] == null)
            {
                events[eventIndex] = new OpenVREvent();
            }

            if (removeOtherListeners)
            {
                events[eventIndex].RemoveAllListeners();
            }

            events[eventIndex].AddListener(action);
        }

        public static void RemoveListener(API.EVREventType eventType, UnityAction<API.VREvent_t> action)
        {
            instance.Remove(eventType, action);
        }
        public void Remove(API.EVREventType eventType, UnityAction<API.VREvent_t> action)
        {
            int eventIndex = (int)eventType;
            if (preloadedEvents || events[eventIndex] != null)
            {
                events[eventIndex].RemoveListener(action);
            }
        }

        public static void Update()
        {
            instance.PollEvents();
        }

        public void PollEvents()
        {
            API.CVRSystem system = API.OpenVR.System;
            if (system != null)
            {
                for (int eventIndex = 0; eventIndex < maxEventsPerUpdate; eventIndex++)
                {
                    if (!system.PollNextEvent(ref vrEvent, vrEventSize))
                        break;

                    int uEventType = (int)vrEvent.eventType;

                    if (events[uEventType] != null && events[uEventType].GetPersistentEventCount() > 0)
                    {
                        events[uEventType].Invoke(vrEvent);
                    }
                }
            }
        }

        #region DefaultEvents
        private void On_VREvent_Quit(API.VREvent_t pEvent)
        {
            API.CVRSystem system = API.OpenVR.System;
            system.AcknowledgeQuit_Exiting();
            Debug.Log("<b>[OpenVR]</b> Quit requested from OpenVR. Exiting application.");
            Application.Quit();
        }
        #endregion
    }
}