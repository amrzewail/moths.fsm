using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moths.Attributes;

namespace Moths.FSM
{
    [System.Serializable]
    public abstract class FSMState : ScriptableObject
    {
        public string phase { get; protected set; } = "";
        public float speed { get; protected set; } = 1;
        public bool mirror { get; protected set; } = false;

        [SerializeField] protected string _name;
        [Space]
        [HideInInspector]
        public FSMState inherit;
        [RequireInterface(typeof(IFSMPlugger))] [SerializeField] [HideInInspector] public List<Object> OnStartPluggers;
        [RequireInterface(typeof(IFSMPlugger))] [SerializeField] [HideInInspector] public List<Object> OnUpdatePluggers;
        [RequireInterface(typeof(IFSMPlugger))] [SerializeField] [HideInInspector] public List<Object> OnExitPluggers;

        [HideInInspector]
        public Transition[] transitions;

        private HashSet<FSMStateFlag> _flags = new HashSet<FSMStateFlag>();

#if UNITY_EDITOR
        protected void OnValidate()
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
        }
#endif

        public virtual void OnEnable()
        {
            phase = "";
        }

        public virtual FSMStateFlag[] GetFlags() => new FSMStateFlag[0];

        protected void SetFlag(FSMStateFlag flag)
        {
            _flags.Add(flag);
        }

        protected void UnsetFlag(FSMStateFlag flag)
        {
            _flags.Remove(flag);
        }

        protected void SetFlag(FSMStateFlag flag, bool value)
        {
            if (value) SetFlag(flag);
            else UnsetFlag(flag);
        }

        public bool HasFlag(FSMStateFlag flag)
        {
            if (string.IsNullOrEmpty(flag.name)) return true;
            return _flags.Contains(flag);
        }

        public virtual bool StartState(ref FSMContext ctx)
        {
            return true;
        }

        public abstract void UpdateState(ref FSMContext ctx);

        public virtual void LateUpdateState(ref FSMContext ctx) { }

        public virtual bool ExitState(ref FSMContext ctx)
        {
            return true;
        }

        public virtual void CleanUp(ref FSMContext ctx) { }

        public void Instantiate()
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (!transitions[i].transition) continue;
                transitions[i].transition = Object.Instantiate(transitions[i].transition);
            }

            if(inherit)
            {
                inherit = Object.Instantiate(inherit);
                inherit.Instantiate();
            }
        }

        public virtual FSMState CheckTransitions(ref FSMContext ctx)
        {
            if (inherit)
            {
                var newState = inherit.CheckTransitions(ref ctx);
                if (newState)
                    return newState;
            }
            for (int i = 0; i < transitions.Length; i++)
            {
                var t = transitions[i];

                if (!HasFlag(t.flag)) continue;

                FSMState newState = t.Test(ref ctx);

                if (!newState) continue;

                return newState;
            }
            return null;
        }

    }

    [System.Serializable]
    public class Transition
    {
        public string flag;
        public FSMTransition transition;
        public FSMState newState;

        public List<Chance> chances;

        [SerializeReference]
        public Transition linked;

        public FSMState Test(ref FSMContext ctx)
        {
            if (transition && !transition.CheckTrue(ref ctx)) return null;

            if (linked != null)
            {
                return linked.Test(ref ctx);
            }

            var state = newState;

            if (chances != null && chances.Count > 0)
            {
                float max = 0;
                for (int i = 0; i < chances.Count; i++) max += chances[i].value;
                float rnd = UnityEngine.Random.Range(0, max);
                max = 0;
                for (int i = 0; i < chances.Count; i++)
                {
                    if (rnd >= max && rnd < max + chances[i].value)
                    {
                        return chances[i].newState;
                    }
                    max += chances[i].value;
                }
            }

            return state;
        }
    }

    [System.Serializable]
    public class Chance
    {
        public float value;
        public FSMState newState;
    }
}