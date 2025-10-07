using UnityEngine;
using System.Collections;

public abstract class ItemBase : MonoBehaviour
{
    [Header("공통 설정")]
    public float moveSpeed = 20f;
    [SerializeField] private float collectTimeout = 1.5f;
    [SerializeField] private float collectDistance = 3f;

    protected Transform player;
    protected bool isCollecting = false;

    private bool collectInvoked = false;
    private Coroutine timeoutCo;
    private bool isInsideTrigger = false;

    [Header("아이템 데이터")]
    public ItemData itemData;

    protected float lifeTimer = 0;
    protected bool OnTimer = false;

    public virtual void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj) player = playerObj.transform;
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

            if (!collectInvoked && Vector3.Distance(transform.position, player.position) <= collectDistance)
            {
                collectInvoked = true;
                Collect(player.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.transform;
        isInsideTrigger = true;

        if (timeoutCo != null)
        {
            StopCoroutine(timeoutCo);
            timeoutCo = null;
        }
        isCollecting = true;
        collectInvoked = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        isInsideTrigger = false;

        if (isCollecting && timeoutCo == null)
        {
            timeoutCo = StartCoroutine(TimeoutCoroutine());
        }
    }

    protected abstract void Collect(GameObject player);

    public void ForceCollect(Transform p)
    {
        player = p;
        isInsideTrigger = true;
        
        if (timeoutCo != null)
        {
            StopCoroutine(timeoutCo);
            timeoutCo = null;
        }
        
        isCollecting = true;
        collectInvoked = false;
    }

    private IEnumerator TimeoutCoroutine()
    {
        yield return new WaitForSeconds(collectTimeout);
        
        if (!isInsideTrigger)
        {
            isCollecting = false;
        }
        
        timeoutCo = null;
    }
}