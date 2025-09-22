using UnityEngine;

public abstract class ItemBase : MonoBehaviour
{
    [Header("공통 설정")]
    public float moveSpeed = 300f;
    protected Transform player;
    protected bool isCollecting = false;

    [Header("아이템 데이터")]
    public ItemData itemData;

    public virtual void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }
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
            {
                Collect(player.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            isCollecting = true;

            transform.position = Vector3.MoveTowards(
               transform.position,
               player.position,
               moveSpeed * Time.deltaTime
           );
        }
    }

    protected abstract void Collect(GameObject player);
}
