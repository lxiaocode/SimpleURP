using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SimpleURP
{
    [CreateAssetMenu(menuName = "Rendering/Create SimpleRendererData")]
    public class SimpleRendererData : UniversalRendererData
    {
        protected override ScriptableRenderer Create()
        {
            return new SimpleRenderer(this);
        }
    }
}
