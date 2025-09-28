using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class OutlineEffect : MonoBehaviour
{
    public Color outlineColor = Color.yellow;
    public float thickness = 1.0f;
    public Material outlineMat;

    void OnEnable()
    {
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (outlineMat != null)
        {
            outlineMat.SetColor("_OutlineColor", outlineColor);
            outlineMat.SetFloat("_Thickness", thickness);
            Graphics.Blit(src, dst, outlineMat);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }
}
