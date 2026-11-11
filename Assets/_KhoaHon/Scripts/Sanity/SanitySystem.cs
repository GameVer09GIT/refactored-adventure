using System.Collections;
using System.Collections.Generic;
using HorrorGame.Inventory;
using KhoaHon.AI;
using KhoaHon.Core;
using KhoaHon.Horror;
using UnityEngine;

namespace KhoaHon.Sanity
{
    /// <summary>
    /// Lý trí ẩn: tính mỗi nhịp từ ánh sáng quanh người chơi, việc nhìn thẳng Mrs. Peake, bị truy đuổi, trốn.
    /// Giá trị lưu ở GameManager.Sanity (các hệ thống khác cộng/trừ trực tiếp). Không có thanh hiển thị —
    /// thể hiện qua hậu kỳ (HorrorPostFX đọc Sanity), nhịp tim, ảo thanh và bóng người thoáng qua ở rìa tầm nhìn.
    /// </summary>
    public class SanitySystem : MonoBehaviour
    {
        public static SanitySystem Instance { get; private set; }

        [SerializeField] private float tick = 0.25f;
        [SerializeField] private float seeDistance = 14f, seeHalfAngle = 22f;
        [SerializeField] private AudioClip whisperClip, keysClip;

        private readonly SanityModel model = new SanityModel();
        private readonly List<Light> sceneLights = new List<Light>();
        private PlayerController player;
        private float acc, nextHallucination;
        private GameObject phantom;

        public int Tier => model.Tier;

        public void Configure(AudioClip whisper, AudioClip keys) { whisperClip = whisper; keysClip = keys; }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type != LightType.Directional && !l.name.StartsWith("Flashlight") && !l.name.StartsWith("KH_")) sceneLights.Add(l);
            nextHallucination = Time.time + 40f;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || player == null || !GameFlow.IsPlaying) return;
            acc += Time.deltaTime;
            if (acc < tick) return;
            float dt = acc; acc = 0f;

            model.Set(gm.Sanity); // hệ thống khác có thể đã cộng/trừ
            var hk = HousekeeperAI.Instance;
            var input = new SanityInput
            {
                inLight = InLight(),
                seeingHousekeeper = SeeingHousekeeper(hk),
                hunted = hk != null && (hk.State == HousekeeperState.Hunt || hk.State == HousekeeperState.Kill),
                hiding = PlayerState.IsHiding,
            };
            if (model.Step(input, dt)) GameEvents.RaiseSanityTierChanged(model.Tier);
            gm.Sanity = model.Value;

            if (model.Tier >= 2 && Time.time >= nextHallucination && !PlayerState.IsHiding)
            {
                nextHallucination = Time.time + (model.Tier == 3 ? Random.Range(18f, 35f) : Random.Range(35f, 70f));
                if (Random.value < 0.55f) StartCoroutine(Phantom());
                else FakeKeys();
            }
        }

        private bool InLight()
        {
            var held = HotbarInventory.Instance != null ? HotbarInventory.Instance.CurrentHeldItem as FlashlightItem : null;
            if (held != null && held.IsOn) return true;
            var p = player.transform.position;
            foreach (var l in sceneLights)
            {
                if (l == null || !l.isActiveAndEnabled || l.intensity < 0.3f) continue;
                if ((l.transform.position - p).sqrMagnitude < l.range * l.range * 0.25f) return true;
            }
            return false;
        }

        private bool SeeingHousekeeper(HousekeeperAI hk)
        {
            if (hk == null || !hk.gameObject.activeInHierarchy || player.playerCamera == null) return false;
            var cam = player.playerCamera;
            var to = hk.transform.position + Vector3.up * 1.5f - cam.position;
            if (to.magnitude > seeDistance || Vector3.Angle(cam.forward, to) > seeHalfAngle) return false;
            return !Physics.Raycast(cam.position, to.normalized, out var hit, to.magnitude - 0.3f, ~0, QueryTriggerInteraction.Ignore)
                   || hit.transform.root == hk.transform.root;
        }

        // ------------------------------------------------------------------ Ảo giác
        /// <summary>Bóng người đứng im ở rìa tầm nhìn; biến mất khi người chơi quay sang nhìn hoặc sau vài giây.</summary>
        private IEnumerator Phantom()
        {
            var cam = player.playerCamera;
            if (cam == null || phantom != null) yield break;
            float side = Random.value < 0.5f ? -1f : 1f;
            var dir = Quaternion.Euler(0f, side * Random.Range(55f, 75f), 0f) * Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var start = player.transform.position + Vector3.up * 1f;
            float dist = Random.Range(6f, 10f);
            if (Physics.Raycast(start, dir, out var wall, dist, ~0, QueryTriggerInteraction.Ignore)) dist = wall.distance - 0.6f;
            if (dist < 3f) yield break;
            var pos = start + dir * dist;
            if (!Physics.Raycast(pos, Vector3.down, out var floor, 3f, ~0, QueryTriggerInteraction.Ignore)) yield break;

            phantom = BuildPhantom(floor.point, player.transform.position);
            if (whisperClip != null) AudioSource.PlayClipAtPoint(whisperClip, floor.point, 0.5f);
            float t = 0f;
            while (t < 3.5f && phantom != null)
            {
                t += Time.deltaTime;
                var to = phantom.transform.position + Vector3.up - cam.position;
                if (Vector3.Angle(cam.forward, to) < 18f && t > 0.2f) { HorrorPostFX.Instance?.Flash(0.25f); break; }
                yield return null;
            }
            if (phantom != null) Destroy(phantom);
            phantom = null;
        }

        private static GameObject BuildPhantom(Vector3 feet, Vector3 lookAt)
        {
            var root = new GameObject("KH_Phantom");
            root.transform.position = feet;
            var face = lookAt - feet; face.y = 0f;
            if (face.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(face);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.01f, 0.01f, 0.012f) };
            void Part(PrimitiveType t, Vector3 p, Vector3 s)
            {
                var g = GameObject.CreatePrimitive(t);
                Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = p; g.transform.localScale = s;
                var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Part(PrimitiveType.Capsule, new Vector3(0, 0.85f, 0), new Vector3(0.42f, 0.85f, 0.32f));
            Part(PrimitiveType.Sphere, new Vector3(0, 1.72f, 0), new Vector3(0.24f, 0.28f, 0.24f));
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.6f, 0.35f, 0.6f)); // váy dài
            return root;
        }

        /// <summary>Ảo thanh: tiếng chìa khóa leng keng ở một bên tai dù bà ta không ở đó.</summary>
        private void FakeKeys()
        {
            if (keysClip == null) return;
            var go = new GameObject("KH_FakeKeys");
            var a = go.AddComponent<AudioSource>();
            a.clip = keysClip; a.spatialBlend = 0f; a.panStereo = Random.value < 0.5f ? -0.85f : 0.85f;
            a.volume = 0.45f * AudioMix.Sfx; a.time = Random.Range(0f, Mathf.Max(0f, keysClip.length - 2.5f));
            a.Play();
            Destroy(go, 2.5f);
        }
    }
}
