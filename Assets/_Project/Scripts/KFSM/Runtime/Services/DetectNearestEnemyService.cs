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
                // �߰� ���� - ���� Ÿ�� ����
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
            //�� �����̳� �ʱ�ȭ
            character.enemyPriorityContainer.Clear();
            //����ִ� Ÿ�� ������ ����

            var activeTargets = character.GetActiveTargetList();
            
            //�Ÿ� ������� ��׷� ��ġ ���
            
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
        // HP�� ���� ���� Ÿ�� ã��
        public BasicObject DetectTarget(CharacterFSMObject character)
        {
            BasicObject bestTarget = null;
            int highestPriority = int.MinValue;
        
            foreach (var kvp in character.enemyPriorityContainer)
            {
                // �߰� ���� - ���� Ÿ�� ����
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
            // 1. ���̺� �ʱ�ȭ
            character.enemyPriorityContainer.Clear();

            // 2. ����ִ� Ÿ�ٸ� ��������
            var activeTargets = character.GetActiveTargetList();
            
            // 3. ���� HP ��� �켱�� ���

            for (int i = 0; i < activeTargets.Count; i++)
            {
                var target = activeTargets[i];

                // ���� HP�� �������� ���� �켱��
                float currentHp = target.GetStat(StatName.CurrentHp);
                
                // HP�� ������ ��ȯ�ؼ� �켱���� ��� (�������� ���� �켱��)
                int priority = Mathf.RoundToInt(1000f - currentHp);
                
                character.enemyPriorityContainer[target] = priority;

            }
        }
    }
}