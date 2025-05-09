using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class FieldDropItemObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemIcon;

    [SerializeField] private float autoCollectDelay = 0.5f;     // ������ �� �ڵ� �������� ��� �ð�
    [SerializeField] private float flySpeed = 10f;              // ���ư��� �ӵ�
    [SerializeField] private Ease flyEase = Ease.InExpo;        // ���ư��� ��¡

    // ƨ�� ȿ�� ���� ������
    private float initialBounceHeight = 1.5f;  // ù ƨ�� ����
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

    [SerializeField] private GameObject fieldDropItemIconPrefab;  // �ν����Ϳ��� �Ҵ��� ������


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
            // ������ ���� (0.3 ~ 1.0)
            randomX = Random.Range(0.5f, 1.4f);
        }
        else
        {
            // ���� ���� (-1.0 ~ -0.3)
            randomX = Random.Range(-0.5f, -1.4f);
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

        // ������ Ÿ�Կ� ���� UI Ÿ�� ��������
        RectTransform targetUI = GetTargetUI();
        if (targetUI == null)
        {
            Debug.LogWarning("Ÿ�� UI�� ã�� �� �����ϴ�.");
            OnItemCollected();
            Destroy(gameObject);
            return;
        }

        // ���� ���� ��ġ�� ��ũ�� ��ǥ�� ��ȯ
        Vector3 screenPos = GameManager.Instance.mainCamera.WorldToScreenPoint(transform.position);

        // ������ �ν��Ͻ�ȭ
        GameObject uiItemObject = null;
        if (fieldDropItemIconPrefab != null)
        {
            // ������ ����
            uiItemObject = Instantiate(fieldDropItemIconPrefab, UIManager.Instance.fieldUICanvas.transform);

            // UI ��ġ ����
            RectTransform rectTransform = uiItemObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = screenPos;

                // ������ ���� �̹��� ������Ʈ ã��
                Image uiImage = uiItemObject.GetComponentInChildren<Image>();
                if (uiImage != null && itemIcon != null && itemIcon.sprite != null)
                {
                    // ��������Ʈ ����
                    uiImage.sprite = itemIcon.sprite;
                    uiImage.color = itemIcon.color;
                }

                // Ÿ�� UI�� ��ũ�� ��ġ ��������
                Vector3[] corners = new Vector3[4];
                targetUI.GetWorldCorners(corners);
                Vector3 targetPos = (corners[0] + corners[1] + corners[2] + corners[3]) / 4;

                // ���ư��� �ִϸ��̼� ������ ����
                Sequence collectSequence = DOTween.Sequence();

                // �ణ ���� �ö󰡴� ȿ��
                collectSequence.Append(
                    rectTransform.DOMove(screenPos + new Vector3(0, 30, 0), 0.1f)
                    .SetEase(Ease.OutQuad)
                );

                // Ÿ������ ���ư�
                float distance = Vector3.Distance(rectTransform.position, targetPos);
                float duration = distance / (flySpeed * 50f); // UI �����Ͽ� �°� �ӵ� ����

                collectSequence.Append(
                    rectTransform.DOMove(targetPos, duration)
                    .SetEase(flyEase)
                );

                // ���� �Ϸ� �� ������Ʈ ����
                collectSequence.OnComplete(() => {
                    // ������ ���� ���� �̺�Ʈ �߻�
                    OnItemCollected();

                    // UI ������Ʈ ����
                    Destroy(uiItemObject);
                });

                // ������ ����
                collectSequence.Play();
            }
        }

        // ���� �ʵ� ������ ��Ȱ��ȭ (�ִϸ��̼��� ���� �� ����)
        gameObject.SetActive(false);
        Destroy(gameObject, 2f); // �����ϰ� 2�� �� ����
    }


    // ������ Ÿ�Կ� ���� Ÿ�� UI ��������
    private RectTransform GetTargetUI()
    {
        FullWindowInGameDlg inGameUI = UIManager.Instance.GetUI<FullWindowInGameDlg>();
        if (inGameUI == null)
        {
            Debug.LogWarning("FullWindowInGameDlg�� ã�� �� �����ϴ�.");
            return null;
        }

        // ������ Ÿ�Կ� ���� ������ Ÿ�� UI ��ȯ
        if (itemData.f_type == ItemType.Currency)
        {
            if (inGameUI.currencyTabButton != null)
            {
                return inGameUI.currencyTabButton.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning("currencyTabButton�� null�Դϴ�.");
            }
        }
        else if (itemData.f_type == ItemType.Equipment_Item)
        {
            if (inGameUI.equipmentTabButton != null)
            {
                return inGameUI.equipmentTabButton.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning("equipmentTabButton�� null�Դϴ�.");
            }
        }

        // �⺻������ ȭ�� �߾��� ��Ÿ���� RectTransform ��ȯ
        GameObject centerObj = new GameObject("ScreenCenter");
        RectTransform centerRect = centerObj.AddComponent<RectTransform>();
        centerRect.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        centerObj.transform.SetParent(UIManager.Instance.fieldUICanvas.transform);

        // �� �ӽ� ������Ʈ�� ��� �� �����ϱ� ���� ���� ���� ����
        Destroy(centerObj, 1f);

        return centerRect;
    }

    // ������ ���� �Ϸ� �� ȣ��
    private void OnItemCollected()
    {
        Debug.Log($"{itemData.f_type} Ÿ�� ������ ���� �Ϸ�!");
        InventoryManager.Instance.OnFieldItemCollected(itemData);
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