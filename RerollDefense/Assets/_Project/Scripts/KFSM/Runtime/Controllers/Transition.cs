using System;
namespace Kylin.FSM
{
    [Serializable]
    public class Transition
    {
        public const int ANY_STATE = -1;
        public int FromStateId;    // -1: AnyState
        public int ToStateId;
        public int RequiredMask;
        public int IgnoreMask;
        public int Priority;
    }
}