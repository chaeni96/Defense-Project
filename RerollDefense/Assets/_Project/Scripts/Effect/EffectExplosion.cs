using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectExplosion : MonoBehaviour
{
    [SerializeField] private AnimationClip animClip; //���ε����ֱ�

    private BasicObject owner;
    private int spawnCount;

    public void InitializeEffect(BasicObject obj, int count)
    {
        owner = obj;
        transform.position = obj.transform.position;

        float length = animClip.length;
        spawnCount = count;
        SpawnEnemy();

        //���� �ִϸ��̼� �����Ŀ� ����Ǿ���� �ִϸ��̼� ���� ���������
        StartCoroutine(DestroyAfterDuration(length));

    }



    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        PoolingManager.Instance.ReturnObject(this.gameObject);
       
    }


    private void SpawnEnemy()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            // ������ ������ ���� (�������� ��������)
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            float randomDistance = UnityEngine.Random.Range(0.3f, 0.8f);  // 1~3 ���� �Ÿ�
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance,
                0
            );

            // ���ο� ��ġ ���
            Vector3 spawnPosition = owner.transform.position + offset;

            // Enemy ����
            EnemyManager.Instance.SpawnEnemy("EnemyNormal", spawnPosition);
        }
    }

}
