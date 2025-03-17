using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    [System.Serializable]
    public struct FSMStateFlag
    {
        public string name { get; private set; }

        public static FSMStateFlag Create(string name)
        {
            return new()
            {
                name = name,
            };
        }

        public static implicit operator FSMStateFlag (string value)
        {
            return new() { name = value };
        }
    }
}