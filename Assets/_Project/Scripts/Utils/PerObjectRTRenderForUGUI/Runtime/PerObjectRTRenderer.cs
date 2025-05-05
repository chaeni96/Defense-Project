using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/PerObjectRTRenderForUGUI/PerObjectRT Renderer", 12)]
    public class PerObjectRTRenderer : MaskableGraphic, IElementPool<PerObjectRTRenderer>, IElementPoolInternal
    {
        [HideInInspector][SerializeField] private Texture m_Texture;
        [HideInInspector][SerializeField] private Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);
        [HideInInspector][SerializeField] private int _poolIndex = -1;
        [SerializeField] private PerObjectRTSource _source;
        
        public int poolIndex => _poolIndex;
        void IElementPoolInternal.SetPoolIndex(int index)
        {
            if (poolIndex == index) return;
            _poolIndex = index;
            SetVerticesDirty();
            SetMaterialDirty();
        }
        
        public Texture texture
        {
            get => m_Texture;
            set
            {
                if (m_Texture == value) return;
                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        
        public override Texture mainTexture
        {
            get
            {
                if (m_Texture == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }
                return m_Texture;
            }
        }
        
        internal Rect uvRect
        {
            get { return m_UVRect; } 
            set
            {
                if (m_UVRect == value) return;
                m_UVRect = value;
                SetVerticesDirty();
            }
        }

        internal PerObjectRTSource source
        {
            get => _source; 
            set
            {
                if (_source == value) return;
                _source = value;
                RegisterRenderElementRebuild();
            }
        }
        
        protected PerObjectRTRenderer()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            IElementPool<PerObjectRTRenderer>.Register(this);
            RegisterRenderElementRebuild();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            IElementPool<PerObjectRTRenderer>.UnRegister(this);
            RegisterRenderElementRebuild();
        }

        private void RegisterRenderElementRebuild()
        {
            PerObjectRTRenderpipeline.RegisterRenderElementRebuild();
        }

        public void CalculateAutoRect()
        {
            if (!source) return;
            float pixelsPerUnit = canvas.referencePixelsPerUnit;
            Vector3 boundsSize = source.Bounds.size;
            float w = boundsSize.x * pixelsPerUnit;
            float h = boundsSize.y * pixelsPerUnit;
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = new Vector2(w, h);
            SetAllDirty();
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            RegisterRenderElementRebuild();
        }
    #endif
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Texture tex = mainTexture;
            vh.Clear();
            if (tex != null)
            {
                var r = GetPixelAdjustedRect();
                var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
                var scaleX = tex.width * tex.texelSize.x;
                var scaleY = tex.height * tex.texelSize.y;
                {
                    var color32 = color;
                    vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMin * scaleY));
                    vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(m_UVRect.xMin * scaleX, m_UVRect.yMax * scaleY));
                    vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMax * scaleY));
                    vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(m_UVRect.xMax * scaleX, m_UVRect.yMin * scaleY));

                    vh.AddTriangle(0, 1, 2);
                    vh.AddTriangle(2, 3, 0);
                }
            }
        }
    }
}