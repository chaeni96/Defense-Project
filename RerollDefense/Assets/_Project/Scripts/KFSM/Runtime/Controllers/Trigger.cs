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
        AttackAnimation = 1 << 7,
        DetectFinish = 1 << 8,
        TargetMiss = 1 << 9,
        TestATrigger = 1 << 10,
        TestBTrigger = 1 << 11,
        MoveFinished = 1 << 12,

    }
}