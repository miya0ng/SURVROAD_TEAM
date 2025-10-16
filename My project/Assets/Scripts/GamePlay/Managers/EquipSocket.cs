using UnityEngine;

public enum SocketType { Front, Back, Top, Left, Right, VehicleRoot, Dropper }

public class EquipSocket : MonoBehaviour
{
    public SocketType type;
    public Transform soket;
    [HideInInspector] public bool occupied;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f));
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, $"[{type}]");
    }
#endif
}