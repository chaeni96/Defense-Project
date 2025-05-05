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
            // �ʿ��� ��� ������Ʈ ���� �߰�

            CheckAllUnitsFinished();


        }

        private void CheckAllUnitsFinished()
        {
            // ��� ������ �ִϸ��̼��� ���´��� Ȯ��
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

            // ��� ������ �ִϸ��̼��� ���´ٸ� ���̺� �Ϸ� ó��
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
