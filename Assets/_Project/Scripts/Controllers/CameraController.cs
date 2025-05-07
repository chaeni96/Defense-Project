using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 3f; // 카메라 이동 부드러움 정도
    [SerializeField] private Vector3 originalPosition; // 초기 카메라 위치
    [SerializeField] private float stopX = 20f; // 카메라가 멈출 X 좌표 (킬존)
    [SerializeField] private float stopDistance = 0.1f; // 도착했다고 판단할 거리
    [SerializeField] private float leadOffset = 2f; // 유닛보다 얼마나 앞서 있을지 (X축 거리)

    private bool isFollowing = false;
    private Transform targetUnit; // 현재 쫓아가는 유닛

    private void Start()
    {
        // 초기 위치 저장
        originalPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (isFollowing)
        {
            // 현재 X 위치가 stopX에 도달했는지 확인
            if (Mathf.Abs(transform.position.x - stopX) <= stopDistance)
            {
                isFollowing = false;
                return;
            }

            // 가장 앞선 유닛 찾기 -> 처음 배치된것과 실제로 달려나갈때 moveSpeed가 유닛마다 달라서 리드유닛이 달라질수있음
            FindLeadUnit();

            if (targetUnit != null)
            {
                // 현재 카메라 위치 저장
                Vector3 currentPosition = transform.position;

                // 타겟 위치 계산 (X만 유닛을 따라가고 Y, Z는 현재 값 유지)
                // 유닛의 X 위치에 leadOffset을 더해 카메라가 유닛보다 앞서도록 함
                Vector3 targetPosition = new Vector3(
                    targetUnit.position.x + leadOffset,  // X는 유닛의 X 위치보다 leadOffset만큼 앞
                    currentPosition.y,                   // Y는 카메라의 현재 Y 위치 유지
                    currentPosition.z                    // Z는 카메라의 현재 Z 위치 유지
                );

                // 부드럽게 이동
                transform.position = Vector3.Lerp(currentPosition, targetPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                // 유닛이 없으면 즉시 원위치로
                ReturnToOriginalPositionImmediately();
            }
        }
    }

    private void FindLeadUnit()
    {
        // 모든 유닛 가져오기
        List<UnitController> allUnits = UnitManager.Instance.GetAllUnits();

        // X 좌표가 가장 큰 활성화된 유닛 찾기
        UnitController leadUnit = allUnits
            .Where(unit => unit != null && unit.isActive)
            .OrderByDescending(unit => unit.transform.position.x)
            .FirstOrDefault();

        // 타겟 유닛 업데이트
        targetUnit = leadUnit?.transform;
    }

    // 외부에서 호출할 메서드들
    public void StartFollowing()
    {
        FindLeadUnit();
        isFollowing = true;
    }

    public void StopFollowing()
    {
        isFollowing = false;
    }

    // 즉시 원래 위치로 돌아가는 메서드
    public void ReturnToOriginalPositionImmediately()
    {
        isFollowing = false;
        transform.position = originalPosition;
    }

    // FSM 연동 메서드
    public void OnBattleStart()
    {
        StartFollowing();
    }

    public void OnBattleEnd()
    {
        ReturnToOriginalPositionImmediately();
    }
}