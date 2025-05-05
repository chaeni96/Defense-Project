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

        // ������ ����
        if (itemIcon != null && item.f_iconImage != null)
        {
            // ������ �ε�� ���ҽ��� �ִٸ� ����
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.f_iconImage.f_addressableKey);
            spriteHandle.Completed += (handle) =>
            {
                // �� ������ gameObject�� �ı��Ǿ��ų� itemIcon�� null���� Ȯ��
                if (this == null || !this.gameObject || itemIcon == null)
                {
                    // ��ü�� �ı��� ���, �ε��� ���ҽ� �����ϰ� ����
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
                    Debug.LogError($"������ ������ �ε� ����: {item.f_iconImage.f_addressableKey}");
                }
            };
        }

        
    }


    private void OnDestroy()
    {
        // Addressable ����
        if (spriteHandle.IsValid())
        {
            // ���⼭ �ݹ� ���ŵ� �����ϸ� ����
            Addressables.Release(spriteHandle);
        }
    }

}
