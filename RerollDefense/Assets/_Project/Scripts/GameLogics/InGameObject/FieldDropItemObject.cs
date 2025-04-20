using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FieldDropItemObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemIcon;

    // ƨ�� ȿ�� ���� ������
     private float initialBounceHeight = 1f;  // ù ƨ�� ����
     private float bounceDuration = 0.15f;      // ƨ�� ���� �ð�
     private float fallDuration = 0.1f;         // �������� ���� �ð�
     private Ease bounceEase = Ease.OutQuad;    // ƨ�� ��¡
     private Ease fallEase = Ease.InQuad;       // ������ ��¡
     private float randomHorizontalForce = 1f; // ���� ���� �̵� ����
     private int bounceCount = 2;               // ƨ�� Ƚ��
     private float bounceReduction = 0.3f;      // ƨ�� ���� ���� ����

    // �εս� ���ٴϴ� ȿ�� ���� ������
     private float floatHeight = 0.15f;          // ���ٴϴ� ����
     private float floatDuration = 1f;        // ���ٴϴ� �ֱ�
     private Ease floatEase = Ease.InOutSine;   // ���ٴϴ� ��¡

    private AsyncOperationHandle<Sprite> sprite;
    private Vector3 finalPosition;                              // ���� ��ġ �����
    private Sequence floatingSequence;                          // ���ٴϴ� ������ �����

    private void Awake()
    {
        // �ʱ⿡�� �������� ����
        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    private void OnDestroy()
    {
        // ������ ����
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }
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

    public void PlayBounceAnimationImmediate(Vector3 startPosition)
    {
        transform.position = startPosition;
        PlayBounceAnimation();
    }
}