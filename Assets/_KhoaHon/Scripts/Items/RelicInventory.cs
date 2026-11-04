using System;
using KhoaHon.Core;
using KhoaHon.Notebook;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>
    /// 5 ô kỷ vật cố định theo Ngũ Hành, không vứt được. Nhặt kỷ vật = nhận luôn trang bùa của nó.
    /// Trạng thái được suy ra từ cờ Relic_* và Talisman_*_Placed, nên không cần lưu riêng.
    /// </summary>
    public class RelicInventory : MonoBehaviour
    {
        public static RelicInventory Instance { get; private set; }

        [Tooltip("Dữ liệu 5 kỷ vật theo thứ tự Mộc, Thủy, Thổ, Kim, Hỏa")]
        [SerializeField] private RelicData[] relics = new RelicData[5];

        public event Action<Hanh> RelicAdded;
        public event Action<Hanh> TalismanPlaced;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public RelicData GetRelicData(Hanh h) => relics != null && (int)h < relics.Length ? relics[(int)h] : null;

        public bool HasRelic(Hanh h) => GameManager.Instance != null && GameManager.Instance.Has(HanhUtil.RelicFlag(h));

        public bool IsTalismanPlaced(Hanh h) => GameManager.Instance != null && GameManager.Instance.Has(HanhUtil.TalismanPlacedFlag(h));

        /// <summary>Có trang bùa trong túi (đã nhặt kỷ vật và chưa đặt lên đài sen).</summary>
        public bool HasTalismanInHand(Hanh h) => HasRelic(h) && !IsTalismanPlaced(h);

        public int RelicCount
        {
            get { int n = 0; for (int i = 0; i < 5; i++) if (HasRelic((Hanh)i)) n++; return n; }
        }

        public void Add(RelicData relic)
        {
            if (relic == null || GameManager.Instance == null) return;
            if (relics[(int)relic.hanh] == null) relics[(int)relic.hanh] = relic;
            GameManager.Instance.SetFlag(HanhUtil.RelicFlag(relic.hanh));
            NotebookManager.Instance?.Add(relic);
            if (relic.talisman != null) NotebookManager.Instance?.Add(relic.talisman);
            if (!string.IsNullOrEmpty(relic.pickupLine)) KhoaHonHUD.Subtitle(relic.pickupLine, 4f);
            RelicAdded?.Invoke(relic.hanh);
        }

        /// <summary>Đặt trang bùa lên đài sen: bùa rời khỏi tab Bùa chú (bài chú đã chép vẫn còn trong Tài liệu).</summary>
        public bool PlaceTalisman(Hanh h)
        {
            if (!HasTalismanInHand(h)) return false;
            GameManager.Instance.SetFlag(HanhUtil.TalismanPlacedFlag(h));
            TalismanPlaced?.Invoke(h);
            return true;
        }
    }
}
