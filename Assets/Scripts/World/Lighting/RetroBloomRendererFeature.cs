using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class RetroBloomRendererFeature : ScriptableRendererFeature
{
    [Tooltip("Configuracao global de intensidade e espalhamento do halo seletivo.")]
    [SerializeField] private RetroBloomSettings settings;
    private BloomPass bloomPass;
    private Material material;

    public override void Create()
    {
        CoreUtils.Destroy(material);
        material = settings != null && settings.compositeShader != null
            ? CoreUtils.CreateEngineMaterial(settings.compositeShader) : null;
        bloomPass = new BloomPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings == null || !settings.enabled || settings.intensity <= 0f || material == null ||
            renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.renderType == CameraRenderType.Overlay)
            return;
        bloomPass.Setup(settings, material);
        renderer.EnqueuePass(bloomPass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

    private sealed class BloomPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId MaskTag = new("ReactiveGlowMask");
        private RetroBloomSettings settings;
        private Material material;
        private sealed class MaskData { public RendererListHandle list; }
        private sealed class CompositeData { public TextureHandle texture; public Material material; public float intensity; public float radius; }

        public void Setup(RetroBloomSettings value, Material effectMaterial)
        {
            settings = value;
            material = effectMaterial;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>();
            var rendering = frameData.Get<UniversalRenderingData>();
            var lights = frameData.Get<UniversalLightData>();
            var desc = graph.GetTextureDesc(resources.activeColorTexture);
            desc.name = "Retro Bloom Mask";
            desc.clearBuffer = true;
            desc.clearColor = Color.clear;
            var mask = graph.CreateTexture(desc);
            var drawing = RenderingUtils.CreateDrawingSettings(MaskTag, rendering, camera, lights, camera.defaultOpaqueSortFlags);
            var filter = new FilteringSettings(RenderQueueRange.opaque);
            var list = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filter));
            using (var builder = graph.AddRasterRenderPass<MaskData>("Retro Bloom Mask", out var data))
            {
                data.list = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc(static (MaskData pass, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(pass.list));
            }

            int divisor = Mathf.Clamp(settings.downsample, 2, 8);
            desc.width = Mathf.Max(1, desc.width / divisor);
            desc.height = Mathf.Max(1, desc.height / divisor);
            desc.name = "Retro Bloom Reduced";
            desc.msaaSamples = MSAASamples.None;
            desc.clearBuffer = false;
            var reduced = graph.CreateTexture(desc);
            graph.AddBlitPass(mask, reduced, Vector2.one, Vector2.zero, passName: "Retro Bloom Downsample");
            using (var builder = graph.AddRasterRenderPass<CompositeData>("Retro Bloom Composite", out var data))
            {
                data.texture = reduced;
                data.material = material;
                data.intensity = settings.intensity;
                data.radius = settings.radius;
                builder.UseTexture(reduced);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositeData pass, RasterGraphContext ctx) =>
                {
                    pass.material.SetFloat("_RetroBloomIntensity", pass.intensity);
                    pass.material.SetFloat("_RetroBloomRadius", pass.radius);
                    Blitter.BlitTexture(ctx.cmd, pass.texture, new Vector4(1, 1, 0, 0), pass.material, 0);
                });
            }
        }
    }
}
