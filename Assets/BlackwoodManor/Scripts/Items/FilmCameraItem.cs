using Blackwood.Inventory;
using Blackwood.Characters;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Items
{
    /// <summary>
    /// Máy ảnh film có flash (Evelyn, slot 3): mỗi lần chụp tốn 1 kiểu phim, có thời gian hồi.
    /// Flash trúng Mrs. Peake trong tầm → bà choáng (HousekeeperBrain.OnFlashed: 3 s rồi chuyển Hunt).
    /// </summary>
    public class FilmCameraItem : HeldItem
    {
        [SerializeField] private AudioClip shutterClip;
        private float nextShot;

        public override void OnPrimaryUse()
        {
            if (Time.time < nextShot || player == null) return;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var stock = FilmStock.Instance;
            if (stock != null && !stock.TryConsume()) { BlackwoodHUD.Toast(T.Get("toast.no_film")); return; }
            nextShot = Time.time + (cfg != null ? cfg.flashCooldown : 1.5f);
            PlayOneShotSound(shutterClip, 0.9f);
            bool stunned = FlashUtil.Fire(player.playerCamera, this);
            if (stunned) BlackwoodHUD.Subtitle(T.Get("sub.flash_stun"), 2.5f);
        }
    }
}
