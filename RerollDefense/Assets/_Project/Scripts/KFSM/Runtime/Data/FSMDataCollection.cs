using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Kylin.FSM
{
    [CreateAssetMenu(fileName = "FSMDataCollection", menuName = "FSM/FSM Data Collection")]
    public class FSMDataCollection : ScriptableObject
    {
        [System.Serializable]
        public class FSMDataEntry
        {
            public string id;
            public FSMDataAsset data;
        }

        public List<FSMDataEntry> entries = new List<FSMDataEntry>();
        public FSMDataAsset GetFSMDataById(string id)
        {
            var entry = entries.FirstOrDefault(e => e.id == id);
            return entry?.data;
        }
        public int IndexOfId(string id)
        {
            return entries.FindIndex(e => e.id == id);
        }
        public void AddFSMData(string id, FSMDataAsset data)
        {
            int index = IndexOfId(id);
            if (index >= 0)
            {
                entries[index].data = data;
            }
            else
            {
                entries.Add(new FSMDataEntry { id = id, data = data });
            }
        }
        public bool RemoveFSMData(string id)
        {
            int index = IndexOfId(id);
            if (index >= 0)
            {
                entries.RemoveAt(index);
                return true;
            }
            return false;
        }
        public List<string> GetAllFSMIds()
        {
            return entries.Select(e => e.id).ToList();
        }
    }
}