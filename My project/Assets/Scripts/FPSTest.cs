using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FPSTest : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}