using System;
using System.Linq;
using UnityEngine;

namespace Kylin.LWDI
{
    /*public static class ViewModelAutoRegister
    {
        public static void RegisterAllViewModels(string currentScene)
        {
            var viewModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseViewModel)))
                .Where(t => !t.IsAbstract)
                .ToList();

            foreach (var type in viewModelTypes)
            {
                var attr = type.GetCustomAttributes(typeof(ViewModelAttribute), true).FirstOrDefault() as ViewModelAttribute;

                if (attr != null && (attr.IsGlobal || attr.SceneNames.Contains(currentScene)))
                {
                    if (!DependencyContainer.Instance.GetAllInstances().ContainsKey(type))
                    {
                        var instance = Activator.CreateInstance(type) as IDependencyObject;
                        DependencyContainer.Register(type, instance);
                    }
                }
            }
        }
    }*/
}