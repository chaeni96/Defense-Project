using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectExplosion : MonoBehaviour
{
    [SerializeField] private AnimationClip animClip; //���ε����ֱ�

    public void InitializeEffect(BasicObject obj)
    {
        transform.position = obj.transform.position;

        float length = animClip.length;

        //���� �ִϸ��̼� �����Ŀ� ����Ǿ���� �ִϸ��̼� ���� ���������
        StartCoroutine(DestroyAfterDuration(length));

    }



    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        PoolingManager.Instance.ReturnObject(this.gameObject);
       
    }


}
