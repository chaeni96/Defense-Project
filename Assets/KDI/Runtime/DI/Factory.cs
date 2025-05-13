using System;

namespace Kylin.LWDI
{
    public class Factory<T> : IFactory<T>, IDependencyObject where T : class, IDependencyObject
    {
        private readonly IScope _scope;
        
        public Factory(IScope scope)
        {
            _scope = scope;
        }
        
        public T Create()
        {
            return _scope.Resolve<T>();
        }
    }
    
    // 매개변수 있는 팩토리 구현
    public class Factory<TParam, T> : IDependencyObject, IFactory<TParam, T> where T : IDependencyObject
    {
        private readonly IScope _scope;
        private readonly Func<TParam, T> _factory;
        
        public Factory(IScope scope, Func<TParam, T> factory)
        {
            _scope = scope;
            _factory = factory;
        }
        
        public T Create(TParam param)
        {
            var instance = _factory(param);
            
            // 의존성 주입
            if (instance is IInjectable injectable)
            {
                injectable.Inject();
            }
            return instance;
        }
    }
    public static class FactoryExtensions
    {
        public static void RegisterFactory<T>(this DependencyContainer container)
            where T : class, IDependencyObject
        {
            // Factory<T>가 이제 IDependencyObject를 구현하므로 등록 가능
            container.Register<IFactory<T>, Factory<T>>(Lifetime.Transient);
        }
    
        public static void RegisterFactory<TParam, T>(this DependencyContainer container, Func<TParam, T> factory)
            where T : IDependencyObject
        {
            container.RegisterFactory<IFactory<TParam, T>>(scope => 
                new Factory<TParam, T>(scope, factory));
        }
    }
}