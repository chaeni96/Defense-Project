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
            // 1) ���� Z Ŀ�� �����ϸ鼭 path ������ ����
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

            // 2) ������ �� path �� ���� Z=0 ��� Ŀ�� �߰�
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
