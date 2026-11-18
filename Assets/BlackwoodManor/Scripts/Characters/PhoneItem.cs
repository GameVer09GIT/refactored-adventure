using Blackwood.Inventory;
using Blackwood.Core;
using Blackwood.Items;
using UnityEngine;

namespace Blackwood.Characters
{
    /// <summary>
    /// Điện thoại livestream của Alex (slot 2). Cầm trên tay thì khung chat phóng to (<see cref="LivestreamChat"/>);
    /// chuột trái bật đèn flash (tốn pin) làm choáng Mrs. Peake. Phát băng cassette qua Sổ tay như máy ghi âm.
    /// </summary>
    public class PhoneItem : HeldItem
    {
        [SerializeField] private AudioClip flashClip;
        private float nextFlash;

        public override void OnEquip(PlayerController playerController)
        {
            base.OnEquip(playerController);
            LivestreamChat.Instance?.SetExpanded(true);
        }

        public override void OnUnequip()
        {
            LivestreamChat.Instance?.SetExpanded(false);
            base.OnUnequip();
        }

        private void Update()
        {
            // Màn hình livestream sáng suốt lúc cầm → hao sạc dự phòng chậm; cạn thì tối màn hình, không flash được
            if (!isEquipped || !GameFlow.IsPlaying) return;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            if (cfg == null || pool == null) return;
            bool was = pool.IsEmptyCell(CellType.PhonePack);
            pool.Drain(CellType.PhonePack, cfg.phoneIdleDrainPerSecond * Time.deltaTime);
            if (!was && pool.IsEmptyCell(CellType.PhonePack)) BatteryReload.AnnounceDead(CellType.PhonePack);
            SetScreen(!pool.IsEmptyCell(CellType.PhonePack));
        }

        private Renderer[] screens;
        private bool screenOn = true;

        /// <summary>Bật/tắt phát sáng màn hình (vật liệu màn hình là ô thứ có chữ "Screen").</summary>
        private void SetScreen(bool on)
        {
            if (screenOn == on) return;
            screenOn = on;
            screens ??= GetComponentsInChildren<Renderer>(true);
            foreach (var r in screens)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains("Screen")) continue;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor("_EmissionColor", on ? mats[i].GetColor("_EmissionColor") : Color.black);
                    mpb.SetColor("_BaseColor", on ? mats[i].GetColor("_BaseColor") : new Color(0.02f, 0.02f, 0.025f));
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }

        public override void OnPrimaryUse()
        {
            if (Time.time < nextFlash || player == null) return;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            float cost = cfg != null ? cfg.phoneFlashBatteryCost : 4f;
            if (pool != null && pool.Charge(CellType.PhonePack) < cost) { BatteryReload.AnnounceDead(CellType.PhonePack); return; }
            pool?.Drain(CellType.PhonePack, cost);
            nextFlash = Time.time + (cfg != null ? cfg.flashCooldown : 1.5f);
            PlayOneShotSound(flashClip, 0.8f);
            bool stunned = FlashUtil.Fire(player.playerCamera, this);
            LivestreamChat.Instance?.OnFlash(stunned);
            if (stunned) BlackwoodHUD.Subtitle(T.Get("sub.flash_stun"), 2.5f);
        }
    }
}
