using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Battle")]
    public class BattleWinState : StateBase
    {

        private CharacterFSMObject characterFSM;

        public override void OnEnter()
        {
            Debug.Log("BattleWinState : State Enter!!");
            characterFSM = Owner as CharacterFSMObject;
            if (characterFSM == null) return;
        }

        public override void OnUpdate()
        {
            // 필요한 경우 업데이트 로직 추가

            CheckAllUnitsFinished();


        }

        private void CheckAllUnitsFinished()
        {
            // 모든 유닛이 애니메이션을 끝냈는지 확인
            List<UnitController> units = UnitManager.Instance.GetAllUnits();
            bool allFinished = true;

            foreach (var unit in units)
            {
                if (unit != null && unit.gameObject.activeSelf)
                {
                    if (!unit.fsmObj.isFinished)
                    {
                        allFinished = false;
                        break;
                    }
                }
            }

            // 모든 유닛이 애니메이션을 끝냈다면 웨이브 완료 처리
            if (allFinished)
            {

            }
        }


        public override void OnExit()
        {
            Debug.Log("BattleWinState : State Exit!!");
        }
    }
}
