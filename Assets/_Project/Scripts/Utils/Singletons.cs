namespace AutoBattle.Scripts.Utils
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static T instance = null;

        public static T Instance
        {
            get
            {
                instance ??= new T();
                return instance;
            }
        }

        public static void DestroyInstance()
        {
            instance = null;
        }
    }
}