using System;
namespace Kylin.FSM
{
    //��Ʈ����ũ ���ڶ�� ��ĳ���� �����Ұ� �̹� ���ӿ��� ������ ������
    [Flags]
    public enum Trigger
    {
        None = 0,
        MoveRequested = 1 << 0,
        AttackRequested = 1 << 1,
        Hit = 1 << 2,
        Bleeding = 1 << 3,
        SuperArmor = 1 << 4,
        AttackReady = 1 << 5,
        AttackFinished = 1 << 6,
        DetectTarget = 1 << 7,
        TargetMiss = 1 << 8,
        ChaseTarget = 1 << 9,
        BattleWin = 1 << 10,
        ReturnToOriginPos = 1 << 11,
        TargetSelected = 1 << 12,
        DamageFinished = 1 << 13,
        SkillRequested = 1 << 14,
        SkillFinished = 1 << 15,
    }
}