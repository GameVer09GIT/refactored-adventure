using System.Collections;
using System.Collections.Generic;
using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Items;
using KhoaHon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.Puzzles
{
    /// <summary>
    /// Bàn thờ gia tộc: 5 bệ nến + bảng chọn trang Vademecum. Đặt đúng → nến bùng sáng; sai → nến phụt tắt,
    /// mất lý trí và gây tiếng động (Mrs. Peake nghe thấy). Đủ 5 → nhà rung, 5 sợi xích dưới bếp đứt, nắp hầm mở.
    /// Trạng thái suy ra từ cờ Talisman_*_Placed và CellarOpened.
    /// </summary>
    public class CandleAltar : MonoBehaviour
    {
        public static CandleAltar Instance { get; private set; }

        [SerializeField] private CandleStand[] stands = new CandleStand[5];
        [Tooltip("Các sợi xích ở nắp hầm (rơi bằng vật lý khi đứt)")]
        [SerializeField] private Transform[] chains = new Transform[0];
        [SerializeField] private AudioClip igniteClip, outClip, rumbleClip, chainsClip;
        [SerializeField] private float wrongSanityLoss = 8f;
        [SerializeField] private float wrongNoiseRadius = 14f;
        [SerializeField] private string checkpointAfter = "CP_Shrine";

        private readonly AltarPuzzle puzzle = new AltarPuzzle();
        private GameObject ui;
        private CandleStand pending;
        private bool sequenceRunning;

        public void Configure(CandleStand[] s, Transform[] chainObjs, AudioClip ignite, AudioClip snuff, AudioClip rumble, AudioClip chainSnap)
        {
            stands = s; chains = chainObjs; igniteClip = ignite; outClip = snuff; rumbleClip = rumble; chainsClip = chainSnap;
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable() => GameEvents.FlagSet += OnFlag;
        private void OnDisable() => GameEvents.FlagSet -= OnFlag;

        private void Start() => SyncFromFlags();

        private void OnFlag(StoryFlag f) => SyncFromFlags();

        /// <summary>Đồng bộ nến với cờ (sau khi nạp save hoặc cheat).</summary>
        public void SyncFromFlags()
        {
            var inv = RelicInventory.Instance;
            foreach (var s in stands)
            {
                if (s == null) continue;
                bool lit = inv != null && inv.IsTalismanPlaced(s.Slot);
                puzzle.SetFilled(s.Slot, lit);
                s.SetLit(lit);
            }
        }

        // ------------------------------------------------------------------ Chọn trang
        public void OpenChoice(CandleStand stand)
        {
            if (ui != null || sequenceRunning) return;
            var inv = RelicInventory.Instance;
            if (inv == null) return;
            var inHand = new List<Hanh>();
            for (int i = 0; i < 5; i++) if (inv.HasTalismanInHand((Hanh)i)) inHand.Add((Hanh)i);
            if (inHand.Count == 0) { KhoaHonHUD.Subtitle(T.Format("altar.no_pages", stand.Plaque), 4f); return; }

            pending = stand;
            ModalUI.Open(this);
            var dim = UIFactory.Panel("KH_AltarUI", UIKit.TopLayer, new Color(0, 0, 0, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ui = dim.gameObject;
            var box = UIFactory.Panel("Box", dim.transform, new Color(0.06f, 0.04f, 0.035f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-460, -100 - inHand.Count * 40), new Vector2(460, 170 + inHand.Count * 40));
            UIFactory.Panel("Edge", box.transform, UIKit.Blood, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero);
            var title = UIKit.Label(box.transform, T.Format("altar.choose", stand.Plaque), 30, UIKit.Ink, TextAlignmentOptions.Top, true, true);
            title.rectTransform.offsetMin = new Vector2(30, 0); title.rectTransform.offsetMax = new Vector2(-30, -26);
            var col = UIKit.Column(box.transform, "Pages", new Vector2(0, 0), new Vector2(1, 1), new Vector2(50, 70), new Vector2(-50, -140), 6);
            foreach (var h in inHand)
            {
                var hh = h;
                var data = inv.GetRelicData(h);
                string label = data != null && data.talisman != null ? data.talisman.Title : h.ToString();
                UIKit.MenuButton(col, label, () => Choose(hh), 28, 48);
            }
            var hint = UIKit.Label(box.transform, T.Get("altar.hint"), 20, UIKit.Dim, TextAlignmentOptions.Bottom);
            hint.rectTransform.offsetMin = new Vector2(30, 22);
        }

        private void CloseChoice()
        {
            if (ui != null) Destroy(ui);
            ui = null; pending = null;
            ModalUI.Close(this);
        }

        private void Update()
        {
            if (ui == null || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseChoice();
        }

        private void Choose(Hanh page)
        {
            var stand = pending;
            CloseChoice();
            if (stand == null) return;
            var inv = RelicInventory.Instance;
            var r = puzzle.Place(stand.Slot, page, inv != null && inv.HasTalismanInHand(page));
            switch (r)
            {
                case AltarResult.Correct:
                    inv.PlaceTalisman(page);
                    stand.SetLit(true);
                    if (igniteClip != null) AudioSource.PlayClipAtPoint(igniteClip, stand.transform.position, 1f);
                    KhoaHonHUD.Subtitle(T.Format("altar.correct", puzzle.FilledCount), 3.5f);
                    if (puzzle.IsComplete) StartCoroutine(BreakChains());
                    break;
                case AltarResult.Wrong:
                    if (outClip != null) AudioSource.PlayClipAtPoint(outClip, stand.transform.position, 1f);
                    if (GameManager.Instance != null) GameManager.Instance.Sanity -= wrongSanityLoss;
                    GameEvents.RaiseNoise(stand.transform.position, wrongNoiseRadius, NoiseType.Altar);
                    HorrorPostFX.Instance?.Flash(0.5f);
                    KhoaHonHUD.Subtitle(T.Get("altar.wrong"), 4f);
                    break;
            }
        }

        // ------------------------------------------------------------------ Xích đứt
        private IEnumerator BreakChains()
        {
            sequenceRunning = true;
            yield return new WaitForSeconds(1.2f);
            if (rumbleClip != null) AmbientScares.Play2D(rumbleClip, 1f);
            CameraShake.Shake(4f, 0.05f);
            KhoaHonHUD.Subtitle(T.Get("altar.rumble"), 4f);
            yield return new WaitForSeconds(1.6f);
            if (chainsClip != null) AmbientScares.Play2D(chainsClip, 0.8f);
            foreach (var c in chains)
            {
                if (c == null) continue;
                var rb = c.GetComponent<Rigidbody>();
                if (rb == null) rb = c.gameObject.AddComponent<Rigidbody>();
                if (c.GetComponent<Collider>() == null) c.gameObject.AddComponent<BoxCollider>();
                rb.isKinematic = false;
                rb.AddForce(Random.insideUnitSphere * 3f + Vector3.up * 2f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
            }
            yield return new WaitForSeconds(2.2f);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetFlag(StoryFlag.CellarOpened);
                gm.SetStage(GameStage.Stage3_Cellar);
            }
            KhoaHonHUD.Subtitle(T.Get("altar.opened"), 5f);
            CheckpointManager.Instance?.ReachById(checkpointAfter);
            gm?.SaveNow();
            sequenceRunning = false;
        }
    }
}
