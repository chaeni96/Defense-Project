using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        void Inject(IScope scope = null);
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
        private static readonly object _lock = new object();
        private static readonly DependencyContainer _instance = new DependencyContainer();
        public static DependencyContainer Instance => _instance;
        
        // 등록 정보를 저장하는 클래스
        public class Registration
        {
            public Type ServiceType { get; set; }
            public Type ImplementationType { get; set; }
            public object Instance { get; set; }
            public Lifetime Lifetime { get; set; }
            public Func<IScope, object> Factory { get; set; }
        }
        
        // Type별 등록 정보 저장 (서비스 타입 -> 등록 정보)
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        // 활성 스코프 스택 - 항상 최소 하나의 루트 스코프가 존재
        private readonly Stack<Scope> _scopeStack = new Stack<Scope>();
        // 현재 실행 컨텍스트의 스코프
        private readonly ThreadLocal<Scope> _executionContextScope = new ThreadLocal<Scope>();
        
        private DependencyContainer()
        {
            // 루트 스코프 생성
            var rootScope = new Scope(this, null);
            _scopeStack.Push(rootScope);
        }
        
        /// <summary>
        /// 새 스코프 생성
        /// </summary>
        public IScope CreateScope(IScope parentScope = null)
        {
            IScope parent = parentScope ?? CurrentScope;
            var scope = new Scope(this, parent);
            
            lock (_lock)
            {
                _scopeStack.Push((Scope)scope);
            }
            
            return scope;
        }
        
        /// <summary>
        /// 현재 스코프를 실행 컨텍스트에 설정
        /// </summary>
        internal void SetCurrentExecutionScope(Scope scope)
        {
            _executionContextScope.Value = scope;
        }
        
        /// <summary>
        /// 스코프 제거
        /// </summary>
        internal void RemoveScope(Scope scope)
        {
            lock (_lock)
            {
                if (_scopeStack.Count > 1 && _scopeStack.Contains(scope))
                {
                    // 스택에서 해당 스코프와 그 위의 모든 스코프 제거
                    var tempStack = new Stack<Scope>();
                    
                    while (_scopeStack.Count > 0)
                    {
                        var current = _scopeStack.Pop();
                        if (current == scope)
                            break;
                            
                        tempStack.Push(current);
                    }
                    
                    // 제거된 스코프 위에 있던 스코프들 복원
                    while (tempStack.Count > 0)
                    {
                        _scopeStack.Push(tempStack.Pop());
                    }
                }
                
                // 실행 컨텍스트 스코프가 제거된 스코프인 경우 초기화
                if (_executionContextScope.Value == scope)
                {
                    _executionContextScope.Value = null;
                }
            }
        }
        
        /// <summary>
        /// 현재 스코프 가져오기 (실행 컨텍스트 우선, 없으면 스택 맨 위)
        /// </summary>
        internal Scope CurrentScope
        {
            get
            {
                var executionScope = _executionContextScope.Value;
                
                if (executionScope != null)
                    return executionScope;
                    
                lock (_lock)
                {
                    return _scopeStack.Count > 0 ? _scopeStack.Peek() : null;
                }
            }
        }
        
        /// <summary>
        /// 타입 등록 - 인터페이스와 구현 타입 지정
        /// </summary>
        public void Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
        {
            if (!typeof(IDependencyObject).IsAssignableFrom(implementationType))
                throw new ArgumentException($"Type {implementationType.Name} must implement IDependencyObject");
            
            lock (_lock)
            {
                _registrations[serviceType] = new Registration
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Lifetime = lifetime
                };
            }
        }
        
        /// <summary>
        /// 타입 등록 - 제네릭 버전
        /// </summary>
        public void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
            where TService : class
            where TImplementation : class, TService, IDependencyObject
        {
            Register(typeof(TService), typeof(TImplementation), lifetime);
        }
        
        /// <summary>
        /// 인스턴스 등록
        /// </summary>
        public void RegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance is IDependencyObject)
            {
                lock (_lock)
                {
                    var serviceType = typeof(TService);
                    
                    _registrations[serviceType] = new Registration
                    {
                        ServiceType = serviceType,
                        Instance = instance,
                        Lifetime = Lifetime.Singleton
                    };
                    
                    // 싱글톤 캐시에 저장
                    _singletonInstances[serviceType] = instance;
                }
            }
            else
                throw new ArgumentException($"Instance must implement IDependencyObject");
        }
        
        /// <summary>
        /// 팩토리 함수 등록
        /// </summary>
        public void RegisterFactory<TService>(Func<IScope, TService> factory, Lifetime lifetime = Lifetime.Singleton)
            where TService : class
        {
            lock (_lock)
            {
                var serviceType = typeof(TService);
                
                _registrations[serviceType] = new Registration
                {
                    ServiceType = serviceType,
                    Factory = scope => factory(scope),
                    Lifetime = lifetime
                };
            }
        }
        
        /// <summary>
        /// 의존성 해결 - 타입 기반
        /// </summary>
        public object Resolve(Type serviceType, IScope scope = null)
        {
            // 사용할 스코프 결정
            var resolutionScope = (scope as Scope) ?? CurrentScope;
            
            lock (_lock)
            {
                // 1. 스코프에서 이미 존재하는 인스턴스 확인
                if (resolutionScope != null)
                {
                    var scopedInstance = resolutionScope.GetInstance(serviceType);
                    if (scopedInstance != null)
                        return scopedInstance;
                }
                
                // 2. 등록 정보 확인
                if (!_registrations.TryGetValue(serviceType, out var registration))
                {
                    // 인터페이스/추상 클래스인 경우 구현체 찾기
                    if (serviceType.IsInterface || serviceType.IsAbstract)
                    {
                        foreach (var reg in _registrations.Values)
                        {
                            if (serviceType.IsAssignableFrom(reg.ServiceType))
                            {
                                return Resolve(reg.ServiceType, resolutionScope);
                            }
                        }
                        
                        throw new InvalidOperationException($"No registration found for {serviceType.Name}");
                    }
                    
                    // 등록되지 않은 구체 클래스는 자동 등록 (IDependencyObject 구현 필수)
                    if (typeof(IDependencyObject).IsAssignableFrom(serviceType))
                    {
                        Register(serviceType, serviceType);
                        registration = _registrations[serviceType];
                    }
                    else
                        throw new InvalidOperationException($"Type {serviceType.Name} must implement IDependencyObject");
                }
                
                // 3. 이미 존재하는 인스턴스 확인
                if (registration.Instance != null)
                    return registration.Instance;
                
                // 4. 라이프타임별 인스턴스 생성
                object instance;
                
                switch (registration.Lifetime)
                {
                    case Lifetime.Singleton:
                        // 싱글톤 캐시 확인
                        if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
                            return singletonInstance;
                            
                        // 새 인스턴스 생성
                        instance = CreateInstance(registration, resolutionScope);
                        _singletonInstances[serviceType] = instance;
                        break;
                        
                    case Lifetime.Scoped:
                        // 스코프 내 인스턴스 생성
                        instance = CreateInstance(registration, resolutionScope);
                        
                        // 현재 스코프에 저장
                        if (resolutionScope != null)
                        {
                            resolutionScope.RegisterInstance(serviceType, instance);
                        }
                        break;
                        
                    case Lifetime.Transient:
                        // 매번 새 인스턴스 생성
                        instance = CreateInstance(registration, resolutionScope);
                        break;
                        
                    default:
                        throw new InvalidOperationException($"Unsupported lifetime: {registration.Lifetime}");
                }
                
                return instance;
            }
        }
        
        /// <summary>
        /// 의존성 해결 - 제네릭 버전
        /// </summary>
        public T Resolve<T>(IScope scope = null) where T : class
        {
            return (T)Resolve(typeof(T), scope);
        }
        
        /// <summary>
        /// 인스턴스 생성
        /// </summary>
        private object CreateInstance(Registration registration, IScope scope)
        {
            // 1. 팩토리가 있는 경우
            if (registration.Factory != null)
                return registration.Factory(scope);
                
            // 2. 구현 타입 확인
            var implementationType = registration.ImplementationType;
            
            // 3. 생성자 선택 - 가장 많은 매개변수를 가진 생성자
            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
            {
                // 기본 생성자만 있는 경우
                return Activator.CreateInstance(implementationType);
            }
            
            var constructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();
                
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                // 매개변수 없는 생성자
                return Activator.CreateInstance(implementationType);
            }
            
            // 4. 생성자 매개변수 해결
            var resolvedParams = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                try
                {
                    resolvedParams[i] = Resolve(paramType, scope);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error resolving parameter '{parameters[i].Name}' of type {paramType.Name} for {implementationType.Name}",
                        ex);
                }
            }
            
            // 5. 인스턴스 생성
            var instance = Activator.CreateInstance(implementationType, resolvedParams);
            
            // 6. 필드 주입 수행
            InjectFields(instance, scope);
            
            return instance;
        }
        
        /// <summary>
        /// 필드 주입
        /// </summary>
        public void InjectFields(object instance, IScope scope = null)
        {
            if (instance == null)
                return;
                
            if (instance is IInjectable injectable)
            {
                injectable.Inject(scope);
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
                            var dependency = Resolve(fieldType, scope);
                            field.SetValue(instance, dependency);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error injecting field '{field.Name}' of type {fieldType.Name} in {instance.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 모든 등록된 인스턴스 가져오기
        /// </summary>
        public IReadOnlyDictionary<Type, object> GetAllInstances()
        {
            Dictionary<Type, object> allInstances = new Dictionary<Type, object>();
            
            lock (_lock)
            {
                // 1. 싱글톤 인스턴스 복사
                foreach (var kvp in _singletonInstances)
                {
                    allInstances[kvp.Key] = kvp.Value;
                }
                
                // 2. 현재 스코프의 인스턴스 복사
                var scope = CurrentScope;
                while (scope != null)
                {
                    var scopedInstances = scope.GetInstances();
                    foreach (var kvp in scopedInstances)
                    {
                        if (!allInstances.ContainsKey(kvp.Key))
                        {
                            allInstances[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    // 부모 스코프로 이동
                    scope = scope.Parent as Scope;
                }
            }
            
            return allInstances;
        }
        
        /// <summary>
        /// 모든 등록 정보 가져오기
        /// </summary>
        public IReadOnlyDictionary<Type, Type> GetAllRegistrations()
        {
            Dictionary<Type, Type> result = new Dictionary<Type, Type>();
            
            lock (_lock)
            {
                foreach (var kvp in _registrations)
                {
                    if (kvp.Value.ImplementationType != null)
                    {
                        result[kvp.Key] = kvp.Value.ImplementationType;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 컨테이너 초기화
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                // 모든 스코프 제거
                while (_scopeStack.Count > 0)
                {
                    var scope = _scopeStack.Pop();
                    scope.Dispose();
                }
                
                // 등록 정보 및 인스턴스 초기화
                _registrations.Clear();
                _singletonInstances.Clear();
                _executionContextScope.Value = null;
                
                // 루트 스코프 다시 생성
                var rootScope = new Scope(this, null);
                _scopeStack.Push(rootScope);
            }
        }
        
        /// <summary>
        /// 씬 스코프 정리
        /// </summary>
        public void ClearSceneScope(string currentScene)
        {
            lock (_lock)
            {
                // 씬 전용 뷰모델 제거
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
                
                // 제거 목록의 등록 및 인스턴스 제거
                foreach (var type in removeList)
                {
                    _registrations.Remove(type);
                    _singletonInstances.Remove(type);
                }
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