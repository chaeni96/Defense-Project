using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FieldDropItemObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemIcon;

    [SerializeField] private float autoCollectDelay = 0.5f;     // ������ �� �ڵ� �������� ��� �ð�
    [SerializeField] private float flySpeed = 10f;              // ���ư��� �ӵ�
    [SerializeField] private Ease flyEase = Ease.InExpo;        // ���ư��� ��¡
    [SerializeField] private float scaleDownDuration = 0.5f;    // ũ�� �پ��� �ð�

    [SerializeField] private Material itemTraolMat;

    // ƨ�� ȿ�� ���� ������
     private float initialBounceHeight = 1f;  // ù ƨ�� ����
     private float bounceDuration = 0.15f;      // ƨ�� ���� �ð�
     private float fallDuration = 0.1f;         // �������� ���� �ð�
     private Ease bounceEase = Ease.OutQuad;    // ƨ�� ��¡
     private Ease fallEase = Ease.InQuad;       // ������ ��¡
     private int bounceCount = 2;               // ƨ�� Ƚ��
     private float bounceReduction = 0.3f;      // ƨ�� ���� ���� ����

    // �εս� ���ٴϴ� ȿ�� ���� ������
     private float floatHeight = 0.15f;          // ���ٴϴ� ����
     private float floatDuration = 1f;        // ���ٴϴ� �ֱ�
     private Ease floatEase = Ease.InOutSine;   // ���ٴϴ� ��¡

    private AsyncOperationHandle<Sprite> sprite;
    private Vector3 finalPosition;                              // ���� ��ġ �����
    private Sequence floatingSequence;                          // ���ٴϴ� ������ �����

    private D_ItemData itemData;

    private void Awake()
    {
        // �ʱ⿡�� �������� ����
        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    public void InitializeItem(D_ItemData item)
    {
        itemData = item;
    }

    public void LoadItemIcon(string addressablekey)
    {
        // ������ �ε��� �̹����� ������ ����
        if (sprite.IsValid())
        {
            Addressables.Release(sprite);
        }

        if (!string.IsNullOrEmpty(addressablekey))
        {
            // �񵿱�� �̹��� �ε�
            sprite = Addressables.LoadAssetAsync<Sprite>(addressablekey);
            sprite.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    itemIcon.sprite = handle.Result;
                    itemIcon.enabled = true;
                    PlayBounceAnimation();
                }
                else
                {
                    Debug.LogWarning($"������ �̹��� �ε� ����: {addressablekey}");
                }
            };
        }
    }

    // ������ ���� �� ȣ���Ͽ� ƨ�� ȿ�� ����
    public void PlayBounceAnimation()
    {
        // ���� ��ġ ����
        Vector3 startPosition = transform.position;

        // ���� ���� ���� (���� �Ǵ� ������)
        bool isRight = Random.value > 0.5f;

        float randomX;
        if (isRight)
        {
            // ������ ���� (1.0 ~ 1.5)
            randomX = Random.Range(0.3f, 1f);
        }
        else
        {
            // ���� ���� (-1.5 ~ -1.0)
            randomX = Random.Range(-0.3f, -1f);
        }

        Vector3 bounceOffset = new Vector3(randomX, 0, 0);

        // ƨ�� ��ǥ ��ġ ���
        Vector3 endPosition = startPosition + bounceOffset;
        finalPosition = endPosition;  // ���� ��ġ ����

        // ������ ��� �߰� ������ (���� ��ġ)
        float peakHeight = initialBounceHeight * 1.5f; // �� ���� ����
        Vector3 controlPoint = startPosition + (endPosition - startPosition) * 0.5f + Vector3.up * peakHeight;

        // ������ ����
        Sequence bounceSequence = DOTween.Sequence();

        // ������ ��� ���� �̵�
        float duration = bounceDuration * 2f; // ������ ���� �ð�

        bounceSequence.Append(
            DOTween.To(
                () => 0f,
                (float t) => {
                    // ������ � ��� (2�� ������ �)
                    Vector3 newPos = (1 - t) * (1 - t) * startPosition +
                                     2 * (1 - t) * t * controlPoint +
                                     t * t * endPosition;
                    transform.position = newPos;
                },
                1f,
                duration
            ).SetEase(Ease.Linear) // ������ ���� ��¡�� ���ԵǾ� �����Ƿ� Linear ���
        );

        // �߷¿� ���� �ٿ ȿ�� ����
        float currentBounceHeight = initialBounceHeight * 0.3f; // ù ��° ū �ٿ �� ���� �ٿ

        for (int i = 0; i < bounceCount; i++)
        {
            // ���� ƨ��� �ִϸ��̼�
            Vector3 bouncePos = endPosition + Vector3.up * currentBounceHeight;
            bounceSequence.Append(transform.DOMove(bouncePos, bounceDuration * (1f - 0.2f * i))
                .SetEase(bounceEase));

            // �������� �ִϸ��̼�
            bounceSequence.Append(transform.DOMove(endPosition, fallDuration * (1f - 0.1f * i))
                .SetEase(fallEase));

            // ƨ�� ���� ����
            currentBounceHeight *= bounceReduction;
        }

        // ƨ�� �ִϸ��̼��� ������ �εս� ���ٴϴ� �ִϸ��̼� ����
        bounceSequence.OnComplete(() => {
            StartFloatingAnimation();
            // �ڵ� ���� ���� (������ ��)
            StartCoroutine(AutoCollectAfterDelay(autoCollectDelay));
        });

        // ������ ����
        bounceSequence.Play();
    }

    // �εս� ���ٴϴ� �ִϸ��̼�
    private void StartFloatingAnimation()
    {
        // ���� �������� �ִٸ� ����
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // �� ������ ����
        floatingSequence = DOTween.Sequence();

        // ���� ������ �ִϸ��̼� (������ �ݺ�)
        floatingSequence.Append(
            transform.DOLocalMoveY(finalPosition.y + floatHeight, floatDuration / 2)
            .SetEase(floatEase)
        );
        floatingSequence.Append(
            transform.DOLocalMoveY(finalPosition.y, floatDuration / 2)
            .SetEase(floatEase)
        );


        // ���� ������ �ִϸ��̼� ���� �ݺ�
        floatingSequence.SetLoops(-1, LoopType.Restart);

        // ������ ����
        floatingSequence.Play();
    }

    // ���� �� �ڵ� ���� ����
    private IEnumerator AutoCollectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CollectItem();
    }

    // ������ ���� �ִϸ��̼�
    private void CollectItem()
    {
        // ���ٴϴ� �ִϸ��̼� ����
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // ������ Ÿ�Կ� ���� ��ǥ ��ġ ����
        Vector3 targetPosition = GetTargetPositionByItemType();

        // ���ư��� �ִϸ��̼� ������ ����
        Sequence collectSequence = DOTween.Sequence();

        // ���� �ణ ���� �ö󰡴� ȿ�� (�������� �����̱� �������� ǥ��)
        collectSequence.Append(
        transform.DOMove(transform.position + Vector3.up * 0.3f, 0.1f)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => {
         // ���� �ö󰡴� ȿ���� �Ϸ�Ǿ��� �� ��Ƽ���� ����
             if (itemTraolMat != null && itemIcon != null)
            {
             itemIcon.material = itemTraolMat;
            }
        })
    );

        // Ÿ������ ���ư�
        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = distance / flySpeed;  // �ӵ��� ���� �ð� ���

        collectSequence.Append(
            transform.DOMove(targetPosition, duration)
            .SetEase(flyEase)
        );

        // ���ÿ� �پ��� ȿ��
        collectSequence.Join(
            transform.DOScale(Vector3.zero, scaleDownDuration)
            .SetEase(Ease.InBack)
            .SetDelay(duration - scaleDownDuration)  // ���� ������ �پ��� ����
        );

        // ���� �Ϸ� �� ������Ʈ ����
        collectSequence.OnComplete(() => {
            // ������ ���� ���� �̺�Ʈ �߻� ����
            OnItemCollected();

            // ���� ������Ʈ ����
            Destroy(gameObject);
        });

        // ������ ����
        collectSequence.Play();
    }


    // Ÿ�Ժ� ��ǥ ��ġ ��ȯ
    private Vector3 GetTargetPositionByItemType()
    {
        // UI ĵ���� ���� ��ġ�� ������� �κ��丮�� �ش� ������ ���� ��ġ�� ���
        // ���� ������ UI ĵ������ ���� ��ġ�� �����ؾ� ��

        // �ӽ� ȭ�� ��ġ (�����δ� UI ĵ������ ���� ��ġ�� �����;� ��)

        if (itemData.f_itemType == ItemType.Currency)
        {

            return new Vector3(-2.82f, -6f, 0);

        }
        else if(itemData.f_itemType == ItemType.Equipment_Item)
        {
            return new Vector3(-2.16f, -6f, 0);
        }

        return new Vector3(0, 0, 0);

      
    }

    // ������ ���� �Ϸ� �� ȣ��
    private void OnItemCollected()
    {

        Debug.Log($"{itemData.f_itemType} Ÿ�� ������ ���� �Ϸ�!");

        // TODO: �̺�Ʈ �ý����̳� GameManager�� ���� ������ ���� �˸�
    }

    public void PlayBounceAnimationImmediate(Vector3 startPosition)
    {
        transform.position = startPosition;
        PlayBounceAnimation();
    }


    private void OnDestroy()
    {
        // ������ ����
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // ��巹���� ���ҽ� ����
        if (sprite.IsValid())
        {
            Addressables.Release(sprite);
        }
    }
}