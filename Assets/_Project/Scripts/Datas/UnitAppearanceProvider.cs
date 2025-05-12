using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using BansheeGz.BGDatabase;
using VHierarchy.Libs;
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
            if (!string.IsNullOrEmpty(spriteKey) && spriteKey.EndsWith("(Clone)"))
            {
                spriteKey = spriteKey.Substring(0, spriteKey.Length - "(Clone)".Length);
            }
            
            if (!string.IsNullOrEmpty(spriteKey) && atlasCache != null)
            {
                var sp = atlasCache.GetSprite(spriteKey);
                if (sp != null) sr.sprite = sp;
            }

            if (spriteKey == null)
            {
                sr.sprite = null;
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
                string rawName = sr.sprite?.name;
                
                if (!string.IsNullOrEmpty(rawName) && rawName.EndsWith("(Clone)"))
                {
                    rawName = rawName.Substring(0, rawName.Length - "(Clone)".Length);
                }
                
                propSprite.SetValue(entity, rawName);
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
        
        unitAppearance.Save();
        //SaveLoadManager.Instance.SaveData();
    }
    public void SaveAsAppearance(string entityName)
    {
        if (unitAppearance == null)
        {
            Debug.LogError("UnitAppearanceProvider: unitAppearance 필드가 할당되지 않았습니다.");
            return;
        }

        // 기존 엔티티가 없으면 생성하고, 이름(f_name)도 설정해 둡니다
        var entity = D_UnitAppearanceData.NewEntity();
        
        entity.f_name = entityName;
        unitAppearance.Entity = entity;

        var type = entity.GetType();
        foreach (var sr in spriteRenderers)
        {
            var key = MakeSafeName(sr.name);
            var propSprite = type.GetProperty($"f_{key}SpriteKey");
            var propTint = type.GetProperty($"f_{key}Tint");
            var propEn = type.GetProperty($"f_{key}Enabled");

            if (propSprite != null)
            {
                string rawName = sr.sprite?.name;
                
                if (!string.IsNullOrEmpty(rawName) && rawName.EndsWith("(Clone)"))
                {
                    rawName = rawName.Substring(0, rawName.Length - "(Clone)".Length);
                }
                
                propSprite.SetValue(entity, rawName);
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
        
        unitAppearance.Save();
    }
}
#if UNITY_EDITOR

[CustomEditor(typeof(UnitAppearanceProvider))]
public class UnitAppearanceProviderEditor : Editor
{
    private const string DefaultSaveAsName = "NewAppearance";
    private const float WindowWidth = 300;
    private const float WindowHeight = 80;
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
        if (GUILayout.Button("Save as.."))
        {
            Vector2 guiPos    = Event.current.mousePosition;
            Vector2 screenPos = GUIUtility.GUIToScreenPoint(guiPos);
            
            SaveAsAppearanceWindow.Open(prov, screenPos);
        }
    }
    public class SaveAsAppearanceWindow : EditorWindow
    {
        private UnitAppearanceProvider _provider;
        private string _newName = DefaultSaveAsName;

        public static void Open(UnitAppearanceProvider provider, Vector2 screenPosition)
        {
            var window = GetWindow<SaveAsAppearanceWindow>(true, "Save Appearance As", true);
            window._provider = provider;
            window._newName  = DefaultSaveAsName;

            // ❷: 팝업이 마우스 바로 위에 뜨도록 position 설정
            window.position = new Rect(
                screenPosition.x,
                screenPosition.y - WindowHeight, // 버튼 위에 뜨게 살짝 위로
                WindowWidth,
                WindowHeight
            );
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Enter new appearance name:", EditorStyles.boldLabel);
            _newName = EditorGUILayout.TextField(_newName);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                if (string.IsNullOrWhiteSpace(_newName))
                {
                    EditorUtility.DisplayDialog("Error", "Name cannot be empty.", "OK");
                }
                else
                {
                    _provider.SaveAsAppearance(_newName);
                    Close();
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
