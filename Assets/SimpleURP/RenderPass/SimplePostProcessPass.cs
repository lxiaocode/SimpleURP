using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimplePostProcessPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_Source;
        private RenderTargetHandle m_Destination;
        
        
        public SimplePostProcessPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle source)
        {
            // 源纹理
            m_Source = source.id;
            m_Destination = RenderTargetHandle.CameraTarget;
        }
        
        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.Blit(m_Source, m_Destination.id);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
        }

        public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        {
            base.OnFinishCameraStackRendering(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
        }
    }
}
