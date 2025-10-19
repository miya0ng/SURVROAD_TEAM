using UnityEngine;

public static class Haptics
{
    public static bool Enabled { get; set; } = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject GetVibrator()
    {
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using var context = activity.Call<AndroidJavaObject>("getApplicationContext");

        int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

        // API 31+ : VibratorManager 경유
        if (sdk >= 31)
        {
            using var vmClass = new AndroidJavaClass("android.os.VibratorManager");
            using var vm = context.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
            if (vm != null)
                return vm.Call<AndroidJavaObject>("getDefaultVibrator"); // Vibrator
        }

        // 구버전: 직접 Vibrator
        return context.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }
#endif

    public static void Light() => Vibrate(20, 50);
    public static void Medium() => Vibrate(35, 150);
    public static void Heavy() => Vibrate(50, 255);

    public static void Vibrate(long millis, int amplitude = 255)
    {
        if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var vibrator = GetVibrator();
            if (vibrator == null) return;

            int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

            // 진동 가능한지
            if (!vibrator.Call<bool>("hasVibrator")) return;

            if (sdk >= 26)
            {
                using var veClass = new AndroidJavaClass("android.os.VibrationEffect");

                // 기기가 amplitude 조절 지원하는지
                bool hasAmp = vibrator.Call<bool>("hasAmplitudeControl");
                int amp = hasAmp ? Mathf.Clamp(amplitude, 1, 255) : -1; // -1 = DEFAULT_AMPLITUDE

                using var effect = veClass.CallStatic<AndroidJavaObject>("createOneShot", millis, amp);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", millis);
            }
        }
        catch { /* no-op */ }
#endif
    }
}