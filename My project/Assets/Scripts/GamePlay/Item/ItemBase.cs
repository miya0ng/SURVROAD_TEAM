using UnityEngine;

public abstract class ItemBase : MonoBehaviour
{
    [Header("공통 설정")]
    public float moveSpeed = 100f; // 흡수 속도
    protected Transform player;
    protected bool isCollecting = false;

    [Header("아이템 데이터")]
    public ItemData itemData;  // CSV에서 채워 넣음

    public float partsValue = 10f;
    public float maxPartsValue = 100f;

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

    // 아이템 효과 발동
    protected abstract void Collect(GameObject player);
}
