using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SimpleURP
{
    [CreateAssetMenu(menuName = "Rendering/Create SimpleRendererData")]
    public class SimpleRendererData : ScriptableRendererData
    {
        protected override ScriptableRenderer Create()
        {
            return new SimpleRenderer(this);
        }
    }
}
