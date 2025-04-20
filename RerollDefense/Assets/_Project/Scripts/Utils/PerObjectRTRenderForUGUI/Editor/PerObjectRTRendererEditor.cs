using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using CatDarkGame.PerObjectRTRenderForUGUI_Editor.Shared;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    [CustomEditor(typeof(PerObjectRTRenderer), true)]
    [CanEditMultipleObjects]
    public class PerObjectRTRendererEditor : GraphicEditor
    {
        private SerializedProperty _sourceProp;
        private GUIContent _sourceContent;

        protected override void OnEnable()
        {
            base.OnEnable();

            _sourceProp = serializedObject.FindProperty("_source");
            _sourceContent = EditorGUIUtility.TrTextContent("Source Object");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            PerObjectRTRenderer component = (PerObjectRTRenderer)target;
            CustomEditorUtils.DrawScriptRef<PerObjectRTRenderer>(component);
            
            serializedObject.Update();
            EditorGUILayout.PropertyField(_sourceProp, _sourceContent);
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                float halfWidth = EditorGUIUtility.currentViewWidth * 0.615f;
                halfWidth -= 20f;
                if (GUILayout.Button("Auto Fit Rect", GUILayout.Width(halfWidth)))
                {
                    Undo.RecordObject(component, "Calculate Auto Rect");
                    component.CalculateAutoRect();
                    EditorUtility.SetDirty(target);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}