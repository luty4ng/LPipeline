using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{

    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    CullingResults cullingResults;
    Lighting lighting = new Lighting();

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), litShaderTagId = new ShaderTagId("CustomLit");
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareForSceneWindow();
        PrepareBuffer();
        if (!Cull(shadowSettings.maxDistance))
            return;

        Setup();
        lighting.Setup(context, cullingResults, shadowSettings);
        // Draw visible Geometry
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        // Draw all unsupported shaders
        DrawUnsupportedShaders();
        // Draw Gizmos
        DrawGizmos();
        // Submit
        Submit();
    }

    private void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    private void Setup()
    {
        // Setup
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear
        );
        buffer.BeginSample(SampleName);
        ExcuteBuffer();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // determine whether orthographic or distance-based sorting applies.
        // determine drawing order.
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        // which kind of shader passes are allowed.
        // var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings); 
        var drawingSettings = new DrawingSettings(unlitShaderTagId, new SortingSettings(camera))
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };


        drawingSettings.SetShaderPassName(1, litShaderTagId);
        // for (int i = 1; i < legacyShaderTagIds.Length; i++)
        // {
        //     drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        // }
        // which render queues are allowed.
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        // draw skybox
        context.DrawSkybox(camera);
        // draw transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExcuteBuffer();
        context.Submit();
    }
}