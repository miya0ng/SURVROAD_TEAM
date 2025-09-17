using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Ui_Game : MonoBehaviour
{
    private WaveManager waveManager;
    public TextMeshProUGUI waveCount;

    public TextMeshProUGUI wavePlayTime;
    public void Awake()
    {
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        wavePlayTime.text = $"{waveManager.WaveTimer:F2}";
        waveCount.text = $"{waveManager.currentWave}";
    }
}