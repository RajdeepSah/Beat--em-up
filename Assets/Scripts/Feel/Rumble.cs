using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Haptic feedback on Android (VibrationEffect one-shots with amplitude on API 26+,
    /// legacy vibrate below). Hard no-op in the editor and on other platforms. Failures are
    /// swallowed once and disable the system — haptics must never throw during combat.
    /// </summary>
    public static class Rumble
    {
        /// <summary>Master haptics toggle (Settings screen). When false all vibration is suppressed.</summary>
        public static bool Enabled = true;

        public static void Light() => Vibrate(15, 80);
        public static void Heavy() => Vibrate(30, 160);
        public static void Hurt() => Vibrate(40, 255);
        public static void GuardBreak() => Vibrate(60, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject s_Vibrator;
        private static int s_Sdk = -1;
        private static bool s_Broken;

        private static void Vibrate(long ms, int amplitude)
        {
            if (!Enabled || s_Broken) return;
            try
            {
                if (s_Vibrator == null)
                {
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    s_Sdk = version.GetStatic<int>("SDK_INT");
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    s_Vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    if (s_Vibrator == null) { s_Broken = true; return; }
                }

                if (s_Sdk >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    s_Vibrator.Call("vibrate", effect);
                }
                else
                {
                    s_Vibrator.Call("vibrate", ms);
                }
            }
            catch
            {
                s_Broken = true;
            }
        }
#else
        private static void Vibrate(long ms, int amplitude) { }
#endif
    }
}
