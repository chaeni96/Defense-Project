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

        //��׷� ��ġ �����̳�, ��׷� = �켱��
        public Dictionary<BasicObject, int> enemyPriorityContainer = new Dictionary<BasicObject, int>();
        public Dictionary<BasicObject, int> allyPriorityContainer = new Dictionary<BasicObject, int>();
        
        // ������ �ӽ� ����Ʈ
        private List<BasicObject> tempTargetList = new List<BasicObject>();
        private List<BasicObject> tempAllyList = new List<BasicObject>();
        protected override void Initialized()
        {
            base.Initialized();

            // basicObject�� ������ ��������
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();
            
            enemyPriorityContainer.Clear();
            allyPriorityContainer.Clear();
            tempTargetList.Clear();
            tempAllyList.Clear();
            
            isEnemy = basicObject.isEnemy;
            isFinished = false;
        }

        //�� ����Ʈ ���������
        public List<BasicObject> GetActiveTargetList()
        {
            if (isEnemy)
            {
                var units = UnitManager.Instance.GetAllUnits();

                for (int i = 0; i < units.Count; ++i)
                {
                    if (units[i] != null && units[i].isActive && units[i].GetStat(StatName.CurrentHp) > 0)
                    {
                        tempTargetList.Add(units[i]);
                    }
                }
            }
            else
            {
                var enemies = EnemyManager.Instance.GetAllEnemys();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] != null && 
                        enemies[i].isActive && 
                        enemies[i].GetStat(StatName.CurrentHp) > 0)
                    {
                        tempTargetList.Add(enemies[i]);
                    }
                }
            }
            
            return tempTargetList;
        }

        //���� ������ �Ʊ� ����Ʈ ���������
        public List<BasicObject> GetActiveAllyList()
        {
            if (!isEnemy)
            {
                var units = UnitManager.Instance.GetAllUnits();

                for (int i = 0; i < units.Count; ++i)
                {
                    if (units[i] != null && units[i] != this.basicObject && units[i].isActive && units[i].GetStat(StatName.CurrentHp) > 0)
                    {
                        tempAllyList.Add(units[i]);
                    }
                }
            }
            else
            {
                var enemies = EnemyManager.Instance.GetAllEnemys();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] != null&& enemies[i] != this.basicObject && enemies[i].isActive && enemies[i].GetStat(StatName.CurrentHp) > 0)
                    {
                        tempAllyList.Add(enemies[i]);
                    }
                }
            }
            
            return tempAllyList;
        }
        
        //���� 
    }
}
