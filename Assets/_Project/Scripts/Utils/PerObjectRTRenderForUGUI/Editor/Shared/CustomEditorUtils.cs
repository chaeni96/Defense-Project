using UnityEditor;
using UnityEngine;

namespace CatDarkGame.PerObjectRTRenderForUGUI_Editor.Shared
{
    public static class CustomEditorUtils
    {
        public enum EnumPropDefault
        {
            Off = 0,
            On,
        }
        
        // Custom Inspector에 자신 Script Ref 표시용 GUI
        public static void DrawScriptRef<T>(object target) where T : MonoBehaviour
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((T)target), typeof(T), false);
            GUI.enabled = true;
        }
    }
}