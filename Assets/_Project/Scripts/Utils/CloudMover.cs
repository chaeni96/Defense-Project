using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CloudMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("�̵� �ӵ� (unit/sec)")]
    public float speed = 1f;
    [Tooltip("����� �� �������� ��� �ð�")]
    public float coolTime = 2f;
    [Tooltip("������ Y�� ���� (min, max)")]
    public Vector2 yRange = new Vector2(-2f, 2f);
    [Tooltip("ȭ�� �ٱ����� ���� �� �󸶸�ŭ �߰��� �̵����� �� ���������� ����")]
    public float offscreenMargin = 1f;

    private Camera mainCam;
    private float spawnX;
    private float despawnX;
    private float zDistance;
    private bool ismove;

    private void Start()
    {
        mainCam = Camera.main;
        // ī�޶�� Ŭ������ Z �Ÿ� ��� (Orthographic ī�޶� ����)
        zDistance = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        ismove = true;
        // ȭ�� ���ʰ� ������ ���� ��ǥ ���
        spawnX = mainCam.ViewportToWorldPoint(new Vector3(0, 0.5f, zDistance)).x - offscreenMargin;
        despawnX = mainCam.ViewportToWorldPoint(new Vector3(1, 0.5f, zDistance)).x + offscreenMargin;

        // �ٷ� �ڷ�ƾ ����
        StartCoroutine(CloudLoop());
    }

    private IEnumerator CloudLoop()
    {
        while (true)
        {
            // 1) ���� ��ġ�� ���� Y ��ġ
            if (!ismove)
            {
                float randomY = Random.Range(yRange.x, yRange.y);
                transform.position = new Vector3(spawnX, randomY, transform.position.z);
            }

            // 2) ���������� �̵�
            while (transform.position.x < despawnX)
            {
                transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
                yield return null;
            }

            // 3) ȭ�� ����� ��� ��Ȱ��ȭ (Ȥ�� �ܼ��� ��ٸ���)
            //    �� �ʿ信 ���� gameObject.SetActive(false); ������ �ð��� ó�� ����
            ismove = false;
            yield return new WaitForSeconds(coolTime);
        }
    }
}
