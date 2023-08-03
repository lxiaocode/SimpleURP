using SimpleURP.RenderPass;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SimpleURP
{
    public class SimpleRenderer : ScriptableRenderer
    {
        // 阴影
        private SimpleMainLightShadowCasterPass m_MainLightShadowCasterPass;
        // 不透明物 + 透明物
        SimpleDrawObjectsPass m_RenderOpaqueForwardPass;
        SimpleDrawObjectsPass m_RenderTransparentForwardPass;
        // 天空盒
        SimpleDrawSkyboxPass m_DrawSkyboxPass;
        // 光源设置
        ForwardLights m_ForwardLights;
        
        public SimpleRenderer(SimpleRendererData data) : base(data)
        {
            m_ForwardLights = new ForwardLights();
            
            m_MainLightShadowCasterPass = new SimpleMainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_RenderOpaqueForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingOpaques, true, RenderQueueRange.opaque, data.opaqueLayerMask);
        
            m_RenderTransparentForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingTransparents, false, RenderQueueRange.transparent, data.transparentLayerMask);
            m_DrawSkyboxPass = new SimpleDrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
                EnqueuePass(m_MainLightShadowCasterPass);
            EnqueuePass(m_RenderOpaqueForwardPass);
            EnqueuePass(m_DrawSkyboxPass);
            EnqueuePass(m_RenderTransparentForwardPass);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 设置光源相关的着色器变量、关键字、Buffer
            m_ForwardLights.Setup(context, ref renderingData);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;
            cullingParameters.conservativeEnclosingSphere = UniversalRenderPipeline.asset.conservativeEnclosingSphere;
            cullingParameters.numIterationsEnclosingSphere = UniversalRenderPipeline.asset.numIterationsEnclosingSphere;
        }
    }
}
