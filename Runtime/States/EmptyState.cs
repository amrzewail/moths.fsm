using UnityEngine;

namespace Moths.FSM.States
{
    [FSMNode("Empty State")]
    public class EmptyState : FSMState
    {
        public override void UpdateState(ref FSMContext ctx)
        {
        }
    }
}