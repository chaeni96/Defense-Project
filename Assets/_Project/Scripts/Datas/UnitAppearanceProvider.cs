using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using BansheeGz.BGDatabase;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 주입?기
/// </summary>
public class UnitAppearanceProvider : MonoBehaviour
{
    public BGEntityGo unitAppearance;
    public List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

    private static SpriteAtlas atlasCache; //일단 걍 전역

    void EnsureAtlas()
    {
        if (atlasCache == null)
            atlasCache = Addressables
                .LoadAssetAsync<SpriteAtlas>("Unit_AppearanceAtlas")
                .WaitForCompletion();
    }

    string MakeSafeName(string raw)
    {
        var sb = new StringBuilder();
        foreach (var c in raw)
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        var s = sb.ToString();
        return string.IsNullOrEmpty(s) || !char.IsLetter(s[0]) ? "F" + s : s;
    }

    public void LoadAppearance()
    {
        EnsureAtlas();
        if (unitAppearance?.Entity is not D_UnitAppearanceData entity) return;

        var type = entity.GetType();
        foreach (var sr in spriteRenderers)
        {
            var key = MakeSafeName(sr.name);
            var propSprite = type.GetProperty($"f_{key}SpriteKey");
            var propTint = type.GetProperty($"f_{key}Tint");
            var propEn = type.GetProperty($"f_{key}Enabled");

            // Sprite
            var spriteKey = propSprite != null ? (string)propSprite.GetValue(entity) : null;
            if (!string.IsNullOrEmpty(spriteKey) && atlasCache != null)
            {
                var sp = atlasCache.GetSprite(spriteKey);
                if (sp != null) sr.sprite = sp;
            }

            // Tint
            if (propTint != null)
            {
                sr.color = (Color)propTint.GetValue(entity);
            }

            // Enabled
            if (propEn != null)
            {
                sr.enabled = (bool)propEn.GetValue(entity);
            }
        }
    }

    /// <summary>현재 SpriteRenderer 상태를 BGDatabase에 저장</summary>
    public void SaveAppearance()
    {
        if (unitAppearance == null)
        {
            Debug.LogError("UnitAppearanceProvider: unitAppearance 필드가 할당되지 않았습니다.");
            return;
        }

        // 기존 엔티티가 없으면 생성하고, 이름(f_name)도 설정해 둡니다
        var entity = unitAppearance.Entity as D_UnitAppearanceData
                  ?? D_UnitAppearanceData.NewEntity();
        if (unitAppearance.Entity == null)
        {
            entity.f_name = "Appearance_" + gameObject.name;
            unitAppearance.Entity = entity;
        }

        var type = entity.GetType();
        foreach (var sr in spriteRenderers)
        {
            var key = MakeSafeName(sr.name);
            var propSprite = type.GetProperty($"f_{key}SpriteKey");
            var propTint = type.GetProperty($"f_{key}Tint");
            var propEn = type.GetProperty($"f_{key}Enabled");

            if (propSprite != null)
            {
                propSprite.SetValue(entity, sr.sprite?.name);
            }

            if (propTint != null)
            {
                propTint.SetValue(entity, sr.color);
            }

            if (propEn != null)
            {
                propEn.SetValue(entity, sr.enabled);
                }
            }

        // 실제 DB에 commit
        SaveLoadManager.Instance.SaveData();
    }
}
#if UNITY_EDITOR

[CustomEditor(typeof(UnitAppearanceProvider))]
public class UnitAppearanceProviderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var prov = (UnitAppearanceProvider)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Load Appearance"))
        {
            prov.LoadAppearance();
            // 변경된 SpriteRenderer가 프리뷰에 반영되도록
            EditorUtility.SetDirty(prov);
        }

        if (GUILayout.Button("Save Appearance"))
        {
            prov.SaveAppearance();
        }
    }
}
#endif
