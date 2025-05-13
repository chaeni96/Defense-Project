using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kylin.LWDI
{
    // Unity MonoBehaviour 확장
    public abstract class DIBehaviour : MonoBehaviour, IInjectable
    {
        protected virtual void Awake()
        {
            Inject();
        }
        
        public void Inject()
        {
            DependencyInjector.Inject(this);
        }
    }
}