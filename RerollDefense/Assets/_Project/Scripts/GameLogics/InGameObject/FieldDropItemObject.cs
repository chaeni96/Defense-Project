using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FieldDropItemObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemIcon;

    [SerializeField] private float autoCollectDelay = 0.5f;     // 떨어진 후 자동 수집까지 대기 시간
    [SerializeField] private float flySpeed = 10f;              // 날아가는 속도
    [SerializeField] private Ease flyEase = Ease.InExpo;        // 날아가는 이징
    [SerializeField] private float scaleDownDuration = 0.5f;    // 크기 줄어드는 시간

    [SerializeField] private Material itemTraolMat;

    // 튕김 효과 관련 변수들
     private float initialBounceHeight = 1f;  // 첫 튕김 높이
     private float bounceDuration = 0.15f;      // 튕김 지속 시간
     private float fallDuration = 0.1f;         // 떨어지는 지속 시간
     private Ease bounceEase = Ease.OutQuad;    // 튕김 이징
     private Ease fallEase = Ease.InQuad;       // 떨어짐 이징
     private int bounceCount = 2;               // 튕김 횟수
     private float bounceReduction = 0.3f;      // 튕김 높이 감소 비율

    // 두둥실 떠다니는 효과 관련 변수들
     private float floatHeight = 0.15f;          // 떠다니는 높이
     private float floatDuration = 1f;        // 떠다니는 주기
     private Ease floatEase = Ease.InOutSine;   // 떠다니는 이징

    private AsyncOperationHandle<Sprite> sprite;
    private Vector3 finalPosition;                              // 최종 위치 저장용
    private Sequence floatingSequence;                          // 떠다니는 시퀀스 저장용

    private D_ItemData itemData;

    private void Awake()
    {
        // 초기에는 아이콘을 숨김
        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    public void InitializeItem(D_ItemData item)
    {
        itemData = item;
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
            // 자동 수집 시작 (딜레이 후)
            StartCoroutine(AutoCollectAfterDelay(autoCollectDelay));
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

    // 지연 후 자동 수집 시작
    private IEnumerator AutoCollectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CollectItem();
    }

    // 아이템 수집 애니메이션
    private void CollectItem()
    {
        // 떠다니는 애니메이션 중지
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // 아이템 타입에 따른 목표 위치 결정
        Vector3 targetPosition = GetTargetPositionByItemType();

        // 날아가는 애니메이션 시퀀스 생성
        Sequence collectSequence = DOTween.Sequence();

        // 먼저 약간 위로 올라가는 효과 (아이템이 움직이기 시작함을 표시)
        collectSequence.Append(
        transform.DOMove(transform.position + Vector3.up * 0.3f, 0.1f)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => {
         // 위로 올라가는 효과가 완료되었을 때 머티리얼 변경
             if (itemTraolMat != null && itemIcon != null)
            {
             itemIcon.material = itemTraolMat;
            }
        })
    );

        // 타겟으로 날아감
        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = distance / flySpeed;  // 속도에 따른 시간 계산

        collectSequence.Append(
            transform.DOMove(targetPosition, duration)
            .SetEase(flyEase)
        );

        // 동시에 줄어드는 효과
        collectSequence.Join(
            transform.DOScale(Vector3.zero, scaleDownDuration)
            .SetEase(Ease.InBack)
            .SetDelay(duration - scaleDownDuration)  // 도착 직전에 줄어들기 시작
        );

        // 수집 완료 후 오브젝트 제거
        collectSequence.OnComplete(() => {
            // 아이템 수집 성공 이벤트 발생 가능
            OnItemCollected();

            // 게임 오브젝트 제거
            Destroy(gameObject);
        });

        // 시퀀스 실행
        collectSequence.Play();
    }


    // 타입별 목표 위치 반환
    private Vector3 GetTargetPositionByItemType()
    {
        // UI 캔버스 월드 위치를 기반으로 인벤토리의 해당 아이템 슬롯 위치를 계산
        // 실제 구현시 UI 캔버스와 슬롯 위치를 참조해야 함

        // 임시 화면 위치 (실제로는 UI 캔버스의 월드 위치를 가져와야 함)

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

    // 아이템 수집 완료 시 호출
    private void OnItemCollected()
    {

        Debug.Log($"{itemData.f_itemType} 타입 아이템 수집 완료!");

        // TODO: 이벤트 시스템이나 GameManager를 통해 아이템 수집 알림
    }

    public void PlayBounceAnimationImmediate(Vector3 startPosition)
    {
        transform.position = startPosition;
        PlayBounceAnimation();
    }


    private void OnDestroy()
    {
        // 시퀀스 정리
        if (floatingSequence != null && floatingSequence.IsActive())
        {
            floatingSequence.Kill();
        }

        // 어드레서블 리소스 해제
        if (sprite.IsValid())
        {
            Addressables.Release(sprite);
        }
    }
}