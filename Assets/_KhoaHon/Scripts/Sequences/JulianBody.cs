using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Interaction;
using UnityEngine;

namespace KhoaHon.Sequences
{
    /// <summary>
    /// Thi thể Julian Vance trong xà lim hầm. Xem → dòng chữ cậu khắc trên sàn, nhận chiếc còi kỷ niệm,
    /// mất lý trí. Cờ FoundQuan + HasWhistle (tên cờ giữ nguyên từ bản gốc).
    /// </summary>
    public class JulianBody : MonoBehaviour, IInteractable
    {
        [SerializeField] private AudioClip sting;
        public void Configure(AudioClip s) => sting = s;

        private static bool Found => GameManager.Instance != null && GameManager.Instance.Has(StoryFlag.FoundQuan);

        public string Prompt => T.Get(Found ? "prompt.julian_seen" : "prompt.julian");
        public bool CanInteract() => true;

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!Found)
            {
                gm.SetFlag(StoryFlag.FoundQuan);
                gm.SetFlag(StoryFlag.HasWhistle);
                gm.Sanity -= 12f;
                if (sting != null) AmbientScares.Play2D(sting, 0.7f);
                HorrorPostFX.Instance?.Flash(0.4f);
                KhoaHonHUD.Toast(T.Get("toast.whistle"));
                CheckpointManager.Instance?.ReachById("CP_CellarStairs");
            }
            string who = gm.Character == PlayerCharacter.Alex ? T.Get("julian.reaction.alex") : T.Get("julian.reaction.evelyn");
            KhoaHonHUD.Subtitle(T.Get("julian.carving") + "\n<size=80%><i>" + who + "</i></size>", 8f);
        }
    }
}
