using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LangTroTan.Presentation
{
    // Presentation-only time of day for the Sơn Môn slice. Follows the server's spatial clock:
    // phase 0..1 per game day starts at dawn, and night begins at phase >= daylight(season), the same
    // table as server/unity/spatial.js. Nothing here feeds back into gameplay.
    public static class SonMonSky
    {
        static readonly float[] Daylight = { 2f / 3f, .75f, 220f / 360f, .5f };

        // QA / preview override of the clock (null = follow the server).
        public static float? PreviewPhase;
        public static int? PreviewSeason;

        static Volume volume;
        static Bloom bloom;
        static ParticleSystem fireflies;
        static bool searched;

        public struct Look
        {
            public Quaternion sun;
            public Color sunColor, ambientSky, ambientEquator, ambientGround, fog;
            public float sunIntensity, glow, bloomIntensity;
            public Color cloudTint;
            public Material sky;
            public bool night;
        }

        public static Look Evaluate(SonMonKit kit, float phase, int season, bool serverNight, string weather)
        {
            if (PreviewPhase.HasValue) phase = PreviewPhase.Value;
            if (PreviewSeason.HasValue) season = PreviewSeason.Value;
            float daylight = Daylight[Mathf.Clamp(season, 0, 3)];
            bool night = PreviewPhase.HasValue ? phase >= daylight : serverNight;
            float storm = weather == "storm" ? .55f : weather == "rain" || weather == "snow" ? .78f : 1f;
            var look = new Look { night = night };
            if (!night)
            {
                float t = Mathf.Clamp01(phase / daylight);                      // 0 dawn .. 1 dusk
                float golden = 1 - Mathf.Clamp01(Mathf.Min(t, 1 - t) / .16f);    // 1 near dawn/dusk
                float elevation = Mathf.Lerp(6, 58, Mathf.Sin(t * Mathf.PI));
                look.sun = Quaternion.Euler(elevation, Mathf.Lerp(-115, 75, t), 0);
                look.sunColor = Color.Lerp(new Color(1f, .93f, .82f), new Color(1f, .62f, .38f), golden);
                look.sunIntensity = Mathf.Lerp(1.45f, 1.2f, golden) * storm;
                look.cloudTint = Color.Lerp(Color.white, new Color(1f, .82f, .72f), golden);
                look.ambientSky = Color.Lerp(new Color(.62f, .74f, .86f), new Color(.86f, .72f, .7f), golden);
                look.ambientEquator = Color.Lerp(new Color(.58f, .64f, .6f), new Color(.8f, .66f, .56f), golden);
                look.ambientGround = new Color(.3f, .34f, .3f);
                look.fog = Color.Lerp(new Color(.72f, .82f, .86f), new Color(.92f, .72f, .62f), golden);
                look.glow = Mathf.Lerp(1f, 1.8f, golden);
                look.bloomIntensity = Mathf.Lerp(.18f, .35f, golden);
                look.sky = golden > .45f && kit.dawnSky != null ? kit.dawnSky : kit.daySky;
            }
            else
            {
                look.sun = Quaternion.Euler(42, 140, 0);                          // moonlight
                look.sunColor = new Color(.55f, .68f, 1f);
                look.sunIntensity = .38f * storm;
                look.cloudTint = new Color(.2f, .26f, .38f);
                look.ambientSky = new Color(.2f, .27f, .42f);
                look.ambientEquator = new Color(.16f, .2f, .3f);
                look.ambientGround = new Color(.07f, .08f, .1f);
                look.fog = new Color(.12f, .17f, .26f);
                look.glow = 3.2f;
                look.bloomIntensity = .75f;
                look.sky = kit.nightSky;
            }
            return look;
        }

        public static void Apply(SonMonKit kit, Look look, Light sun, Camera cam, float blend)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Slerp(sun.transform.rotation, look.sun, blend);
                sun.color = Color.Lerp(sun.color, look.sunColor, blend);
                sun.intensity = Mathf.Lerp(sun.intensity, look.sunIntensity, blend);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, look.ambientSky, blend);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, look.ambientEquator, blend);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, look.ambientGround, blend);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, look.fog, blend);
            Shader.SetGlobalColor("_XianxiaFog", RenderSettings.fogColor);
            if (cam != null) cam.backgroundColor = RenderSettings.fogColor;
            if (look.sky != null && RenderSettings.skybox != look.sky) RenderSettings.skybox = look.sky;

            if (kit.cloudMaterial != null)
            {
                var target = kit.cloudDayColor * look.cloudTint;
                target.a = kit.cloudDayColor.a;
                kit.cloudMaterial.SetColor("_BaseColor", Color.Lerp(kit.cloudMaterial.GetColor("_BaseColor"), target, blend));
            }
            foreach (var entry in kit.glowMaterials)
            {
                if (entry.material == null) continue;
                entry.material.SetColor("_EmissionColor", entry.baseEmission * look.glow);
            }

            if (!searched)
            {
                searched = true;
                foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                    if (v.isGlobal && v.profile != null && v.profile.TryGet(out bloom)) { volume = v; break; }
                var go = GameObject.Find("SonMon Night fireflies");
                if (go != null) fireflies = go.GetComponent<ParticleSystem>();
            }
            if (bloom != null) bloom.intensity.Override(Mathf.Lerp(bloom.intensity.value, look.bloomIntensity, blend));
            if (fireflies != null)
            {
                var emission = fireflies.emission;
                emission.enabled = look.night;
            }
        }

        // Call when the scene is rebuilt so cached scene objects are looked up again.
        public static void Reset()
        {
            searched = false;
            volume = null;
            bloom = null;
            fireflies = null;
        }
    }

    // Slow rotation (formation circles) with a gentle breathing glow scale.
    public sealed class SonMonSpin : MonoBehaviour
    {
        public float degreesPerSecond = 6;
        public float pulse = .03f;
        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            transform.Rotate(0, degreesPerSecond * Time.deltaTime, 0, Space.World);
            transform.localScale = baseScale * (1 + Mathf.Sin(Time.time * 1.3f) * pulse);
        }
    }
}
