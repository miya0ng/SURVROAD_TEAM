// Assets/Scripts/Enemy/Movement/AStarCarMotor.cs
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
[DisallowMultipleComponent]
public class AStarCarMotor : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private EnemyCarController car;
    [SerializeField] private Transform target;

    [Header("Pathfinding")]
    [SerializeField] private float repathInterval = 0.5f;
    [SerializeField] private float waypointReachDist = 2.0f;
    [SerializeField] private float lookAhead = 5f;

    private Seeker seeker;
    private Path currentPath;
    private readonly List<Vector3> waypoints = new();
    private int wpIndex;
    private float repathT;

    // 공개 시그니처 추가 없이 EnemyDriver가 내부적으로만 호출
    public void Bind(EnemyCarController c, Transform t) { car = c; target = t; }

    void Awake()
    {
        seeker = GetComponent<Seeker>();
        if (!car) car = GetComponent<EnemyCarController>();
    }

    void OnEnable()
    {
        RequestPath();
    }

    void OnDisable()
    {
        if (currentPath != null)
        {
            currentPath.Release(this);   // silent=false 권장 (버그 빨리 발견)
            currentPath = null;
        }
    }

    void Update()
    {
        if (!target) return;

        repathT -= Time.deltaTime;
        if (repathT <= 0f)
        {
            RequestPath();
            repathT = repathInterval;
        }

        if (waypoints.Count == 0) return;

        Vector3 pos = transform.position;
        Vector3 wp = waypoints[Mathf.Clamp(wpIndex, 0, waypoints.Count - 1)];
        Vector3 to = (wp - pos); to.y = 0f;

        if (to.magnitude <= waypointReachDist)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Count) { car.SetDesired(0f, 0f); return; }
            wp = waypoints[wpIndex];
            to = (wp - pos); to.y = 0f;
        }

        Vector3 steerTarget = wp;
        if (wpIndex + 1 < waypoints.Count)
        {
            Vector3 next = waypoints[wpIndex + 1];
            float d = Vector3.Distance(wp, next);
            steerTarget = Vector3.Lerp(wp, next, Mathf.Clamp01(lookAhead / Mathf.Max(0.0001f, d)));
        }

        Vector3 dir = (steerTarget - pos); dir.y = 0f;
        if (dir.sqrMagnitude < 0.0004f) { car.SetDesired(0f, 0f); return; }

        float desiredThrottle = 1f;
        float desiredSteer = SignedAngleOnPlane(transform.forward, dir.normalized, Vector3.up) / 45f;
        car.SetDesired(Mathf.Clamp(desiredSteer, -1f, 1f), desiredThrottle);
    }

    void RequestPath()
    {
        if (!target || seeker == null) return;
        if (currentPath != null && !currentPath.IsDone()) return;
        seeker.StartPath(transform.position, target.position, OnPathComplete);
    }
    void OnPathComplete(Path p)
    {
        if (p == null || p.error) return;

        if (currentPath != null)
        {
            currentPath.Release(this);
            currentPath = null;
        }

        currentPath = p;
        currentPath.Claim(this);

        waypoints.Clear();
        wpIndex = 0;
        var vp = p.vectorPath;
        for (int i = 0; i < vp.Count; i++) waypoints.Add(vp[i]);
    }

    static float SignedAngleOnPlane(Vector3 a, Vector3 b, Vector3 up)
    {
        var right = Vector3.Cross(up, a);
        a = Vector3.Cross(right, up);
        b = Vector3.Cross(Vector3.Cross(up, b), up);
        return Vector3.SignedAngle(a, b, up);
    }
}

