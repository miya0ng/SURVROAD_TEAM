using UnityEngine;

public class NormalPartItem : ItemBase
{
    private ParticleSystem ps;
    protected override void Collect(GameObject player)
    {
        // 파티클 실행
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            transform.parent = null; // 부모 분리 (흡수된 자리에서 재생되게)
            ps.Play();
            Destroy(gameObject, ps.main.duration);
        }
    }
}