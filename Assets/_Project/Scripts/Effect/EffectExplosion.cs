using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectExplosion : MonoBehaviour
{
    [SerializeField] private AnimationClip animClip; //바인딩해주기

    public void InitializeEffect(BasicObject obj)
    {
        transform.position = obj.transform.position;

        float length = animClip.length;

        //폭발 애니메이션 끝난후에 실행되어야함 애니메이션 길이 가지고오기
        StartCoroutine(DestroyAfterDuration(length));

    }



    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        PoolingManager.Instance.ReturnObject(this.gameObject);
       
    }


}
