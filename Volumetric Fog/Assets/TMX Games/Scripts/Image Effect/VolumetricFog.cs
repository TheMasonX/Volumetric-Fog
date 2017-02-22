using System.Collections;
using System.Collections.Generic;
using TMX.Utils;
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

    [ContextMenu("Random Offsets")]
    public void GenerateRandomSphere ()
    {
        string output = "static const float3 Offsets[8] = {\n";
        for(int i = 0; i < 8; i++)
        {
            var rand = MathUtils.RandomInUnitSphere();
            output += string.Format("\tfloat3({0}, {1}, {2}),\n", rand.x.ToString("N4"), rand.y.ToString("N4"), rand.z.ToString("N4"));
        }
        output += "};";
        Debug.Log(output);
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

        int pass = 0;

        if (downsample)
        {
            var downscaleTemp = RenderTexture.GetTemporary(Mathf.CeilToInt(Screen.width * downsampleSize), Mathf.CeilToInt(Screen.height * downsampleSize), 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 8);
            CustomGraphicsBlit(source, downscaleTemp, fogMaterial, pass);
            //Graphics.Blit(source, downscaleTemp, fogMaterial, pass);
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
            var temp = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 8);
            CustomGraphicsBlit(source, temp, fogMaterial, pass);

            if (blur)
            {
                float blurSizeCalc = blurSize * .01f;
                float blurSizeCalc_SQR = blurSizeCalc * blurSizeCalc;
                fogMaterial.SetVector("_Offset", new Vector2(blurSizeCalc, 0f));
                Graphics.Blit(temp, destination, fogMaterial, 1);
                fogMaterial.SetVector("_Offset", new Vector2(0f, blurSizeCalc));
                Graphics.Blit(destination, temp, fogMaterial, 1);
            }

            Shader.SetGlobalTexture("_FogTex", temp);
            Shader.SetGlobalTexture("_SceneTex", source);
            Graphics.Blit(temp, destination, fogMaterial, 2);
            RenderTexture.ReleaseTemporary(temp);
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
