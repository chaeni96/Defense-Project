using UnityEngine;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    internal static class ProfilerStrings
    {
        private const string HeadTitle = "[PerObjectRTRenderForUGUI]";
        internal static readonly string AllocateRTHandle = $"{HeadTitle} AllocateRTHandle";
        internal static readonly string RenderElementRebuild = $"{HeadTitle} RenderElementRebuild";
        internal static readonly string CalculateSliceData = $"{HeadTitle} CalculateSliceData";
        internal static readonly string CalculateLocalBounds = $"{HeadTitle} CalculateLocalBounds";
        internal static readonly string CalculateOrthoViewProjection = $"{HeadTitle} CalculateOrthoViewProjection";
        internal static readonly string ProcessBoundsUpdate = $"{HeadTitle} ProcessBoundsUpdate";
    }
    
    
    
}