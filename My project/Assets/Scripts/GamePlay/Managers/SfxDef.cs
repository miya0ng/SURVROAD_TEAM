using UnityEngine;

[CreateAssetMenu(menuName = "Audio/SfxDef")]
public class SfxDef : ScriptableObject
{
    public string id;                
    public AudioClip clip;
    [Range(0f, 5f)] public float volume = 1f;
    public bool spatial = false;
    public float pitchMin = 1f, pitchMax = 1f;
    public float maxDistance = 25f;
    public int priority = 128;
}