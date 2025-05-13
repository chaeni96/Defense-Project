using System;
using System.Collections.Generic;

namespace Kylin.LWDI
{
    public interface IScope : IDisposable
    {
        object Resolve(Type type);
        T Resolve<T>();
        void RegisterInstance(Type type, object instance);
    }
    public class Scope : IScope
    {
        private readonly DependencyContainer _container;
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        
        public Scope(DependencyContainer container)
        {
            _container = container;
        }
        
        public object Resolve(Type type)
        {
            return _container.Resolve(type);
        }
        
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }
        
        public void RegisterInstance(Type type, object instance)
        {
            _scopedInstances[type] = instance;
        }
        
        public object ResolveScoped(Type type, DependencyContainer.Registration registration)
        {
            if (_scopedInstances.TryGetValue(type, out var instance))
                return instance;
                
            if (registration.Factory != null)
            {
                instance = registration.Factory(this);
            }
            else
            {
                var implementationType = registration.ImplementationType;
                instance = Activator.CreateInstance(implementationType);
                
                _container.InjectFields(instance);
            }
            
            _scopedInstances[type] = instance;
            return instance;
        }
        
        public IReadOnlyDictionary<Type, object> GetScopedInstances()
        {
            return _scopedInstances;
        }
        
        public void Dispose()
        {
            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            _scopedInstances.Clear();
            
            _container.PopScope(this);
        }
        public object TryGetInstance(Type type)
        {
            if (_scopedInstances.TryGetValue(type, out var instance))
                return instance;
    
            // 등록된 타입 중에서 해당 타입을 구현/상속하는 것 찾기
            if (type.IsInterface || type.IsAbstract)
            {
                foreach (var entry in _scopedInstances)
                {
                    if (type.IsAssignableFrom(entry.Key))
                    {
                        return entry.Value;
                    }
                }
            }
    
            return null;
        }
    }
}