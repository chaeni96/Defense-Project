using System;
using System.Collections.Generic;
using System.Linq;

namespace Kylin.LWDI
{
    // 의존성 빌더 패턴 구현
    public class DependencyBuilder<T> where T : class
    {
        private readonly DependencyContainer _container;
        private readonly Type _serviceType;
        private Type _implementationType;
        private Lifetime _lifetime = Lifetime.Singleton;
        private object _instance;
        private Func<IScope, object> _factory;
        
        internal DependencyBuilder(DependencyContainer container, Type serviceType)
        {
            _container = container;
            _serviceType = serviceType;
        }
        
        /// <summary>
        /// 구현타입 지정임.. 이후 as메서드 써줄것(FinishRegistration 임)
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        public DependencyBuilder<T> To<TImplementation>() 
            where TImplementation : IDependencyObject, T
        {
            _implementationType = typeof(TImplementation);
            return this;
        }
        
        /// <summary>
        /// 싱글톤처럼
        /// </summary>
        public void AsSingleton()
        {
            _lifetime = Lifetime.Singleton;
            FinishRegistration();
        }
        
        
        /// <summary>
        /// 매번 새 인스턴스
        /// </summary>
        public void AsTransient()
        {
            _lifetime = Lifetime.Transient;
            FinishRegistration();
        }
        
        /// <summary>
        /// 스코프 잡고 사용하는거임 using이랑 쓸것
        /// </summary>
        public void AsScoped()
        {
            _lifetime = Lifetime.Scoped;
            FinishRegistration();
        }
        
        // FromInstance - 기존 인스턴스 등록
        public void FromInstance(T instance)
        {
            if (instance is IDependencyObject)
            {
                _instance = instance;
                FinishRegistration();
            }
            else
            {
                throw new ArgumentException($"Instance must implement IDependencyObject");
            }
        }
        
        /// <summary>
        /// 펙토리 등록하기
        /// </summary>
        /// <param name="factory"></param>
        public void FromFactory(Func<IScope, T> factory)
        {
            _factory = scope => factory(scope);
            FinishRegistration();
        }
        
        private void FinishRegistration()
        {
            if (_instance != null)
            {
                _container.RegisterInstance((T)_instance);
            }
            else if (_factory != null)
            {
                var typedFactory = new Func<IScope, T>(scope => (T)_factory(scope));
                _container.RegisterFactory(typedFactory, _lifetime);
            }
            else if (_implementationType != null)
            {
                _container.Register(_serviceType, _implementationType, _lifetime);
            }
            else
            {
                throw new InvalidOperationException("Registration is incomplete");
            }
        }
    }
}