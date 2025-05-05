using System;
using CatDarkGame.PerObjectRTRenderForUGUI_Editor.Shared;
using UnityEditor;
using UnityEngine;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    [CustomEditor(typeof(PerObjectRTSource), true)]
    [CanEditMultipleObjects]
    public class PerObjectRTSourceEditor : Editor
    {
        private SerializedProperty _boundsProp;
        private SerializedProperty _autoBoundsModeProp;
        private SerializedProperty _sortingMode;

        private void OnEnable()
        {
            _boundsProp = serializedObject.FindProperty("_bounds");
            _autoBoundsModeProp = serializedObject.FindProperty("_autoBoundsMode");
            _sortingMode = serializedObject.FindProperty("_sortingMode");
        }

        private void OnDisable()
        {
            
        } 

        public override void OnInspectorGUI()
        {
            PerObjectRTSource component = (PerObjectRTSource)target;
            CustomEditorUtils.DrawScriptRef<PerObjectRTSource>(component);
            
            serializedObject.Update();
            /*bool isAutoBoundsModeEnable = _autoBoundsModeProp.boolValue;
            var isAutoBoundsModeEnable_Enum = (CustomEditorUtils.EnumPropDefault)EditorGUILayout.EnumPopup("Bounds Mode",
                (CustomEditorUtils.EnumPropDefault)(_autoBoundsModeProp.boolValue ? 1 : 0));
            _autoBoundsModeProp.boolValue = ((int)isAutoBoundsModeEnable_Enum != 0);*/
            EditorGUILayout.PropertyField(_autoBoundsModeProp);
            bool isAutoBoundsModeEnable = _autoBoundsModeProp.boolValue;
            
            GUI.enabled = !isAutoBoundsModeEnable;
            EditorGUILayout.PropertyField(_boundsProp);
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                float halfWidth = EditorGUIUtility.currentViewWidth * 0.615f;
                halfWidth -= 20f;
                if (GUILayout.Button("Auto Fit Bounds", GUILayout.Width(halfWidth)))
                {
                    Undo.RecordObject(component, "Calculate Auto Bounds");
                    component.CalculateAutoBounds();
                    EditorUtility.SetDirty(target);
                }
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_sortingMode, new GUIContent("Sorting Mode"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}