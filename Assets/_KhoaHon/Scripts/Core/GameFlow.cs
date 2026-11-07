using System;

namespace KhoaHon.Core
{
    public enum FlowState { Boot, Menu, Intro, Playing, Paused }
    public enum StartMode { None, NewGame, Continue }

    /// <summary>Luồng game (menu → intro → chơi → tạm dừng). AI và hù dọa chỉ chạy khi Playing.</summary>
    public static class GameFlow
    {
        public static FlowState State { get; private set; } = FlowState.Boot;
        /// <summary>Nạp lại scene từ menu: bỏ qua menu một lần và vào thẳng chế độ này.</summary>
        public static StartMode PendingStart = StartMode.None;
        /// <summary>Đã qua màn cảnh báo + chỉnh độ sáng trong phiên này.</summary>
        public static bool BootShown;
        /// <summary>Nhân vật đã chọn ở màn chọn nhân vật, áp dụng khi scene nạp lại cho New Game.</summary>
        public static PlayerCharacter PendingCharacter = PlayerCharacter.Evelyn;

        public static event Action<FlowState> Changed;

        public static bool IsPlaying => State == FlowState.Playing;

        public static void Set(FlowState s)
        {
            if (State == s) return;
            State = s;
            Changed?.Invoke(s);
        }
    }

    /// <summary>Âm lượng theo nhóm (lưu trong cài đặt). Tổng = AudioListener.volume; các nhóm nhân vào nguồn của Khóa Hồn.</summary>
    public static class AudioMix
    {
        public static float Music = 0.8f, Sfx = 1f, Ambience = 0.85f, Voice = 1f;
        public static event Action Changed;
        public static void Notify() => Changed?.Invoke();
    }
}
