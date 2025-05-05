using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    /// <summary>
    /// Unity Inspector�� ���� ������ ���׸� Dictionary.
    /// Ű�� ���� ���� ����Ʈ�� ����ȭ�ϰ�,
    /// ISerializationCallbackReceiver�� Dictionary�� �����մϴ�.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        // ��Ÿ�ӿ� ���� ����ϴ� ��ųʸ�
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        /// <summary>�ε����� ��� ����</summary>
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

        // Inspector �� ��Ÿ��: ����Ʈ�κ��� ��ųʸ� ����
        public void OnAfterDeserialize()
        {
            _dict = new Dictionary<TKey, TValue>();
            int n = Math.Min(_keys.Count, _values.Count);
            for (int i = 0; i < n; i++)
            {
                if (_keys[i] != null) // nullable TKey ����
                    _dict[_keys[i]] = _values[i];
            }
        }

        // ��Ÿ�� �� Inspector : ��ųʸ� ������ ����Ʈ�� ���
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
