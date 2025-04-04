using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moths.FSM
{
    public interface IFSMPlugger
    {
        void Execute(ref FSMContext ctx);
    }
}