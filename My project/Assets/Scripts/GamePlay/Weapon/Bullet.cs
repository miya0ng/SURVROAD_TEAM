using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Bullet : MonoBehaviour
{
    private LineRenderer tracer;
    private TeamId ownerTeam;

    private float lifeTime = 1f;
    private float damage;

    private GameObject player;
    private float bulletSpeed = 30f;
    private List<Vector3> points = new();
    private void Awake()
    {
        player = GameObject.FindWithTag("Player");
        tracer = GetComponent<LineRenderer>();
    }

    public void Init(float speed, float lifeTime, float damage, TeamId team)
    {
        this.lifeTime = lifeTime;
        this.damage = damage;
        bulletSpeed = speed;
        ownerTeam = team;

        Vector3 bulletVelocity = transform.forward * bulletSpeed;
        tracer.positionCount = 0;
        points.Clear();

        points.Add(transform.position);
        tracer.positionCount = 2;
        tracer.SetPosition(0, transform.position);
        tracer.SetPosition(1, transform.position + bulletVelocity * Time.deltaTime);

        StartCoroutine(Flight(bulletVelocity));
    }

    private IEnumerator Flight(Vector3 velocity)
    {
        float elapsed = 0f;

        while (elapsed < lifeTime)
        {
            transform.position += velocity * Time.deltaTime;

            tracer.positionCount++;
            tracer.SetPosition(tracer.positionCount - 1, transform.position);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other == player.GetComponent<Collider>())
        {
            return;
        }

        if (other.TryGetComponent<LivingEntity>(out var entity))
        {
            if (entity.TryGetComponent<IDamagable>(out var target))
                target.OnDamage(damage);

            Destroy(gameObject);
        }
    }
}