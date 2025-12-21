using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // Добавьте это пространство имён

public class FisheyeRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(0, 1)] public float strength = 0.3f;
        public LayerMask fisheyeLayer;
    }

    public Settings settings = new Settings();
    private FisheyePass fisheyePass;

    public override void Create()
    {
        fisheyePass = new FisheyePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.strength > 0)
        {
            renderer.EnqueuePass(fisheyePass);
        }
    }

    class FisheyePass : ScriptableRenderPass
    {
        private Settings settings;
        private Material fisheyeMaterial;
        private RTHandle tempTextureHandle; // Заменили RenderTargetHandle на RTHandle

        public FisheyePass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            // Создаем материал
            fisheyeMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Fisheye");
        }

        // Этот метод вызывается для освобождения ресурсов
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (tempTextureHandle != null)
            {
                tempTextureHandle.Release(); // Освобождаем RTHandle
                tempTextureHandle = null;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (fisheyeMaterial == null || settings.strength == 0) return;

            CommandBuffer cmd = CommandBufferPool.Get("Fisheye Effect");

            // Настраиваем материал
            fisheyeMaterial.SetFloat("_Strength", settings.strength);

            // Создаем дескриптор RTHandle
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Отключаем буфер глубины для временной текстуры

            // Получаем RTHandle через систему RTHandles
            RenderingUtils.ReAllocateIfNeeded(ref tempTextureHandle, descriptor,
                name: "_FisheyeTemp", wrapMode: TextureWrapMode.Clamp,
                filterMode: FilterMode.Bilinear);

            // Рендерим с эффектом, используя RTHandle
            RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Первый Blit: из основного буфера во временный с эффектом
            Blit(cmd, cameraTargetHandle, tempTextureHandle, fisheyeMaterial, 0);

            // Второй Blit: из временного обратно в основной буфер
            Blit(cmd, tempTextureHandle, cameraTargetHandle);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}