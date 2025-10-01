using UnityEngine;
using System.Collections;

public abstract class ItemBase : MonoBehaviour
{
    [Header("공통 설정")]
    public float moveSpeed = 20f;
    [SerializeField] private float collectTimeout = 2.5f;
    [SerializeField] private float collectDistance = 3f;

    protected Transform player;
    protected bool isCollecting = false;

    private bool collectInvoked = false;
    private Coroutine timeoutCo;

    private bool timeoutActive = false;
    private bool leftAfterTimeout = false;

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

            // Collect는 한 번만
            if (!collectInvoked && Vector3.Distance(transform.position, player.position) <= collectDistance)
            {
                collectInvoked = true;
                Collect(player.gameObject);

                // 4초 동안 파괴되지 않으면 추적 중단(타임아웃)
                if (timeoutCo != null) StopCoroutine(timeoutCo);
                timeoutCo = StartCoroutine(StopFollowingIfNotDestroyed(collectTimeout));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        player = other.transform;

        if (timeoutActive && !leftAfterTimeout)
        {
            return;
        }

        isCollecting = true;
        collectInvoked = false;
        timeoutActive = false;
        leftAfterTimeout = false;

        // 첫 프레임 보정 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (timeoutActive)
            leftAfterTimeout = true;
    }

    protected abstract void Collect(GameObject player);

    public void ForceCollect(Transform p)
    {
        player = p;
        isCollecting = true;
        collectInvoked = false;
        timeoutActive = false;
        leftAfterTimeout = false;
    }

    private IEnumerator StopFollowingIfNotDestroyed(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isCollecting = false;
        timeoutActive = true;
        leftAfterTimeout = false;

    }
}
