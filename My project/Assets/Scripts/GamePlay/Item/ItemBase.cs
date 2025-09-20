using UnityEngine;

public abstract class ItemBase : MonoBehaviour
{
    [Header("���� ����")]
    public float moveSpeed = 10f; // ��� �ӵ�
    protected Transform player;
    protected bool isCollecting = false;

    [Header("������ ������")]
    public ItemData itemData;  // CSV���� ä�� ����

    protected virtual void Update()
    {
        if (isCollecting && player != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, player.position) < 1f)
                Collect(player.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            isCollecting = true;
        }
    }

    // ������ ȿ�� �ߵ�
    protected abstract void Collect(GameObject player);
}
