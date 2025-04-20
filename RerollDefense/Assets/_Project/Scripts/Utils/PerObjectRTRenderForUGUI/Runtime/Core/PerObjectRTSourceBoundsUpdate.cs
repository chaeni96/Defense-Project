using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


// 참고용코드, 추후 Job전환을 위한
namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundsData
    {
        public float3 center;
        public float3 extents;
        public Vector3 size
        {
            get => this.extents * 2f;
            set => this.extents = value * 0.5f;
        }
        
        public Vector3 min
        {
            get => this.center - this.extents;
            set => this.SetMinMax(value, this.max);
        }
        
        public Vector3 max
        {
            get => this.center + this.extents;
            set => this.SetMinMax(this.min, value);
        }
        public BoundsData(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.extents = size * 0.5f;
        }
        
        public static bool operator ==(BoundsData lhs, BoundsData rhs)
        {
            return math.all(lhs.center == rhs.center) && math.all(lhs.extents == rhs.extents);
        }
    
        public static bool operator !=(BoundsData lhs, BoundsData rhs) => !(lhs == rhs);
        
        public override bool Equals(object obj)
        {
            if (!(obj is BoundsData)) return false;
            return this == (BoundsData)obj;
        }
    
        public override int GetHashCode()
        {
            return center.GetHashCode() ^ extents.GetHashCode();
        }
        
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            float3 minF3 = new float3(min.x, min.y, min.z);
            float3 maxF3 = new float3(max.x, max.y, max.z);
            this.extents = (maxF3 - minF3) * 0.5f;
            this.center = minF3 + this.extents;
        }
        
        public void Encapsulate(Vector3 point)
        {
            this.SetMinMax(Vector3.Min(this.min, point), Vector3.Max(this.max, point));
        }

        public void Expand(Vector3 amount)
        {
            this.extents = new float3(this.extents.x + amount.x * 0.5f,
                this.extents.y + amount.y * 0.5f,
                this.extents.z + amount.z * 0.5f);
        }
    }
    
    /// <summary>
    /// 여러 PerObjectRTSource 객체의 바운드 업데이트를 관리하는 싱글톤 매니저
    /// </summary>
    public class PerObjectRTSourceBoundsUpdate
    {
        private static PerObjectRTSourceBoundsUpdate g_Inst;
        private static PerObjectRTSourceBoundsUpdate Get() { return g_Inst ??= new PerObjectRTSourceBoundsUpdate(); }
        
        // 업데이트가 필요한 소스 목록
        private HashSet<PerObjectRTSource> _dirtySourcesSet = new HashSet<PerObjectRTSource>();
        private List<PerObjectRTSource> _dirtySources = new List<PerObjectRTSource>();
        
        // 병렬 처리를 위한 데이터
        private JobHandle _boundsJobHandle;
        private bool _isJobScheduled = false;
        private NativeArray<float3> _cornerPointsArray;
        private NativeArray<int> _sourceStartIndices;
        private NativeArray<int> _sourceCornerCounts;
        private NativeArray<BoundsMinMax> _resultBounds;
        
        // 데이터 준비용 임시 버퍼
        private List<Vector3> _cornerPoints = new List<Vector3>();
        
        // 최소 바운드 크기
        private Vector3 _minBoundsSize = new Vector3(0.1f, 0.1f, 0.1f);
        
        // 성능 설정
        private int _batchSize = 64;
        
        /// <summary>
        /// 바운드 최소 크기 설정
        /// </summary>
        public Vector3 MinBoundsSize
        {
            get => _minBoundsSize;
            set => _minBoundsSize = value;
        }
        
        /// <summary>
        /// 배치 크기 설정 (고급 성능 옵션)
        /// </summary>
        public int BatchSize
        {
            get => _batchSize;
            set => _batchSize = Mathf.Max(1, value);
        }

        private PerObjectRTSourceBoundsUpdate()
        {
            Application.onBeforeRender += ProcessBoundsUpdate;
        #if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload += Dispose;
        #endif
        }

        ~PerObjectRTSourceBoundsUpdate()
        {
            Dispose();
        }
        
        private void Dispose()
        {
            Application.onBeforeRender -= ProcessBoundsUpdate;
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= Dispose;
#endif
            DisposeNativeArrays();
            _dirtySourcesSet.Clear();
            _dirtySources.Clear();
            g_Inst = null;
        }
        
        /// <summary>
        /// 소스 객체를 업데이트 필요 목록에 등록
        /// </summary>
        public static void RegisterDirtySource(PerObjectRTSource source)
        {
            if (!source) return;
            var manager = Get();
            if (manager._dirtySourcesSet.Add(source)) manager._dirtySources.Add(source);
        }
        
        /// <summary>
        /// 등록된 모든 소스의 바운드 업데이트 처리
        /// </summary>
        private void ProcessBoundsUpdate()
        {
            if (_isJobScheduled)
            {
                ProcessJobResults();
            }
            else if (_dirtySources.Count > 0)
            {
                ScheduleBoundsCalculationJob();
            }
        }
        
        /// <summary>
        /// 모든 등록된 소스의 바운드 업데이트를 즉시 실행
        /// </summary>
        public void ForceUpdateAllBounds()
        {
            if (_isJobScheduled)
            {
                // 실행 중인 Job 완료
                _boundsJobHandle.Complete();
                ProcessJobResults();
            }
            
            if (_dirtySources.Count > 0)
            {
                ScheduleBoundsCalculationJob();
                _boundsJobHandle.Complete();
                ProcessJobResults();
            }
        }
        
        // 바운드 최소/최대 값 저장 구조체
        struct BoundsMinMax
        {
            public float3 Min;
            public float3 Max;
        }
        
        /// <summary>
        /// 모든 코너 포인트를 월드 공간에서 로컬 공간으로 변환하는 Job
        /// </summary>
        struct TransformCornerPointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> WorldCorners;
            [ReadOnly] public NativeArray<int> SourceStartIndices;
            [ReadOnly] public NativeArray<int> SourceCornerCounts;
            [ReadOnly] public NativeArray<float4x4> WorldToLocalMatrices;
            
            [WriteOnly] public NativeArray<BoundsMinMax> ResultBounds;
            
            public void Execute(int index)
            {
                int startIndex = SourceStartIndices[index];
                int cornerCount = SourceCornerCounts[index];
                float4x4 worldToLocalMatrix = WorldToLocalMatrices[index];
                
                if (cornerCount == 0)
                {
                    ResultBounds[index] = new BoundsMinMax
                    {
                        Min = new float3(0, 0, 0),
                        Max = new float3(0, 0, 0)
                    };
                    return;
                }
                
                // 첫 번째 포인트 변환
                float3 firstLocal = math.transform(worldToLocalMatrix, WorldCorners[startIndex]);
                float3 min = firstLocal;
                float3 max = firstLocal;
                
                // 나머지 포인트 변환 및 포함
                for (int i = 1; i < cornerCount; i++)
                {
                    float3 local = math.transform(worldToLocalMatrix, WorldCorners[startIndex + i]);
                    min = math.min(min, local);
                    max = math.max(max, local);
                }
                
                // 결과 저장
                ResultBounds[index] = new BoundsMinMax { Min = min, Max = max };
            }
        }
        
        /// <summary>
        /// Bounds 계산 Job을 예약
        /// </summary>
        private void ScheduleBoundsCalculationJob()
        {
            if (_isJobScheduled || _dirtySources.Count == 0) return;
            try
            {
                // 기존 Native 배열 정리
                DisposeNativeArrays();
                
                // 소스 개수
                int sourceCount = _dirtySources.Count;
                
                // 임시 리스트 초기화
                _cornerPoints.Clear();
                
                // 변환 행렬 배열 생성
                var worldToLocalMatrices = new NativeArray<float4x4>(sourceCount, Allocator.TempJob);
                _sourceStartIndices = new NativeArray<int>(sourceCount, Allocator.TempJob);
                _sourceCornerCounts = new NativeArray<int>(sourceCount, Allocator.TempJob);
                
                // 각 소스별 코너 포인트 수집
                for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
                {
                    PerObjectRTSource source = _dirtySources[sourceIndex];
                    if (source == null) continue;
                    
                    Transform sourceTransform = source.transform;
                    worldToLocalMatrices[sourceIndex] = sourceTransform.worldToLocalMatrix;
                    
                    // 시작 인덱스 기록
                    _sourceStartIndices[sourceIndex] = _cornerPoints.Count;
                    
                    // 렌더러 순회하며 코너 포인트 수집
                    int cornerCountForSource = 0;
                    foreach (var renderer in source.Renderers)
                    {
                        if (!renderer || !renderer.enabled || !renderer.gameObject.activeSelf) continue;
                        if (renderer is ParticleSystemRenderer) continue;
                        
                        Bounds worldBounds = renderer.bounds;
                        worldBounds.size = new Vector3(worldBounds.size.x * sourceTransform.localScale.x,
                            worldBounds.size.y * sourceTransform.localScale.y,
                            worldBounds.size.z * sourceTransform.localScale.z);
                        
                        // 8개 코너 포인트 추가
                        _cornerPoints.Add(new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z));
                        _cornerPoints.Add(new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z));
                        _cornerPoints.Add(new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z));
                        _cornerPoints.Add(new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z));
                        _cornerPoints.Add(new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z));
                        _cornerPoints.Add(new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z));
                        _cornerPoints.Add(new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z));
                        _cornerPoints.Add(new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z));
                        
                        cornerCountForSource += 8;
                    }
                    
                    // 이 소스에 대한 코너 포인트 개수 기록
                    _sourceCornerCounts[sourceIndex] = cornerCountForSource;
                }
                
                // 모든 코너 포인트를 Native 배열로 변환
                int totalCornerCount = _cornerPoints.Count;
                if (totalCornerCount == 0)
                {
                    // 처리할 포인트가 없으면 빠르게 반환
                    worldToLocalMatrices.Dispose();
                    _sourceStartIndices.Dispose();
                    _sourceCornerCounts.Dispose();
                    _dirtySourcesSet.Clear();
                    _dirtySources.Clear();
                    return;
                }
                
                _cornerPointsArray = new NativeArray<float3>(totalCornerCount, Allocator.TempJob);
                
                for (int i = 0; i < totalCornerCount; i++)
                {
                    _cornerPointsArray[i] = new float3(_cornerPoints[i].x, _cornerPoints[i].y, _cornerPoints[i].z);
                }
                
                // 결과 배열 생성
                _resultBounds = new NativeArray<BoundsMinMax>(sourceCount, Allocator.TempJob);
                
                // Job 생성 및 예약
                var job = new TransformCornerPointsJob
                {
                    WorldCorners = _cornerPointsArray,
                    SourceStartIndices = _sourceStartIndices,
                    SourceCornerCounts = _sourceCornerCounts,
                    WorldToLocalMatrices = worldToLocalMatrices,
                    ResultBounds = _resultBounds
                };
                
                int innerLoopBatchCount = Mathf.Max(8, sourceCount / SystemInfo.processorCount);
                _boundsJobHandle = job.Schedule(sourceCount, innerLoopBatchCount);
                _isJobScheduled = true;
                
                // 현재 프레임 끝에 예약된 작업 실행
                JobHandle.ScheduleBatchedJobs();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error scheduling bounds calculation job: {e.Message}");
                DisposeNativeArrays();
                _isJobScheduled = false;
            }
        }
        
        /// <summary>
        /// Job 결과 처리
        /// </summary>
        private void ProcessJobResults()
        {
            if (!_isJobScheduled) return;
            
            try
            {
                // Job이 완료됐는지 확인
                if (!_boundsJobHandle.IsCompleted) return;
                
                // Job 완료 대기
                _boundsJobHandle.Complete();
                _isJobScheduled = false;
                
                // 결과 적용
                for (int i = 0; i < _dirtySources.Count; i++)
                {
                    var source = _dirtySources[i];
                    if (source == null) continue;
                    
                    if (_sourceCornerCounts[i] > 0)
                    {
                        BoundsMinMax result = _resultBounds[i];
                        
                        // 바운드 생성
                        Vector3 center = new Vector3(
                            (result.Min.x + result.Max.x) * 0.5f,
                            (result.Min.y + result.Max.y) * 0.5f,
                            (result.Min.z + result.Max.z) * 0.5f
                        );
                        
                        Vector3 size = new Vector3(
                            result.Max.x - result.Min.x,
                            result.Max.y - result.Min.y,
                            result.Max.z - result.Min.z
                        );
                        
                        // 최소 크기 적용
                        ApplyMinimumSize(ref size);
                        
                        // 소스에 바운드 설정
                    //    source.Bounds = new BoundsData(center, size);
                    }
                }
                
                // 처리 완료된 소스 목록 초기화
                _dirtySourcesSet.Clear();
                _dirtySources.Clear();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing bounds calculation results: {e.Message}");
            }
            finally
            {
                DisposeNativeArrays();
            }
        }
        
        /// <summary>
        /// 바운드 크기가 최소 크기보다 작으면 확장
        /// </summary>
        private void ApplyMinimumSize(ref Vector3 size)
        {
            if (size.x < _minBoundsSize.x) size.x = _minBoundsSize.x;
            if (size.y < _minBoundsSize.y) size.y = _minBoundsSize.y;
            if (size.z < _minBoundsSize.z) size.z = _minBoundsSize.z;
        }
        
        /// <summary>
        /// Native 배열 해제
        /// </summary>
        private void DisposeNativeArrays()
        {
            if (_cornerPointsArray.IsCreated) _cornerPointsArray.Dispose();
            if (_sourceStartIndices.IsCreated) _sourceStartIndices.Dispose();
            if (_sourceCornerCounts.IsCreated) _sourceCornerCounts.Dispose();
            if (_resultBounds.IsCreated) _resultBounds.Dispose();
        }
    }
}