using System;
using System.Collections;
using Assets.MPipeline.Custom_Data.TextureCurve;
using MPipeline.SRP_Assets.Passes;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MPipeline.SRP_Assets.Passes {

    public class LineDrawingControlPanel : MonoBehaviour, ILineDrawingData {

        // Interactive panel 
        public (int value, int propid) RenderPass =>
        (
            ContourRenderingPass.RenderBrushPathPass,
            Shader.PropertyToID("_RenderMode")
        );
        
        public bool RenderVectorizedCurves = false;
        public bool TemporalCoherentMode = false;
                      
        [Range(0.1f, 25f)] public float LineScale = 1;
        [Range(1, 12)] public float LineWidth = 6;

        // Stamping is not supported in this version
        [NonSerialized][Range(1, 12)] public float StampLengthSetting = 6;

        // 32 is hard-coded at shader side, for details, see MAX_STAMP_QUAD_SCALE in "CustomShaderInputs.hlsl"        
        public Vector2 minMaxWidth = new Vector2(3, 16);
     
        public MyTextureCurve DepthCurve;     
        public Vector2 minMaxDepth = new Vector2(0f, 1f);

        [NonSerialized] public MyTextureCurve CurvatureCurve;
        [NonSerialized] public Vector2 minMaxCurv = new Vector2(0f, 0.1f);
       
        public MyTextureCurve CurveShape;

        [NonSerialized] public MyTextureCurve DensityCurve;

        [Range(0f, 1f)] public float OrientThreshold = 0.1f;

        /// <summary>
        /// Control line width using curvature value.
        /// <list type="bullet">
        /// <item>.xy: Range of curvature that have effect on line width</item>
        /// <item>.zw: Reserved</item>
        /// </list>
        /// </summary>
        public Vector4 CurvatureParameters => new Vector4(minMaxCurv.x, minMaxCurv.y, 0, 0);

        /// <summary>
        /// Control line width using depth value.
        /// <list type="bullet">
        /// <item>.xy: Range of depth that have effect on line width</item>
        /// <item>.zw: Reserved</item>
        /// </list>
        /// </summary>
        public Vector4 DepthParameters => new Vector4(minMaxDepth.x, minMaxDepth.y, 0, 0);

        public enum VectorizedPathStyle
        {
            Segmentation, 
            UV,
            Textured
        };

        public VectorizedPathStyle Style;
       
        public Texture2D BrushTexture;
        
        [Range(1u, 4u)] public int BrushCount = 1;
        
        [Range(0f, 1f)] public float DebugParams1;

        public (int shaderPropId, int value) PathStyle => (
            Shader.PropertyToID("_PathStyle"),
            (int)Style
        );

        [NonSerialized] public int BrushTexID;
        [NonSerialized] public int BrushCountID;

        [Range(12, 22)] public int ListRankingJumps = 16;

        [NonSerialized] public int CurvatureSmoothingIterations = 1;
        [NonSerialized] public int CurvatureDerivativeSmoothingIterations = 3;

        [NonSerialized][Range(0f, 1f)] public float DebugParams0;
        // [NonSerialized][Range(0f, 1f)] public float DebugParams1;
        [NonSerialized][Range(0f, 1f)] public float DebugParams2;
        [NonSerialized][Range(0f, 1f)] public float DebugParams3;
 
        // Control which RT to display on screen
        public int debugOutput = -1;

        /*
        private static IEnumerable _lineDrawingTextureToPresent = new ValueDropdownList<int>()
        {
            {"Camera Target", -1},
            {"Debug Texture #0", LineDrawingTextures.DebugTexture},
            {"Debug Texture #1", LineDrawingTextures.DebugTexture1},
            {"Contour GBuffer", LineDrawingTextures.ContourGBufferTexture}
        };
        */

        public LineDrawingControlPanel(float pdBdTs) { }

        public (float scale, int id) StrokeWidth =>
        (
            LineWidth * LineScale,
            Shader.PropertyToID("_LineWidth")
        );

        public (float scale, int id) StrokeLength =>
        (
            StampLengthSetting * LineScale,
            Shader.PropertyToID("_StampLength")
        );

        public (float4 scaleMinMax, int id) StrokeScaleRange =>
        (
            new float4(
                // Min scale.xy
                minMaxWidth.x / (LineWidth * LineScale),
                minMaxWidth.x / (LineWidth * LineScale),
                // Max scale.xy
                minMaxWidth.y / (StampLengthSetting * LineScale),
                minMaxWidth.y / (StampLengthSetting * LineScale)
            ),
            Shader.PropertyToID("_LineWidthMinMax")
        );

        public float4 DebugParams() {
            return new float4(DebugParams0, DebugParams1, DebugParams2, DebugParams3);
        }

        public void OnEnable() {
            Init(Camera.main);
        }

        private void InitControlCurves() {
            MyTextureCurve.SetupTextureCurve("CurveTextures/CurveTexture", "_CurveTex_0", ref CurvatureCurve);
            MyTextureCurve.SetupTextureCurve("CurveTextures/ParamCurve", "_CurveTex_1", ref CurveShape);
            MyTextureCurve.SetupTextureCurve("CurveTextures/DensityCurve", "_CurveTex_2", ref DensityCurve);
            MyTextureCurve.SetupTextureCurve("CurveTextures/DepthCurve", "_CurveTex_3", ref DepthCurve);
        }

        private void InitBrushTextures() {
            BrushTexture ??= UnityEngine.Resources.Load<Texture2D>("BrushPatterns/BrushTexture");
            BrushTexID = Shader.PropertyToID("_BrushTex_Main");
            BrushCountID = Shader.PropertyToID("_BrushTexCount");
            UnityEngine.Resources.Load<Texture2D>("BrushPatterns/Noise/WhiteNoise");
            Shader.PropertyToID("_BrushTex_ColorJitter");
        }

        public void InitPaperTextures() {
            UnityEngine.Resources.Load<Texture2D>("BrushPatterns/PaperHeightField");
            Shader.PropertyToID("_PaperHeightMap");
        }

        public void Init(Camera cam) {
            InitControlCurves();
            InitBrushTextures();
            InitPaperTextures();
        }

        public void Update() {
            /*
            DepthCurve.UpdateData();
            CurvatureCurve.UpdateData();
            ParameterCurve.UpdateData();
            DensityCurve.UpdateData();
            */
        }

    }

}