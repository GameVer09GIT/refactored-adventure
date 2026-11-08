using KhoaHon.AI;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Làm dáng đi của bà Quản Gia "sai": đầu nghiêng lệch, cổ giật cục, vai gù, đi khập khiễng (một bên nặng một bên nhẹ
    /// như băng #1), khựng hình khi truy đuổi. Chạy sau Animator (LateUpdate) nên không cần clip riêng.
    /// </summary>
    public class HousekeeperCreep : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private HousekeeperAI ai;

        private Transform head, neck, spine, hips, lArm, rArm, jaw;
        private Transform[] lipsUp;
        private Vector3[] lipsInit;
        private Quaternion jawInit;
        private float twitchUntil, nextTwitch;
        private Quaternion twitchRot = Quaternion.identity;
        private float stutterUntil, nextStutter;

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (ai == null) ai = GetComponentInParent<HousekeeperAI>();
            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                spine = animator.GetBoneTransform(HumanBodyBones.Chest); if (spine == null) spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                lArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                jaw = animator.GetBoneTransform(HumanBodyBones.Jaw);
                if (head != null)
                {
                    var l = new System.Collections.Generic.List<Transform>();
                    foreach (Transform c in head) if (c.name.StartsWith("lip_up")) l.Add(c);
                    lipsUp = l.ToArray();
                    lipsInit = new Vector3[lipsUp.Length];
                    for (int i = 0; i < lipsUp.Length; i++) lipsInit[i] = lipsUp[i].localPosition;
                }
                if (jaw != null) jawInit = jaw.localRotation;
            }
            nextTwitch = Time.time + Random.Range(1f, 3f);
            nextStutter = Time.time + Random.Range(2f, 5f);
        }

        private void LateUpdate()
        {
            if (animator == null || head == null) return;
            bool hunting = ai != null && (ai.State == HousekeeperState.Hunt || ai.State == HousekeeperState.Kill);
            bool stunned = ai != null && ai.State == HousekeeperState.Stunned;

            // Khựng hình (đông cứng animator vài khung) khi săn
            if (hunting && Time.time >= nextStutter)
            {
                stutterUntil = Time.time + Random.Range(0.06f, 0.16f);
                nextStutter = Time.time + Random.Range(0.6f, 1.8f);
            }
            animator.speed = Time.time < stutterUntil ? 0f : (hunting ? Random.Range(1.05f, 1.35f) : 0.85f);

            // Giật cổ ngẫu nhiên
            if (Time.time >= nextTwitch)
            {
                twitchRot = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(-40f, 40f), Random.Range(-35f, 35f));
                twitchUntil = Time.time + Random.Range(0.08f, 0.2f);
                nextTwitch = Time.time + (hunting ? Random.Range(0.4f, 1.2f) : Random.Range(1.5f, 4.5f));
            }
            float t = Time.time;
            float baseTilt = hunting ? 32f : 22f;
            var tilt = Quaternion.Euler(8f + Mathf.Sin(t * 0.7f) * 4f, Mathf.Sin(t * 0.37f) * 10f, baseTilt + Mathf.Sin(t * 1.3f) * 5f);
            head.localRotation = head.localRotation * tilt * (Time.time < twitchUntil ? twitchRot : Quaternion.identity);
            if (neck != null) neck.localRotation = neck.localRotation * Quaternion.Euler(10f, 0f, 6f);
            // Miệng há hoác như đang gào (hàm trễ xuống, môi trên nhếch lộ răng); lúc săn há rộng hơn và run
            if (jaw != null)
            {
                float gape = hunting ? 26f + Mathf.Sin(t * 23f) * 3f : 14f + Mathf.Sin(t * 0.9f) * 4f;
                jaw.localRotation = jawInit * Quaternion.Euler(0f, 0f, -gape); // đặt tuyệt đối: không cộng dồn qua từng khung
            }
            if (lipsUp != null)
                for (int i = 0; i < lipsUp.Length; i++) lipsUp[i].localPosition = lipsInit[i] + new Vector3(0f, hunting ? 0.004f : 0.0025f, 0f);
            // Gù lưng + vai sụp, run khi choáng
            if (spine != null) spine.localRotation = spine.localRotation * Quaternion.Euler(hunting ? 18f : 12f, 0f, stunned ? Mathf.Sin(t * 40f) * 6f : 0f);
            if (lArm != null) lArm.localRotation = lArm.localRotation * Quaternion.Euler(0f, 0f, hunting ? -18f : -8f);
            if (rArm != null) rArm.localRotation = rArm.localRotation * Quaternion.Euler(0f, 0f, hunting ? 18f : 8f);
            // Khập khiễng: hông lệch theo nhịp bước
            if (hips != null && ai != null)
            {
                var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
                float speed = agent != null ? agent.velocity.magnitude : 0f;
                float limp = Mathf.Clamp01(speed / 1.3f) * Mathf.Max(0f, Mathf.Sin(t * (hunting ? 9f : 4.2f)));
                hips.localPosition += new Vector3(0f, -0.06f * limp, 0f);
                hips.localRotation = hips.localRotation * Quaternion.Euler(0f, 0f, 7f * limp);
            }
        }
    }
}
