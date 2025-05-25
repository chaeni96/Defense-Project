using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    public class CharacterFSMObject : FSMObjectBase
    {
        //타겟 관련 필드

        public BasicObject CurrentTarget;
        public BasicObject basicObject;//이 상태머신을 들고있는 오브젝트(유닛, 에너미)

        protected override void Initialized()
        {
            base.Initialized();

            // basicObject가 없으면 가져오기
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();
            
            isEnemy = basicObject.isEnemy;
            isFinished = false;
        }
        
    }
}
