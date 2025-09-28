using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Renderer))]
public class Outline3DController : MonoBehaviour
{
    [SerializeField] Color outlineColor = Color.yellow;
    [SerializeField] float pixelThickness = 2f;   // 원하는 두께(px)
    [SerializeField] float minWorldThickness = 0.005f;
    [SerializeField] float pulseDuration = 1f;

    static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    static readonly int ThickID = Shader.PropertyToID("_OutlineThickness");
    static readonly int AlphaID = Shader.PropertyToID("_OutlineAlpha");

    Renderer rend;
    MaterialPropertyBlock mpb;
    Tween pulseTween;
    Camera cam;

    float outlineAlpha = 0f;

    void Awake()
    {
        cam = Camera.main;
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        // 초기 세팅
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(ColorID, outlineColor);
        mpb.SetFloat(ThickID, minWorldThickness);
        mpb.SetFloat(AlphaID, 0f);
        rend.SetPropertyBlock(mpb);
    }

    void OnEnable()
    {
        // Pulse 애니메이션
        pulseTween = DOTween.To(() => outlineAlpha,
                                v => { outlineAlpha = v; ApplyBlock(); },
                                1f, pulseDuration)
                            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDisable()
    {
        pulseTween?.Kill();
        outlineAlpha = 0f;
        ApplyBlock(); // 꺼질 때 원상 복귀
    }

    void LateUpdate()
    {
        if (!cam) return;

        float dist = Vector3.Distance(cam.transform.position, rend.bounds.center);
        float worldPerPixel = 2f * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad) * dist / Screen.height;
        float worldThickness = Mathf.Max(minWorldThickness, worldPerPixel * pixelThickness);

        mpb.SetFloat(ThickID, worldThickness);
        rend.SetPropertyBlock(mpb);
    }

    void ApplyBlock()
    {
        mpb.SetColor(ColorID, outlineColor);
        mpb.SetFloat(AlphaID, outlineAlpha);
        rend.SetPropertyBlock(mpb);
    }
}
