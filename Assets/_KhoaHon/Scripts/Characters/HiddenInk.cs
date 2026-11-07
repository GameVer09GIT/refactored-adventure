using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Dấu vết chỉ thấy qua kính nhìn đêm của Alex: dấu chân huỳnh quang, chữ máu trên tường, gợi ý mã két.
    /// Renderer con bị tắt mặc định; <see cref="NightVisionItem"/> bật/tắt toàn bộ qua <see cref="SetAll"/>.
    /// </summary>
    public class HiddenInk : MonoBehaviour
    {
        private static readonly List<HiddenInk> all = new List<HiddenInk>();
        public static bool Revealed { get; private set; }

        private Renderer[] renderers;

        private void Awake() => renderers = GetComponentsInChildren<Renderer>(true);

        private void OnEnable() { all.Add(this); Apply(); }
        private void OnDisable() => all.Remove(this);

        private void Apply()
        {
            if (renderers == null) return;
            foreach (var r in renderers) if (r != null) r.enabled = Revealed;
        }

        public static void SetAll(bool on)
        {
            Revealed = on;
            foreach (var h in all) h.Apply();
        }
    }
}
