using SimpleURP.RenderPass;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP
{
    public class SimpleRenderer : ScriptableRenderer
    {
        public SimpleRenderer(ScriptableRendererData data) : base(data)
        {
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            EnqueuePass(new SimpleDrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox));
        }
    }
}
