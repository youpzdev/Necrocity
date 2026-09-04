using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class GameSave
{
    public const int SchemaVersion = 1;

    public static class Keys
    {
        public const string Version = "schemaVersion";
        public const string Resources = "resources";
        public const string Components = "components";
        public const string Items = "items";
        public const string TimeOfDay = "timeOfDay";

        public static string Gainer(string id) => "gainer." + id;
    }

    [Serializable]
    public class GainerState
    {
        public int Level = 1;
        public float Progress;
        public bool Producing = true;
        public long SavedAtUtc;
    }

    private const string RootKey = "necrocity";

    private static readonly Dictionary<string, string> values = new();
    private static bool loaded;
    private static bool dirty;

    public static bool HasPendingChanges => dirty;

    public static T Get<T>(string key, T defaultValue = default)
    {
        EnsureLoaded();
        if (!values.TryGetValue(key, out var raw)) return defaultValue;

        try { return JsonConvert.DeserializeObject<T>(raw); }
        catch { return defaultValue; }
    }

    public static void Set<T>(string key, T value)
    {
        EnsureLoaded();

        string raw = JsonConvert.SerializeObject(value);
        if (values.TryGetValue(key, out var stored) && stored == raw) return;

        values[key] = raw;
        dirty = true;
    }

    public static bool Has(string key)
    {
        EnsureLoaded();
        return values.ContainsKey(key);
    }

    public static void Delete(string key)
    {
        EnsureLoaded();
        if (values.Remove(key)) dirty = true;
    }

    public static void DeleteAll()
    {
        EnsureLoaded();
        values.Clear();
        values[Keys.Version] = JsonConvert.SerializeObject(SchemaVersion);
        dirty = true;
        FlushNow();
    }

    public static void FlushNow()
    {
        EnsureLoaded();
        if (!dirty) return;

        dirty = false;
        Save.Set(RootKey, values);
    }

    public static long NowUtc() => DateTime.UtcNow.Ticks;

    public static double SecondsSince(long utcTicks)
    {
        if (utcTicks <= 0) return 0;

        double seconds = (DateTime.UtcNow - new DateTime(utcTicks, DateTimeKind.Utc)).TotalSeconds;
        return seconds > 0 ? seconds : 0;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        var stored = Save.Get<Dictionary<string, string>>(RootKey, null);
        if (stored != null)
        {
            foreach (var pair in stored) values[pair.Key] = pair.Value;
        }

        Migrate();
    }

    private static void Migrate()
    {
        int version = 0;
        if (values.TryGetValue(Keys.Version, out var raw))
        {
            try { version = JsonConvert.DeserializeObject<int>(raw); }
            catch { version = 0; }
        }

        if (version == SchemaVersion) return;

        if (version > SchemaVersion) values.Clear();

        values[Keys.Version] = JsonConvert.SerializeObject(SchemaVersion);
        dirty = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        values.Clear();
        loaded = false;
        dirty = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRunner()
    {
        var host = new GameObject("GameSave");
        host.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<Runner>();
    }

    private class Runner : MonoBehaviour
    {
        private void LateUpdate()
        {
            FlushNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushNow();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) FlushNow();
        }

        private void OnApplicationQuit()
        {
            FlushNow();
        }
    }
}
