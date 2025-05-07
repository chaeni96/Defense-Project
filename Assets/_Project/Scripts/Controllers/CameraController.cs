using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 3f; // ī�޶� �̵� �ε巯�� ����
    [SerializeField] private Vector3 originalPosition; // �ʱ� ī�޶� ��ġ
    [SerializeField] private float stopX = 20f; // ī�޶� ���� X ��ǥ (ų��)
    [SerializeField] private float stopDistance = 0.1f; // �����ߴٰ� �Ǵ��� �Ÿ�
    [SerializeField] private float leadOffset = 2f; // ���ֺ��� �󸶳� �ռ� ������ (X�� �Ÿ�)

    private bool isFollowing = false;
    private Transform targetUnit; // ���� �Ѿư��� ����

    private void Start()
    {
        // �ʱ� ��ġ ����
        originalPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (isFollowing)
        {
            // ���� X ��ġ�� stopX�� �����ߴ��� Ȯ��
            if (Mathf.Abs(transform.position.x - stopX) <= stopDistance)
            {
                isFollowing = false;
                return;
            }

            // ���� �ռ� ���� ã�� -> ó�� ��ġ�ȰͰ� ������ �޷������� moveSpeed�� ���ָ��� �޶� ���������� �޶���������
            FindLeadUnit();

            if (targetUnit != null)
            {
                // ���� ī�޶� ��ġ ����
                Vector3 currentPosition = transform.position;

                // Ÿ�� ��ġ ��� (X�� ������ ���󰡰� Y, Z�� ���� �� ����)
                // ������ X ��ġ�� leadOffset�� ���� ī�޶� ���ֺ��� �ռ����� ��
                Vector3 targetPosition = new Vector3(
                    targetUnit.position.x + leadOffset,  // X�� ������ X ��ġ���� leadOffset��ŭ ��
                    currentPosition.y,                   // Y�� ī�޶��� ���� Y ��ġ ����
                    currentPosition.z                    // Z�� ī�޶��� ���� Z ��ġ ����
                );

                // �ε巴�� �̵�
                transform.position = Vector3.Lerp(currentPosition, targetPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                // ������ ������ ��� ����ġ��
                ReturnToOriginalPositionImmediately();
            }
        }
    }

    private void FindLeadUnit()
    {
        // ��� ���� ��������
        List<UnitController> allUnits = UnitManager.Instance.GetAllUnits();

        // X ��ǥ�� ���� ū Ȱ��ȭ�� ���� ã��
        UnitController leadUnit = allUnits
            .Where(unit => unit != null && unit.isActive)
            .OrderByDescending(unit => unit.transform.position.x)
            .FirstOrDefault();

        // Ÿ�� ���� ������Ʈ
        targetUnit = leadUnit?.transform;
    }

    // �ܺο��� ȣ���� �޼����
    public void StartFollowing()
    {
        FindLeadUnit();
        isFollowing = true;
    }

    public void StopFollowing()
    {
        isFollowing = false;
    }

    // ��� ���� ��ġ�� ���ư��� �޼���
    public void ReturnToOriginalPositionImmediately()
    {
        isFollowing = false;
        transform.position = originalPosition;
    }

    // FSM ���� �޼���
    public void OnBattleStart()
    {
        StartFollowing();
    }

    public void OnBattleEnd()
    {
        ReturnToOriginalPositionImmediately();
    }
}