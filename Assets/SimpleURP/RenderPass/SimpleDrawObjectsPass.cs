using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP.RenderPass
{
    public class SimpleDrawObjectsPass : ScriptableRenderPass
    {
        private bool m_IsOpaque;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private FilteringSettings m_FilteringSettings;
        
        public SimpleDrawObjectsPass(RenderPassEvent evt, ShaderTagId[] shaderTagIds, bool opaque, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            renderPassEvent = evt;
            m_IsOpaque = opaque;
            foreach (ShaderTagId shaderTagId in shaderTagIds)
                m_ShaderTagIdList.Add(shaderTagId);

            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }
        
        public SimpleDrawObjectsPass(RenderPassEvent evt, bool opaque, RenderQueueRange renderQueueRange, LayerMask layerMask)
            : this(evt, new ShaderTagId[]{new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly")},
                opaque, renderQueueRange, layerMask)
        { }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            
            var sortFlags = m_IsOpaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortFlags };
            DrawingSettings settings = new DrawingSettings(m_ShaderTagIdList[0], sortingSettings)
            {
                perObjectData = renderingData.perObjectData,
            };
            for (int i = 1; i < m_ShaderTagIdList.Count; ++i)
                settings.SetShaderPassName(i, m_ShaderTagIdList[i]);

            context.DrawRenderers(renderingData.cullResults, ref settings, ref m_FilteringSettings);
        }
    }
}
