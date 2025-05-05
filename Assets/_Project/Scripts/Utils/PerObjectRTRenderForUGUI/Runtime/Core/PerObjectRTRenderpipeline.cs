using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public class PerObjectRTRenderpipeline
    {
        private static PerObjectRTRenderpipeline g_Inst;
        private static PerObjectRTRenderpipeline Get() { return g_Inst ??= new PerObjectRTRenderpipeline(); }

        private static readonly string k_CommandBufferName = "PerObjectRTRenderpipeline";
        private static readonly string k_PassName = "PerObjectRTRenderForUGUI Pass";
        private static readonly string k_RTName = "PerObjectRTRenderForUGUI Texture";

        private ProfilingSampler _profilingSampler = new ProfilingSampler(k_PassName);
        private static bool IsCanvasRebuild = false;
        private RTHandle _rtHandle;

        private readonly List<PerObjectRTRenderer> _rebuildRendererQueue = new List<PerObjectRTRenderer>();
        private readonly HashSet<PerObjectRTSource> _renderSourceQueue = new HashSet<PerObjectRTSource>();

        private PerObjectRTRenderpipeline()
        {
            RenderPipelineManager.beginContextRendering += OnBeginFrameRendering;
        }

        ~PerObjectRTRenderpipeline()
        {
            RenderPipelineManager.beginContextRendering -= OnBeginFrameRendering;
            Dispose();
        }

        private void Dispose()
        {
            _rtHandle?.Release();
            _rebuildRendererQueue.Clear();
            _renderSourceQueue.Clear();
        }

        internal static void RegisterRenderElementRebuild()
        {
            var instance = Get();
            if (!IsCanvasRebuild)
            {
                Canvas.willRenderCanvases += instance.OnBeforeCanvasRebuild;
                IsCanvasRebuild = true;
            }
        }

        private void OnBeginFrameRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            RenderExecute(context);
        }

        private void OnBeforeCanvasRebuild()
        {
            Canvas.willRenderCanvases -= OnBeforeCanvasRebuild;
            IsCanvasRebuild = false;
            Profiler.BeginSample(ProfilerStrings.RenderElementRebuild);
            {
                RenderElementRebuild();
            }
        }

        private void RenderElementRebuild()
        {
            _rebuildRendererQueue.Clear();
            _renderSourceQueue.Clear();
            foreach (var rendererElement in IElementPool<PerObjectRTRenderer>.ElementPool)
            {
                PerObjectRTSource sourceElement = rendererElement.source;
                if (!PerObjectRTSource.CheckRenderActiveState(sourceElement))
                {
                    rendererElement.texture = Texture2D.whiteTexture;
                    rendererElement.uvRect = new Rect(0, 0, 1, 1);
                    continue;
                }
                _renderSourceQueue.Add(sourceElement);
                _rebuildRendererQueue.Add(rendererElement);
            }

            int sliceCount = _renderSourceQueue.Count;
            if (sliceCount < 1)
            {
                _rtHandle?.Release();
                return;
            }

            PerObjectRTRenderAtlasLayout.CalculateSliceData(sliceCount, out int slicesPerRow, out Vector2 scaledSliceSize, out Vector2Int atlasResolution);
            ReAllocateIfNeeded(atlasResolution.x, atlasResolution.y);

            int renderIndex = -1;
            foreach (var sourceElement in _renderSourceQueue)
            {
                renderIndex++;
                int xIndex = renderIndex % slicesPerRow;
                int yIndex = renderIndex / slicesPerRow;
                Vector2 position = new Vector2(xIndex * scaledSliceSize.x, yIndex * scaledSliceSize.y);
                Rect viewport = new Rect(position.x, position.y, scaledSliceSize.x, scaledSliceSize.y);
                Rect uvRect = new Rect(position.x / atlasResolution.x, position.y / atlasResolution.y, scaledSliceSize.x / atlasResolution.x, scaledSliceSize.y / atlasResolution.y);

                sourceElement.Viewport = viewport;

                for (var i = _rebuildRendererQueue.Count - 1; i >= 0; i--)
                {
                    var rendererElement = _rebuildRendererQueue[i];
                    if (rendererElement.source == sourceElement)
                    {
                        rendererElement.uvRect = uvRect;
                        rendererElement.texture = _rtHandle.rt;
                        _rebuildRendererQueue.RemoveAt(i);
                    }
                }
            }
        }

        private void RenderExecute(ScriptableRenderContext context)
        {
            int sliceCount = _renderSourceQueue.Count;
            if (sliceCount < 1) return;
            RTHandle rtHandle = _rtHandle;
            if (rtHandle == null || rtHandle.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(k_CommandBufferName);
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                 cmd.SetRenderTarget(rtHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                 cmd.ClearRenderTarget(false, true, Color.clear);

                 foreach (var sourceElement in _renderSourceQueue)
                 {
                     sourceElement.RenderExecute(cmd);
                 }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private void ReAllocateIfNeeded(int width, int height)
        {
            string name = k_RTName;

            var descriptor = new RenderTextureDescriptor
            {
                width = width,
                height = height,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                colorFormat = RenderTextureFormat.ARGB32,
                sRGB = true,
                depthBufferBits = 0,
                enableRandomWrite = false,
                vrUsage = VRTextureUsage.None,
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1
            };

            bool needReAllocRT = CheckReAllocRT(_rtHandle, descriptor, name);
            if (!needReAllocRT) return;

            Profiler.BeginSample(ProfilerStrings.AllocateRTHandle);
            {
                _rtHandle?.Release();
                _rtHandle = RTHandles.Alloc(width, height, name: name);

                var rt = _rtHandle.rt;
                rt.filterMode = FilterMode.Point;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.anisoLevel = 1;
                rt.mipMapBias = 0.0f;
            }
            Profiler.EndSample();
        }

        private static bool CheckReAllocRT(RTHandle handle, RenderTextureDescriptor descriptor, string name = "")
        {
            if (handle == null || handle.rt == null) return true;
            if (handle.rt.width != descriptor.width || handle.rt.height != descriptor.height) return true;
            if (handle.rt.descriptor.colorFormat != descriptor.colorFormat) return true;
            if (handle.name != name) return true;
            return false;
        }
    }
}
