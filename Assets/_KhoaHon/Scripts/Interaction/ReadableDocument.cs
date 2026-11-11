using KhoaHon.Core;
using KhoaHon.Notebook;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Tài liệu đặt cố định trong nhà (bảng đồng, di thư, mẩu báo…). Đọc được nhiều lần,
    /// lần đầu đọc thì tự ghi vào Sổ tay. Port ý tưởng từ FpsHorrorKit.LetterUIManager.
    /// </summary>
    public class ReadableDocument : MonoBehaviour, IInteractable
    {
        [SerializeField] private DocumentData document;

        public DocumentData Document => document;
        public void SetDocument(DocumentData d) => document = d;

        public string Prompt => T.Format("prompt.read", document != null ? document.Title : "");

        public bool CanInteract() => document != null;

        public void Interact()
        {
            if (document == null) return;
            DocumentReaderUI.Instance?.Open(document);
        }
    }
}
