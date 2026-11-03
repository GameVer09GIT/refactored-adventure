using System;
using UnityEngine;

namespace HorrorGame.Inventory
{
    [Serializable]
    public struct FingerPose
    {
        [Tooltip("Góc cuộn đốt 1 (khớp gốc) - độ")]
        public float joint1;

        [Tooltip("Góc cuộn đốt 2 (khớp giữa) - độ")]
        public float joint2;

        [Tooltip("Góc cuộn đốt 3 (khớp đầu ngón) - độ")]
        public float joint3;

        [Tooltip("Góc xòe/khép ngang (spread) - độ")]
        public float spread;

        public FingerPose(float j1, float j2, float j3, float s = 0f)
        {
            joint1 = j1;
            joint2 = j2;
            joint3 = j3;
            spread = s;
        }

        public static FingerPose Lerp(FingerPose a, FingerPose b, float t)
        {
            return new FingerPose(
                Mathf.Lerp(a.joint1, b.joint1, t),
                Mathf.Lerp(a.joint2, b.joint2, t),
                Mathf.Lerp(a.joint3, b.joint3, t),
                Mathf.Lerp(a.spread, b.spread, t)
            );
        }
    }

    /// <summary>
    /// Lưu cấu hình tư thế các ngón tay cho vật phẩm cầm tay (Hand Pose)
    /// </summary>
    [CreateAssetMenu(fileName = "NewHandPose", menuName = "Horror Game/Hand Pose")]
    public class HandPose : ScriptableObject
    {
        [Header("Thông tin tư thế")]
        [Tooltip("Tên tư thế")]
        public string poseName = "NewPose";

        [Tooltip("Mô tả tư thế")]
        [TextArea(1, 3)]
        public string description = "";

        [Header("Tư thế các ngón tay")]
        [Tooltip("Ngón cái (đốt 1, 2)")]
        public FingerPose thumb = new FingerPose(15f, 20f, 0f, -10f);

        [Tooltip("Ngón trỏ (đốt 1, 2, 3)")]
        public FingerPose index = new FingerPose(45f, 50f, 40f, -4f);

        [Tooltip("Ngón giữa (đốt 1, 2, 3)")]
        public FingerPose middle = new FingerPose(50f, 55f, 45f, 0f);

        [Tooltip("Ngón áp út (đốt 1, 2, 3)")]
        public FingerPose ring = new FingerPose(52f, 55f, 45f, 4f);

        [Tooltip("Ngón út (đốt 1, 2, 3)")]
        public FingerPose pinky = new FingerPose(55f, 60f, 50f, 8f);

        [Header("Góc xoay cổ tay điều chỉnh")]
        [Tooltip("Góc xoay bù cổ tay (Euler angles)")]
        public Vector3 wristRotationOffset = Vector3.zero;

        [Tooltip("Vị trí bù cổ tay (mét)")]
        public Vector3 wristPositionOffset = Vector3.zero;

        public static HandPose CreateInstance(string name, FingerPose th, FingerPose idx, FingerPose mid, FingerPose rng, FingerPose pnk, Vector3 wristRot = default, Vector3 wristPos = default)
        {
            var pose = CreateInstance<HandPose>();
            pose.name = name;
            pose.thumb = th;
            pose.index = idx;
            pose.middle = mid;
            pose.ring = rng;
            pose.pinky = pnk;
            pose.wristRotationOffset = wristRot;
            pose.wristPositionOffset = wristPos;
            return pose;
        }
    }
}
