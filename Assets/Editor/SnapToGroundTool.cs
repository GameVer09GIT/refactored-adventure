using UnityEngine;
using UnityEditor;

public class SnapToGroundTool
{
    [MenuItem("Tools/Snap Selected To Ground %#g")]
    static void SnapToGround()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Chưa chọn object nào để snap!");
            return;
        }

        int snappedCount = 0;
        int skippedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // Bỏ qua object không phải vật thể 3D thực (Light, Camera, Player, empty GameObject...)
            if (obj.GetComponent<Renderer>() == null && obj.GetComponentInChildren<Renderer>() == null)
            {
                skippedCount++;
                continue;
            }

            // Bỏ qua Player, Camera, Light dù có Renderer con
            if (obj.GetComponent<Camera>() != null || obj.GetComponent<Light>() != null
                || obj.name.ToLower().Contains("player"))
            {
                skippedCount++;
                continue;
            }

            Undo.RecordObject(obj.transform, "Snap To Ground");

            Vector3 rayStart = obj.transform.position + Vector3.up * 500f;
            Ray ray = new Ray(rayStart, Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundGround = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == obj.transform || hit.transform.IsChildOf(obj.transform))
                    continue;

                float currentGap = obj.transform.position.y - hit.point.y;

                // CHỈ snap nếu đang lơ lửng cách mặt đất hơn 0.05m
                // Tránh việc "sửa" cả những object vốn đã đúng vị trí
                if (Mathf.Abs(currentGap) > 0.05f)
                {
                    obj.transform.position = hit.point;
                    snappedCount++;
                }
                foundGround = true;
                break;
            }

            if (!foundGround) skippedCount++;
        }

        Debug.Log($"Đã snap {snappedCount} object. Bỏ qua {skippedCount} object (không cần thiết hoặc không phải vật thể).");
    }
}