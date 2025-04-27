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
                var typeFields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
                   type == typeof(Vector3);
        }

        private SerializableParameter.ParameterType GetParameterType(Type fieldType)
        {
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

            return SerializableParameter.ParameterType.String; // ±âº»°ª
        }

        public void BuildInspector(VisualElement container)
        {
            if (stateEntry.Id == Transition.ANY_STATE)
            {
                return;
            }

            var fields = GetSerializeFields();
            if (fields.Count == 0)
            {
                return;
            }

            var header = new Label("State Parameters");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 10;
            header.style.marginBottom = 5;
            container.Add(header);

            if (stateEntry.Parameters == null)
            { 
                stateEntry.Parameters = new List<SerializableParameter>();
            }

            Dictionary<string, SerializableParameter> existingParams = stateEntry.Parameters.ToDictionary(p => p.Name, p => p);

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

                    stateEntry.Parameters.Add(param);
                }

                VisualElement fieldEditor = CreateFieldEditor(param);
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

        private VisualElement CreateFieldEditor(SerializableParameter param)
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
                case SerializableParameter.ParameterType.Int:
                    var intField = new IntegerField();
                    intField.style.flexGrow = 1;
                    int intValue = (int)param.GetValue();
                    intField.value = intValue;
                    intField.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(intField);
                    break;

                case SerializableParameter.ParameterType.Float:
                    var floatField = new FloatField();
                    floatField.style.flexGrow = 1;
                    float floatValue = (float)param.GetValue();
                    floatField.value = floatValue;
                    floatField.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(floatField);
                    break;

                case SerializableParameter.ParameterType.Bool:
                    var boolField = new Toggle();
                    bool boolValue = (bool)param.GetValue();
                    boolField.value = boolValue;
                    boolField.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(boolField);
                    break;

                case SerializableParameter.ParameterType.String:
                    var stringField = new TextField();
                    stringField.style.flexGrow = 1;
                    string stringValue = (string)param.GetValue();
                    stringField.value = stringValue;
                    stringField.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(stringField);
                    break;

                case SerializableParameter.ParameterType.Vector2:
                    var vector2Field = new Vector2Field();
                    vector2Field.style.flexGrow = 1;
                    vector2Field.Q("unity-x-input").style.flexGrow = 1;
                    vector2Field.Q("unity-y-input").style.flexGrow = 1;
                    Vector2 vector2Value = (Vector2)param.GetValue();
                    vector2Field.value = vector2Value;
                    vector2Field.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(vector2Field);
                    break;

                case SerializableParameter.ParameterType.Vector3:
                    var vector3Field = new Vector3Field();
                    vector3Field.style.flexGrow = 1;
                    vector3Field.Q("unity-x-input").style.flexGrow = 1;
                    vector3Field.Q("unity-y-input").style.flexGrow = 1;
                    vector3Field.Q("unity-z-input").style.flexGrow = 1;
                    Vector3 vector3Value = (Vector3)param.GetValue();
                    vector3Field.value = vector3Value;
                    vector3Field.RegisterValueChangedCallback(evt => { param.SetValue(evt.newValue);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    fieldContainer.Add(vector3Field);
                    break;
            }

            return container;
        }
    }
}