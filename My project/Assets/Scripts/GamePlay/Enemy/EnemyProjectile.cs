// Assets/Scripts/Enemy/Combat/EnemyProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float life = 4f;
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask hitMask;  // 플레이어/환경
    private float traveled;
    private Vector3 lastPos;

    public void Setup(int dmg, float maxRange)
    {
        damage = dmg;
        life = Mathf.Max(life, maxRange / 10f); // 속도/사거리 따라 대충 유지
    }

    void OnEnable()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);

        // 간단한 터널링 방지(레이캐스트 보정)
        Vector3 pos = transform.position;
        Vector3 dir = pos - lastPos;
        float len = dir.magnitude;
        if (len > 0.001f)
        {
            if (Physics.Raycast(lastPos, dir.normalized, out var hit, len + 0.05f, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit.collider, hit.point);
            }
        }
        lastPos = pos;
    }

    void OnCollisionEnter(Collision c)
    {
        OnHit(c.collider, c.GetContact(0).point);
    }

    void OnHit(Collider col, Vector3 at)
    {
        var le = col.GetComponentInParent<LivingEntity>();
        if (le != null)
        {
            le.OnDamage(damage, null);
        }
        Destroy(gameObject);
    }
}
