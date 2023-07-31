# DrawSkyboxPass



## 一、绘制天空盒



### 1.1 渲染顺序

ScriptableRenderPass 是所有 RenderPass 的基类。其中 renderPassEvent 属性定义了该 RenderPass 的渲染顺序，绘制天空盒一般将其定义为 RenderPassEvent.BeforeRenderingSkybox。



### 1.2 绘制天空盒

绘制天空盒直接使用 Unity 提供的 context.DrawSkybox 方法即可完成绘制天空盒的绘制

```C#
var camera = renderingData.cameraData.camera;
context.DrawSkybox(camera);
```

