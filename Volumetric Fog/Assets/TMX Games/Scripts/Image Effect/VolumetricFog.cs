using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Volumetric Fog")]
[ImageEffectAllowedInSceneView]
public class VolumetricFog : PostEffectsBase
{
    public Shader fogShader;
    public Material fogMaterial;
    public bool downsample;
    [Range(0.05f, 1f)]
    public float downsampleSize = .5f;
    public bool blur;
    [Range(0f, 1f)]
    public float blurSize = .1f;

    private Camera cam;
    private Transform camtr;

    public override bool CheckResources ()
    {
        CheckSupport(true);

        if (!isSupported || fogMaterial == null)
            ReportAutoDisable();
        return isSupported;
    }

    [ImageEffectAllowedInSceneView]
    [ImageEffectOpaque]
    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
        if (CheckResources() == false)
        {
            Graphics.Blit(source, destination);
            return;
        }

        cam = Camera.current;

        camtr = cam.transform;
        float camNear = cam.nearClipPlane;
        float camFar = cam.farClipPlane;
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        Vector3 toRight = camtr.right * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
        Vector3 toTop = camtr.up * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 topLeft = (camtr.forward * camNear - toRight + toTop);
        float camScale = topLeft.magnitude * camFar / camNear;

        topLeft.Normalize();
        topLeft *= camScale;

        Vector3 topRight = (camtr.forward * camNear + toRight + toTop);
        topRight.Normalize();
        topRight *= camScale;

        Vector3 bottomRight = (camtr.forward * camNear + toRight - toTop);
        bottomRight.Normalize();
        bottomRight *= camScale;

        Vector3 bottomLeft = (camtr.forward * camNear - toRight - toTop);
        bottomLeft.Normalize();
        bottomLeft *= camScale;

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        fogMaterial.SetMatrix("_FrustumCornersWS", frustumCorners);

        var sceneMode = RenderSettings.fogMode;
        var sceneDensity = RenderSettings.fogDensity;
        var sceneStart = RenderSettings.fogStartDistance;
        var sceneEnd = RenderSettings.fogEndDistance;
        Vector4 sceneParams;
        bool linear = (sceneMode == FogMode.Linear);
        float diff = linear ? sceneEnd - sceneStart : 0.0f;
        float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
        sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
        sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
        sceneParams.z = linear ? -invDiff : 0.0f;
        sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;
        fogMaterial.SetVector("_SceneFogParams", sceneParams);

        int pass = 0;

        if (downsample)
        {
            var downscaleTemp = RenderTexture.GetTemporary(Mathf.CeilToInt(Screen.width * downsampleSize), Mathf.CeilToInt(Screen.height * downsampleSize), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 8);
            CustomGraphicsBlit(source, downscaleTemp, fogMaterial, pass);
            if (blur)
            {
                float blurSizeCalc = blurSize * .01f;
                float blurSizeCalc_SQR = blurSizeCalc * blurSizeCalc;
                fogMaterial.SetVector("_Offset", new Vector2(blurSizeCalc, 0f));
                Graphics.Blit(downscaleTemp, destination, fogMaterial, 1);
                fogMaterial.SetVector("_Offset", new Vector2(0f, blurSizeCalc));
                Graphics.Blit(destination, downscaleTemp, fogMaterial, 1);

                fogMaterial.SetVector("_Offset", new Vector2(blurSizeCalc_SQR, blurSizeCalc_SQR));
                Graphics.Blit(downscaleTemp, destination, fogMaterial, 1);
                fogMaterial.SetVector("_Offset", new Vector2(-blurSizeCalc_SQR, -blurSizeCalc_SQR));
                Graphics.Blit(destination, downscaleTemp, fogMaterial, 1);

                fogMaterial.SetVector("_Offset", new Vector2(-blurSizeCalc_SQR, blurSizeCalc_SQR));
                Graphics.Blit(downscaleTemp, destination, fogMaterial, 1);
                fogMaterial.SetVector("_Offset", new Vector2(blurSizeCalc_SQR, -blurSizeCalc_SQR));
                Graphics.Blit(destination, downscaleTemp, fogMaterial, 1);

            }

            Shader.SetGlobalTexture("_FogTex", downscaleTemp);
            Shader.SetGlobalTexture("_SceneTex", source);

            Graphics.Blit(downscaleTemp, destination, fogMaterial, 2);
            RenderTexture.ReleaseTemporary(downscaleTemp);
        }
        else
        {
            CustomGraphicsBlit(source, destination, fogMaterial, pass);

            if (blur)
            {
                float blurSizeCalc = blurSize * .01f;
                float blurSizeCalc_SQR = blurSizeCalc * blurSizeCalc;
                fogMaterial.SetVector("_Offset", new Vector2(blurSizeCalc, 0f));
                Graphics.Blit(destination, destination, fogMaterial, 1);
                fogMaterial.SetVector("_Offset", new Vector2(0f, blurSizeCalc));
                Graphics.Blit(destination, destination, fogMaterial, 1);
            }

            Shader.SetGlobalTexture("_FogTex", destination);
            Shader.SetGlobalTexture("_SceneTex", source);
            Graphics.Blit(destination, destination, fogMaterial, 2);
        }
    }

    static void CustomGraphicsBlit (RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
    {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }
}
