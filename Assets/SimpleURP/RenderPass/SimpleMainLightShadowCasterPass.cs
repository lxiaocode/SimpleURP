using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimpleMainLightShadowCasterPass : ScriptableRenderPass
    {
        private static class MainLightShadowConstantBuffer
        {
            public static int _WorldToShadow;
            public static int _ShadowParams;
            public static int _CascadeShadowSplitSpheres0;
            public static int _CascadeShadowSplitSpheres1;
            public static int _CascadeShadowSplitSpheres2;
            public static int _CascadeShadowSplitSpheres3;
            public static int _CascadeShadowSplitSphereRadii;
        }
        
        int renderTargetWidth;
        int renderTargetHeight;
        
        int m_MainLightShadowmapId;
        RenderTexture m_MainLightShadowmapTexture;
        
        // ==============================================================================
        // 级联阴影相关
        const int k_MaxCascades = 4;                // 最大级联数量
        int m_ShadowCasterCascadesCount;            // 级联数量
        ShadowSliceData[] m_CascadeSlices;          // 各个级联的 ShadowSliceData
        Vector4[] m_CascadeSplitDistances;
        Matrix4x4[] m_MainLightShadowMatrices;

        public SimpleMainLightShadowCasterPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;

            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];
            m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            
            MainLightShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            MainLightShadowConstantBuffer._ShadowParams = Shader.PropertyToID("_MainLightShadowParams");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            MainLightShadowConstantBuffer._CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            
            // 全局的阴影纹理
            m_MainLightShadowmapId = Shader.PropertyToID("_MainLightShadowmapTexture");
        }

        public bool Setup(ref RenderingData renderingData)
        {
            // 是否支持主光源阴影
            if (!renderingData.shadowData.supportsMainLightShadows)
                return false;
            Clear();
            
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

            m_ShadowCasterCascadesCount = renderingData.shadowData.mainLightShadowCascadesCount;
            // 级联阴影最大分辨率
            int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(
                renderingData.shadowData.mainLightShadowmapWidth,
                renderingData.shadowData.mainLightShadowmapHeight, 
                m_ShadowCasterCascadesCount);
            renderTargetWidth = renderingData.shadowData.mainLightShadowmapWidth;
            renderTargetHeight = (m_ShadowCasterCascadesCount == 2) ?
                renderingData.shadowData.mainLightShadowmapHeight >> 1 :
                renderingData.shadowData.mainLightShadowmapHeight;
            
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                // ?
                bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                    shadowMainLightIndex, cascadeIndex, renderTargetWidth, renderTargetHeight, shadowResolution, light.shadowNearPlane,
                    out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex]);

                if (!success)
                    return false;
            }
            m_MainLightShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(renderTargetWidth, renderTargetHeight, 16);
            
            return true;
        }

        private bool CascadesSetup(ref RenderingData renderingData, int shadowLightIndex, Light light)
        {
            m_ShadowCasterCascadesCount = renderingData.shadowData.mainLightShadowCascadesCount;
            // 级联阴影最大分辨率
            int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(
                renderingData.shadowData.mainLightShadowmapWidth,
                renderingData.shadowData.mainLightShadowmapHeight, 
                m_ShadowCasterCascadesCount);
            renderTargetWidth = renderingData.shadowData.mainLightShadowmapWidth;
            renderTargetHeight = (m_ShadowCasterCascadesCount == 2) ?
                renderingData.shadowData.mainLightShadowmapHeight >> 1 :
                renderingData.shadowData.mainLightShadowmapHeight;
            
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                // ?
                bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                    shadowLightIndex, cascadeIndex, renderTargetWidth, renderTargetHeight, shadowResolution, light.shadowNearPlane,
                    out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex]);

                if (!success)
                    return false;
            }

            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(new RenderTargetIdentifier(m_MainLightShadowmapTexture));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderMainLightCascadeShadowmap(context, ref renderingData);
        }

        /// <summary>
        /// 渲染级联阴影
        /// </summary>
        void RenderMainLightCascadeShadowmap(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 光源信息
            int shadowLightIndex = renderingData.lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;
            VisibleLight shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
            
            CommandBuffer cmd = CommandBufferPool.Get();
            // 渲染级联阴影
            var settings = new ShadowDrawingSettings(renderingData.cullResults, shadowLightIndex);
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                // 设置阴影裁剪数据
                settings.splitData = m_CascadeSlices[cascadeIndex].splitData;
                
                // 这里对应 shader 中的 GetShadowPositionHClip，用于计算阴影偏移
                Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, ref renderingData.shadowData, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].resolution);
                ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                // 生成阴影图时用于区分定向光和点状光的，它们用不同公式计算偏移
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);
                
                // 设置视口偏移，因为要在一张 RT 上渲染多个级联阴影，各个级联渲染的位置由偏移决定
                cmd.SetViewport(new Rect(m_CascadeSlices[cascadeIndex].offsetX, m_CascadeSlices[cascadeIndex].offsetY, m_CascadeSlices[cascadeIndex].resolution, m_CascadeSlices[cascadeIndex].resolution));
                // 设置 VP 矩阵
                cmd.SetViewProjectionMatrices(m_CascadeSlices[cascadeIndex].viewMatrix, m_CascadeSlices[cascadeIndex].projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                // 绘制 Shadowmap
                context.DrawShadows(ref settings);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            SetupMainLightShadowReceiverConstants(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 设置 Shader 用于接收阴影的相关变量
        ///     阴影采样时使用的关键字
        ///     _MainLightShadowmapTexture：阴影图集
        ///     _MainLightWorldToShadow：阴影矩阵
        ///     _MainLightShadowParams：阴影参数
        ///     _CascadeShadowSplitSpheres：级联阴影距离
        /// </summary>
        /// <param name="cmd"></param>
        void SetupMainLightShadowReceiverConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 没有使用级联阴影的关键字
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, renderingData.shadowData.mainLightShadowCascadesCount == 1);
            // 使用级联阴影的关键字
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, renderingData.shadowData.mainLightShadowCascadesCount > 1);
            // 使用软阴影的关键字
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, false);

            // 将 Shadowmap 设置为全局纹理
            cmd.SetGlobalTexture(m_MainLightShadowmapId, m_MainLightShadowmapTexture);
            
            // 阴影矩阵，将片元的世界坐标变换到阴影纹理像素坐标上
            // 首先让片元的世界坐标左乘光源裁剪空间的VP矩阵，转换到光源裁剪空间，
            // 然后将其坐标从[-1,1]缩放到[0,1]，然后根据Tile偏移和缩放到对应光源的Tile上，就可以进行采样了。
            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < cascadeCount; ++i)
                m_MainLightShadowMatrices[i] = m_CascadeSlices[i].shadowTransform;
            // 无操作矩阵，可避免 shader 中使用分支
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            for (int i = cascadeCount; i <= k_MaxCascades; ++i)
                m_MainLightShadowMatrices[i] = noOpShadowMatrix;
            cmd.SetGlobalMatrixArray(MainLightShadowConstantBuffer._WorldToShadow, m_MainLightShadowMatrices);
            
            // (x: shadowStrength, y: 1.0 if soft shadows, 0.0 otherwise, z: main light fade scale, w: main light fade bias)
            cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams,
                new Vector4(1f, 0f, 0f, 0f));
            
            // 级联阴影距离
            if (m_ShadowCasterCascadesCount > 1)
            {
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres0,
                    m_CascadeSplitDistances[0]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres1,
                    m_CascadeSplitDistances[1]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres2,
                    m_CascadeSplitDistances[2]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres3,
                    m_CascadeSplitDistances[3]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSphereRadii, new Vector4(
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w));
            }

        }

        
        private void Clear()
        {
            m_MainLightShadowmapTexture = null;

            for (int i = 0; i < m_MainLightShadowMatrices.Length; ++i)
                m_MainLightShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
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
