using AutoBattle.Scripts.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace AutoBattle.Scripts.Managers
{
    public class AtlasManager : Singleton<AtlasManager>
    {
        private SpriteAtlas itemIconAtlas;

        public void Initialize()
        {
            itemIconAtlas = Addressables.LoadAssetAsync<SpriteAtlas>("ItemIconAtlas").WaitForCompletion();
        }

        public Sprite GetItemIcon(string iconName)
        {
            var icon = itemIconAtlas.GetSprite(iconName);

            if (icon == null)
            {
                Debug.LogError($"Icon not found: {iconName}");
                return null;
            }

            return icon;
        }
    }
}