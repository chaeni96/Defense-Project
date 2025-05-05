using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    /// <summary>
    /// Unity Inspector에 노출 가능한 제네릭 Dictionary.
    /// 키와 값을 각각 리스트로 직렬화하고,
    /// ISerializationCallbackReceiver로 Dictionary를 복원합니다.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        // 런타임에 실제 사용하는 딕셔너리
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        /// <summary>인덱서로 사용 가능</summary>
        public TValue this[TKey key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public int Count => _dict.Count;

        public Dictionary<TKey, TValue>.KeyCollection Keys => _dict.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => _dict.Values;

        public void Add(TKey key, TValue value) => _dict.Add(key, value);
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);
        public void Clear() => _dict.Clear();

        // Inspector → 런타임: 리스트로부터 딕셔너리 복원
        public void OnAfterDeserialize()
        {
            _dict = new Dictionary<TKey, TValue>();
            int n = Math.Min(_keys.Count, _values.Count);
            for (int i = 0; i < n; i++)
            {
                if (_keys[i] != null) // nullable TKey 지원
                    _dict[_keys[i]] = _values[i];
            }
        }

        // 런타임 → Inspector : 딕셔너리 내용을 리스트에 기록
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in _dict)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }
    }
}
