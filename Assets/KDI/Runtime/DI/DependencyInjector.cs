using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Kylin.LWDI
{
    /// <summary>
    /// 의존성 주입
    /// </summary>
    public static class DependencyInjector
    {
        private static DependencyContainer Container => DependencyContainer.Instance;
        public static void Inject<T>(this T target, IScope scope = null) where T : IInjectable
        {
            if (target == null)
            {
                return;
            }
            
            IScope resolutionScope = scope ?? Container.CurrentScope;
            //주입
            InjectFields(target, resolutionScope);
            InjectProperties(target, resolutionScope);
        }
        
        private static void InjectFields<T>(T target, IScope scope) where T : IInjectable
        {
            var fields = target.GetType().GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>() != null)
                {
                    var fieldType = field.FieldType;
                    try
                    {
                        var instance = scope != null 
                            ? scope.Resolve(fieldType) 
                            : Container.Resolve(fieldType);
                            
                        field.SetValue(target, instance);
                        
                        // ViewModel 참조 카운트 관리
                        if (instance is BaseViewModel viewModel)
                        {
                            viewModel.AddReference();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DI Error] 필드 '{field.Name}'에 '{fieldType.Name}' 주입 실패: {ex.Message}");
                    }
                }
            }
        }
        
        private static void InjectProperties<T>(T target, IScope scope) where T : IInjectable
        {
            var properties = target.GetType().GetProperties(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<InjectAttribute>() != null && 
                    property.CanWrite)
                {
                    var propertyType = property.PropertyType;
                    try
                    {
                        var instance = scope != null 
                            ? scope.Resolve(propertyType) 
                            : Container.Resolve(propertyType);
                            
                        property.SetValue(target, instance);
                        
                        // ViewModel 참조 카운트 관리(필요 없을지도..)
                        if (instance is BaseViewModel viewModel)
                        {
                            viewModel.AddReference();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DI Error] 프로퍼티 '{property.Name}'에 '{propertyType.Name}' 주입 실패: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 인스턴스 등록
        /// </summary>
        public static void RegisterInstance<T>(T instance) where T : class, IDependencyObject
        {
            Container.RegisterInstance(instance);
        }
        
        /// <summary>
        /// 타입 등록
        /// </summary>
        public static void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
            where TImplementation : class, TService, IDependencyObject
            where TService : class
        {
            Container.Register<TService, TImplementation>(lifetime);
        }
        
        /// <summary>
        /// 팩토리 등록
        /// </summary>
        public static void RegisterFactory<TService>(Func<IScope, TService> factory, Lifetime lifetime = Lifetime.Singleton)
            where TService : class
        {
            Container.RegisterFactory(factory, lifetime);
        }
        
        /// <summary>
        /// 모든 등록된 인스턴스 가져오기
        /// </summary>
        public static IReadOnlyDictionary<Type, object> GetAllInstances()
        {
            // 다음 메서드가 DependencyContainer에 있다고 가정
            return Container.GetAllInstances();
        }
        
        /// <summary>
        /// 의존성 해결
        /// </summary>
        public static T Resolve<T>() where T : class
        {
            return Container.Resolve<T>();
        }
        
        /// <summary>
        /// 의존성 해결 (비제네릭)
        /// </summary>
        public static object Resolve(Type type)
        {
            return Container.Resolve(type);
        }
        
        /// <summary>
        /// 스코프 생성
        /// </summary>
        public static IScope CreateScope(IScope parentScope = null)
        {
            return Container.CreateScope(parentScope);
        }
        
        /// <summary>
        /// 시스템 초기화
        /// </summary>
        public static void Initialize()
        {
            // LWDIManager 생성 등 필요한 초기화 집어넣을것..!
            LWDIManager.Initialize();
        }
    }
}