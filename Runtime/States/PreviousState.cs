using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moths.FSM;

namespace Moths.FSM.States
{
    [FSMNode("Previous State")]
    public class PreviousState : FSMState, IFSMPreviousState
    {
        public const string NAME = "Previous State";

        public override void UpdateState(ref FSMContext ctx)
        {
            
        }
    }
}