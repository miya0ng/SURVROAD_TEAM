using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonArsenal
{
    public class PolygonSoundSpawn : MonoBehaviour
    {

        public GameObject prefabSound;

        public bool destroyWhenDone = true;
        public bool soundPrefabIsChild = false;
        [Range(0.01f, 10f)]
        public float pitchRandomMultiplier = 1f;

        // Use this for initialization
        void Start()
        {
            GameObject m_Sound = Instantiate(prefabSound, transform.position, Quaternion.identity);
            AudioSource m_Source = m_Sound.GetComponent<AudioSource>();

            // [핵심] 우리 Mixer로 라우팅 + BGM보다 낮은 중요도
            var am = AudioManager.I;
            if (am)
            {
                m_Source.outputAudioMixerGroup = am.sfxGroup;
                m_Source.priority = 200;          // BGM은 0, SFX는 낮은 우선순위
                m_Source.ignoreListenerPause = false;
            }

            // 2D/3D 정책(원하는대로)
            // m_Source.spatialBlend = 1f; // 3D면
            m_Source.dopplerLevel = 0f;

            // 피치 랜덤
            if (pitchRandomMultiplier != 1)
            {
                if (Random.value < .5f) m_Source.pitch *= Random.Range(1 / pitchRandomMultiplier, 1);
                else m_Source.pitch *= Random.Range(1, pitchRandomMultiplier);
            }

            // PlayOnAwake 안 돼있을 수 있으니 보장
            if (!m_Source.isPlaying) m_Source.Play();

            if (destroyWhenDone)
            {
                float life = m_Source.clip ? m_Source.clip.length / Mathf.Max(0.01f, m_Source.pitch) : 1f;
                Destroy(m_Sound, life);
            }

            if (soundPrefabIsChild) m_Sound.transform.SetParent(transform);
        }
    }
}
