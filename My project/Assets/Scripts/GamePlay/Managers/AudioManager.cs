using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }
    [Header("Mixer")]
    public AudioMixer mixer;         
    public AudioMixerGroup bgmGroup, sfxGroup;

    [Header("BGM")]
    public AudioSource bgmA, bgmB;
    AudioSource _activeBGM;
    float _bgmFadeSec = 0.7f;

    [Header("SFX Pool")]
    public int initialSfxSources = 16;
    readonly Queue<AudioSource> sfxPool = new();
    readonly HashSet<AudioSource> sfxInUse = new();

    [Header("DB")]
    public SfxDef[] sfxDefs;
    Dictionary<string, SfxDef> sfxMap;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        sfxMap = new();
        foreach (var d in sfxDefs) if (d) sfxMap[d.id] = d;

        for (int i = 0; i < initialSfxSources; i++) sfxPool.Enqueue(MakeSfxSource());

        if (!bgmA) bgmA = gameObject.AddComponent<AudioSource>();
        if (!bgmB) bgmB = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { bgmA, bgmB })
        {
            s.outputAudioMixerGroup = bgmGroup;
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f;
        }
        _activeBGM = bgmA;
    }

    AudioSource MakeSfxSource()
    {
        var go = new GameObject("SFX");
        go.transform.SetParent(transform);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = false; a.outputAudioMixerGroup = sfxGroup;
        return a;
    }

    AudioSource RentSfx()
    {
        var src = sfxPool.Count > 0 ? sfxPool.Dequeue() : MakeSfxSource();
        sfxInUse.Add(src); return src;
    }
    void ReturnSfx(AudioSource src)
    {
        sfxInUse.Remove(src);
        src.transform.SetParent(transform); src.clip = null;
        sfxPool.Enqueue(src);
    }

    void Update()
    {
        foreach (var src in new List<AudioSource>(sfxInUse))
            if (!src.isPlaying) ReturnSfx(src);
    }

    public void PlayBGM(AudioClip clip, float fadeSec = -1f)
    {
       // Debug.Log($"[AudioManager] PlayBGM: {clip?.name}, fadeSec={fadeSec}");
        if (!clip) return;
        Debug.Log($"[AudioManager] Success");
        var next = (_activeBGM == bgmA) ? bgmB : bgmA;
        next.clip = clip; next.volume = 0f; next.Play();
        StartCoroutine(FadeSwap(_activeBGM, next, fadeSec > 0 ? fadeSec : _bgmFadeSec));
        _activeBGM = next;
    }
    System.Collections.IEnumerator FadeSwap(AudioSource from, AudioSource to, float t)
    {
        float e = 0; while (e < t)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / t);
            if (from) from.volume = 1f - k;
            if (to) to.volume = k;
            yield return null;
        }
        if (from) { from.Stop(); from.volume = 1f; }
        to.volume = 1f;
    }

    public void StopBGM(float fadeSec = 0.5f)
    {
        StartCoroutine(FadeOut(_activeBGM, fadeSec));
    }
    System.Collections.IEnumerator FadeOut(AudioSource a, float t)
    {
        if (!a || !a.isPlaying) yield break;
        float v = a.volume, e = 0;
        while (e < t) { e += Time.unscaledDeltaTime; a.volume = Mathf.Lerp(v, 0, e / t); yield return null; }
        a.Stop(); a.volume = 1f;
    }

    public void PlaySFX(string id, Vector3? pos = null)
    {
        if (!sfxMap.TryGetValue(id, out var def) || !def.clip) return;
        var a = RentSfx();
        a.transform.position = pos ?? Vector3.zero;
        a.spatialBlend = def.spatial ? 1f : 0f;
        a.maxDistance = def.maxDistance;
        a.priority = def.priority;
        a.volume = def.volume;
        a.pitch = Random.Range(def.pitchMin, def.pitchMax);
        a.clip = def.clip;
        a.Play();
    }

    public AudioClip CurrentBGM => _activeBGM ? _activeBGM.clip : null;
    public void PlayBGMIfDifferent(AudioClip clip, float fadeSec = -1f)
    {
        if (!clip) return;
        if (CurrentBGM == clip && IsBgmPlaying()) return;
        PlayBGM(clip, fadeSec);
    }

    // Settings 연동 (0..1 -> dB)
    public void SetBgmVolume(float v) => mixer.SetFloat("BGM_dB", Lin2dB(v));
    public float GetBgmVolum() { mixer.GetFloat("BGM_dB", out var v); return Mathf.Pow(10f, v / 20f); }
    public void SetSfxVolume(float v) => mixer.SetFloat("SFX_dB", Lin2dB(v));
    public void SetMasterVolume(float v) => mixer.SetFloat("Master_dB", Lin2dB(v));
    float Lin2dB(float v) => Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;
    public bool IsBgmPlaying() => _activeBGM != null && _activeBGM.isPlaying;
}