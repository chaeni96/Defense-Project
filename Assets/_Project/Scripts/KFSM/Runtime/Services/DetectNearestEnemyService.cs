using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.KFSM.Runtime.Services
{
    public class NearestEnemyDetectService : IDetectService
    {
        public BasicObject DetectTarget(Transform origin, List<BasicObject> targetList, bool findEnemy)
        {
            if (targetList == null || targetList.Count == 0)
                return null;
            
            return targetList
                .Where(b => b != null && 
                            b.isEnemy == findEnemy && 
                            b.GetStat(StatName.CurrentHp) > 0 &&
                            b.isActive)
                .OrderBy(b => Vector2.Distance(origin.position, b.transform.position))
                .FirstOrDefault();
        }
    }



    public class LowestHpDetectService : IDetectService
    {
        // HP가 가장 낮은 타겟 찾기
        public BasicObject DetectTarget(Transform origin, List<BasicObject> targetList, bool findEnemy)
        {
            if (targetList == null || targetList.Count == 0)
                return null;
            
            return targetList
                .Where(b => b != null && 
                            b.isEnemy == findEnemy && 
                            b.GetStat(StatName.CurrentHp) > 0 &&
                            b.gameObject.activeSelf)
                .OrderBy(b => b.GetStat(StatName.CurrentHp))  // HP 낮은 순으로 정렬
                .ThenBy(b => Vector2.Distance(origin.position, b.transform.position))  // HP가 같으면 거리순
                .FirstOrDefault();
        }
    }
}