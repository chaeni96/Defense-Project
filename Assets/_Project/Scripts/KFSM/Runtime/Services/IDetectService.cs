using System.Collections.Generic;
using Kylin.LWDI;
using UnityEngine;

namespace _Project.Scripts.KFSM.Runtime.Services
{
    public interface IDetectService : IDependencyObject
    {
        BasicObject DetectTarget(Transform origin, List<BasicObject> targetList, bool findEnemy);

    }
}