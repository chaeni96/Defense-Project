using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Kylin.LWDI
{
    public static class DependencyInjector
    {
        // 정적 컨테이너 인스턴스에 접근
        private static DependencyContainer Container => DependencyContainer.Instance;
        
        // 필드 주입 메서드 (기존과 유사하지만 개선)
        public static void Inject<T>(T target) where T : IInjectable
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
                        var instance = Container.Resolve(fieldType);
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
        public static void InjectWithScope<T>(T target, IScope scope) where T : IInjectable
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
                        // 현재 스코프에서 먼저 검사
                        object instance = null;
                
                        if (scope != null && scope is Scope scopeImpl)
                        {
                            instance = scopeImpl.TryGetInstance(fieldType);
                        }
                
                        // 스코프에 없으면 컨테이너에서 해결
                        if (instance == null)
                        {
                            instance = Container.Resolve(fieldType);
                        }
                
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
        // 인스턴스 등록 확장 메서드
        public static void RegisterInstance<T>(T instance) where T : class, IDependencyObject
        {
            Container.RegisterInstance(instance);
        }
        
        // 타입 등록 확장 메서드
        public static void Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
            where TImplementation : IDependencyObject, TService
            where TService : class
        {
            Container.Register<TService, TImplementation>(lifetime);
        }
        
        // 모든 등록된 인스턴스 가져오기
        public static IReadOnlyDictionary<Type, object> GetAllInstances()
        {
            return Container.GetAllInstances();
        }
        
        // 씬 스코프 관리
        public static void ClearSceneScope(string currentScene)
        {
            Container.ClearSceneScope(currentScene);
        }
        
        // 스코프 생성
        public static IScope CreateScope()
        {
            return Container.CreateScope();
        }
        
        //메모리 누수 방지용 클린업
        public static void CleanupReferences<T>(T target) where T : IInjectable
        {
            var fields = target.GetType().GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>() != null)
                {
                    var value = field.GetValue(target);
                    if (value is BaseViewModel viewModel)
                    {
                        viewModel.RemoveReference();
                    }
                    field.SetValue(target, null);
                }
            }
        }
    }
}