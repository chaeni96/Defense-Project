using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

namespace _Project.Scripts.KFSM.Runtime.Services
{
    public interface IDetectService : IDependencyObject
    {
        BasicObject DetectTarget(CharacterFSMObject character);
        void UpdateTargetPriority(CharacterFSMObject character); // 어그로 수치 관리

    }
}