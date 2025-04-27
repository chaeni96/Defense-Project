using UnityEngine;
using System.Collections.Generic;
namespace Kylin.FSM
{
    [CreateAssetMenu(fileName = "FSMData", menuName = "FSM/FSM Data Asset")]
    public class FSMDataAsset : ScriptableObject
    {
        public int InitialStateId = 0;
        public List<StateEntry> StateEntries = new();
        public List<TransitionEntry> Transitions = new();
    }
}