using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>Đánh dấu một phòng (tên + khung) để dễ tìm trong Scene view. Không có logic runtime.</summary>
    public class RoomMarker : MonoBehaviour
    {
        public string displayName = "Phòng";
        public Vector3 size = new Vector3(8f, 3.4f, 8f);
        public Color color = new Color(1f, 0.8f, 0.2f, 0.6f);

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position + Vector3.up * size.y * 0.5f, size);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (size.y + 0.3f), displayName);
#endif
        }
    }
}
