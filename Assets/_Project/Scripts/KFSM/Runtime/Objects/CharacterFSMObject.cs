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

            // 초기 타겟 설정
            UpdateTarget();


            isFinished = false;
        }

        // 타겟 업데이트 메서드
        public virtual void UpdateTarget()
        {
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();

            if (basicObject != null)
            {
                // 이전 타겟 저장
                var oldTarget = CurrentTarget;

                // 새 타겟 찾기
                var newTarget = basicObject.GetTarget();

                if (newTarget != null)
                {
                    // 활성화 상태 확인
                    if (!newTarget.gameObject.activeSelf)
                    {
                        newTarget = null;
                    }
                    else
                    {
                        if (newTarget != null)
                        {
                            if (newTarget.GetStat(StatName.CurrentHp) <= 0)
                            {
                                newTarget = null;
                            }
                        }
                    }
                }
                CurrentTarget = newTarget;
            }
        }

        // 타겟 유효성 검사
        public virtual bool IsTargetValid()
        {
            if (CurrentTarget == null)
                return false;

            return CurrentTarget.gameObject.activeSelf;
        }

        // 타겟과의 거리 계산
        public virtual float GetDistanceToTarget()
        {
            if (CurrentTarget == null)
                return float.MaxValue;

            return Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(CurrentTarget.transform.position.x, CurrentTarget.transform.position.y)
            );
        }


        // 애니메이션 이벤트로 호출될 메서드
        public void OnAttackHit()
        {
            if (basicObject != null && CurrentTarget != null)
            {
                if (CurrentTarget.isEnemy)
                {
                    var enemyObj = CurrentTarget.GetComponent<Enemy>();
                    if (enemyObj != null)
                    {
                        // 데미지 계산 및 적용만 담당
                        float damage = basicObject.GetStat(StatName.ATK);
                        enemyObj.onDamaged(basicObject, damage);
                    }
                }
                else
                {
                    var enemyObj = CurrentTarget.GetComponent<UnitController>();

                    if (enemyObj != null)
                    {
                        // 데미지 계산 및 적용만 담당
                        float damage = basicObject.GetStat(StatName.ATK);
                        enemyObj.onDamaged(basicObject, damage);
                    }
                }

                
            }
        }


        public void FinishBattleWinAnimation()
        {
            isFinished = true;
        }
    }
}
