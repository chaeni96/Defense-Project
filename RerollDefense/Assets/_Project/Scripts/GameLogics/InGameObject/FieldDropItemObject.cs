using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FieldDropItemObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemIcon;

    // 튕김 효과 관련 변수들
     private float initialBounceHeight = 1f;  // 첫 튕김 높이
     private float bounceDuration = 0.15f;      // 튕김 지속 시간
     private float fallDuration = 0.1f;         // 떨어지는 지속 시간
     private Ease bounceEase = Ease.OutQuad;    // 튕김 이징
     private Ease fallEase = Ease.InQuad;       // 떨어짐 이징
     private float randomHorizontalForce = 1f; // 랜덤 수평 이동 정도
     private int bounceCount = 2;               // 튕김 횟수
     private float bounceReduction = 0.3f;      // 튕김 높이 감소 비율

    // 두둥실 떠다니는 효과 관련 변수들
     private float floatHeight = 0.15f;          // 떠다니는 높이
     private float floatDuration = 1f;        // 떠다니는 주기
     private Ease floatEase = Ease.InOutSine;   // 떠다니는 이징

    private AsyncOperationHandle<Sprite> sprite;
    private Vector3 finalPosition;                              // 최종 위치 저장용
    private Sequence floatingSequence;                          // 떠다니는 시퀀스 저장용

    private void Awake()
    {
        // 초기에는 아이콘을 숨김
        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    private void OnDestroy()
    {
        // 시퀀스 정리
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }
    }

    public void LoadItemIcon(string addressablekey)
    {
        // 이전에 로드한 이미지가 있으면 해제
        if (sprite.IsValid())
        {
            Addressables.Release(sprite);
        }

        if (!string.IsNullOrEmpty(addressablekey))
        {
            // 비동기로 이미지 로드
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
                    Debug.LogWarning($"아이템 이미지 로드 실패: {addressablekey}");
                }
            };
        }
    }

    // 아이템 생성 시 호출하여 튕김 효과 실행
    // 아이템 생성 시 호출하여 튕김 효과 실행
    public void PlayBounceAnimation()
    {
        // 현재 위치 저장
        Vector3 startPosition = transform.position;

        // 방향 랜덤 결정 (왼쪽 또는 오른쪽)
        bool isRight = Random.value > 0.5f;

        float randomX;
        if (isRight)
        {
            // 오른쪽 방향 (1.0 ~ 1.5)
            randomX = Random.Range(0.3f, 1f);
        }
        else
        {
            // 왼쪽 방향 (-1.5 ~ -1.0)
            randomX = Random.Range(-0.3f, -1f);
        }

        Vector3 bounceOffset = new Vector3(randomX, 0, 0);

        // 튕김 목표 위치 계산
        Vector3 endPosition = startPosition + bounceOffset;
        finalPosition = endPosition;  // 최종 위치 저장

        // 베지어 곡선용 중간 제어점 (높은 위치)
        float peakHeight = initialBounceHeight * 1.5f; // 더 높은 정점
        Vector3 controlPoint = startPosition + (endPosition - startPosition) * 0.5f + Vector3.up * peakHeight;

        // 시퀀스 생성
        Sequence bounceSequence = DOTween.Sequence();

        // 베지어 곡선을 따라 이동
        float duration = bounceDuration * 2f; // 적절한 지속 시간

        bounceSequence.Append(
            DOTween.To(
                () => 0f,
                (float t) => {
                    // 베지어 곡선 계산 (2차 베지어 곡선)
                    Vector3 newPos = (1 - t) * (1 - t) * startPosition +
                                     2 * (1 - t) * t * controlPoint +
                                     t * t * endPosition;
                    transform.position = newPos;
                },
                1f,
                duration
            ).SetEase(Ease.Linear) // 베지어 내에 이징이 포함되어 있으므로 Linear 사용
        );

        // 중력에 의한 바운스 효과 구현
        float currentBounceHeight = initialBounceHeight * 0.3f; // 첫 번째 큰 바운스 후 작은 바운스

        for (int i = 0; i < bounceCount; i++)
        {
            // 위로 튕기는 애니메이션
            Vector3 bouncePos = endPosition + Vector3.up * currentBounceHeight;
            bounceSequence.Append(transform.DOMove(bouncePos, bounceDuration * (1f - 0.2f * i))
                .SetEase(bounceEase));

            // 떨어지는 애니메이션
            bounceSequence.Append(transform.DOMove(endPosition, fallDuration * (1f - 0.1f * i))
                .SetEase(fallEase));

            // 튕김 높이 감소
            currentBounceHeight *= bounceReduction;
        }

        // 튕김 애니메이션이 끝나면 두둥실 떠다니는 애니메이션 시작
        bounceSequence.OnComplete(() => {
            StartFloatingAnimation();
        });

        // 시퀀스 실행
        bounceSequence.Play();
    }
    // 두둥실 떠다니는 애니메이션
    private void StartFloatingAnimation()
    {
        // 이전 시퀀스가 있다면 정리
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // 새 시퀀스 생성
        floatingSequence = DOTween.Sequence();

        // 수직 움직임 애니메이션 (영원히 반복)
        floatingSequence.Append(
            transform.DOLocalMoveY(finalPosition.y + floatHeight, floatDuration / 2)
            .SetEase(floatEase)
        );
        floatingSequence.Append(
            transform.DOLocalMoveY(finalPosition.y, floatDuration / 2)
            .SetEase(floatEase)
        );


        // 수직 움직임 애니메이션 무한 반복
        floatingSequence.SetLoops(-1, LoopType.Restart);

        // 시퀀스 실행
        floatingSequence.Play();
    }

    public void PlayBounceAnimationImmediate(Vector3 startPosition)
    {
        transform.position = startPosition;
        PlayBounceAnimation();
    }
}