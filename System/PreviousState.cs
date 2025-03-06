using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FiniteStateMachine;

namespace FiniteStateMachine.Player
{
    [CreateAssetMenu(fileName = NAME, menuName = "FSM/" + NAME)]
    public class PreviousState : FSMState, IFSMPreviousState
    {
        public const string NAME = "Previous State";

        public override void UpdateState(ref FSMContext ctx)
        {
            
        }
    }
}