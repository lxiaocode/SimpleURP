using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimpleMainLightShadowCasterPass : ScriptableRenderPass
    {
        int renderTargetWidth;
        int renderTargetHeight;
        
        int m_MainLightShadowmapId;
        RenderTexture m_MainLightShadowmapTexture;
        
        ShadowSliceData[] m_CascadeSlices = new ShadowSliceData[1];

        public SimpleMainLightShadowCasterPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            m_MainLightShadowmapId = Shader.PropertyToID("_MainLightShadowmapTexture");
        }

        public bool Setup(ref RenderingData renderingData)
        {
            // 是否支持主光源阴影
            if (!renderingData.shadowData.supportsMainLightShadows)
                return false;

            // 主光源是否存在
            int shadowMainLightIndex = renderingData.lightData.mainLightIndex;
            if (shadowMainLightIndex == -1)
                return false;

            // 主光源是否投射阴影
            VisibleLight shadowMainLight = renderingData.lightData.visibleLights[shadowMainLightIndex];
            Light light = shadowMainLight.light;
            if (light.shadows == LightShadows.None)
                return false;

            // 光源中是否存在接收阴影的物体
            Bounds bounds;
            if (!renderingData.cullResults.GetShadowCasterBounds(shadowMainLightIndex, out bounds))
                return false;

            int resolution = Mathf.Min(renderingData.shadowData.mainLightShadowmapWidth, renderingData.shadowData.mainLightShadowmapHeight);
            renderTargetWidth = renderingData.shadowData.mainLightShadowmapWidth;
            renderTargetHeight = renderingData.shadowData.mainLightShadowmapHeight;
            
            bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                shadowMainLightIndex, 0, renderTargetWidth, renderTargetHeight, resolution, light.shadowNearPlane,
                out  Vector4 v, out m_CascadeSlices[0]);

            m_MainLightShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(renderTargetWidth, renderTargetHeight, 16);
            
            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(new RenderTargetIdentifier(m_MainLightShadowmapTexture));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int shadowLightIndex = renderingData.lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;
            VisibleLight shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
            
            
            CommandBuffer cmd = CommandBufferPool.Get();
            var settings = new ShadowDrawingSettings(renderingData.cullResults, shadowLightIndex);

            settings.splitData = m_CascadeSlices[0].splitData;
            // 这里对于shader中的GetShadowPositionHClip
            // ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, Vector4.zero);
            Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, ref renderingData.shadowData, m_CascadeSlices[0].projectionMatrix, m_CascadeSlices[0].resolution);
            ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
            // 生成阴影图时用于区分定向光和点状光的，它们用不同公式计算偏移
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);
            cmd.SetViewProjectionMatrices(m_CascadeSlices[0].viewMatrix, m_CascadeSlices[0].projectionMatrix);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawShadows(ref settings);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, false);
            
            cmd.SetGlobalTexture(m_MainLightShadowmapId, m_MainLightShadowmapTexture);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (m_MainLightShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_MainLightShadowmapTexture);
                m_MainLightShadowmapTexture = null;
            }
        }
    }
}
