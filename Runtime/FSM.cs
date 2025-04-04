using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moths.FSM
{
    [CreateAssetMenu(menuName = "FSM/FSM")]
    public class FSM : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] Object _graphProperties;
        public Object graphProperties { get => _graphProperties; set => _graphProperties = value; }
#else
        public Object graphProperties => null;
#endif


        public FSMState startState;

    }
}
