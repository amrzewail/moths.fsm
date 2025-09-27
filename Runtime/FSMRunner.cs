using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moths.FSM
{
    using Object = UnityEngine.Object;

    public struct FSMProps
    {
        public float stateStartTime;
    }

    public interface ITransitionalState
    {
        bool ShouldTestTransitionsAfterStart();
    }

    [System.Serializable]
    public class FSMRunner
    {
        private FSMState[] _stateBuffer = new FSMState[3];
        private int _currentStateIndexInBuffer = 0;

        private FSMProps _fsmProps;
        private FSMContext _context;
        private bool _skipLateUpdate = false;

        public FSMState currentState => _stateBuffer[Repeat(_currentStateIndexInBuffer, _stateBuffer.Length)];

        public FSMContext Context { get => _context; }

        public FSM CurrentFSM { get; private set; }

        public event Action<FSMState> StateChanged;

        public void Awake()
        {
            _context = FSMContext.Create();
        }

        public void Start(FSM fsm)
        {
            CurrentFSM = fsm;
            if (!fsm) return;

            TransitionToState(fsm.startState);

            while(TestTransitions());
        }

        public bool TryStart(FSM fsm)
        {
            if (!currentState)
            {
                Start(fsm);
                return true;
            }

            if (currentState.ExitState(ref _context))
            {
                currentState.OnExitPluggers.ForEach(x => (x as IFSMPlugger).Execute(ref _context));
                Start(fsm);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            CurrentFSM = null;
        }

        public void Update()
        {
            if (currentState)
            {
                var transitioned = TestTransitions();
                _skipLateUpdate = transitioned;
                if (!transitioned)
                {
                    currentState.OnUpdatePluggers.ForEach(x => (x as IFSMPlugger).Execute(ref _context));
                    currentState.UpdateState(ref _context);
                }
            }
        }

        public void LateUpdate()
        {
            if (_skipLateUpdate)
            {
                _skipLateUpdate = false;
                return;
            }

            if (currentState)
            {
                currentState.LateUpdateState(ref _context);
            }
        }

        private FSMState InstantiateState(FSMState state)
        {
            var st = Object.Instantiate(state);
            st.Instantiate();
            return st;
        }

        private bool TestTransitions()
        {
            bool transitioned = false;

            FSMState newState = null;

            var parentState = CurrentFSM.parentState;
            if (parentState)
            {
                newState = parentState.CheckTransitions(ref _context);
            }

            if (!newState)
            {
                newState = currentState.CheckTransitions(ref _context);
            }

            if (newState)
            {
                if (currentState.ExitState(ref _context))
                {
                    currentState.OnExitPluggers.ForEach(x => (x as IFSMPlugger).Execute(ref _context));
                    TransitionToState(newState);
                    transitioned = true;
                    if (newState is ITransitionalState transitionalState && transitionalState.ShouldTestTransitionsAfterStart())
                    {
                        TestTransitions();
                    }
                }
            }

            return transitioned;
        }

        private void TransitionToState(FSMState state)
        {
            if (currentState) currentState.CleanUp(ref _context);

            if (state is IFSMPreviousState)
            {
                _currentStateIndexInBuffer--;
                state = _stateBuffer[Repeat(_currentStateIndexInBuffer, _stateBuffer.Length)];
            }
            else
            {
                _currentStateIndexInBuffer++;
            }

            //state = InstantiateState(state);
            _stateBuffer[Repeat(_currentStateIndexInBuffer, _stateBuffer.Length)] = state;
            _fsmProps.stateStartTime = Time.time;
            _context.SetValue(_fsmProps);

            currentState.ClearFlags();
            currentState.OnStartPluggers.ForEach(x => (x as IFSMPlugger).Execute(ref _context));
            currentState.StartState(ref _context);

            StateChanged?.Invoke(currentState);
        }

        private int Repeat(int value, int length)
        {
            value %= length;
            if (value < 0) value += length;
            return value;
        }


        public void SetContextValue<T>(T value) => _context.SetValue(value);

        public T GetContextValue<T>()
        {
            if (_context.TryReadValue<T>(out T v)) return v;
            return default(T);
        }
    }
}