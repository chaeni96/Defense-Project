using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectExplosion : MonoBehaviour
{
    [SerializeField] private AnimationClip animClip; //바인딩해주기

    private BasicObject owner;
    private int spawnCount;

    public void InitializeEffect(BasicObject obj, int count)
    {
        owner = obj;
        transform.position = obj.transform.position;

        float length = animClip.length;
        spawnCount = count;
        SpawnEnemy();

        //폭발 애니메이션 끝난후에 실행되어야함 애니메이션 길이 가지고오기
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
            // 랜덤한 오프셋 생성 (원형으로 퍼지도록)
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            float randomDistance = UnityEngine.Random.Range(0.3f, 0.8f);  // 1~3 유닛 거리
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance,
                0
            );

            // 새로운 위치 계산
            Vector3 spawnPosition = owner.transform.position + offset;

            // Enemy 생성
            EnemyManager.Instance.SpawnEnemy("EnemyNormal", spawnPosition);
        }
    }

}
