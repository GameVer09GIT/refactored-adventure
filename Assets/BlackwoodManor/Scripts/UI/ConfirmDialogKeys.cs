using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackwood.UI
{
    /// <summary>Phím tắt cho hộp xác nhận (<see cref="MainMenuController.Confirm"/>): Esc = Không.</summary>
    public class ConfirmDialogKeys : MonoBehaviour
    {
        public System.Action Cancel;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel?.Invoke();
        }
    }
}
