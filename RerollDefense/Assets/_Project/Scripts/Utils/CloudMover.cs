using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CloudMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("이동 속도 (unit/sec)")]
    public float speed = 1f;
    [Tooltip("사라진 후 재등장까지 대기 시간")]
    public float coolTime = 2f;
    [Tooltip("스폰될 Y축 범위 (min, max)")]
    public Vector2 yRange = new Vector2(-2f, 2f);
    [Tooltip("화면 바깥으로 나간 뒤 얼마만큼 추가로 이동했을 때 리스폰할지 마진")]
    public float offscreenMargin = 1f;

    private Camera mainCam;
    private float spawnX;
    private float despawnX;
    private float zDistance;
    private bool ismove;

    private void Start()
    {
        mainCam = Camera.main;
        // 카메라와 클라우드의 Z 거리 계산 (Orthographic 카메라 가정)
        zDistance = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        ismove = true;
        // 화면 왼쪽과 오른쪽 월드 좌표 계산
        spawnX = mainCam.ViewportToWorldPoint(new Vector3(0, 0.5f, zDistance)).x - offscreenMargin;
        despawnX = mainCam.ViewportToWorldPoint(new Vector3(1, 0.5f, zDistance)).x + offscreenMargin;

        // 바로 코루틴 시작
        StartCoroutine(CloudLoop());
    }

    private IEnumerator CloudLoop()
    {
        while (true)
        {
            // 1) 스폰 위치와 랜덤 Y 배치
            if (!ismove)
            {
                float randomY = Random.Range(yRange.x, yRange.y);
                transform.position = new Vector3(spawnX, randomY, transform.position.z);
            }

            // 2) 오른쪽으로 이동
            while (transform.position.x < despawnX)
            {
                transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
                yield return null;
            }

            // 3) 화면 벗어나면 잠깐 비활성화 (혹은 단순히 기다리기)
            //    ※ 필요에 따라 gameObject.SetActive(false); 등으로 시각적 처리 가능
            ismove = false;
            yield return new WaitForSeconds(coolTime);
        }
    }
}
