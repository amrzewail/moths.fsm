using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine {

    public abstract class FSMTransition : ScriptableObject
    {
        [SerializeField] string _name;
        [Space]

        public bool negative = false;
        [Range(0f, 1f)]
        public float probability = 1.0f;
        public float probabilityCheckCooldown = 0;


        private float _lastProbabilityCheck { get; set; } = 0;

        protected abstract bool IsTrue(ref FSMContext ctx);

        public bool CheckTrue(ref FSMContext ctx)
        {
            return ((negative ? !IsTrue(ref ctx) : IsTrue(ref ctx)) && IsProbabilitySuccess());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_name))
            {
                if (_name != this.name)
                {
                    this.name = _name;
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.Refresh();
                }
            }

            string name = this.name;
            string NOT = " (Not)";
            bool NOTExistsInName = name.Substring(name.Length - NOT.Length, NOT.Length) == NOT;

            if (negative)
            {
                if (!NOTExistsInName)
                {
                    this.name += NOT;
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            else
            {
                if (NOTExistsInName)
                {
                    this.name = this.name.Substring(0, name.Length - NOT.Length);
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
        }
#endif
        public virtual void OnEnable()
        {
            _lastProbabilityCheck = -50;
        }

        public bool IsProbabilitySuccess()
        {
            if (Time.time - _lastProbabilityCheck >= probabilityCheckCooldown)
            {
                _lastProbabilityCheck = Time.time;
                return Random.Range(0f, 1f) <= probability;
            }
            return false;
        }
    }
}