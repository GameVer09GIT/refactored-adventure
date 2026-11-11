using System;
using System.Collections;
using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace KhoaHon.Sequences
{
    /// <summary>
    /// Giai đoạn 0 — "Chiếc bẫy không lối thoát": ngồi trong xe trước cổng dinh thự giữa đêm mưa, giữ W để lái đi.
    /// Lần 1: bóng phụ nữ giữa đường → phanh → tối sầm → tỉnh lại trước cổng.
    /// Lần 2: biển báo đổi thành tên người mất tích, radio phát tiếng khóc → lại về cổng.
    /// Lần 3: động cơ chết máy, cổng sắt tự mở → bước vào sân.
    /// Nhìn quanh bằng chuột; S/Space để phanh. Có thể bỏ qua bằng giữ Esc 1 giây (chỉ khi đã từng xem).
    /// </summary>
    public class GateLoopSequence : MonoBehaviour
    {
        public static GateLoopSequence Instance { get; private set; }

        [Header("Đường đi (z = const, lái về phía tây)")]
        [SerializeField] private Vector3 start = new Vector3(86f, 10f, -18f);
        [SerializeField] private float endX = 46f;
        [SerializeField] private GameObject carModel;
        [SerializeField] private GameObject signModel;
        [SerializeField] private Transform[] gateDoors = new Transform[0];
        [SerializeField] private AudioClip engineLoop, brakeClip, engineDieClip, radioClip, gateClip, stingerClip;

        private Transform car;
        private Light[] headlights;
        private AudioSource engine, radio;
        private float yaw, pitch, speed;
        private GameObject[] signs;
        private TextMeshPro promptText;

        public void Configure(GameObject carPrefab, GameObject sign, Transform[] doors, AudioClip eng, AudioClip brake, AudioClip die, AudioClip radioC, AudioClip gate, AudioClip sting)
        {
            carModel = carPrefab; signModel = sign; gateDoors = doors; engineLoop = eng; brakeClip = brake; engineDieClip = die; radioClip = radioC; gateClip = gate; stingerClip = sting;
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool ShouldRun => GameManager.Instance != null && !GameManager.Instance.Has(StoryFlag.GateLoop3);

        public void Run(Action done) => StartCoroutine(Sequence(done));

        private IEnumerator Sequence(Action done)
        {
            GameFlow.Set(FlowState.Intro);
            BuildCar();
            var cam = CutsceneKit.Begin(car.TransformPoint(new Vector3(-0.38f, 1.18f, -0.15f)), car.rotation, 62f, false);
            cam.transform.SetParent(car, true);
            CutsceneKit.SetFade(1f);
            float oldFog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = 0.035f;
            var fogVol = ThickFog(0.11f);

            for (int loop = 1; loop <= 3; loop++)
            {
                ResetCar();
                yield return CutsceneKit.Fade(0f, 1.6f);
                if (loop == 1) yield return CutsceneKit.Line(T.Get("gate.hint"), 3.2f);
                if (loop == 2) yield return CutsceneKit.Line(T.Get("gate.again"), 2.6f);
                if (loop == 3)
                {
                    yield return Loop3();
                    break;
                }
                yield return Drive(loop);
                GameManager.Instance?.SetFlag(loop == 1 ? StoryFlag.GateLoop1 : StoryFlag.GateLoop2);
            }

            // Bước ra khỏi xe, vào sân
            GameManager.Instance?.SetFlag(StoryFlag.GateLoop3);
            yield return CutsceneKit.Fade(1f, 1.2f);
            RenderSettings.fogDensity = oldFog;
            if (fogVol != null) Destroy(fogVol);
            if (engine != null) engine.Stop();
            if (radio != null) radio.Stop();
            CutsceneKit.End(false);
            car.gameObject.SetActive(true); // xe để lại trước cổng
            foreach (var l in headlights) if (l != null) l.enabled = false;
            if (signs != null) foreach (var s in signs) if (s != null) Destroy(s);
            CutsceneKit.SetFade(0f);
            done?.Invoke();
        }

        private void BuildCar()
        {
            if (car != null) return;
            car = (carModel != null ? Instantiate(carModel) : GameObject.CreatePrimitive(PrimitiveType.Cube)).transform;
            car.name = "KH_IntroCar";
            foreach (var col in car.GetComponentsInChildren<Collider>()) Destroy(col);
            headlights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Headlight" + i);
                go.transform.SetParent(car, false);
                go.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 0.65f, 2.3f);
                go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot; l.range = 38f; l.spotAngle = 48f; l.innerSpotAngle = 18f; l.intensity = 22f;
                l.color = new Color(1f, 0.93f, 0.78f); l.shadows = LightShadows.Soft;
                headlights[i] = l;
            }
            var dash = new GameObject("DashGlow").AddComponent<Light>();
            dash.transform.SetParent(car, false); dash.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.5f);
            dash.type = LightType.Point; dash.range = 0.9f; dash.intensity = 0.06f; dash.color = new Color(0.35f, 0.6f, 1f);
            engine = car.gameObject.AddComponent<AudioSource>(); engine.clip = engineLoop; engine.loop = true; engine.spatialBlend = 0f; engine.volume = 0.5f * AudioMix.Sfx;
            radio = car.gameObject.AddComponent<AudioSource>(); radio.loop = true; radio.spatialBlend = 0f; radio.volume = 0f;
        }

        private void ResetCar()
        {
            car.SetPositionAndRotation(start, Quaternion.Euler(0f, -90f, 0f)); // mũi xe hướng tây
            yaw = 0f; pitch = 0f; speed = 0f;
            foreach (var l in headlights) l.enabled = true;
            if (engineLoop != null && !engine.isPlaying) engine.Play();
        }

        private void LookAround()
        {
            var m = Mouse.current;
            if (m != null)
            {
                var d = m.delta.ReadValue() * 0.08f;
                yaw = Mathf.Clamp(yaw + d.x, -75f, 75f);
                pitch = Mathf.Clamp(pitch - d.y, -25f, 25f);
            }
            var cam = CutsceneKit.Camera;
            if (cam != null) cam.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private bool Accel => Keyboard.current != null && Keyboard.current.wKey.isPressed;
        private bool Brake => Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.spaceKey.isPressed);

        /// <summary>Lái tới cuối đường; trả về khi sự kiện của vòng lặp kết thúc.</summary>
        private IEnumerator Drive(int loop)
        {
            GameObject woman = null;
            bool eventFired = false;
            if (loop == 2)
            {
                SpawnSigns();
                if (radioClip != null) { radio.clip = radioClip; radio.Play(); }
            }
            float idle = 0f;
            while (true)
            {
                LookAround();
                float target = Brake ? 0f : (Accel ? 13f : speed);
                // Không bấm gì lâu quá thì xe tự lăn bánh để không kẹt
                idle = Accel || Brake ? 0f : idle + Time.deltaTime;
                if (idle > 6f) target = 7f;
                speed = Mathf.MoveTowards(speed, target, Time.deltaTime * (Brake ? 14f : 4f));
                car.position += car.forward * speed * Time.deltaTime;
                engine.pitch = 0.7f + speed / 13f * 0.8f;
                float x = car.position.x;

                if (loop == 1)
                {
                    if (woman == null && x < start.x - 18f)
                        woman = CutsceneKit.Silhouette(new Vector3(x - 14f, start.y, start.z), car.position);
                    if (woman != null && !eventFired && Vector3.Distance(car.position, woman.transform.position) < 9f)
                    {
                        eventFired = true;
                        if (brakeClip != null) AmbientScares.Play2D(brakeClip, 1f);
                        if (stingerClip != null) AmbientScares.Play2D(stingerClip, 0.9f);
                        HorrorPostFX.Instance?.Flash(1f);
                        CameraShake.Shake(0.8f, 0.04f);
                        float t = 0f;
                        while (t < 0.9f) { t += Time.deltaTime; speed = Mathf.MoveTowards(speed, 0f, Time.deltaTime * 25f); car.position += car.forward * speed * Time.deltaTime; yield return null; }
                        CutsceneKit.SetFade(1f);
                        engine.Stop();
                        if (woman != null) Destroy(woman);
                        yield return CutsceneKit.Wait(1.5f);
                        yield return CutsceneKit.Line(T.Get("gate.wake"), 2.8f);
                        yield break;
                    }
                }
                else if (loop == 2)
                {
                    radio.volume = Mathf.MoveTowards(radio.volume, 0.6f * AudioMix.Sfx, Time.deltaTime * 0.3f);
                    if (x < endX + 4f)
                    {
                        yield return CutsceneKit.Fade(1f, 1.4f);
                        radio.Stop(); engine.Stop();
                        if (signs != null) foreach (var s in signs) if (s != null) Destroy(s);
                        yield return CutsceneKit.Line(T.Get("gate.wake2"), 2.8f);
                        yield break;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator Loop3()
        {
            yield return CutsceneKit.Line(T.Get("gate.try"), 2.2f);
            float t = 0f;
            while (t < 2.5f) { t += Time.deltaTime; LookAround(); speed = Mathf.MoveTowards(speed, 5f, Time.deltaTime * 3f); car.position += car.forward * speed * Time.deltaTime; yield return null; }
            // Chết máy: đèn chập chờn rồi tắt
            engine.Stop();
            if (engineDieClip != null) AmbientScares.Play2D(engineDieClip, 1f);
            for (int i = 0; i < 6; i++) { foreach (var l in headlights) l.enabled = i % 2 == 1; yield return new WaitForSeconds(0.12f + i * 0.04f); }
            foreach (var l in headlights) l.enabled = false;
            t = 0f;
            while (t < 1.2f) { t += Time.deltaTime; LookAround(); speed = Mathf.MoveTowards(speed, 0f, Time.deltaTime * 6f); car.position += car.forward * speed * Time.deltaTime; yield return null; }
            yield return CutsceneKit.Line(T.Get("gate.dead"), 2.6f);
            // Cổng sắt tự mở
            if (gateClip != null) AmbientScares.Play2D(gateClip, 1f);
            t = 0f;
            var baseRot = new Quaternion[gateDoors.Length];
            for (int i = 0; i < gateDoors.Length; i++) if (gateDoors[i] != null) baseRot[i] = gateDoors[i].localRotation;
            while (t < 3f)
            {
                t += Time.deltaTime; LookAround();
                for (int i = 0; i < gateDoors.Length; i++)
                    if (gateDoors[i] != null) gateDoors[i].localRotation = baseRot[i] * Quaternion.Euler(0f, (i % 2 == 0 ? -1f : 1f) * Mathf.SmoothStep(0f, 80f, t / 3f), 0f);
                yield return null;
            }
            yield return CutsceneKit.Line(T.Get("gate.open"), 3f);
        }

        private void SpawnSigns()
        {
            string[] names = { T.Get("gate.sign.1"), T.Get("gate.sign.2"), T.Get("gate.sign.3") };
            signs = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var pos = new Vector3(start.x - 14f - i * 9f, start.y, start.z - 2.6f);
                var s = signModel != null ? Instantiate(signModel) : new GameObject("Sign");
                s.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 90f, 0f)); // bảng quay về phía xe đang tới
                var tgo = new GameObject("Text");
                tgo.transform.SetParent(s.transform, false);
                tgo.transform.localPosition = new Vector3(0f, 1.85f, -0.09f);
                tgo.transform.localRotation = Quaternion.identity;
                var t = tgo.AddComponent<TextMeshPro>();
                var style = GameManager.Instance != null ? GameManager.Instance.Style : null;
                if (style != null) t.font = style.sansBoldFont ?? style.sansFont;
                t.text = names[i]; t.fontSize = 3.2f; t.alignment = TextAlignmentOptions.Center; t.color = new Color(0.95f, 0.95f, 0.9f);
                t.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);
                signs[i] = s;
            }
        }

        private static GameObject ThickFog(float density)
        {
            var go = new GameObject("KH_IntroFogVolume");
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 80;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var f = p.Add<KHVolumetricFog>(true);
            f.density.value = density; f.baseHeight.value = 10f; f.heightFalloff.value = 0.25f; f.maxDistance.value = 60f;
            f.additionalLightsIntensity.value = 1.6f; f.mainLightIntensity.value = 1.2f; f.noiseIntensity.value = 0.75f;
            f.tint.value = new Color(0.75f, 0.8f, 0.88f);
            v.sharedProfile = p;
            return go;
        }
    }
}
