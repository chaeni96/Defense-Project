using System;
namespace Kylin.FSM
{
    [Serializable]
    public class TransitionEntry
    {
        public int FromStateId;
        public int ToStateId;
        public Trigger[] RequiredTriggers;
        public Trigger[] IgnoreTriggers;
        public int Priority;
    }
}