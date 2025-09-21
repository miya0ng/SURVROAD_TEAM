//using DG.Tweening;
//using UnityEngine;

//public class HitFlash : MonoBehaviour
//{
//    private Material[] mats;
//    private int flashID;

//    void Awake()
//    {
//        var renderer = GetComponentInChildren<Renderer>();
//        if (renderer != null)
//        {
//            mats = renderer.materials;
//            flashID = Shader.PropertyToID("_FlashAmount");
//        }
//    }

//    public void Start()
//    {

//    }

//    public void PlayFlash()
//    {
//        if (mats == null || mats.Length == 0) return;

//        foreach (var mat in mats)
//        {
//            if (mat.HasProperty(flashID))
//            {
//                mat.DOKill();
//                mat.SetFloat(flashID, 0f);

//                mat.DOFloat(1f, flashID, 0.1f)
//                   .SetLoops(2, LoopType.Yoyo);
//            }
//        }
//    }
//}
using DG.Tweening;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private Renderer rend;
    private Material[] originalMats;

    [SerializeField] private Material flashMat;   // Èò»ö ¹øÂ½¿ë
    [SerializeField] private float flashDuration = 0.1f;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalMats = rend.materials;
    }

    public void PlayFlash()
    {
        if (rend == null || flashMat == null) return;

        Material[] mats = new Material[originalMats.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = flashMat;

        rend.materials = mats;

        DOVirtual.DelayedCall(flashDuration, () =>
        {
            rend.materials = originalMats;
        });
    }
}
