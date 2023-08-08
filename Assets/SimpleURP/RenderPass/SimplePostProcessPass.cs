using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimplePostProcessPass : ScriptableRenderPass
    {
        // 相机中间RT
        private RenderTargetIdentifier m_Source;
        // lut查找纹理
        private RenderTargetHandle m_InternalLut;
        
        // 后处理材质库
        private MaterialLibrary m_Materials;
        
        // 后处理材质使用的中间RT
        private int sourceTex = Shader.PropertyToID("_SourceTex");
        
        public SimplePostProcessPass(RenderPassEvent evt, PostProcessData data)
        {
            renderPassEvent = evt;
            
            // 加载后处理材质
            m_Materials = new MaterialLibrary(data);
        }

        public void Setup(in RenderTargetHandle source,  in RenderTargetHandle internalLut)
        {
            m_Source = source.id;
            m_InternalLut = internalLut;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("PostProcess")))
            {
                Render(cmd, ref renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            
            // 设置lut参数
            SetupColorGrading(cmd, ref renderingData, m_Materials.uber);
            
            // 设置相机中间RT
            cmd.SetGlobalTexture(sourceTex, m_Source);
            
            // 获取相机渲染目标，帧缓冲 或 RT
            RenderTargetHandle cameraTargetHandle = RenderTargetHandle.CameraTarget;
            RenderTargetIdentifier cameraTarget = cameraData.targetTexture != null
                ? new RenderTargetIdentifier(cameraData.targetTexture)
                : cameraTargetHandle.Identifier();

            // 设置相机渲染目标
            cmd.SetRenderTarget(cameraTarget,
                RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare,RenderBufferStoreAction.DontCare);
            // 绘制后处理
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Materials.uber);
        }

        
        #region Color Grading

        /// <summary>
        /// 设置Lut相关参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderingData"></param>
        /// <param name="material"></param>
        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material material)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;
            
            // 设置材质参数
            float postExposureLinear = 1f; // TODO 曝光量
            // lut纹理
            cmd.SetGlobalTexture(ShaderConstants._InternalLut, m_InternalLut.Identifier());
            // lut参数，scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)，w 曝光量
            material.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            if (hdr)
            {
                material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
            }
        }

        #endregion
        
        
        
        /// <summary>
        /// 后处理材质
        /// </summary>
        class MaterialLibrary
        {
            public readonly Material stopNaN;
            public readonly Material subpixelMorphologicalAntialiasing;
            public readonly Material gaussianDepthOfField;
            public readonly Material bokehDepthOfField;
            public readonly Material cameraMotionBlur;
            public readonly Material paniniProjection;
            public readonly Material bloom;
            public readonly Material scalingSetup;
            public readonly Material easu;
            public readonly Material uber;
            public readonly Material finalPass;
            public readonly Material lensFlareDataDriven;

            public MaterialLibrary(PostProcessData data)
            {
                // NOTE NOTE NOTE NOTE NOTE NOTE
                // If you create something here you must also destroy it in Cleanup()
                // or it will leak during enter/leave play mode cycles
                // NOTE NOTE NOTE NOTE NOTE NOTE
                stopNaN = Load(data.shaders.stopNanPS);
                subpixelMorphologicalAntialiasing = Load(data.shaders.subpixelMorphologicalAntialiasingPS);
                gaussianDepthOfField = Load(data.shaders.gaussianDepthOfFieldPS);
                bokehDepthOfField = Load(data.shaders.bokehDepthOfFieldPS);
                cameraMotionBlur = Load(data.shaders.cameraMotionBlurPS);
                paniniProjection = Load(data.shaders.paniniProjectionPS);
                bloom = Load(data.shaders.bloomPS);
                scalingSetup = Load(data.shaders.scalingSetupPS);
                easu = Load(data.shaders.easuPS);
                uber = Load(data.shaders.uberPostPS);
                finalPass = Load(data.shaders.finalPostPassPS);
                lensFlareDataDriven = Load(data.shaders.LensFlareDataDrivenPS);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(stopNaN);
                CoreUtils.Destroy(subpixelMorphologicalAntialiasing);
                CoreUtils.Destroy(gaussianDepthOfField);
                CoreUtils.Destroy(bokehDepthOfField);
                CoreUtils.Destroy(cameraMotionBlur);
                CoreUtils.Destroy(paniniProjection);
                CoreUtils.Destroy(bloom);
                CoreUtils.Destroy(scalingSetup);
                CoreUtils.Destroy(easu);
                CoreUtils.Destroy(uber);
                CoreUtils.Destroy(finalPass);
                CoreUtils.Destroy(lensFlareDataDriven);
            }
        }
        
        
        
        static class ShaderConstants
        {
            public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
            public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int _PongTexture = Shader.PropertyToID("_PongTexture");
            public static readonly int _PingTexture = Shader.PropertyToID("_PingTexture");

            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");

            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_RGBM = Shader.PropertyToID("_Bloom_RGBM");
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int _FlareOcclusionTex = Shader.PropertyToID("_FlareOcclusionTex");
            public static readonly int _FlareOcclusionIndex = Shader.PropertyToID("_FlareOcclusionIndex");
            public static readonly int _FlareTex = Shader.PropertyToID("_FlareTex");
            public static readonly int _FlareColorValue = Shader.PropertyToID("_FlareColorValue");
            public static readonly int _FlareData0 = Shader.PropertyToID("_FlareData0");
            public static readonly int _FlareData1 = Shader.PropertyToID("_FlareData1");
            public static readonly int _FlareData2 = Shader.PropertyToID("_FlareData2");
            public static readonly int _FlareData3 = Shader.PropertyToID("_FlareData3");
            public static readonly int _FlareData4 = Shader.PropertyToID("_FlareData4");
            public static readonly int _FlareData5 = Shader.PropertyToID("_FlareData5");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

            public static readonly int _ScalingSetupTexture = Shader.PropertyToID("_ScalingSetupTexture");
            public static readonly int _UpscaledTexture = Shader.PropertyToID("_UpscaledTexture");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }

    }
}
