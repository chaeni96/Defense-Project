using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class SlotItemObject : MonoBehaviour
{

    [SerializeField] private Image itemIcon;

    private BGId itemId;
    private D_ItemData itemData;
    private AsyncOperationHandle<Sprite> spriteHandle;


    public void InitializeSlot(BGId id, D_ItemData item)
    {
        if (item == null) return;

        itemId = id;
        itemData = item;

        // 아이콘 설정
        if (itemIcon != null && item.f_iconImage != null)
        {
            // 기존에 로드된 리소스가 있다면 해제
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.f_iconImage.f_addressableKey);
            spriteHandle.Completed += (handle) =>
            {
                // 이 시점에 gameObject가 파괴되었거나 itemIcon이 null인지 확인
                if (this == null || !this.gameObject || itemIcon == null)
                {
                    // 객체가 파괴된 경우, 로드한 리소스 해제하고 리턴
                    Addressables.Release(handle);
                    return;
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    itemIcon.sprite = handle.Result;
                    itemIcon.enabled = true;
                }
                else
                {
                    Debug.LogError($"아이템 아이콘 로드 실패: {item.f_iconImage.f_addressableKey}");
                }
            };
        }

        
    }


    private void OnDestroy()
    {
        // Addressable 해제
        if (spriteHandle.IsValid())
        {
            // 여기서 콜백 제거도 가능하면 제거
            Addressables.Release(spriteHandle);
        }
    }

}
