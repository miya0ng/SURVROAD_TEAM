using UnityEngine;

public interface IAimStrategy
{
    Transform GetAimTarget(Transform self, TeamId teamId);
    Quaternion GetRotationTowards(Transform self, Transform target);
}