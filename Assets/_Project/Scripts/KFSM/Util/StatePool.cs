using System.Collections.Generic;
using BansheeGz.BGDatabase;

namespace Kylin.FSM
{
    public class StatePool
    {
        private Dictionary<string, Stack<StateBase>> _statePool =
            new BGHashtableForSerialization<string, Stack<StateBase>>();
        
        private object _statelLock = new object();
        
        //public 
    }
}