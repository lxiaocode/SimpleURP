using SimpleURP.RenderPass;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SimpleURP
{
    public class SimpleRenderer : ScriptableRenderer
    {
        // RenderPass
        private SimpleMainLightShadowCasterPass m_MainLightShadowCasterPass;    // 主光源阴影
        private SimpleDrawObjectsPass m_RenderOpaqueForwardPass;                // 不透明物体
        private SimpleDrawObjectsPass m_RenderTransparentForwardPass;           // 透明物体
        private SimpleDrawSkyboxPass m_DrawSkyboxPass;                          // 天空盒

        private ColorGradingLutPass m_colorGradingLutPass;                      // 渲染Lut
        private SimplePostProcessPass m_PostProcessPass;                        // 后处理

        
        
        // 光源设置
        private ForwardLights m_ForwardLights;

        

        // 中间RT
        private RenderTargetHandle m_cameraTarget;
        private RenderTargetHandle m_InternalLut;

        
        
        public SimpleRenderer(SimpleRendererData data) : base(data)
        {
            // 中间渲染纹理
            m_cameraTarget.Init("_SimpleCameraTexture");
            m_InternalLut.Init("_InternalGradingLut");

            // 光源相关的 Shader 变量/关键字
            m_ForwardLights = new ForwardLights();
            
            // RenderPass
            m_MainLightShadowCasterPass = new SimpleMainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_RenderOpaqueForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingOpaques, "RenderOpaque", true, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_RenderTransparentForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingTransparents, "RenderTransparent", false, RenderQueueRange.transparent, data.transparentLayerMask);
            m_DrawSkyboxPass = new SimpleDrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);

            m_colorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data.postProcessData);
            
            m_PostProcessPass = new SimplePostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 设置中间渲染纹理
            bool intermediateRenderTexture = renderingData.cameraData.postProcessEnabled;
            if (intermediateRenderTexture)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(m_cameraTarget.id, desc, FilterMode.Bilinear);
                context.ExecuteCommandBuffer(cmd);

                ConfigureCameraTarget(m_cameraTarget.id, m_cameraTarget.id);
            }
            
            
            // RenderPass
            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
                EnqueuePass(m_MainLightShadowCasterPass);
            
            m_colorGradingLutPass.Setup(m_InternalLut);
            EnqueuePass(m_colorGradingLutPass);
            
            EnqueuePass(m_RenderOpaqueForwardPass);
            EnqueuePass(m_DrawSkyboxPass);
            EnqueuePass(m_RenderTransparentForwardPass);
            
            m_PostProcessPass.Setup(in m_cameraTarget, in m_InternalLut);
            EnqueuePass(m_PostProcessPass);
        }

        
        /// <summary>
        /// 设置光照参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 设置光源相关的着色器变量、关键字、Buffer
            m_ForwardLights.Setup(context, ref renderingData);
        }

        
        /// <summary>
        /// 设置裁剪参数
        /// </summary>
        /// <param name="cullingParameters"></param>
        /// <param name="cameraData"></param>
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;
            cullingParameters.conservativeEnclosingSphere = UniversalRenderPipeline.asset.conservativeEnclosingSphere;
            cullingParameters.numIterationsEnclosingSphere = UniversalRenderPipeline.asset.numIterationsEnclosingSphere;
        }
    }
}
