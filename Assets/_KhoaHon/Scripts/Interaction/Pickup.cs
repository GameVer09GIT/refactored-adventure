using KhoaHon.Core;
using KhoaHon.Items;
using KhoaHon.Notebook;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Vật nhặt được. Một lần nhặt có thể cho nhiều thứ: chìa khóa, pin, phim, cờ cốt truyện,
    /// mục Sổ tay (tài liệu/băng/kỷ vật). Nhặt xong thì ẩn và không xuất hiện lại sau khi nạp game.
    /// </summary>
    public class Pickup : MonoBehaviour, IInteractable, ISaveParticipant
    {
        [Tooltip("Id duy nhất trong scene")]
        [SerializeField] private string pickupId = "Pickup_";
        [Tooltip("Key TextTable của tên vật (hiện trong prompt và thông báo)")]
        [SerializeField] private string nameTextKey = "item.generic";

        [Header("Phần thưởng")]
        [SerializeField] private string giveKeyId = "";
        [Tooltip("% pin cộng thêm (0 = không)")]
        [SerializeField] private float batteryAmount = 0f;
        [Tooltip("Số kiểu phim cộng thêm (0 = không)")]
        [SerializeField] private int filmAmount = 0;
        [SerializeField] private StoryFlag[] flags = new StoryFlag[0];
        [Tooltip("Tài liệu / băng / kỷ vật thêm vào Sổ tay")]
        [SerializeField] private NotebookSource[] notebookSources = new NotebookSource[0];
        [Tooltip("Mở ngay tài liệu đầu tiên để đọc sau khi nhặt")]
        [SerializeField] private bool openFirstDocument = false;
        [Tooltip("Key TextTable của gợi ý hiện sau khi nhặt (tùy chọn)")]
        [SerializeField] private string hintTextKey = "";
        [SerializeField] private AudioClip pickupClip;

        private bool collected;

        public string PickupId => pickupId;
        public bool Collected => collected;

        public void Configure(string id, string nameKey)
        {
            pickupId = id; nameTextKey = nameKey;
        }

        public void SetRewards(string key, float battery, int film, StoryFlag[] newFlags, NotebookSource[] sources, bool openDoc, string hintKey)
        {
            giveKeyId = key; batteryAmount = battery; filmAmount = film;
            flags = newFlags ?? new StoryFlag[0];
            notebookSources = sources ?? new NotebookSource[0];
            openFirstDocument = openDoc; hintTextKey = hintKey;
        }

        public string Prompt => T.Format("prompt.pickup", T.Get(nameTextKey));

        public bool CanInteract() => !collected;

        public void Interact()
        {
            if (collected) return;
            collected = true;

            if (!string.IsNullOrEmpty(giveKeyId)) KeyRing.Instance?.Add(giveKeyId);
            if (batteryAmount > 0f) BatteryPool.Instance?.Add(batteryAmount);
            if (filmAmount > 0) FilmStock.Instance?.Add(filmAmount);
            foreach (var f in flags) GameManager.Instance?.SetFlag(f);

            DocumentData firstDoc = null;
            foreach (var s in notebookSources)
            {
                if (s == null) continue;
                if (s is RelicData relic) RelicInventory.Instance?.Add(relic);
                else NotebookManager.Instance?.Add(s);
                if (firstDoc == null && s is DocumentData d) firstDoc = d;
            }

            if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
            KhoaHonHUD.Toast(T.Format("toast.got_item", T.Get(nameTextKey)));
            GameEvents.RaiseItemCollected(pickupId);

            gameObject.SetActive(false);

            if (openFirstDocument && firstDoc != null) DocumentReaderUI.Instance?.Open(firstDoc);
            if (!string.IsNullOrEmpty(hintTextKey)) KhoaHonHUD.Subtitle(T.Get(hintTextKey), 5f);
        }

        public void Capture(SaveData data)
        {
            if (collected && !data.collected.Contains(pickupId)) data.collected.Add(pickupId);
        }

        public void Restore(SaveData data)
        {
            collected = data.collected.Contains(pickupId);
            gameObject.SetActive(!collected);
        }
    }
}
