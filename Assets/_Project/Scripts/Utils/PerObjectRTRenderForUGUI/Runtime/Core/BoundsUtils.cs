using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public static class BoundsUtils
    {
        private static readonly Vector3[] s_BoundsCorners = new Vector3[8];
        private static readonly Vector3[] s_ViewCorners = new Vector3[8];
        
        public static void ConvertBoundsToCorners(Bounds bounds, Vector3[] corners)
        {
            if (corners==null) throw new ArgumentNullException(nameof(corners));
            if (corners.Length!=8) throw new ArgumentException("Planes array must be of length 8.", nameof(corners));
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(min.x, min.y, max.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(min.x, max.y, max.z);
            corners[4] = new Vector3(max.x, min.y, min.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(max.x, max.y, min.z);
            corners[7] = new Vector3(max.x, max.y, max.z);
        }
        
        public static void CalculateLocalBounds(Transform transform, List<Renderer> renderers, out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.one);
            if (renderers == null || renderers.Count < 1) return;

            Bounds localBounds = new Bounds();
            bool firstPoint = true;
            foreach (var renderer in renderers)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeSelf) continue;
                if (renderer is ParticleSystemRenderer) continue;
                
                Bounds rendererWorldBounds = renderer.bounds;
                Vector3[] corners = s_BoundsCorners;
                ConvertBoundsToCorners(rendererWorldBounds, corners);
                
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 localCorner = transform.InverseTransformPoint(corners[i]); // 월드 좌표를 로컬 좌표로 변환
                    if (firstPoint)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        firstPoint = false;
                        continue;
                    }
                    localBounds.Encapsulate(localCorner);
                }
            }
            
            if (!firstPoint)
            {
                Vector3 minSize = new Vector3(0.01f, 0.01f, 0.01f); // 최소 크기 값
                Vector3 currentSize = localBounds.size;
                Vector3 requiredExpansion = Vector3.zero;
                if (currentSize.x < minSize.x) requiredExpansion.x = (minSize.x - currentSize.x) * 0.5f;
                if (currentSize.y < minSize.y) requiredExpansion.y = (minSize.y - currentSize.y) * 0.5f;
                if (currentSize.z < minSize.z) requiredExpansion.z = (minSize.z - currentSize.z) * 0.5f;
                if (requiredExpansion != Vector3.zero) localBounds.Expand(requiredExpansion * 2.0f);
                bounds = localBounds;
            }
        }
        
        public static Bounds ConvertLocalToWorldBounds(Transform transform, Bounds localBounds)
        {
            Vector3[] corners = s_BoundsCorners;
            ConvertBoundsToCorners(localBounds, corners);
            Vector3 firstWorldPoint = transform.TransformPoint(corners[0]);
            Bounds worldBounds = new Bounds(firstWorldPoint, Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(corners[i]);
                worldBounds.Encapsulate(worldPoint);
            }
            return worldBounds;
        }
        
        public static void CalculateOrthoViewProjection(Transform transform, Bounds bounds, out Matrix4x4 outViewMatrix, out Matrix4x4 outProjMatrix)
        {
            Vector3 padding = new Vector3(0.0f, 0.0f, 0.1f);
            float distance = 5.0f;
            float nearClipOverride = 0.1f;
            Bounds localBounds = bounds;
            localBounds.extents = new Vector3(
                localBounds.extents.x + padding.x,
                localBounds.extents.y + padding.y,
                localBounds.extents.z + padding.z);
            Vector3[] localCorners = s_BoundsCorners;
            ConvertBoundsToCorners(localBounds, localCorners);

            Vector3 localFrontCenter = new Vector3(localBounds.center.x, localBounds.center.y, localBounds.max.z);
            Vector3 worldFrontCenter = transform.TransformPoint(localFrontCenter);
            Vector3 objFront = transform.TransformDirection(Vector3.forward).normalized;
            Vector3 eye = worldFrontCenter - objFront * distance;
            Vector3 up = transform.TransformDirection(Vector3.up).normalized;
            Matrix4x4 viewMatrix = Matrix4x4.LookAt(eye, worldFrontCenter, up);
            Vector3[] viewCorners = s_ViewCorners;
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 worldCorner = transform.TransformPoint(localCorners[i]);
                viewCorners[i] = viewMatrix.MultiplyPoint(worldCorner);
            }

            float left = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MaxValue;
            float top = float.MinValue;
            float viewMaxZ = float.NegativeInfinity;
            float viewMinZ = float.PositiveInfinity;
            foreach (var v in viewCorners)
            {
                if (v.x < left) left = v.x;
                if (v.x > right) right = v.x;
                if (v.y < bottom) bottom = v.y;
                if (v.y > top) top = v.y;
                if (v.z > viewMaxZ) viewMaxZ = v.z;
                if (v.z < viewMinZ) viewMinZ = v.z;
            }
            float nearPlane = (viewMaxZ < 0.0f) ? -viewMaxZ : nearClipOverride;
            float farPlane = (viewMinZ < 0.0f) ? -viewMinZ : nearClipOverride + 1.0f;

            Matrix4x4 projMatrix = Matrix4x4.Ortho(left, right, bottom, top, nearPlane, farPlane);
            outViewMatrix = viewMatrix;
            outProjMatrix = projMatrix;
        }
        
        public static void CalculateOrthoViewProjectionJob(float4x4 localToWorldMatrix, Bounds bounds, out Matrix4x4 outViewMatrix, out Matrix4x4 outProjMatrix)
        {
            float3 boundsCenter = bounds.center;
            float3 boundsExtends = bounds.extents;
            float4x4 viewMatrix = float4x4.zero;
            float4x4 projMatrix = float4x4.zero;
            unsafe
            {
                fixed(Vector3* sBoundsCornersPtr = &s_BoundsCorners[0])
                fixed(Vector3* sViewCornersPtr = &s_ViewCorners[0])
                {
                    var calculateViewProjJob = new CalculateViewProjJob
                    {
                        LocalCorners = (float3*)sBoundsCornersPtr,
                        ViewCorners = (float3*)sViewCornersPtr,
                        LocalToWorldMatrix = localToWorldMatrix,
                        BoundsCenter = boundsCenter,
                        BoundsExtents = boundsExtends,
                        Padding = new float3(0.0f, 0.0f, 0.1f),
                        Distance = 5.0f,
                        NearClipOverride = 0.1f,
                        ViewMatrix = (float4x4*)UnsafeUtility.AddressOf(ref viewMatrix),
                        ProjMatrix = (float4x4*)UnsafeUtility.AddressOf(ref projMatrix),
                    };
                    calculateViewProjJob.Run();
                }
            }
            outViewMatrix = viewMatrix;
            outProjMatrix = projMatrix;
        }

        [BurstCompile]
        private unsafe struct CalculateViewProjJob : IJob
        {
            [NativeDisableUnsafePtrRestriction][ReadOnly] public float3* LocalCorners;
            [NativeDisableUnsafePtrRestriction][ReadOnly] public float3* ViewCorners;
            [ReadOnly] public float4x4 LocalToWorldMatrix;
            [ReadOnly] public float3 BoundsCenter;
            [ReadOnly] public float3 BoundsExtents;
            [ReadOnly] public float3 Padding;
            [ReadOnly] public float Distance;
            [ReadOnly] public float NearClipOverride;
            [NativeDisableUnsafePtrRestriction] public float4x4* ViewMatrix;
            [NativeDisableUnsafePtrRestriction] public float4x4* ProjMatrix;
            
            public void Execute()
            {
                float3 extentsWithPadding = new float3(BoundsExtents + Padding);
                float3 min = new float3(BoundsCenter - extentsWithPadding);
                float3 max = new float3(BoundsCenter + extentsWithPadding);
           
                LocalCorners[0] = min.xyz;
                LocalCorners[1] = new float3(min.x, min.y, max.z);
                LocalCorners[2] = new float3(min.x, max.y, min.z);
                LocalCorners[3] = new float3(min.x, max.y, max.z);
                LocalCorners[4] = new float3(max.x, min.y, min.z);
                LocalCorners[5] = new float3(max.x, min.y, max.z);
                LocalCorners[6] = new float3(max.x, max.y, min.z);
                LocalCorners[7] = max.xyz;
        
                float3 localFrontCenter = new float3(BoundsCenter.x, BoundsCenter.y, max.z);
                float4 localFrontCenter4 = new float4(localFrontCenter, 1.0f);
                float4 worldFrontCenter4 = math.mul(LocalToWorldMatrix, localFrontCenter4);
                float3 worldFrontCenter = worldFrontCenter4.xyz / worldFrontCenter4.w;
                
                float3 objForward = math.normalize(math.mul(LocalToWorldMatrix, new float4(0, 0, 1, 0)).xyz);
                float3 objUp = math.normalize(math.mul(LocalToWorldMatrix, new float4(0, 1, 0, 0)).xyz);
                float3 eye = worldFrontCenter - objForward * Distance;
                float4x4 viewMatrix = float4x4.LookAt(eye, worldFrontCenter, objUp);
                for (int i = 0; i < 8; i++)
                {
                    float4 localCorner4 = new float4(LocalCorners[i], 1.0f);
                    float4 worldCorner4 = math.mul(LocalToWorldMatrix, localCorner4);
                    float3 worldCorner = worldCorner4.xyz / worldCorner4.w;
                    float4 viewCorner4 = math.mul(viewMatrix, new float4(worldCorner, 1.0f));
                    ViewCorners[i] = viewCorner4.xyz / viewCorner4.w;
                }
                
                float left = float.MaxValue;
                float right = float.MinValue;
                float bottom = float.MaxValue;
                float top = float.MinValue;
                float viewMaxZ = float.NegativeInfinity;
                float viewMinZ = float.PositiveInfinity;
                for (int i = 0; i < 8; i++)
                {
                    left = math.min(left, ViewCorners[i].x);
                    right = math.max(right, ViewCorners[i].x);
                    bottom = math.min(bottom, ViewCorners[i].y);
                    top = math.max(top, ViewCorners[i].y);
                    viewMaxZ = math.max(viewMaxZ, ViewCorners[i].z);
                    viewMinZ = math.min(viewMinZ, ViewCorners[i].z);
                }
                
                float nearPlane = (viewMaxZ < 0.0f) ? -viewMaxZ : NearClipOverride;
                float farPlane = (viewMinZ < 0.0f) ? -viewMinZ : NearClipOverride + 1.0f;
                float4x4 projMatrix = new float4x4(
                    new float4(2f / (right - left), 0f, 0f, 0f),
                    new float4(0f, 2f / (top - bottom), 0f, 0f),
                    new float4(0f, 0f, -2f / (farPlane - nearPlane), 0f),
                    new float4(-(right + left) / (right - left), -(top + bottom) / (top - bottom), 
                              -(farPlane + nearPlane) / (farPlane - nearPlane), 1f));
                
                *ViewMatrix = viewMatrix;
                *ProjMatrix = projMatrix;
            }
        }
    }
}