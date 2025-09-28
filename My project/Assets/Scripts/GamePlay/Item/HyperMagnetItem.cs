using UnityEngine;

public class HyperMagnetItem : ItemBase
{
    [SerializeField] float radius = 999f;     // 필요 반경
    [SerializeField] LayerMask itemLayer;     // 아이템 레이어 지정 권장

    protected override void Collect(GameObject player)
    {
        var me = player.transform;
        var cols = Physics.OverlapSphere(me.position, radius, itemLayer);

        foreach (var c in cols)
        {
            var it = c.GetComponentInParent<ItemBase>();
            if (!it || it == this) continue;
            it.ForceCollect(me);
        }
        Destroy(gameObject);
    }
}