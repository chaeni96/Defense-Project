using CatDarkGame.PerObjectRTRenderForUGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CatDarkGame.PerObjectRTRenderForUGUI_Editor
{
    internal class PerObjectRTRendererMenu
    {
        [MenuItem("GameObject/UI/PerObjectRT Renderer", false, 2018)]
        private static void AddParticleEmpty(MenuCommand menuCommand)
        {
            EditorApplication.ExecuteMenuItem("GameObject/UI/Image");
            var ui = Selection.activeGameObject;
            Object.DestroyImmediate(ui.GetComponent<Image>());
            
            var rtRenderer = ui.AddComponent<PerObjectRTRenderer>();
            rtRenderer.name = "UIPerObjectRTRenderer";
        }
    }

    
}
