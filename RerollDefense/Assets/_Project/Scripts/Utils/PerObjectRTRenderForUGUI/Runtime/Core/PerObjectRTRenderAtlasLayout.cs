using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public static class PerObjectRTRenderAtlasLayout
    {
        private static readonly Vector2Int k_sliceResolution_Min = new Vector2Int(64, 64);
        private static readonly Vector2Int k_maxAtlasResolution_Max = new Vector2Int(2048, 2048);

        private static Vector2Int _sliceResolution = new Vector2Int(256, 256);
        private static Vector2Int _maxAtlasResolution = new Vector2Int(2048, 2048);

        public static Vector2Int SliceResolution
        {
            get => _sliceResolution;
            set
            {
                Vector2Int setValue = Vector2Int.Max(value, k_sliceResolution_Min);
                if (_sliceResolution == setValue) return;
                _sliceResolution = setValue;
                PerObjectRTRenderpipeline.RegisterRenderElementRebuild();
            }
        }

        public static Vector2Int AtlasResolution
        {
            get => _maxAtlasResolution;
            set
            {
                Vector2Int setValue = Vector2Int.Min(value, k_maxAtlasResolution_Max);
                if (_maxAtlasResolution == setValue) return;
                _maxAtlasResolution = setValue;
                PerObjectRTRenderpipeline.RegisterRenderElementRebuild();
            }
        }

        internal static void CalculateSliceData(int sliceCount, out int slicesPerRow, out Vector2 scaledSliceSize, out Vector2Int atlasResolution)
        {
            if (sliceCount < 1)
            {
                slicesPerRow = 0;
                scaledSliceSize = Vector2.zero;
                atlasResolution = Vector2Int.one;
                return;
            }

            Profiler.BeginSample(ProfilerStrings.CalculateSliceData);

            Vector2Int sliceResolution = SliceResolution;
            Vector2Int maxAtlasResolution = AtlasResolution;

            int maxSlicesPerRow = maxAtlasResolution.x / sliceResolution.x;
            int maxRows = maxAtlasResolution.y / sliceResolution.y;
            int maxAllowedSlices = maxSlicesPerRow * maxRows;

            // Slice 개수가 최대치를 초과하는 경우 Slice 크기를 축소
            scaledSliceSize = sliceResolution;
            if (sliceCount > maxAllowedSlices)
            {
                float uvScale = Mathf.Sqrt((float)maxAllowedSlices / sliceCount);
                scaledSliceSize.x = sliceResolution.x * uvScale;
                scaledSliceSize.y = sliceResolution.y * uvScale;
            }
            slicesPerRow = Mathf.FloorToInt(maxAtlasResolution.x / scaledSliceSize.x);
            int rowsNeeded = Mathf.CeilToInt((float)sliceCount / slicesPerRow);

            // Atlas 크기 계산 (Slice 축소가 없다면 최적의 크기, 축소가 있다면 최대 크기 유지)
            atlasResolution = sliceCount > maxAllowedSlices
                ? maxAtlasResolution
                : new Vector2Int(
                    Mathf.CeilToInt(Mathf.Min(sliceCount, slicesPerRow) * scaledSliceSize.x),
                    Mathf.CeilToInt(rowsNeeded * scaledSliceSize.y)
                );

            Profiler.EndSample();
        }

        internal static void CalculateOrthoViewProjection(Transform transform, Bounds bounds, Vector3 padding, out Matrix4x4 viewMatrix4X4, out Matrix4x4 projMatrix4X4)
        {
            Profiler.BeginSample(ProfilerStrings.CalculateOrthoViewProjection);

            float distance = 5.0f;
            float nearClipOverride = 0.1f;
            Bounds localBounds = bounds;
            localBounds.extents = new Vector3(localBounds.extents.x + padding.x,
                localBounds.extents.y + padding.y,
                localBounds.extents.z + padding.z);

            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            Vector3[] localCorners = new Vector3[8];
            localCorners[0] = new Vector3(min.x, min.y, min.z);
            localCorners[1] = new Vector3(min.x, min.y, max.z);
            localCorners[2] = new Vector3(min.x, max.y, min.z);
            localCorners[3] = new Vector3(min.x, max.y, max.z);
            localCorners[4] = new Vector3(max.x, min.y, min.z);
            localCorners[5] = new Vector3(max.x, min.y, max.z);
            localCorners[6] = new Vector3(max.x, max.y, min.z);
            localCorners[7] = new Vector3(max.x, max.y, max.z);

            Vector3 localFrontCenter = new Vector3(localBounds.center.x, localBounds.center.y, max.z);
            Vector3 worldFrontCenter = transform.TransformPoint(localFrontCenter);
            Vector3 objFront = transform.TransformDirection(Vector3.forward).normalized;
            Vector3 eye = worldFrontCenter - objFront * distance;
            Vector3 up = transform.TransformDirection(Vector3.up).normalized;
            Matrix4x4 viewMatrix = Matrix4x4.LookAt(eye, worldFrontCenter, up);

            Vector3[] viewCorners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 worldCorner = transform.TransformPoint(localCorners[i]);
                viewCorners[i] = viewMatrix.MultiplyPoint(worldCorner);
            }

            float left   = float.MaxValue;
            float right  = float.MinValue;
            float bottom = float.MaxValue;
            float top    = float.MinValue;
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
            float nearPlane = (viewMaxZ < 0) ? -viewMaxZ : nearClipOverride;
            float farPlane  = (viewMinZ < 0) ? -viewMinZ : nearClipOverride + 1f;


            Matrix4x4 projMatrix = Matrix4x4.Ortho(left, right, bottom, top, nearPlane, farPlane);
            //Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
            viewMatrix4X4 = viewMatrix;
            projMatrix4X4 = projMatrix;

            Profiler.EndSample();
        }
    }
}
