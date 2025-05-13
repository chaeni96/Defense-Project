using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kylin.LWDI
{
    public class LWDIManager : MonoBehaviour
    {
        private static LWDIManager _instance;
        
        private string _currentScene;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _currentScene = SceneManager.GetActiveScene().name;
            DependencyContainerExtensions.SetCurrentScene(_currentScene);
            
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            _currentScene = newScene.name;
            DependencyContainerExtensions.SetCurrentScene(_currentScene);
        }
        
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            
            if (_instance == this)
            {
                DependencyContainer.Instance.Clear();
                _instance = null;
            }
        }
        
        public static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("LWDI Manager");
                _instance = go.AddComponent<LWDIManager>();
            }
        }
    }
}