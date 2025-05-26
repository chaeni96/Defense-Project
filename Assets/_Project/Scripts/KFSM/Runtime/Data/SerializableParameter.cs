using System;
using UnityEngine;

namespace Kylin.FSM
{
   [Serializable]
    public class SerializableParameter
    {
        public enum ParameterType
        {
            Int,
            Float,
            Bool,
            String,
            Vector2,
            Vector3
        }

        public string Name;
        public ParameterType Type;
        public string StringValue;

        public object GetValue()
        {
            switch (Type)
            {
                case ParameterType.Int:
                    return int.TryParse(StringValue, out int intValue) ? intValue : 0;
                case ParameterType.Float:
                    return float.TryParse(StringValue, out float floatValue) ? floatValue : 0f;
                case ParameterType.Bool:
                    return bool.TryParse(StringValue, out bool boolValue) && boolValue;
                case ParameterType.String:
                    return StringValue;
                case ParameterType.Vector2:
                    try
                    {
                        string[] parts = StringValue.Trim('(', ')').Split(',');
                        float x = float.Parse(parts[0].Trim());
                        float y = float.Parse(parts[1].Trim());
                        return new Vector2(x, y);
                    }
                    catch
                    {
                        return Vector2.zero;
                    }
                case ParameterType.Vector3:
                    try
                    {
                        string[] parts = StringValue.Trim('(', ')').Split(',');
                        float x = float.Parse(parts[0].Trim());
                        float y = float.Parse(parts[1].Trim());
                        float z = float.Parse(parts[2].Trim());
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        return Vector3.zero;
                    }
                default:
                    return null;
            }
        }

        public void SetValue(object value)
        {
            if (value == null)
            {
                StringValue = "";
                return;
            }

            switch (Type)
            {
                case ParameterType.Int:
                    if (value is int intValue)
                    {
                        StringValue = intValue.ToString();
                    }
                    break;
                case ParameterType.Float:
                    if (value is float floatValue)
                    {
                        StringValue = floatValue.ToString();
                    }
                    break;
                case ParameterType.Bool:
                    if (value is bool boolValue)
                    { 
                        StringValue = boolValue.ToString(); 
                    }
                    break;

                case ParameterType.String:
                    StringValue = value.ToString();
                    break;
                case ParameterType.Vector2:
                    if (value is Vector2 vector2)
                    {
                        StringValue = $"({vector2.x},{vector2.y})";
                    }
                    break;
                case ParameterType.Vector3:
                    if (value is Vector3 vector3)
                    { 
                        StringValue = $"({vector3.x},{vector3.y},{vector3.z})"; 
                    }
                    break;
            }
        }
    }
}