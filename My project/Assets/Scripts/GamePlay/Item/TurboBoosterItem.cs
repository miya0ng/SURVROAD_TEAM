using UnityEngine;

public class TurboBoosterItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var booster = player.GetComponent<PlayerStatusEffects>();
        if (booster)
        {
            float duration = (itemData != null && itemData.Duration > 0f) ? itemData.Duration : 5f;
            float cooldown = 2f; // 필요시 CSV/데이터에서
            booster.UnlockTurbo(duration, cooldown);  // 버튼 활성 준비
        }
        Destroy(gameObject);
    }
}
