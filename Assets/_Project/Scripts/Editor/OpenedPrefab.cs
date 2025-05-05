using UnityEditor;
using UnityEngine;

[FilePath("ScriptableSingletonAssets/SceneHelperSingleton.asset", FilePathAttribute.Location.PreferencesFolder)]
internal class SceneHelperSingleton : ScriptableSingleton<SceneHelperSingleton>
{
    internal string OpenedScenePath;
    internal string OpenedPrefabPath;
    internal GameObject Prefab;
}
