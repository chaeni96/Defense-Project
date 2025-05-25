using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    public class CharacterFSMObject : FSMObjectBase
    {
        //Ÿ�� ���� �ʵ�

        public BasicObject CurrentTarget;
        public BasicObject basicObject;//�� ���¸ӽ��� ����ִ� ������Ʈ(����, ���ʹ�)

        protected override void Initialized()
        {
            base.Initialized();

            // basicObject�� ������ ��������
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();
            
            isEnemy = basicObject.isEnemy;
            isFinished = false;
        }
        
    }
}
