using UnityEngine;

namespace Kylin.LWDI
{
    public abstract class BaseView : MonoBehaviour, IInjectable
    {
        /// <summary>
        /// view는 꼬일 수 있기 때문에 생성자보다 awake에서 일괄처리.. 각 데이터 불러오고 난 다음 처리해야할것 이것도 씬별 관리 주의(특히 뷰를 걍 신에 냅두지 말것
        /// 일단 DIBehaviour로 일반적인 유닛은 사용되니 view쓰려고 하지 말것 사실상 이름만 다르고 다 똑같은데 나중에 UI분리가 필요할까 해서 냅둠(참고)
        /// </summary>
        protected virtual void Awake() => Inject();
        public void Inject() => DependencyInjector.Inject(this);
    }
}