// 자동 생성된 StateFactory - 수정 금지
using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Kylin.FSM
{
    public static partial class StateFactory
    {
        // 필드 캐시 - 매번 리플렉션을 사용하지 않도록 성능 최적화
        private static Dictionary<string, Dictionary<string, FieldInfo>> _typeFieldsCache =
            new Dictionary<string, Dictionary<string, FieldInfo>>();

        public static StateBase CreateState(StateEntry stateEntry)
        {
            if (stateEntry == null || string.IsNullOrEmpty(stateEntry.stateTypeName))
                return null;

            StateBase state = null;

            switch (stateEntry.stateTypeName)
            {
                case "AttackState": state = new AttackState(); break;
                case "ChaseState": state = new ChaseState(); break;
                case "IdleState": state = new IdleState(); break;
                case "MoveForwardState": state = new MoveForwardState(); break;
                case "TestAState": state = new TestAState(); break;
                case "TestBState": state = new TestBState(); break;
                default:
                    Debug.LogError($"Unknown stateType: {stateEntry.stateTypeName}");
                    return null;
            }

            state.SetID(stateEntry.Id);
            // 파라미터 값 설정
            if (stateEntry.Parameters != null && stateEntry.Parameters.Count > 0)
            {
                InitializeStateParameters(state, stateEntry);
            }

            return state;
        }

        private static void InitializeStateParameters(StateBase state, StateEntry stateEntry)
        {
            Type stateType = state.GetType();
            string typeName = stateType.FullName;

            if (!_typeFieldsCache.TryGetValue(typeName, out var fieldsDict))
            {
                fieldsDict = new Dictionary<string, FieldInfo>();
                var currentType = stateType;
                while (currentType != null && currentType != typeof(StateBase) && currentType != typeof(object))
                {
                    var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var field in fields)
                    {
                        if (field.GetCustomAttribute<SerializeField>() != null || field.IsPublic)
                            fieldsDict[field.Name] = field;
                    }
                    currentType = currentType.BaseType;
                }
                _typeFieldsCache[typeName] = fieldsDict;
            }

            foreach (var param in stateEntry.Parameters)
            {
                if (fieldsDict.TryGetValue(param.Name, out var fieldInfo))
                {
                    try
                    {
                        var value = param.GetValue();
                        fieldInfo.SetValue(state, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to set parameter {param.Name} on state {typeName}: {ex.Message}");
                    }
                }
            }
        }

        public static StateBase[] CreateStates(FSMDataAsset dataAsset)
        {
            if (dataAsset.StateEntries == null || dataAsset.StateEntries.Count == 0)
                return new StateBase[0];

            var list = new List<StateBase>();
            foreach (var entry in dataAsset.StateEntries)
            {
                if (entry.Id == Transition.ANY_STATE) continue;
                var state = CreateState(entry);
                list.Add(state);
            }
            return list.ToArray();
        }
    }
}
