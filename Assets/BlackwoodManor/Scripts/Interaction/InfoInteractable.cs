using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Interaction
{
    /// <summary>
    /// Vật chỉ để xem: hiện prompt (ví dụ "Bị khóa bằng 5 sợi xích") và phụ đề khi nhấn E.
    /// </summary>
    public class InfoInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptKey = "prompt.inspect";
        [SerializeField] private string messageKey = "";
        [SerializeField] private float messageDuration = 3.5f;

        public void Configure(string prompt, string message) { promptKey = prompt; messageKey = message; }

        public string Prompt => T.Get(promptKey);
        public bool CanInteract() => !string.IsNullOrEmpty(messageKey);
        public void Interact() => BlackwoodHUD.Subtitle(T.Get(messageKey), messageDuration);
    }
}
