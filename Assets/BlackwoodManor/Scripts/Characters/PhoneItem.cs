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

        public override void OnPrimaryUse()
        {
            if (Time.time < nextFlash || player == null) return;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            float cost = cfg != null ? cfg.phoneFlashBatteryCost : 6f;
            if (pool != null && pool.Current < cost) { BlackwoodHUD.Toast(T.Get("toast.no_battery")); return; }
            pool?.Drain(cost);
            nextFlash = Time.time + (cfg != null ? cfg.flashCooldown : 1.5f);
            PlayOneShotSound(flashClip, 0.8f);
            bool stunned = FlashUtil.Fire(player.playerCamera, this);
            LivestreamChat.Instance?.OnFlash(stunned);
            if (stunned) BlackwoodHUD.Subtitle(T.Get("sub.flash_stun"), 2.5f);
        }
    }
}
