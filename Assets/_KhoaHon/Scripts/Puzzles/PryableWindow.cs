using KhoaHon.Core;
using KhoaHon.Interaction;
using KhoaHon.Narrative;
using KhoaHon.UI;
using UnityEngine;

namespace KhoaHon.Puzzles
{
    /// <summary>
    /// Cửa sổ sảnh trệt bị đóng đinh. Có xà beng (từ balo của Julian) thì nạy được và bỏ chạy → Kết ẩn "Kẻ Hèn Nhát".
    /// Hỏi xác nhận trước vì đây là kết thúc sớm.
    /// </summary>
    public class PryableWindow : MonoBehaviour, IInteractable
    {
        [SerializeField] private AudioClip pryClip;

        public void Configure(AudioClip clip) => pryClip = clip;

        private static bool HasCrowbar => GameManager.Instance != null && GameManager.Instance.Has(StoryFlag.HasCrowbar);

        public string Prompt => T.Get(HasCrowbar ? "prompt.window_pry" : "prompt.window_nailed");

        public bool CanInteract() => true;

        public void Interact()
        {
            if (!HasCrowbar) { KhoaHonHUD.Subtitle(T.Get("msg.window_nailed"), 3.5f); return; }
            MainMenuController.Confirm(T.Get("ui.confirm_flee"), () =>
            {
                if (pryClip != null) AudioSource.PlayClipAtPoint(pryClip, transform.position, 1f);
                EndingDirector.Instance?.Play(EndingId.Coward);
            });
        }
    }
}
