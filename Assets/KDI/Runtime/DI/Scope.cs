using System;
using System.Collections.Generic;

namespace Kylin.LWDI
{
    /// <summary>
    /// 스코프 인터페이스 - 의존성 해결 범위 정의
    /// </summary>
    public interface IScope : IDisposable
    {
        /// <summary>
        /// 부모 스코프 참조
        /// </summary>
        IScope Parent { get; }
        
        /// <summary>
        /// 스코프 내에서 타입 해결
        /// </summary>
        object Resolve(Type type);
        
        /// <summary>
        /// 스코프 내에서 타입 해결 (제네릭 버전)
        /// </summary>
        T Resolve<T>() where T : class;
        
        /// <summary>
        /// 인스턴스 등록
        /// </summary>
        void RegisterInstance(Type type, object instance);
        
        /// <summary>
        /// 인스턴스 등록 (제네릭 버전)
        /// </summary>
        void RegisterInstance<T>(T instance) where T : class;
        
        /// <summary>
        /// 스코프 액티베이션 - 현재 스레드의 실행 컨텍스트에 스코프 설정
        /// </summary>
        IDisposable Activate();
    }
    
    /// <summary>
    /// 스코프 구현체 - 객체 수명주기 관리
    /// </summary>
    public class Scope : IScope
    {
        private readonly DependencyContainer _container;
        private readonly IScope _parent;
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private bool _isDisposed;
        
        public Scope(DependencyContainer container, IScope parent)
        {
            _container = container;
            _parent = parent;
        }
        
        /// <summary>
        /// 부모 스코프
        /// </summary>
        public IScope Parent => _parent;
        
        /// <summary>
        /// 타입 해결
        /// </summary>
        public object Resolve(Type type)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Scope");
                
            return _container.Resolve(type, this);
        }
        
        /// <summary>
        /// 타입 해결 (제네릭 버전)
        /// </summary>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        
        /// <summary>
        /// 인스턴스 등록
        /// </summary>
        public void RegisterInstance(Type type, object instance)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Scope");
                
            _instances[type] = instance;
            
            // 구현된 인터페이스도 등록
            foreach (var interfaceType in instance.GetType().GetInterfaces())
            {
                if (typeof(IDependencyObject).IsAssignableFrom(interfaceType))
                {
                    _instances[interfaceType] = instance;
                }
            }
        }
        
        /// <summary>
        /// 인스턴스 등록 (제네릭 버전)
        /// </summary>
        public void RegisterInstance<T>(T instance) where T : class
        {
            RegisterInstance(typeof(T), instance);
        }
        
        /// <summary>
        /// 스코프 내 인스턴스 조회
        /// </summary>
        internal object GetInstance(Type type)
        {
            // 현재 스코프에서 인스턴스 확인
            if (_instances.TryGetValue(type, out var instance))
                return instance;
                
            // 인터페이스인 경우 구현체 확인
            if (type.IsInterface || type.IsAbstract)
            {
                foreach (var entry in _instances)
                {
                    if (type.IsAssignableFrom(entry.Key))
                    {
                        return entry.Value;
                    }
                }
            }
            
            // 부모 스코프에서 확인
            if (_parent != null && _parent is Scope parentScope)
            {
                return parentScope.GetInstance(type);
            }
            
            return null;
        }
        
        /// <summary>
        /// 현재 스코프 내 모든 인스턴스 가져오기
        /// </summary>
        public IReadOnlyDictionary<Type, object> GetInstances()
        {
            return _instances;
        }
        
        /// <summary>
        /// 스코프 활성화 - 현재 실행 컨텍스트에 스코프 설정
        /// </summary>
        public IDisposable Activate()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Scope");
                
            // 이전 스코프 저장 후 현재 스코프로 설정
            _container.SetCurrentExecutionScope(this);
            
            // 스코프 비활성화를 위한 토큰 반환
            return new ScopeActivationToken(this, _container);
        }
        
        /// <summary>
        /// 스코프 및 리소스 해제
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            
            // 스코프 내 IDisposable 객체 정리
            foreach (var instance in _instances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception) 
                    { 
                        // 예외 무시 - 다른 객체 정리는 계속 진행
                    }
                }
            }
            
            _instances.Clear();
            
            // 컨테이너에서 스코프 제거
            _container.RemoveScope(this);
        }
        
        /// <summary>
        /// 스코프 활성화 토큰
        /// </summary>
        private class ScopeActivationToken : IDisposable
        {
            private readonly Scope _scope;
            private readonly DependencyContainer _container;
            
            public ScopeActivationToken(Scope scope, DependencyContainer container)
            {
                _scope = scope;
                _container = container;
            }
            
            public void Dispose()
            {
                if (_scope._isDisposed)
                    return;
                    
                // 부모 스코프로 되돌리기
                if (_scope.Parent is Scope parentScope)
                {
                    _container.SetCurrentExecutionScope(parentScope);
                }
                else
                {
                    _container.SetCurrentExecutionScope(null);
                }
            }
        }
    }
}