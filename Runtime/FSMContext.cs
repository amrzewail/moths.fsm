using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    public class FSMContext
    {
        private Dictionary<Type, object> _types;

        public object this[Type type] => _types[type];

        public static FSMContext Create()
        {
            var context = new FSMContext();
            context._types = new Dictionary<Type, object>(64);
            return context;
        }

        public bool TryReadValue<TValue>(out TValue value)
        {
            if (_types.TryGetValue(typeof(TValue), out object v))
            {
                value = (TValue)v;
                return true;
            }
            value = default;
            return false;
        }

        public TValue ReadValue<TValue>()
        {
            if (TryReadValue<TValue>(out var value)) return value;
            return default(TValue);
        }

        public void SetValue<TValue>(TValue value)
        {
            _types[typeof(TValue)] = value;
        }
    }
}