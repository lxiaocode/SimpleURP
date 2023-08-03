using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UISettings : MonoBehaviour
{
    public UniversalRenderPipelineAsset simple;
    public UniversalRenderPipelineAsset urp;
    
    public void SwitchSimple()
    {
        GraphicsSettings.defaultRenderPipeline = simple;
    }
    public void SwitchURP()
    {
        GraphicsSettings.defaultRenderPipeline = urp;
    }
}
