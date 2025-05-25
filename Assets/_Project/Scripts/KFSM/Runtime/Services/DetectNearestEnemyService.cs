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
        // HP�� ���� ���� Ÿ�� ã��
        public BasicObject DetectTarget(Transform origin, List<BasicObject> targetList, bool findEnemy)
        {
            if (targetList == null || targetList.Count == 0)
                return null;
            
            return targetList
                .Where(b => b != null && 
                            b.isEnemy == findEnemy && 
                            b.GetStat(StatName.CurrentHp) > 0 &&
                            b.gameObject.activeSelf)
                .OrderBy(b => b.GetStat(StatName.CurrentHp))  // HP ���� ������ ����
                .ThenBy(b => Vector2.Distance(origin.position, b.transform.position))  // HP�� ������ �Ÿ���
                .FirstOrDefault();
        }
    }
}