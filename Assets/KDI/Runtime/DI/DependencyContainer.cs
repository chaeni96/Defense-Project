using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Kylin.LWDI
{
    public enum Lifetime
    {
        Transient,  // 매번 새 인스턴스 생성
        Singleton,  // 단일 인스턴스 유지
        Scoped      // 특정 범위 내 단일 인스턴스
    }
    
    public interface IDependencyObject
    {
    }
    public interface IInjectable
    {
        public void Inject();
    }
    
    public interface IFactory<T>
    {
        T Create();
    }
    
    public interface IFactory<TParam, T>
    {
        T Create(TParam param);
    }
    public class DependencyContainer
    {
        private static readonly DependencyContainer _instance = new DependencyContainer();
        
        public static DependencyContainer Instance => _instance;
        
        // 등록 정보를 저장하는 클래스
        public class Registration
        {
            public Type ImplementationType { get; set; }
            public object Instance { get; set; }
            public Lifetime Lifetime { get; set; }
            public Func<IScope, object> Factory { get; set; }
        }
        
        // 등록 정보 저장소
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        
        // 싱글톤 인스턴스 캐시
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        
        //활성화된 스코프
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        
        //멀티 스레드용 락
        private readonly object _lock = new object(); //전반적으로 문제 생길곳에 다 쓸것.. 지금은 리졸브만 예시로 해뒀음 TODO : 김기린
        
        private DependencyContainer()
        {
            PushScope();
        }
        public IScope CreateScope()
        {
            var scope = new Scope(this);
            _scopes.Push(scope);
            return scope;
        }
        
        private void PushScope()
        {
            _scopes.Push(new Scope(this));
        }
        internal void PopScope(Scope scope)
        {
            if (_scopes.Count > 0 && _scopes.Peek() == scope)
            {
                _scopes.Pop();
                
                // 루트 스코프는 항상 유지
                if (_scopes.Count == 0)
                {
                    PushScope();
                }
            }
        }
        
        // 현재 스코프 가져오기
        private Scope CurrentScope => _scopes.Count > 0 ? _scopes.Peek() : null;
        
        // 등록 메서드 - 타입 기반
        public void Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
        {
            if (!typeof(IDependencyObject).IsAssignableFrom(implementationType))
                throw new ArgumentException($"Type {implementationType.Name} must implement IDependencyObject");
                
            _registrations[serviceType] = new Registration
            {
                ImplementationType = implementationType,
                Lifetime = lifetime
            };
        }
        
        // 등록 메서드 - 제네릭 버전
        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
            where TImplementation : TService
            where TService : class
        {
            if (!typeof(IDependencyObject).IsAssignableFrom(typeof(TImplementation)))
            {
                throw new ArgumentException($"Type {typeof(TImplementation).Name} must implement IDependencyObject");
            }
    
            Register(typeof(TService), typeof(TImplementation), lifetime);
        }
        
        // 인스턴스 등록 메서드
        public void RegisterInstance<TService>(TService instance)
        {
            if (instance is IDependencyObject dependencyObject)
            {
                _registrations[typeof(TService)] = new Registration
                {
                    Instance = instance,
                    Lifetime = Lifetime.Singleton
                };
                
                // 싱글톤 캐시에 저장
                _singletonInstances[typeof(TService)] = instance;
            }
            else
                throw new ArgumentException($"Instance must implement IDependencyObject");
        }
        
        // 팩토리 등록 메서드
        public void RegisterFactory<TService>(Func<IScope, TService> factory, Lifetime lifetime = Lifetime.Singleton)
            where TService : class
        {
            _registrations[typeof(TService)] = new Registration
            {
                Factory = scope => factory(scope),
                Lifetime = lifetime
            };
        }
        
        // 해결 메서드 - 타입 기반
        public object Resolve(Type serviceType)
        {
            lock (_lock)
            {
                // 등록 정보 확인
                if (!_registrations.TryGetValue(serviceType, out var registration))
                {
                    // 등록되지 않은 인터페이스
                    if (serviceType.IsInterface || serviceType.IsAbstract)
                        throw new InvalidOperationException($"No registration found for {serviceType.Name}");
                
                    // 등록되지 않은 구체 클래스는 자동 등록
                    if (typeof(IDependencyObject).IsAssignableFrom(serviceType))
                    {
                        Register(serviceType, serviceType);
                        registration = _registrations[serviceType];
                    }
                    else
                        throw new InvalidOperationException($"Type {serviceType.Name} must implement IDependencyObject");
                }
            
                // 이미 인스턴스가 있는 경우
                if (registration.Instance != null)
                    return registration.Instance;
            
                // 라이프타임에 따른 인스턴스 생성
                switch (registration.Lifetime)
                {
                    case Lifetime.Singleton:
                        if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
                            return singletonInstance;
                        
                        var newSingleton = CreateInstance(registration, serviceType);
                        _singletonInstances[serviceType] = newSingleton;
                        return newSingleton;
                    
                    case Lifetime.Scoped:
                        return CurrentScope.ResolveScoped(serviceType, registration);
                    
                    case Lifetime.Transient:
                        return CreateInstance(registration, serviceType);
                    
                    default:
                        throw new InvalidOperationException($"Unsupported lifetime: {registration.Lifetime}");
                }
            }
        }
        
        // 해결 메서드 - 제네릭 버전
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        
        // 인스턴스 생성 메서드
        private object CreateInstance(Registration registration, Type serviceType)
        {
            // 팩토리가 있는 경우
            if (registration.Factory != null)
                return registration.Factory(CurrentScope);
                
            // 구현 타입
            var implementationType = registration.ImplementationType;
            
            // 모든 생성자 확인
            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
            {
                // 기본 생성자만 있는 경우
                return Activator.CreateInstance(implementationType);
            }
            
            // 가장 많은 매개변수를 가진 생성자 사용 (생성자 주입)
            var constructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();
                
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                // 매개변수 없는 생성자
                return Activator.CreateInstance(implementationType);
            }
            
            // 매개변수 해결
            var resolvedParams = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                try
                {
                    resolvedParams[i] = Resolve(paramType);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error resolving parameter '{parameters[i].Name}' of type {paramType.Name} for {implementationType.Name}",
                        ex);
                }
            }
            
            // 인스턴스 생성
            var instance = Activator.CreateInstance(implementationType, resolvedParams);
            
            // 필드 주입 수행
            InjectFields(instance);
            
            return instance;
        }
        
        // 필드 주입 메서드
        public void InjectFields(object instance)
        {
            if (instance is IInjectable injectable)
            {
                injectable.Inject();
            }
            else
            {
                // 수동 필드 주입
                var fields = instance.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectAttribute>() != null)
                    {
                        var fieldType = field.FieldType;
                        try
                        {
                            var dependency = Resolve(fieldType);
                            field.SetValue(instance, dependency);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Error injecting field '{field.Name}' of type {fieldType.Name} in {instance.GetType().Name}",
                                ex);
                        }
                    }
                }
            }
        }
        
        // 모든 등록 가져오기
        public IReadOnlyDictionary<Type, Type> GetAllRegistrations()
        {
            var result = new Dictionary<Type, Type>();
            foreach (var kvp in _registrations)
            {
                if (kvp.Value.ImplementationType != null)
                {
                    result[kvp.Key] = kvp.Value.ImplementationType;
                }
            }
            return result;
        }
        
        // 모든 인스턴스 가져오기
        public IReadOnlyDictionary<Type, object> GetAllInstances()
        {
            var result = new Dictionary<Type, object>();
            
            // 싱글톤 인스턴스
            foreach (var kvp in _singletonInstances)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            // 현재 스코프의 인스턴스
            if (CurrentScope != null)
            {
                foreach (var kvp in CurrentScope.GetScopedInstances())
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
        
        // 모든 등록 및 인스턴스 지우기
        public void Clear()
        {
            _registrations.Clear();
            _singletonInstances.Clear();
            
            // 스코프 초기화
            while (_scopes.Count > 0)
            {
                _scopes.Pop();
            }
            
            // 루트 스코프 생성
            PushScope();
        }
        
        // 씬 전환 시 스코프 지우기
        public void ClearSceneScope(string currentScene)
        {
            // 뷰모델 스코프 처리
            var removeList = new List<Type>();
            
            foreach (var kvp in _registrations)
            {
                var type = kvp.Value.ImplementationType ?? kvp.Key;
                var attr = type.GetCustomAttributes(typeof(ViewModelAttribute), true)
                    .FirstOrDefault() as ViewModelAttribute;
                    
                if (attr != null && !attr.IsGlobal && !attr.SceneNames.Contains(currentScene))
                {
                    removeList.Add(kvp.Key);
                }
            }
            
            foreach (var type in removeList)
            {
                _registrations.Remove(type);
                _singletonInstances.Remove(type);
            }
        }
    }
    // 컨테이너 확장 메서드
    public static class DependencyContainerExtensions
    {
        // Bind - 빌더 패턴 시작
        public static DependencyBuilder<T> Bind<T>(this DependencyContainer container) where T : class
        {
            return new DependencyBuilder<T>(container, typeof(T));
        }
        // 현재 씬 등록
        private static string _currentScene;
        
        public static void SetCurrentScene(string sceneName)
        {
            _currentScene = sceneName;
            DependencyContainer.Instance.ClearSceneScope(sceneName);
            RegisterViewModelsForScene(sceneName);
        }
        
        // 씬에 맞는 뷰모델 자동 등록
        public static void RegisterViewModelsForScene(string sceneName)
        {
            var viewModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseViewModel)))
                .Where(t => !t.IsAbstract)
                .ToList();

            foreach (var type in viewModelTypes)
            {
                var attr = type.GetCustomAttributes(typeof(ViewModelAttribute), true)
                    .FirstOrDefault() as ViewModelAttribute;

                if (attr != null && (attr.IsGlobal || attr.SceneNames.Contains(sceneName)))
                {
                    var container = DependencyContainer.Instance;
                    bool isRegistered = container.GetAllInstances().ContainsKey(type);
                    
                    if (!isRegistered)
                    {
                        // 자동 등록
                        var lifetime = attr.IsGlobal ? Lifetime.Singleton : Lifetime.Scoped;
                        container.Register(type, type, lifetime);
                    }
                }
            }
        }
    }
}