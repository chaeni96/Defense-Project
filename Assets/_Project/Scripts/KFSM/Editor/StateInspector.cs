using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kylin.FSM
{
    public class StateInspector
    {
        private SerializedObject serializedObject;
        private StateEntry stateEntry;
        private Type stateType;
        private static Dictionary<string, List<FieldInfo>> _typeFieldsCache = new Dictionary<string, List<FieldInfo>>();
        private static Dictionary<Type, List<Type>> _serviceImplementationsCache = new Dictionary<Type, List<Type>>();

        public StateInspector(SerializedObject serializedObject, StateEntry stateEntry)
        {
            this.serializedObject = serializedObject;
            this.stateEntry = stateEntry;

            stateType = GetStateType(stateEntry.stateTypeName);
        }

        private Type GetStateType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == typeName.Split('.').Last())
                        return type;
                }
            }

            return null;
        }

        private List<FieldInfo> GetSerializeFields()
        {
            if (stateType == null)
                return new List<FieldInfo>();

            if (_typeFieldsCache.TryGetValue(stateType.FullName, out var cachedFields))
                return cachedFields;

            var fields = new List<FieldInfo>();

            Type currentType = stateType;
            while (currentType != null && currentType != typeof(StateBase) && currentType != typeof(object))
            {
                var typeFields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var field in typeFields)
                {
                    if (field.GetCustomAttribute<SerializeField>() != null || field.IsPublic)
                    {
                        if (IsSupportedType(field.FieldType))
                        {
                            fields.Add(field);
                        }
                    }
                }

                currentType = currentType.BaseType;
            }

            _typeFieldsCache[stateType.FullName] = fields;

            return fields;
        }

        private bool IsSupportedType(Type type)
        {
            return type == typeof(int) ||
                   type == typeof(float) ||
                   type == typeof(bool) ||
                   type == typeof(string) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type.IsInterface;
        }

        private SerializableParameter.ParameterType GetParameterType(Type fieldType)
        {
            if (fieldType.IsInterface)
                return SerializableParameter.ParameterType.ServiceReference;
            if (fieldType == typeof(int))
                return SerializableParameter.ParameterType.Int;
            if (fieldType == typeof(float))
                return SerializableParameter.ParameterType.Float;
            if (fieldType == typeof(bool))
                return SerializableParameter.ParameterType.Bool;
            if (fieldType == typeof(string))
                return SerializableParameter.ParameterType.String;
            if (fieldType == typeof(Vector2))
                return SerializableParameter.ParameterType.Vector2;
            if (fieldType == typeof(Vector3))
                return SerializableParameter.ParameterType.Vector3;

            return SerializableParameter.ParameterType.String; // 기본값
        }

        public void BuildInspector(VisualElement container)
{
    if (stateEntry.Id == Transition.ANY_STATE)
        return;

    var fields = GetSerializeFields();
    if (fields.Count == 0)
        return;

    var header = new Label("State Parameters");
    header.style.fontSize = 14;
    header.style.unityFontStyleAndWeight = FontStyle.Bold;
    header.style.marginTop = 10;
    header.style.marginBottom = 5;
    container.Add(header);

    if (stateEntry.Parameters == null)
        stateEntry.Parameters = new List<SerializableParameter>();

    Dictionary<string, SerializableParameter> existingParams = 
        stateEntry.Parameters.ToDictionary(p => p.Name, p => p);

    foreach (var field in fields)
    {
        SerializableParameter param;
        if (!existingParams.TryGetValue(field.Name, out param))
        {
            param = new SerializableParameter
            {
                Name = field.Name,
                Type = GetParameterType(field.FieldType),
                StringValue = GetDefaultValueForType(field.FieldType)
            };

            // 인터페이스 타입인 경우 추가 정보 저장
            if (field.FieldType.IsInterface)
            {
                // FullName이 null일 수 있으므로 체크
                param.ServiceInterfaceType = field.FieldType.FullName ?? field.FieldType.AssemblyQualifiedName;
                
                // 그래도 null이면 직접 구성
                if (string.IsNullOrEmpty(param.ServiceInterfaceType))
                {
                    param.ServiceInterfaceType = $"{field.FieldType.Namespace}.{field.FieldType.Name}";
                }
                
                Debug.Log($"Set ServiceInterfaceType for {field.Name}: {param.ServiceInterfaceType}");
            }

            stateEntry.Parameters.Add(param);
        }
        else
        {
            // 기존 파라미터가 있지만 ServiceInterfaceType이 없는 경우 재설정
            if (param.Type == SerializableParameter.ParameterType.ServiceReference && 
                string.IsNullOrEmpty(param.ServiceInterfaceType))
            {
                param.ServiceInterfaceType = field.FieldType.FullName ?? field.FieldType.AssemblyQualifiedName;
                
                if (string.IsNullOrEmpty(param.ServiceInterfaceType))
                {
                    param.ServiceInterfaceType = $"{field.FieldType.Namespace}.{field.FieldType.Name}";
                }
            }
        }

        VisualElement fieldEditor = CreateFieldEditor(param, field);
        if (fieldEditor != null)
        {
            container.Add(fieldEditor);
        }
    }

    EditorUtility.SetDirty(serializedObject.targetObject);
}

        private string GetDefaultValueForType(Type type)
        {
            if (type == typeof(int))
            {
                return "0";
            }

            if (type == typeof(float))
            {
                return "0";
            }

            if (type == typeof(bool))
            {
                return "false";
            }

            if (type == typeof(string))
            {
                return "";
            }

            if (type == typeof(Vector2))
            {
                return "(0,0)";
            }

            if (type == typeof(Vector3))
            {
                return "(0,0,0)";
            }

            return "";
        }

        private VisualElement CreateFieldEditor(SerializableParameter param, FieldInfo fieldInfo)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 5;
            container.style.width = new StyleLength(StyleKeyword.Initial);
            container.style.flexGrow = 1;

            var label = new Label(param.Name);
            label.style.width = 120;
            label.style.marginRight = 5;
            label.style.flexShrink = 0;
            container.Add(label);

            var fieldContainer = new VisualElement();
            fieldContainer.style.flexGrow = 1;
            fieldContainer.style.flexDirection = FlexDirection.Row;
            container.Add(fieldContainer);

            switch (param.Type)
            {
                case SerializableParameter.ParameterType.ServiceReference:
                    CreateServiceDropdown(fieldContainer, param);
                    break;

                case SerializableParameter.ParameterType.Int:
                    var intField = new IntegerField();
                    intField.style.flexGrow = 1;
                    int intValue = (int)param.GetValue();
                    intField.value = intValue;
                    intField.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(intField);
                    break;

                case SerializableParameter.ParameterType.Float:
                    var floatField = new FloatField();
                    floatField.style.flexGrow = 1;
                    float floatValue = (float)param.GetValue();
                    floatField.value = floatValue;
                    floatField.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(floatField);
                    break;

                case SerializableParameter.ParameterType.Bool:
                    var boolField = new Toggle();
                    bool boolValue = (bool)param.GetValue();
                    boolField.value = boolValue;
                    boolField.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(boolField);
                    break;

                case SerializableParameter.ParameterType.String:
                    var stringField = new TextField();
                    stringField.style.flexGrow = 1;
                    string stringValue = (string)param.GetValue();
                    stringField.value = stringValue;
                    stringField.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(stringField);
                    break;

                case SerializableParameter.ParameterType.Vector2:
                    var vector2Field = new Vector2Field();
                    vector2Field.style.flexGrow = 1;
                    Vector2 vector2Value = (Vector2)param.GetValue();
                    vector2Field.value = vector2Value;
                    vector2Field.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(vector2Field);
                    break;

                case SerializableParameter.ParameterType.Vector3:
                    var vector3Field = new Vector3Field();
                    vector3Field.style.flexGrow = 1;
                    Vector3 vector3Value = (Vector3)param.GetValue();
                    vector3Field.value = vector3Value;
                    vector3Field.RegisterValueChangedCallback(evt =>
                    {
                        param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(vector3Field);
                    break;
            }

            return container;
        }

        // 서비스 드롭다운 생성 메서드 추가!
       private void CreateServiceDropdown(VisualElement container, SerializableParameter param)
{
    var dropdown = new DropdownField();
    dropdown.style.flexGrow = 1;
    
    // null 체크 추가
    if (string.IsNullOrEmpty(param.ServiceInterfaceType))
    {
        dropdown.choices = new List<string> { "Error: Interface type not set" };
        dropdown.value = "Error: Interface type not set";
        dropdown.SetEnabled(false);
        container.Add(dropdown);
        Debug.LogError($"ServiceInterfaceType is null or empty for parameter: {param.Name}");
        return;
    }
    
    // 인터페이스 타입 가져오기
    Type interfaceType = null;
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        try
        {
            interfaceType = assembly.GetType(param.ServiceInterfaceType);
            if (interfaceType != null) break;
        }
        catch
        {
            // 어셈블리 접근 오류 무시
        }
    }
    
    if (interfaceType == null)
    {
        dropdown.choices = new List<string> { $"Invalid Interface: {param.ServiceInterfaceType}" };
        dropdown.value = $"Invalid Interface: {param.ServiceInterfaceType}";
        dropdown.SetEnabled(false);
        container.Add(dropdown);
        return;
    }
    
    // 나머지 코드는 동일...
    var implementations = GetServiceImplementations(interfaceType);
    
    var choices = new List<string> { "None" };
    var choiceToType = new Dictionary<string, string> { ["None"] = "" };
    
    foreach (var implType in implementations)
    {
        string displayName = FormatServiceName(implType.Name);
        choices.Add(displayName);
        choiceToType[displayName] = implType.FullName;
    }
    
    dropdown.choices = choices;
    
    // 현재 선택된 서비스 찾기
    if (!string.IsNullOrEmpty(param.ServiceImplementationType))
    {
        var currentType = implementations.FirstOrDefault(
            t => t.FullName == param.ServiceImplementationType);
        if (currentType != null)
        {
            dropdown.value = FormatServiceName(currentType.Name);
        }
        else
        {
            dropdown.value = "None";
        }
    }
    else
    {
        dropdown.value = "None";
    }
    
    // 변경 이벤트
    dropdown.RegisterValueChangedCallback(evt =>
    {
        param.ServiceImplementationType = choiceToType.ContainsKey(evt.newValue) 
            ? choiceToType[evt.newValue] 
            : "";
        EditorUtility.SetDirty(serializedObject.targetObject);
    });
    
    container.Add(dropdown);
}

        // 인터페이스 구현체 찾기
        private List<Type> GetServiceImplementations(Type interfaceType)
        {
            // 캐시 확인
            if (_serviceImplementationsCache.TryGetValue(interfaceType, out var cached))
            {
                return cached;
            }

            var implementations = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass &&
                                    !t.IsAbstract &&
                                    interfaceType.IsAssignableFrom(t))
                        .ToList();

                    implementations.AddRange(types);
                }
                catch
                {
                    // 일부 어셈블리는 접근 불가능할 수 있음
                }
            }

            // 캐시에 저장
            _serviceImplementationsCache[interfaceType] = implementations;

            return implementations;
        }

        // 서비스 이름 포맷팅
        private string FormatServiceName(string typeName)
        {
            // "NearestEnemyDetectService" -> "Nearest Enemy Detect"
            var name = typeName.Replace("Service", "");

            // CamelCase를 공백으로 분리
            var formatted = System.Text.RegularExpressions.Regex.Replace(
                name,
                "([a-z])([A-Z])",
                "$1 $2"
            );

            return formatted;
        }

    }
}