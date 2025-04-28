using System;
namespace Kylin.FSM
{
    //비트마스크 모자라면 어캐할지 생각할것 이번 게임에선 괜찮지 않을까
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