namespace Kylin.LWDI
{
    public abstract class BaseViewModel : IDependencyObject, IInjectable
    {
        private bool _isActiveViewModel;
        private int _referenceCount;
        
        protected BaseViewModel() 
        {
            Inject();
        }
        
        public void Inject(IScope scope = null)
        {
            DependencyInjector.Inject(this, scope);
        }
        
        public void AddReference()
        {
            _referenceCount++;
            if (!_isActiveViewModel)
            {
                _isActiveViewModel = true;
                OnActivate();
            }
        }

        public void RemoveReference()
        {
            _referenceCount--;
            if (_referenceCount <= 0 && _isActiveViewModel)
            {
                _isActiveViewModel = false;
                OnDeactivate();
            }
        }
        
        /// <summary>
        /// 뷰모델 활성화시 호출(모든 구독 여기서 처리할것)
        /// 어디라도 주입되었을때 활성화라고 취급함
        /// </summary>
        protected virtual void OnActivate() { }
        
        /// <summary>
        /// 뷰모델 비 활성화시 호출(모든 이벤트, 구독해제 여기서 처리할것)
        /// 아무곳에서도 주입해둔곳이 없을때 비활성화라고 취급함
        /// </summary>
        protected virtual void OnDeactivate() { }
        
        // 현재 참조 카운트 확인 (디버깅용)
        public int ReferenceCount => _referenceCount;
        
        // 활성화 상태 확인 (디버깅용)
        public bool IsActive => _isActiveViewModel;
    }
    
}