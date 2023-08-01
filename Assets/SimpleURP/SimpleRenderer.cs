using SimpleURP.RenderPass;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP
{
    public class SimpleRenderer : ScriptableRenderer
    {
        SimpleDrawObjectsPass m_RenderOpaqueForwardPass;
        SimpleDrawObjectsPass m_RenderTransparentForwardPass;
        SimpleDrawSkyboxPass m_DrawSkyboxPass;
        
        public SimpleRenderer(SimpleRendererData data) : base(data)
        {
            m_RenderOpaqueForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingOpaques, true, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_RenderTransparentForwardPass = new SimpleDrawObjectsPass(RenderPassEvent.BeforeRenderingTransparents, false, RenderQueueRange.transparent, data.transparentLayerMask);
            
            m_DrawSkyboxPass = new SimpleDrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            EnqueuePass(m_RenderOpaqueForwardPass);
            EnqueuePass(m_DrawSkyboxPass);
            EnqueuePass(m_RenderTransparentForwardPass);
        }
    }
}
