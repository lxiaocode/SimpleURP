# SimpleDrawObjectsPass



## 一、绘制几何体

执行 context.DrawRenderers 用于绘制几何体，这个方法有两个重要的参数：

1. DrawingSettings 决定了支持使用哪些 Shader Pass，以及如何对渲染对象排序。
2. FilteringSettings 决定了如何过滤接收到的对象，以便只绘制需要的对象。例如：RenderQueue、LayerMask。

