using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationZFixer
{
    [MenuItem("Tools/Animation/Freeze Z in Selected Clips")]
    static void FreezeZInClips()
    {
        foreach (var clip in Selection.GetFiltered<AnimationClip>(SelectionMode.DeepAssets))
        {
            // 1) 기존 Z 커브 삭제하면서 path 정보도 수집
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var zPaths = new List<string>();
            foreach (var b in bindings)
            {
                if (b.propertyName.EndsWith("m_LocalPosition.z"))
                {
                    zPaths.Add(b.path);
                    AnimationUtility.SetEditorCurve(clip, b, null);
                }
            }

            // 2) 수집한 각 path 에 대해 Z=0 상수 커브 추가
            foreach (var p in zPaths)
            {
                var zeroCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(clip.length, 0f)
                );
                var zBinding = new EditorCurveBinding
                {
                    path = p,
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.z"
                };
                AnimationUtility.SetEditorCurve(clip, zBinding, zeroCurve);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Selected clips frozen Z position.");
    }
}
