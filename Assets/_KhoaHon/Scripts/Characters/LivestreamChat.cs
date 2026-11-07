using System.Collections.Generic;
using KhoaHon.AI;
using KhoaHon.Core;
using KhoaHon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Khung livestream của Alex (góc trái màn hình): nút LIVE, số người xem, luồng chat.
    /// Cầm điện thoại thì phóng to. Tài khoản ma (không UID, chữ đỏ) báo trước khi Mrs. Peake đến gần.
    /// Chỉ hoạt động khi nhân vật là Alex và đang chơi.
    /// </summary>
    public class LivestreamChat : MonoBehaviour
    {
        public static LivestreamChat Instance { get; private set; }

        [SerializeField] private float minInterval = 2.2f, maxInterval = 5.5f;
        [SerializeField] private float nearDistance = 14f;

        private static readonly string[] Handles =
        {
            "nightowl_88", "skeptic_steve", "ghostgirl_x", "Brennan_TV", "lil_mothman", "cryptid_carl", "jess.exe", "hauntedhank",
            "VanceFan01", "xXspookXx", "doubter_dan", "midnight_mae", "paranormal_pete", "ashford_local", "kat_in_the_dark", "rob_streams",
        };
        private static readonly string[] GhostHandles = { "[không UID]", "[no UID]", "????", "_ _ _", "ELEANOR", "13", "the_keeper" };

        private RectTransform panel, list;
        private TextMeshProUGUI viewersText, liveText, handleText;
        private readonly List<TextMeshProUGUI> lines = new List<TextMeshProUGUI>();
        private readonly Queue<string> history = new Queue<string>();
        private bool expanded;
        private float nextMsg, viewers = 312f;
        private System.Random rng = new System.Random();
        private Dictionary<string, int> poolSizes = new Dictionary<string, int>();
        private HousekeeperState lastHkState;
        private float lastDist = 999f;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private bool ActiveForCharacter => GameManager.Instance != null && GameManager.Instance.Character == PlayerCharacter.Alex;

        private void Start()
        {
            Build();
            GameEvents.ItemCollected += OnItem;
            GameEvents.PlayerCaught += OnCaught;
            Loc.Changed += ResetPools;
        }

        private void OnDisable()
        {
            GameEvents.ItemCollected -= OnItem;
            GameEvents.PlayerCaught -= OnCaught;
            Loc.Changed -= ResetPools;
        }

        private void ResetPools() => poolSizes.Clear();

        private void Build()
        {
            var hud = KhoaHonHUD.Instance;
            if (hud == null) return;
            panel = UIFactory.Rect("KH_Livestream", hud.Root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 170), new Vector2(470, 470));
            var bg = panel.gameObject.AddComponent<Image>(); bg.color = new Color(0.02f, 0.02f, 0.03f, 0.55f);
            var top = UIFactory.Rect("Top", panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -44), new Vector2(-14, -8));
            var badge = UIFactory.Panel("LiveBadge", top, new Color(0.85f, 0.08f, 0.08f, 1f), new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(70, 0));
            liveText = UIKit.Label(badge.transform, "LIVE", 20, Color.white, TextAlignmentOptions.Center, false, true);
            viewersText = UIKit.Label(top, "", 20, UIKit.Ink, TextAlignmentOptions.MidlineLeft, false, true);
            viewersText.rectTransform.offsetMin = new Vector2(84, 0);
            handleText = UIKit.Label(top, "VanceAfterDark", 18, UIKit.Dim, TextAlignmentOptions.MidlineRight);
            list = UIFactory.Rect("List", panel, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -52));
            var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.LowerLeft; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 4;
            for (int i = 0; i < 9; i++)
            {
                var row = UIKit.Row(list, 30);
                var t = UIKit.Label(row, "", 19, UIKit.Ink, TextAlignmentOptions.BottomLeft);
                t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
                lines.Add(t);
            }
            panel.gameObject.SetActive(false);
            SetExpanded(false);
        }

        public void SetExpanded(bool on)
        {
            expanded = on;
            if (panel == null) return;
            panel.offsetMax = on ? new Vector2(520, 640) : new Vector2(430, 420);
            panel.GetComponent<Image>().color = on ? new Color(0.02f, 0.02f, 0.03f, 0.8f) : new Color(0.02f, 0.02f, 0.03f, 0.4f);
            for (int i = 0; i < lines.Count; i++) lines[i].transform.parent.gameObject.SetActive(on || i >= lines.Count - 5);
            if (handleText != null) handleText.gameObject.SetActive(on);
        }

        private void Update()
        {
            if (panel == null) { Build(); if (panel == null) return; }
            bool show = ActiveForCharacter && GameFlow.State == FlowState.Playing;
            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
            if (!show) return;

            var threat = CurrentThreat();
            var stage = GameManager.Instance.Stage;
            float target = ChatDirector.TargetViewers(stage, threat);
            viewers = Mathf.MoveTowards(viewers, target, Time.deltaTime * 25f) + (Mathf.PerlinNoise(Time.time * 0.3f, 1f) - 0.5f) * 2f;
            viewersText.text = T.Format("chat.viewers", Mathf.Max(1, Mathf.RoundToInt(viewers)));
            liveText.alpha = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 2f));

            // Tài khoản ma lên tiếng ngay khi bà ta bắt đầu săn
            var hk = HousekeeperAI.Instance;
            if (hk != null && hk.State != lastHkState)
            {
                if (hk.State == HousekeeperState.Hunt && ChatDirector.Depth(stage) >= 2) Post("chat.ghost.hunt", true);
                lastHkState = hk.State;
            }
            if (Time.time >= nextMsg)
            {
                nextMsg = Time.time + Mathf.Lerp(minInterval, maxInterval, (float)rng.NextDouble()) * (threat == Threat.Hunting ? 0.5f : 1f);
                string pool = ChatDirector.PickPool(stage, threat, (float)rng.NextDouble());
                Post(pool, pool.StartsWith("chat.ghost"));
            }
        }

        private Threat CurrentThreat()
        {
            var hk = HousekeeperAI.Instance;
            var pc = Camera.main != null ? Camera.main.transform : null;
            if (hk == null || pc == null) return Threat.None;
            if (hk.State == HousekeeperState.Hunt || hk.State == HousekeeperState.Kill) return Threat.Hunting;
            float d = Vector3.Distance(hk.transform.position, pc.position);
            bool approaching = d < lastDist - 0.01f;
            lastDist = d;
            return d < nearDistance && (approaching || hk.State == HousekeeperState.Suspicious) ? Threat.Near : Threat.None;
        }

        private int PoolSize(string prefix)
        {
            if (poolSizes.TryGetValue(prefix, out int n)) return n;
            n = 0;
            while (T.Table != null && T.Table.Has(prefix + "." + (n + 1))) n++;
            poolSizes[prefix] = n;
            return n;
        }

        private void Post(string pool, bool ghost)
        {
            int n = PoolSize(pool);
            if (n == 0) return;
            string msg = T.Get(pool + "." + (rng.Next(n) + 1));
            string who = ghost ? GhostHandles[rng.Next(GhostHandles.Length)] : Handles[rng.Next(Handles.Length)];
            string color = ghost ? "#D2302A" : "#8FB7E8";
            string line = ghost ? $"<color={color}><b>{who}</b>: <i>{msg}</i></color>" : $"<color={color}><b>{who}</b></color>: {msg}";
            history.Enqueue(line);
            while (history.Count > lines.Count) history.Dequeue();
            int start = lines.Count - history.Count, i = 0;
            foreach (var h in history) lines[start + i++].text = h;
            for (int k = 0; k < start; k++) lines[k].text = "";
        }

        public void OnFlash(bool stunned) { if (ActiveForCharacter) Post(stunned ? "chat.flash_hit" : "chat.flash", false); }
        private void OnItem(string id) { if (ActiveForCharacter && rng.NextDouble() < 0.6) Post("chat.item", false); }
        private void OnCaught() { if (ActiveForCharacter) Post("chat.caught", false); }
    }
}
