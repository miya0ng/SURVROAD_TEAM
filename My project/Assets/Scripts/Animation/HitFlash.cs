using DG.Tweening;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private Material mat;

    void Awake()
    {
        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            mat = renderer.material; // 인스턴스화
    }

    public void PlayFlash()
    {
        if (mat == null) return;

        // 깜빡임 값 초기화
        mat.SetFloat("_FlashAmount", 0f);

        // 트윈 실행
        mat.DOKill();
        mat.DOFloat(1f, "_FlashAmount", 0.1f)
           .SetLoops(2, LoopType.Yoyo);
    }
}
