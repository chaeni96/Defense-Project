using UnityEngine;
using System;
using System.Collections.Generic;
namespace Kylin.FSM
{
    [Serializable]
    public class StateEntry
    {
        public int Id;
        public string stateTypeName;
        public Vector2 position;
        //public SerializableDictionary<string, string> InitParams = new();


        public List<SerializableParameter> Parameters = new List<SerializableParameter>();
    }
}