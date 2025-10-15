using UnityEngine;

[RequireComponent(typeof(PlayerBehaviour))]
public class ItemTester : MonoBehaviour
{
    [Header("Test")]
    public int testItemId = 32101; // 인스펙터에서 ID 넣고 T 키로 사용

    [Header("Refs")]
    [SerializeField] private PlayerBehaviour player;          // Heal 등
    [SerializeField] private PlayerStatusEffects status;      // 무적/공속/부스터
    [SerializeField] private LayerMask enemyMask = ~0;        // EMP/충격파 대상

    void Reset() { player = GetComponent<PlayerBehaviour>(); if (!status) status = GetComponent<PlayerStatusEffects>(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            UseItemById(testItemId);
        }
    }

    public void UseItemById(int id)
    {

    }

}