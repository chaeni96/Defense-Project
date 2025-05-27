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

        //어그로 수치 컨테이너, 어그로 = 우선도
        public Dictionary<BasicObject, int> enemyPriorityContainer = new Dictionary<BasicObject, int>();
        public Dictionary<BasicObject, int> allyPriorityContainer = new Dictionary<BasicObject, int>();
        
        // 재사용할 임시 리스트
        private List<BasicObject> tempTargetList = new List<BasicObject>();
        private List<BasicObject> tempAllyList = new List<BasicObject>();
        protected override void Initialized()
        {
            base.Initialized();

            // basicObject가 없으면 가져오기
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();
            
            enemyPriorityContainer.Clear();
            allyPriorityContainer.Clear();
            tempTargetList.Clear();
            tempAllyList.Clear();
            
            isEnemy = basicObject.isEnemy;
            isFinished = false;
        }

        //적 리스트 가지고오기
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

        //나를 제외한 아군 리스트 가지고오기
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
        
        //죽은 
    }
}
