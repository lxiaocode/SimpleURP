using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimpleDrawSkyboxPass : ScriptableRenderPass
    {
        public SimpleDrawSkyboxPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            context.DrawSkybox(camera);
        }
    }
}
