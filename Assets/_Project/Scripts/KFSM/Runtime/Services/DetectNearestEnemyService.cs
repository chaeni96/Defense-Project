using System.Collections.Generic;
using System.Linq;
using Kylin.FSM;
using UnityEngine;

namespace _Project.Scripts.KFSM.Runtime.Services
{
    public class NearestEnemyDetectService : IDetectService
    {
        public BasicObject DetectTarget(CharacterFSMObject character)
        {

            BasicObject bestTarget = null;
            int highestPriority = int.MinValue;
        
            foreach (var kvp in character.enemyPriorityContainer)
            {
                // 추가 검증 - 죽은 타겟 제외
                if (kvp.Key == null || kvp.Key.GetStat(StatName.CurrentHp) <= 0)
                    continue;
                
                if (kvp.Value > highestPriority)
                {
                    highestPriority = kvp.Value;
                    bestTarget = kvp.Key;
                }
            }
        
            return bestTarget;
        }

        public void UpdateTargetPriority(CharacterFSMObject character)
        {
            //적 컨테이너 초기화
            character.enemyPriorityContainer.Clear();
            //살아있는 타겟 가지고 오기

            var activeTargets = character.GetActiveTargetList();
            
            //거리 기반으로 어그로 수치 계산
            
            Vector3 myPos = character.transform.position;

            for (int i = 0; i < activeTargets.Count; ++i)
            {
                float distance = Vector3.Distance(myPos, activeTargets[i].transform.position);
                int priority = 100 - Mathf.RoundToInt(distance);
                
                character.enemyPriorityContainer[activeTargets[i]] = priority;
            }
        }
    }



    public class LowestHpDetectService : IDetectService
    {
        // HP가 가장 낮은 타겟 찾기
        public BasicObject DetectTarget(CharacterFSMObject character)
        {
            BasicObject bestTarget = null;
            int highestPriority = int.MinValue;
        
            foreach (var kvp in character.enemyPriorityContainer)
            {
                // 추가 검증 - 죽은 타겟 제외
                if (kvp.Key == null || kvp.Key.GetStat(StatName.CurrentHp) <= 0)
                    continue;
                
                if (kvp.Value > highestPriority)
                {
                    highestPriority = kvp.Value;
                    bestTarget = kvp.Key;
                }
            }
        
            return bestTarget;
        }

        public void UpdateTargetPriority(CharacterFSMObject character)
        {
            // 1. 테이블 초기화
            character.enemyPriorityContainer.Clear();

            // 2. 살아있는 타겟만 가져오기
            var activeTargets = character.GetActiveTargetList();
            
            // 3. 현재 HP 기반 우선도 계산

            for (int i = 0; i < activeTargets.Count; i++)
            {
                var target = activeTargets[i];

                // 현재 HP가 낮을수록 높은 우선도
                float currentHp = target.GetStat(StatName.CurrentHp);
                
                // HP를 음수로 변환해서 우선도로 사용 (낮을수록 높은 우선도)
                int priority = Mathf.RoundToInt(1000f - currentHp);
                
                character.enemyPriorityContainer[target] = priority;

            }
        }
    }
}