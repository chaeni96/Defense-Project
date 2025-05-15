using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public enum RTSortingMode
    {
        ZPosition,
        OrderInLayer,
        None
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class PerObjectRTSource : MonoBehaviour, IElementPool<PerObjectRTSource>, IElementPoolInternal
    {
        private static readonly List<Material> MaterialRefs = new List<Material>();
        
        [HideInInspector][SerializeField] private int _poolIndex = -1;
        [SerializeField] private readonly List<Renderer> _renderers = new List<Renderer>();
        [SerializeField] private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one);
        [HideInInspector][SerializeField] private bool _init = false;
        [SerializeField] private bool _autoBoundsMode = false;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;

        [Header("Sorting")]
        [SerializeField] private RTSortingMode _sortingMode = RTSortingMode.ZPosition;
        [HideInInspector] [SerializeField] private bool _needSort = true;

        private static readonly Comparison<Renderer> s_ByZ = CompareByZ;
        private static readonly Comparison<Renderer> s_ByOrder = CompareByOrder;
        private static int CompareByZ(Renderer a, Renderer b) =>
            a.transform.position.z.CompareTo(b.transform.position.z);
        private static int CompareByOrder(Renderer a, Renderer b) =>
            a.sortingOrder.CompareTo(b.sortingOrder);

        public RTSortingMode SortingMode
        {
            get => _sortingMode;
            set
            {
                if (_sortingMode == value) return;
                _sortingMode = value;
                _needSort = true;
                RegisterRenderElementRebuild();
            }
        }


        public int poolIndex => _poolIndex;
        void IElementPoolInternal.SetPoolIndex(int index) { _poolIndex = index; }
        public List<Renderer> Renderers => _renderers;
        public Bounds Bounds
        {
            get => _bounds;
            set
            {
                if (_bounds == value) return;
                _bounds = value;
                RegisterBoundsUpdate();
            }
        }
        internal Bounds InternalBounds { set => _bounds = value; }
        public Matrix4x4 ViewMatrix { get; internal set; }
        public Matrix4x4 ProjMatrix { get; internal set; }
        public Rect Viewport { get; internal set; }
        public bool AutoBoundsMode => _autoBoundsMode;
        
        private void OnEnable()
        {
            if (HasSourceComponentParents(transform))
            {
                this.enabled = false;
                _init = false;
                return;
            }
            _init = true;
            CollectRenderers();
            IElementPool<PerObjectRTSource>.Register(this);
            RegisterRenderElementRebuild();

            _needSort = true;
            RegisterBoundsUpdate();
        }

      
        private void OnDisable()
        {
            if (!_init) return;
            Dispose();
            IElementPool<PerObjectRTSource>.UnRegister(this);
            RegisterRenderElementRebuild();
            _init = false;
        }

        private void Dispose()
        {
            _renderers.Clear();
            MaterialRefs.Clear();
            
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_init) return;
            _needSort = true;
            RegisterBoundsUpdate();
            RegisterRenderElementRebuild();
        }
#endif
        
        private void Update()
        {
            if (!_init) return;
            
            if (HasTransformChanged())
            {
                CacheTransformData();
                RegisterBoundsUpdate();
            }
        }

        private void OnTransformChildrenChanged()
        {
            if (!_init) return;
            CollectRenderers();
            RegisterRenderElementRebuild();
            if(_init) RegisterBoundsUpdate();
        }

        private void OnTransformParentChanged()
        {
            if (HasSourceComponentParents(transform))
            {
                this.enabled = false;
                return;
            }
            this.enabled = true;
            if(_init) RegisterBoundsUpdate();
        }
        
        private void CollectRenderers()
        {
            Transform rootTransform = transform;
            Renderer[] allRenderers = rootTransform.GetComponentsInChildren<Renderer>(true);
            _renderers.Clear();
            if (allRenderers == null) return;
            foreach (var r in allRenderers)
            {
                _renderers.Add(r);
            }
            _needSort = true;
            RegisterBoundsUpdate();
        }

        internal void RenderExecute(CommandBuffer cmd)
        {
            if (_needSort)
            {
                switch (_sortingMode)
                {
                    case RTSortingMode.ZPosition:
                        _renderers.Sort(s_ByZ);
                        break;
                    case RTSortingMode.OrderInLayer:
                        _renderers.Sort(s_ByOrder);
                        break;
                    default : break;
                }
                _needSort = false;
            }

            cmd.SetViewport(Viewport);
            cmd.SetViewProjectionMatrices(ViewMatrix, ProjMatrix);
                     
            foreach (var r in Renderers)
            {
                if(!r.enabled || !r.gameObject.activeInHierarchy) continue;
                r.GetSharedMaterials(MaterialRefs);
                for (var k = 0; k < MaterialRefs.Count; k++)
                {
                    Material material = MaterialRefs[k];
                    if(!material) continue;
                    //int shaderPassIndex = 0;   // Shader?ì„œ ì²«ë²ˆì§?? ì–¸ Passë§??Œë”ë§??¸ì¶œ (Pass ì½”ë“œê°€ ë§??„ì— ? ì–¸?˜ì•¼??
                    cmd.DrawRenderer(r, material, k, 0);
                }
            }
            MaterialRefs.Clear();
        }
        
        private void RegisterRenderElementRebuild()
        {
            PerObjectRTRenderpipeline.RegisterRenderElementRebuild();
        }

        private void RegisterBoundsUpdate()
        {
            PerObjectRTSourceBoundsManager.RegisterBoundsUpdate(this);
        }
        
        public void CalculateAutoBounds()
        {
            if (AutoBoundsMode) return;
            Profiler.BeginSample(ProfilerStrings.CalculateLocalBounds);
                BoundsUtils.CalculateLocalBounds(transform, Renderers, out _bounds);
            Profiler.EndSample();
            RegisterBoundsUpdate();
        }
        
        private void CacheTransformData()
        {
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastScale = transform.localScale;
            _needSort = true;
        }
        
        private bool HasTransformChanged()
        {
            return _lastPosition != transform.position ||
                   _lastRotation != transform.rotation ||
                   _lastScale != transform.localScale;
        }

        internal static bool CheckRenderActiveState(PerObjectRTSource source)
        {
            if (!source ||
                !source.enabled ||
                !source.gameObject.activeSelf ||
                source.Renderers.Count < 1) return false;
            return true;
        }
        
        private static bool HasSourceComponentParents(Transform transform)
        {
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.TryGetComponent<PerObjectRTSource>(out _)) return true;
                current = current.parent;
            }
            return false;
        }
        
#if UNITY_EDITOR
        private const string k_GizmoPath = "Packages/com.catdarkgame.perobjectrtrenderforugui/Editor/Gizmo/";
        private const string k_ProjectionIcon = k_GizmoPath + "RTSource.png";
        public void OnDrawGizmos()
        {
            if (!_init) return;
            if (!UnityEditor.Selection.Contains(gameObject)) return;
            
            string gizmoName = k_ProjectionIcon;
            Bounds boundsWS = BoundsUtils.ConvertLocalToWorldBounds(transform, _bounds);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsWS.center, boundsWS.size);
            Gizmos.DrawIcon(boundsWS.center - Vector3.forward * boundsWS.extents.z, gizmoName, true);
        }
#endif
    }
}