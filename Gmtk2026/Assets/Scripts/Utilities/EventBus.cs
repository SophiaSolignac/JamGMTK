using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnBocal.Events
{
    public static class GlobalEventBus
    {
        private static Dictionary<Enum, UnityEvent> _events = new();

        public static void Connect(Enum pName, UnityAction pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName]?.Invoke();
        }
    }

    public static class EventBus
    {
        private static Dictionary<Enum, UnityEvent> _events = new();

        public static void Connect(Enum pName, UnityAction pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName]?.Invoke();
            GlobalEventBus.Invoke(pName);
        }
    }

    public static class EventBus<Type01>
    {
        private static Dictionary<Enum, UnityEvent<Type01>> _events = new();

        public static void Connect(Enum pName, UnityAction<Type01> pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction<Type01> pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName, Type01 pArg01)
        {
            if (_events.ContainsKey(pName))
                _events[pName].Invoke(pArg01);
            EventBus.Invoke(pName);
            GlobalEventBus.Invoke(pName);
        }
    }

    public static class EventBus<Type01, Type02>
    {
        private static Dictionary<Enum, UnityEvent<Type01, Type02>> _events = new();

        public static void Connect(Enum pName, UnityAction<Type01, Type02> pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction<Type01, Type02> pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName, Type01 pArg01, Type02 pArg02)
        {
            if (_events.ContainsKey(pName))
                _events[pName].Invoke(pArg01, pArg02);
            EventBus.Invoke(pName);
            GlobalEventBus.Invoke(pName);
        }
    }

    public static class EventBus<Type01, Type02, Type03>
    {
        private static Dictionary<Enum, UnityEvent<Type01, Type02, Type03>> _events = new();

        public static void Connect(Enum pName, UnityAction<Type01, Type02, Type03> pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction<Type01, Type02, Type03> pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName, Type01 pArg01, Type02 pArg02, Type03 pArg03)
        {
            if (_events.ContainsKey(pName))
                _events[pName].Invoke(pArg01, pArg02, pArg03);
            EventBus.Invoke(pName);
            GlobalEventBus.Invoke(pName);
        }
    }

    public static class EventBus<Type01, Type02, Type03, Type04>
    {
        private static Dictionary<Enum, UnityEvent<Type01, Type02, Type03, Type04>> _events = new();

        public static void Connect(Enum pName, UnityAction<Type01, Type02, Type03, Type04> pMethod)
        {
            if (!_events.ContainsKey(pName)) _events[pName] = new();
            _events[pName].AddListener(pMethod);
        }

        public static void Disconnect(Enum pName, UnityAction<Type01, Type02, Type03, Type04> pMethod)
        {
            if (!_events.ContainsKey(pName)) return;
            _events[pName].RemoveListener(pMethod);
        }

        public static void Invoke(Enum pName, Type01 pArg01, Type02 pArg02, Type03 pArg03, Type04 pArg04)
        {
            if (_events.ContainsKey(pName))
                _events[pName].Invoke(pArg01, pArg02, pArg03, pArg04);
            EventBus.Invoke(pName);
            GlobalEventBus.Invoke(pName);
        }
    }

}