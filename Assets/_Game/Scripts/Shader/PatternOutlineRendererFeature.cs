using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace PatternRendering
{

    public sealed class PatternOutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Material compositeMaterial;
        public Material CompositeMaterial { get => compositeMaterial; set => compositeMaterial = value; }
        OutlinePass pass;

        public override void Create() => pass = new OutlinePass();

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var type = renderingData.cameraData.cameraType;
            if (compositeMaterial == null || type == CameraType.Preview || type == CameraType.Reflection)
                return;
            pass.Material = compositeMaterial;
            renderer.EnqueuePass(pass);
        }

        sealed class OutlinePass : ScriptableRenderPass
        {
            static readonly ShaderTagId DataTag = new ShaderTagId("PatternOutline");
            static readonly int SourceId = Shader.PropertyToID("_BlitTexture");
            static readonly int ScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            static readonly int NormalsId = Shader.PropertyToID("_PatternOutlineNormals");
            static readonly int StyleId = Shader.PropertyToID("_PatternOutlineStyle");
            static readonly int ColorId = Shader.PropertyToID("_PatternOutlineColor");
            static readonly int DepthId = Shader.PropertyToID("_CameraDepthTexture");
            static readonly int SizeId = Shader.PropertyToID("_PatternOutlineSize");
            readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            public Material Material { private get; set; }

            public OutlinePass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            sealed class DataPass { public RendererListHandle renderers; }
            sealed class CompositePass
            {
                public TextureHandle source, normals, style, color, depth;
                public Material material;
                public MaterialPropertyBlock properties;
                public Vector4 size;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid())
                    return;

                var descriptor = camera.cameraTargetDescriptor;
                descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.bindMS = false;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                var normals = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor,
                    "Pattern outline normals and mask", true, FilterMode.Point);
                var style = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor,
                    "Pattern outline material settings", true, FilterMode.Point);
                descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                var color = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor,
                    "Pattern outline color", true, FilterMode.Point);

                using (var builder = graph.AddRasterRenderPass<DataPass>("Pattern outline material data", out var data))
                {
                    var rendering = frameData.Get<UniversalRenderingData>();
                    var lights = frameData.Get<UniversalLightData>();
                    var drawing = RenderingUtils.CreateDrawingSettings(DataTag, rendering, camera, lights,
                        camera.defaultOpaqueSortFlags);
                    var filtering = new FilteringSettings(RenderQueueRange.opaque, camera.camera.cullingMask);
                    data.renderers = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
                    builder.UseRendererList(data.renderers);


                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(normals, 0, AccessFlags.Write);
                    builder.SetRenderAttachment(style, 1, AccessFlags.Write);
                    builder.SetRenderAttachment(color, 2, AccessFlags.Write);
                    builder.SetRenderFunc(static (DataPass d, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(d.renderers));
                }

                var colorDescriptor = graph.GetTextureDesc(resources.activeColorTexture);
                colorDescriptor.name = "Camera color with pattern outlines";
                colorDescriptor.clearBuffer = false;
                var destination = graph.CreateTexture(colorDescriptor);
                using (var builder = graph.AddRasterRenderPass<CompositePass>("Pattern outline composite", out var data))
                {
                    data.source = resources.activeColorTexture;
                    data.normals = normals;
                    data.style = style;
                    data.color = color;
                    data.depth = resources.cameraDepthTexture;
                    data.material = Material;
                    data.properties = properties;
                    data.size = new Vector4(1f / descriptor.width, 1f / descriptor.height, descriptor.width, descriptor.height);
                    builder.UseTexture(data.source, AccessFlags.Read);
                    builder.UseTexture(data.normals, AccessFlags.Read);
                    builder.UseTexture(data.style, AccessFlags.Read);
                    builder.UseTexture(data.color, AccessFlags.Read);
                    builder.UseTexture(data.depth, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CompositePass d, RasterGraphContext context) =>
                    {
                        var block = d.properties;
                        block.Clear();
                        block.SetTexture(SourceId, (RTHandle)d.source);
                        block.SetTexture(NormalsId, (RTHandle)d.normals);
                        block.SetTexture(StyleId, (RTHandle)d.style);
                        block.SetTexture(ColorId, (RTHandle)d.color);
                        block.SetTexture(DepthId, (RTHandle)d.depth);
                        block.SetVector(ScaleBiasId, new Vector4(1, 1, 0, 0));
                        block.SetVector(SizeId, d.size);
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, block);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
