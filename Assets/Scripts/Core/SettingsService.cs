using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Persistent player settings — audio volumes, camera-shake intensity, haptics — saved via
    /// PlayerPrefs (mirroring ScoreManager's IRONHOLD_-prefixed key pattern). <see cref="LoadAndApply"/>
    /// runs at boot so audio/shake/haptics reflect saved prefs from the first frame; each setter
    /// write-throughs and Save()s. Values are pushed into the managers through their static hooks, so
    /// this service needs no references to the (Bootstrap-owned) manager instances.
    /// </summary>
    public static class SettingsService
    {
        public const string KeyMaster  = "IRONHOLD_VOL_MASTER";
        public const string KeyMusic   = "IRONHOLD_VOL_MUSIC";
        public const string KeySfx     = "IRONHOLD_VOL_SFX";
        public const string KeyShake   = "IRONHOLD_SHAKE";
        public const string KeyHaptics = "IRONHOLD_HAPTICS";

        public static float Master  { get; private set; } = 1f;
        public static float Music   { get; private set; } = 0.8f;
        public static float Sfx     { get; private set; } = 1f;
        public static float Shake   { get; private set; } = 1f;
        public static bool  Haptics { get; private set; } = true;

        public static void LoadAndApply()
        {
            Master  = PlayerPrefs.GetFloat(KeyMaster, 1f);
            Music   = PlayerPrefs.GetFloat(KeyMusic, 0.8f);
            Sfx     = PlayerPrefs.GetFloat(KeySfx, 1f);
            Shake   = PlayerPrefs.GetFloat(KeyShake, 1f);
            Haptics = PlayerPrefs.GetInt(KeyHaptics, 1) != 0;

            AudioListener.volume  = Master;
            MusicManager.Volume01 = Music;
            SfxManager.BusVolume  = Sfx;
            GameConfig.ShakeScale = Shake;
            Rumble.Enabled        = Haptics;
        }

        public static void SetMaster(float v)  { Master = Mathf.Clamp01(v); AudioListener.volume = Master;  Persist(KeyMaster, Master); }
        public static void SetMusic(float v)   { Music  = Mathf.Clamp01(v); MusicManager.Volume01 = Music;  Persist(KeyMusic, Music); }
        public static void SetSfx(float v)     { Sfx    = Mathf.Clamp01(v); SfxManager.BusVolume  = Sfx;    Persist(KeySfx, Sfx); }
        public static void SetShake(float v)   { Shake  = Mathf.Clamp01(v); GameConfig.ShakeScale = Shake;  Persist(KeyShake, Shake); }

        public static void SetHaptics(bool on)
        {
            Haptics = on;
            Rumble.Enabled = on;
            PlayerPrefs.SetInt(KeyHaptics, on ? 1 : 0);
            PlayerPrefs.Save();
            if (on) Rumble.Light(); // confirmation buzz
        }

        private static void Persist(string key, float v)
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
        }
    }
}
