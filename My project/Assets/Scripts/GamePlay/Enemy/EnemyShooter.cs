using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyShooter : EnemyBehaviourBase
{
    [Header("Shooter")]
    public float orbitAngular = 35f; // (참고용) 원운동 감각, 실제 이동은 Nav+RB
    public float strafeSpeed = 0.6f; // 탄젠트 이동량 스케일

    private float orbitSign = 1f;    // 좌/우 회전 방향
    private float swapT;             // 좌우 전환 타이머

    protected override void OnEnable()
    {
        base.OnEnable();
        orbitSign = Random.value < 0.5f ? -1f : 1f;
        swapT = Random.Range(1.5f, 3.5f);

        if (agent && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected override void TickMove(float dt)
    {
        if (!target || !agent || !agent.isOnNavMesh) return;

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float d = to.magnitude;
        if (d < 0.001f) return;

        Vector3 dir = to / d;
        float R = Mathf.Max(1f, desiredRadius);

        // 반경 유지 + 탄젠트 스트레이프(원운동 느낌)
        Vector3 tangent = Quaternion.AngleAxis(orbitSign * 90f, Vector3.up) * dir;

        // 반경 밴드 내에서 목표점 형성
        float clampedDist = Mathf.Clamp(d, R - 1f, R + 1f);
        Vector3 desiredPos =
            target.position
            + dir * clampedDist
            + tangent * (strafeSpeed * 2f);

        agent.SetDestination(desiredPos);

        // 주기적으로 좌/우 전환
        swapT -= dt;
        if (swapT <= 0f)
        {
            orbitSign *= -1f;
            swapT = Random.Range(1.5f, 3.5f);
        }

        // 사격 로직(쿨다운 등)은 별도 컴포넌트/Update에서 처리 권장
        // 예: GunController.TryFire(target)
    }
}