namespace KhoaHon.Interaction
{
    /// <summary>Vật thể tương tác được bằng phím E.</summary>
    public interface IInteractable
    {
        /// <summary>Chuỗi gợi ý hiển thị giữa-dưới màn hình (đã dịch).</summary>
        string Prompt { get; }
        bool CanInteract();
        void Interact();
    }
}
