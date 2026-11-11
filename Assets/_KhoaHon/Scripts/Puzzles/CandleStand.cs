using KhoaHon.Core;
using KhoaHon.Interaction;
using KhoaHon.Items;
using UnityEngine;

namespace KhoaHon.Puzzles
{
    /// <summary>
    /// Một trong 5 bệ nến ở Phòng Thờ, đặt dưới tấm bia khắc tên một Mắt Trận.
    /// Người chơi chọn trang Vademecum để đặt lên; <see cref="CandleAltar"/> kiểm tra đúng/sai.
    /// </summary>
    public class CandleStand : MonoBehaviour, IInteractable
    {
        [Tooltip("Mắt Trận của tấm bia phía trên bệ")]
        [SerializeField] private Hanh slot;
        [Tooltip("Key TextTable tên khắc trên bia")]
        [SerializeField] private string plaqueKey;
        [Tooltip("Ngọn lửa (Light + mesh) bật khi đặt đúng")]
        [SerializeField] private GameObject flame;

        public Hanh Slot => slot;
        public string Plaque => T.Get(plaqueKey);

        public void Configure(Hanh h, string plaque, GameObject flameObj)
        {
            slot = h; plaqueKey = plaque; flame = flameObj;
        }

        private bool Filled => RelicInventory.Instance != null && RelicInventory.Instance.IsTalismanPlaced(slot);

        public string Prompt => Filled ? T.Format("prompt.stand_lit", Plaque) : T.Format("prompt.stand", Plaque);

        public bool CanInteract() => !Filled && CandleAltar.Instance != null && !ModalUI.IsAnyOpen;

        public void Interact() => CandleAltar.Instance?.OpenChoice(this);

        public void SetLit(bool lit)
        {
            if (flame != null) flame.SetActive(lit);
        }
    }
}
