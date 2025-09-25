// Assets/Scripts/Enemy/EnemyCarDataWrapper.cs
using System;
using UnityEngine;
using UnityEngine.AI;

public enum EnemySize { Small = 1, Medium = 2, Large = 3 }
public enum EnemyMoveStyle { Rush, Shooter, Suicide, Heavy }

[Serializable]
public struct EnemyMoveConfig
{
    public float speed;
    public float acceleration;
    public float angularSpeed;
    public float mass;
    public float linearDamping;
    public float angularDamping;
    public EnemySize size;
    public EnemyMoveStyle style;
    public string prefabName;
    public string name;
    public int id;
}

public class EnemyCarDataWrapper
{
    public EnemyCarData Row { get; }

    public EnemyCarDataWrapper(EnemyCarData row)
    {
        Row = row ?? throw new ArgumentNullException(nameof(row));
    }

    public int ID => Row.ID;
    public string Name => Row.Name;
    public string PrefabName => Row.PrefabName;
    public EnemySize Size => (EnemySize)Mathf.Clamp(Row.Type, 1, 3);
    public bool IsUnlocked => true;

    public EnemyMoveStyle MoveStyle =>
        Row.AttackType switch
        {
            2 => EnemyMoveStyle.Shooter,
            3 => EnemyMoveStyle.Suicide,
            4 => EnemyMoveStyle.Heavy,
            _ => (Size == EnemySize.Large ? EnemyMoveStyle.Heavy : EnemyMoveStyle.Rush)
        };

    public EnemyMoveConfig ToRuntimeConfig()
    {
        float speed = Mathf.Max(3f, Row.MaxSpeed);
        float accel = Mathf.Lerp(2.5f, 8.5f, Mathf.Clamp01(Row.Acceleration / 100f));
        float angular = Mathf.Lerp(90f, 540f, Mathf.Clamp01(Row.Handling / 100f));

        float baseMass = Size switch
        {
            EnemySize.Small => 800f,
            EnemySize.Medium => 1500f,
            EnemySize.Large => 3000f,
            _ => 1200f
        };
        float mass = baseMass * Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(Row.Durability / 200f));

        return new EnemyMoveConfig
        {
            id = Row.ID,
            name = Row.Name,
            prefabName = Row.PrefabName,
            size = Size,
            style = MoveStyle,
            speed = speed,
            acceleration = accel,
            angularSpeed = angular,
            linearDamping = 0.2f,
            angularDamping = 2f,
            mass = mass,
        };
    }

    public void ApplyTo(NavMeshAgent agent, Rigidbody rb, bool agentAsKinematic = true)
    {
        var cfg = ToRuntimeConfig();

        if (agent)
        {
            agent.speed = cfg.speed;
            agent.acceleration = cfg.acceleration;
            agent.angularSpeed = cfg.angularSpeed;

            if (agentAsKinematic)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
        }

        if (rb)
        {
            rb.isKinematic = false;
            rb.mass = cfg.mass;
            rb.linearDamping = cfg.linearDamping;
            rb.angularDamping = cfg.angularDamping;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
