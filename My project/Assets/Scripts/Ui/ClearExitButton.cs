using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ClearExitButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string titleSceneName = "Title"; // 타이틀 씬 이름
    [SerializeField] private float waitSeconds = 3f;          // 대기 시간(초, 이 동안은 입력 무시)

    [Header("UI (Optional)")]
    [Tooltip("3초 경과 후에만 보이게 될 '아무 키나 누르세요' 안내 오브젝트")]
    [SerializeField] private GameObject pressAnyKeyHint;

    private bool loading;
    private Coroutine co;

    void OnEnable()
    {
        co = StartCoroutine(CoWaitThenListen());
    }

    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        co = null;
    }

    private IEnumerator CoWaitThenListen()
    {
        if (pressAnyKeyHint) pressAnyKeyHint.SetActive(false);
        yield return new WaitForSecondsRealtime(waitSeconds);

        if (pressAnyKeyHint) pressAnyKeyHint.SetActive(true);

        while (!loading)
        {
            if (AnyPressed())
            {
                loading = true;
                Time.timeScale = 1f;
                SceneManager.LoadScene(titleSceneName);
                yield break;
            }
            yield return null;
        }
    }

    private bool AnyPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System
        var k = UnityEngine.InputSystem.Keyboard.current;
        var m = UnityEngine.InputSystem.Mouse.current;
        var g = UnityEngine.InputSystem.Gamepad.current;
        var t = UnityEngine.InputSystem.Touchscreen.current;

        bool key = k != null && k.anyKey.wasPressedThisFrame;
        bool mouse = m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.middleButton.wasPressedThisFrame);
        bool gamepad = g != null && g.allControls.Exists(c =>
        {
            var b = c as UnityEngine.InputSystem.Controls.ButtonControl;
            return b != null && b.wasPressedThisFrame;
        });
        bool touch = t != null && t.primaryTouch.press.wasPressedThisFrame;

        return key || mouse || gamepad || touch;
#else
        // Legacy Input Manager
        if (Input.anyKeyDown) return true;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return true;
        if (Input.touchCount > 0) return true;
        return false;
#endif
    }
}
