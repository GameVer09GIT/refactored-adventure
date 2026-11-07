using System.Collections;
using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Narrative;
using KhoaHon.Notebook;
using UnityEngine;

namespace KhoaHon.Sequences
{
    /// <summary>
    /// Cutscene của 3 kết thúc (đăng ký vào EndingDirector.CutsceneHook), chạy trước màn chữ kết:
    /// I — Giải thoát: thiêu kỷ vật, Cổ Vật vỡ, Lord tan biến, dinh thự sập, bình minh.
    /// II — Tân Chủ Nhân: đọc ngược bài chú thứ năm, mười ba oan hồn quỳ; vài tháng sau, bóng người ở cửa sổ tầng 2.
    /// Ẩn — Kẻ Hèn Nhát: lái xe lúc 2 giờ sáng, bóng quản gia ở ghế sau; phòng 13 bệnh viện tâm thần.
    /// </summary>
    public class EndingCutscenes : MonoBehaviour
    {
        [SerializeField] private RitualSequence ritual;
        [SerializeField] private GameObject carModel;
        [SerializeField] private Transform asylumCell;
        [SerializeField] private Vector3 houseView = new Vector3(80.7f, 12.2f, -26f);
        [SerializeField] private Vector3 houseTarget = new Vector3(88f, 16f, 14f);
        [SerializeField] private Vector3 windowPos = new Vector3(92.7f, 18.02f, 8.3f); // ô cửa tầng 2 không đóng ván
        [SerializeField] private AudioClip shatterClip, rumbleClip, whisperClip, engineLoop, stingClip, scratchClip, chainsClip;
        [SerializeField] private TalismanData fifthVerse;

        public void Configure(RitualSequence r, GameObject car, Transform cell, TalismanData verse,
            AudioClip shatter, AudioClip rumble, AudioClip whisper, AudioClip engine, AudioClip sting, AudioClip scratch, AudioClip chains)
        {
            ritual = r; carModel = car; asylumCell = cell; fifthVerse = verse;
            shatterClip = shatter; rumbleClip = rumble; whisperClip = whisper; engineLoop = engine; stingClip = sting; scratchClip = scratch; chainsClip = chains;
        }

        private void OnEnable() => EndingDirector.CutsceneHook = Play;
        private void OnDisable() { if (EndingDirector.CutsceneHook == (System.Func<EndingId, IEnumerator>)Play) EndingDirector.CutsceneHook = null; }

        private IEnumerator Play(EndingId id)
        {
            switch (id)
            {
                case EndingId.Release: yield return Release(); break;
                case EndingId.NewMaster: yield return NewMaster(); break;
                default: yield return Coward(); break;
            }
            CutsceneKit.SetFade(1f);
        }

        /// <summary>Góc máy nghiêng nhìn Cổ Vật (Lord ở hậu cảnh), tránh bị tường/cột che.</summary>
        private Camera RitualShot(Vector3 focus, float dist)
        {
            var lord = ritual != null ? ritual.Lord : null;
            var lordP = lord != null ? lord.position + Vector3.up * 1.5f : focus + Vector3.back;
            var dir = focus - lordP; dir.y = 0f; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, dir);
            var pos = focus + side * dist + dir * 0.9f + Vector3.up * 0.35f;
            var cam = CutsceneKit.Active ? CutsceneKit.Camera : CutsceneKit.Begin(pos, Quaternion.identity);
            cam.transform.SetParent(null);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Vector3.Lerp(focus, lordP, 0.25f) - pos));
            HorrorPostFX.CinematicFocus = false;
            return cam;
        }

        /// <summary>Tắt bớt sương thể tích cho cảnh ngoài trời có nắng (không thì tán xạ trắng xóa).</summary>
        private static GameObject ClearSky()
        {
            var go = new GameObject("KH_ClearSkyVolume");
            var v = go.AddComponent<UnityEngine.Rendering.Volume>(); v.isGlobal = true; v.priority = 90;
            var p = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            var f = p.Add<KhoaHon.Rendering.KHVolumetricFog>(true);
            f.density.value = 0.012f; f.mainLightIntensity.value = 0.35f; f.additionalLightsIntensity.value = 0.5f; f.baseHeight.value = 10f; f.heightFalloff.value = 0.4f;
            f.tint.value = new Color(1f, 0.85f, 0.75f);
            v.sharedProfile = p;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.55f, 0.45f, 0.42f);
            return go;
        }

        // ------------------------------------------------------------------ Kết 1
        private IEnumerator Release()
        {
            var relic = ritual != null ? ritual.Relic : null;
            var lord = ritual != null ? ritual.Lord : null;
            var focus = relic != null ? relic.position : transform.position;
            var cam = RitualShot(focus, 2.6f);
            yield return CutsceneKit.Line(T.Get("end.1.a"), 3.5f);
            // Vạc bùng lửa đỏ
            var fire = new GameObject("KH_CauldronFire").AddComponent<Light>();
            fire.transform.position = focus - Vector3.up * 1.0f; fire.type = LightType.Point; fire.range = 9f; fire.color = new Color(1f, 0.35f, 0.1f);
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { fire.intensity = Mathf.Lerp(0f, 18f, t / 1.5f) * (0.8f + 0.2f * Mathf.PerlinNoise(t * 20f, 0f)); yield return null; }
            yield return CutsceneKit.Line(T.Get("end.1.b"), 3f);
            // Búa giáng xuống — Cổ Vật vỡ
            if (shatterClip != null) AmbientScares.Play2D(shatterClip, 1f);
            if (chainsClip != null) AmbientScares.Play2D(chainsClip, 0.8f);
            CameraShake.Shake(2.5f, 0.07f);
            HorrorPostFX.Instance?.Flash(1f);
            if (relic != null) relic.gameObject.SetActive(false);
            yield return CutsceneKit.Fade(0.85f, 0.15f, Color.white);
            yield return CutsceneKit.Fade(0f, 1.2f, Color.white);
            // Lord tan thành tro
            if (lord != null)
                for (float t = 0f; t < 2f; t += Time.deltaTime) { lord.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, t / 2f); lord.position += Vector3.up * Time.deltaTime * 0.4f; yield return null; }
            yield return CutsceneKit.Line(T.Get("end.1.c"), 4f);
            if (rumbleClip != null) AmbientScares.Play2D(rumbleClip, 1f);
            yield return CutsceneKit.Fade(1f, 2f, Color.white);
            Destroy(fire.gameObject);

            // Bình minh ngoài dinh thự
            CutsceneKit.Camera.transform.SetPositionAndRotation(houseView, Quaternion.LookRotation(houseTarget - houseView));
            ClearSky();
            var sun = new GameObject("KH_Dawn").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.72f, 0.48f); sun.intensity = 0.9f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(8f, 60f, 0f);
            RenderSettings.ambientLight = new Color(0.16f, 0.13f, 0.14f);
            yield return CutsceneKit.Fade(0f, 2.5f, Color.white);
            if (rumbleClip != null) AmbientScares.Play2D(rumbleClip, 0.8f);
            CameraShake.Shake(3f, 0.03f);
            float k = 0f;
            var p0 = houseView;
            while (k < 6f) { k += Time.deltaTime; CutsceneKit.Camera.transform.position = p0 + new Vector3(0f, 0f, -k * 0.35f); yield return null; }
            yield return CutsceneKit.Line(T.Get("end.1.d"), 3.5f);
        }

        // ------------------------------------------------------------------ Kết 2
        private IEnumerator NewMaster()
        {
            var relic = ritual != null ? ritual.Relic : null;
            var focus = relic != null ? relic.position : transform.position;
            var cam = RitualShot(focus, 2.0f);
            yield return CutsceneKit.Line(T.Get("end.2.a"), 3.2f);
            HorrorPostFX.Instance?.Flash(0.8f);
            // Đọc ngược bài chú thứ năm
            var lines = fifthVerse != null ? fifthVerse.ChantLines : null;
            if (lines != null)
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (whisperClip != null) AmbientScares.Play2D(whisperClip, 0.5f);
                    var words = lines[i].TrimEnd('.').Split(' ');
                    System.Array.Reverse(words);
                    yield return CutsceneKit.Line("<color=#C03A30><i>" + string.Join(" ", words) + "</i></color>", 2.4f);
                    var a = cam.transform.position; cam.transform.position = Vector3.Lerp(a, focus, 0.12f);
                }
            CameraShake.Shake(2f, 0.05f);
            yield return CutsceneKit.Line(T.Get("end.2.b"), 4f);
            yield return CutsceneKit.Fade(1f, 2f);
            // Vài tháng sau
            yield return CutsceneKit.Line(T.Get("end.2.later"), 2.5f);
            var view = new Vector3(93f, 13.6f, -15f);
            cam.transform.SetPositionAndRotation(view, Quaternion.LookRotation(windowPos + Vector3.up * 0.9f - view));
            cam.fieldOfView = 38f;
            var fig = CutsceneKit.Silhouette(windowPos, view);
            var glow = new GameObject("KH_WindowGlow").AddComponent<Light>();
            glow.transform.position = windowPos + new Vector3(0f, 1.4f, 1.6f); glow.type = LightType.Point; glow.range = 7f; glow.intensity = 45f; glow.color = new Color(1f, 0.4f, 0.2f);
            GameObject car = null;
            if (carModel != null) { car = Instantiate(carModel, new Vector3(84f, 10f, -16f), Quaternion.Euler(0f, 20f, 0f)); foreach (var c in car.GetComponentsInChildren<Collider>()) Destroy(c); }
            yield return CutsceneKit.Fade(0f, 2f);
            yield return CutsceneKit.Wait(1.5f);
            float t2 = 0f;
            while (t2 < 3f) { t2 += Time.deltaTime; cam.fieldOfView = Mathf.Lerp(38f, 18f, t2 / 3f); yield return null; }
            yield return CutsceneKit.Line(T.Get("end.2.quote"), 3.5f, new Color(0.9f, 0.3f, 0.25f));
            Destroy(fig); Destroy(glow.gameObject); if (car != null) Destroy(car);
        }

        // ------------------------------------------------------------------ Kết ẩn
        private IEnumerator Coward()
        {
            yield return CutsceneKit.Fade(1f, 1f);
            var car = carModel != null ? Instantiate(carModel) : new GameObject("Car");
            foreach (var c in car.GetComponentsInChildren<Collider>()) Destroy(c);
            car.transform.SetPositionAndRotation(new Vector3(86f, 10f, -18f), Quaternion.Euler(0f, -90f, 0f));
            var hl = new GameObject("Headlight").AddComponent<Light>();
            hl.transform.SetParent(car.transform, false); hl.transform.localPosition = new Vector3(0f, 0.7f, 2.4f);
            hl.type = LightType.Spot; hl.range = 35f; hl.spotAngle = 55f; hl.intensity = 25f; hl.color = new Color(1f, 0.92f, 0.78f);
            var cam = CutsceneKit.Begin(car.transform.TransformPoint(new Vector3(-0.38f, 1.18f, -0.15f)), car.transform.rotation, 62f);
            cam.transform.SetParent(car.transform, true);
            var eng = car.AddComponent<AudioSource>(); eng.clip = engineLoop; eng.loop = true; eng.volume = 0.5f * AudioMix.Sfx; if (engineLoop != null) eng.Play();
            yield return CutsceneKit.Fade(0f, 1.5f);
            yield return CutsceneKit.Line(T.Get("end.3.a"), 3f);
            float t = 0f;
            while (t < 4f) { t += Time.deltaTime; car.transform.position += car.transform.forward * 9f * Time.deltaTime; yield return null; }
            // Liếc gương chiếu hậu… quay lại ghế sau
            var ghost = CutsceneKit.Silhouette(car.transform.TransformPoint(new Vector3(0.35f, 0.15f, -0.9f)), car.transform.position + car.transform.forward * 5f);
            ghost.transform.SetParent(car.transform, true);
            ghost.transform.localScale = Vector3.one * 0.62f;
            var r0 = cam.transform.localRotation;
            t = 0f;
            while (t < 2.2f) { t += Time.deltaTime; car.transform.position += car.transform.forward * 9f * Time.deltaTime; cam.transform.localRotation = Quaternion.Slerp(r0, Quaternion.Euler(10f, 160f, 0f), Mathf.SmoothStep(0, 1, t / 2.2f)); yield return null; }
            if (stingClip != null) AmbientScares.Play2D(stingClip, 1f);
            HorrorPostFX.Instance?.Flash(1f);
            CameraShake.Shake(0.6f, 0.05f);
            yield return CutsceneKit.Wait(0.8f);
            CutsceneKit.SetFade(1f);
            eng.Stop();
            cam.transform.SetParent(null); // camera đang là con của xe — tách ra trước khi xóa xe
            Destroy(car);
            yield return CutsceneKit.Wait(1.2f);

            // Phòng 13
            if (asylumCell != null)
            {
                var cell = CutsceneKit.Camera;
                var p = asylumCell.position + new Vector3(0f, 1.6f, -2.2f);
                cell.transform.SetParent(null);
                cell.transform.SetPositionAndRotation(p, Quaternion.LookRotation(asylumCell.position + new Vector3(0f, 1.2f, 2f) - p));
                var src = asylumCell.gameObject.AddComponent<AudioSource>(); src.clip = scratchClip; src.loop = true; src.spatialBlend = 0f; src.volume = 0.6f * AudioMix.Sfx;
                if (scratchClip != null) src.Play();
                yield return CutsceneKit.Fade(0f, 2f);
                yield return CutsceneKit.Line(T.Get("end.3.b"), 4f);
                t = 0f;
                while (t < 3f) { t += Time.deltaTime; cell.transform.position += cell.transform.forward * 0.15f * Time.deltaTime; yield return null; }
                src.Stop();
            }
        }
    }
}
