using UnityEngine;

public class NormalPartItem : ItemBase
{
    private ParticleSystem ps;
    protected override void Collect(GameObject player)
    {
        // ��ƼŬ ����
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            transform.parent = null; // �θ� �и� (����� �ڸ����� ����ǰ�)
            ps.Play();
            Destroy(gameObject, ps.main.duration);
        }
    }
}