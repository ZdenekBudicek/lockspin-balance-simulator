// Minimal UnityEngine stand-in so the real LockSpinSession source compiles outside Unity.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Abs(float v) => Math.Abs(v);
        public static int Abs(int v) => Math.Abs(v);
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Repeat(float t, float l) => Clamp(t - (float)Math.Floor(t / l) * l, 0, l);
        public static float DeltaAngle(float c, float t) { float d = Repeat(t - c, 360); if (d > 180) d -= 360; return d; }
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0;
        public static float SmoothStep(float from, float to, float t) { t = Clamp01(t); t = -2f * t * t * t + 3f * t * t; return to * t + from * (1f - t); }
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
    }

    public class Object { }
    public class ScriptableObject : Object { }
    [AttributeUsage(AttributeTargets.All)] public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
    [AttributeUsage(AttributeTargets.All)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
    public static class Resources { public static T Load<T>(string p) where T : class => null; }
    public static class Debug { public static void Log(object o) => Console.WriteLine(o); }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        public static int GetInt(string k, int d = 0) => ints.TryGetValue(k, out var v) ? v : d;
        public static void SetInt(string k, int v) => ints[k] = v;
        public static void Save() { }
        public static void DeleteKey(string k) => ints.Remove(k);
    }

    // Match the public-field JSON shape used by Unity without thread-local clone state.
    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object value) => System.Text.Json.JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
    }
}
