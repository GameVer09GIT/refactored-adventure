using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Quản lý các UI chặn điều khiển (Sổ tay, đọc tài liệu, keypad, đối thoại).
    /// Khi có ít nhất 1 UI mở: tắt PlayerController, mở chuột. Đóng hết thì khôi phục.
    /// </summary>
    public static class ModalUI
    {
        private static readonly HashSet<object> open = new HashSet<object>();

        public static bool IsAnyOpen => open.Count > 0;

        /// <summary>Frame cuối cùng một UI được đóng — để phím E đóng UI không đồng thời kích hoạt tương tác.</summary>
        public static int LastClosedFrame { get; private set; } = -1;

        public static void Open(object owner)
        {
            if (owner == null || !open.Add(owner)) return;
            if (open.Count == 1) SetPlayerControl(false);
        }

        public static void Close(object owner)
        {
            if (owner == null || !open.Remove(owner)) return;
            LastClosedFrame = Time.frameCount;
            if (open.Count == 0) SetPlayerControl(true);
        }

        public static bool IsOpen(object owner) => owner != null && open.Contains(owner);

        public static void Reset() => open.Clear();

        private static void SetPlayerControl(bool enabled)
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.SetControlEnabled(enabled);
        }
    }
}
