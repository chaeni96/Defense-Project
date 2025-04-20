using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public class PerObjectRTSourceBoundsManager
    {
        private static PerObjectRTSourceBoundsManager g_Inst;
        private static PerObjectRTSourceBoundsManager Get() { return g_Inst ??= new PerObjectRTSourceBoundsManager(); }
        private static bool IsProcess = false;
        private readonly HashSet<PerObjectRTSource> _renderSourceQueue = new HashSet<PerObjectRTSource>();
        
        private PerObjectRTSourceBoundsManager()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload += Dispose;
#endif
        }

        ~PerObjectRTSourceBoundsManager()
        {
            Dispose();
        }

        private void Dispose()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= Dispose;
#endif
            _renderSourceQueue.Clear();
        }
        
        public static void RegisterBoundsUpdate(PerObjectRTSource source)
        {
            if (!source) return;
            var instance = Get();
            if (!IsProcess)
            {
                Canvas.preWillRenderCanvases += instance.OnProcessBoundsUpdate;
                IsProcess = true;
            }
            instance._renderSourceQueue.Add(source);
        }

        private void OnProcessBoundsUpdate()
        {
            Canvas.preWillRenderCanvases -= OnProcessBoundsUpdate;
            IsProcess = false;
            Profiler.BeginSample(ProfilerStrings.ProcessBoundsUpdate);
            {
                ProcessBoundsUpdate();
            }
            Profiler.EndSample();
        }
        
        // TODO - RTSource 데이터를 Array로 관리해서 멀티쓰레딩으로 전환 필요. (현재 싱글쓰레드)
        private void ProcessBoundsUpdate()
        {
            foreach (var source in _renderSourceQueue)
            {
                if (!source || !source.isActiveAndEnabled) continue;

                Bounds bounds = source.Bounds;
                if (source.AutoBoundsMode)
                {
                    Profiler.BeginSample(ProfilerStrings.CalculateLocalBounds);
                    BoundsUtils.CalculateLocalBounds(source.transform, source.Renderers, out bounds);
                    source.InternalBounds = bounds;
                    Profiler.EndSample();
                }
                
                Profiler.BeginSample(ProfilerStrings.CalculateOrthoViewProjection);
                    BoundsUtils.CalculateOrthoViewProjectionJob(source.transform.localToWorldMatrix, bounds, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix);
                    //BoundsUtils.CalculateOrthoViewProjection(source.transform, bounds, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix);
                    source.ViewMatrix = viewMatrix;
                    source.ProjMatrix = projMatrix;
                Profiler.EndSample();
            }
            _renderSourceQueue.Clear();
        }
    }
}