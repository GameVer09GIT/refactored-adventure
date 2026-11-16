using KhoaHon.AI;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Lớp chuyển động thủ tục chồng lên Animator của Mrs. Peake (chạy ở LateUpdate, không cần clip riêng):
    /// dáng "sai" — đầu nghiêng lệch, vai gù, đi khập khiễng, hàm há — nhưng mọi thứ đều nội suy mượt (không giật khung);
    /// nghiêng người khi rẽ; ngả lùi, quay mặt đi khi bị rọi đèn pin; mắt rực đỏ khi đang nhờn đèn.
    /// </summary>
    public class HousekeeperCreep : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private HousekeeperAI ai;

        private Transform head, neck, spine, hips, lArm, rArm, jaw, visualRoot;
        private Transform[] lipsUp;
        private Vector3[] lipsInit;
        private Quaternion jawInit;
        private HousekeeperBodyIK bodyIK;
        private UnityEngine.AI.NavMeshAgent agent;

        // Giật cổ: mục tiêu ngẫu nhiên, vào nhanh – ra chậm (không bật tắt tức thời)
        private Quaternion twitchRot = Quaternion.identity;
        private float twitchW, twitchTarget, twitchHold, nextTwitch;
        private float huntW, gapeW, limpPhase, lean, lastYaw, recoilW;

        private Renderer[] eyes;
        private MaterialPropertyBlock mpb;
        private Color eyeBase = new Color(0.55f, 0.6f, 0.66f);
        private Color eyeNow;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (ai == null) ai = GetComponentInParent<HousekeeperAI>();
            if (ai != null) agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
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
                visualRoot = animator.transform.parent;
                bodyIK = animator.GetComponent<HousekeeperBodyIK>();
                if (bodyIK == null) bodyIK = animator.gameObject.AddComponent<HousekeeperBodyIK>();
                bodyIK.ai = ai;

                var list = new System.Collections.Generic.List<Renderer>();
                foreach (var r in animator.GetComponentsInChildren<Renderer>(true))
                    if (r.name.StartsWith("eye_")) list.Add(r);
                eyes = list.ToArray();
                if (eyes.Length > 0 && eyes[0].sharedMaterial != null && eyes[0].sharedMaterial.HasProperty(EmissionId))
                    eyeBase = eyes[0].sharedMaterial.GetColor(EmissionId);
                eyeNow = eyeBase;
                mpb = new MaterialPropertyBlock();
            }
            nextTwitch = Time.time + Random.Range(1f, 3f);
            lastYaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (animator == null || head == null) return;
            float dt = Time.deltaTime;
            float t = Time.time;
            var state = ai != null ? ai.State : HousekeeperState.Patrol;
            bool hunting = state == HousekeeperState.Hunt || state == HousekeeperState.Kill;
            bool blinded = ai != null && ai.BlindedStun;
            bool stunned = state == HousekeeperState.Stunned && !blinded;
            float shield = bodyIK != null ? bodyIK.ShieldWeight : 0f;

            huntW = Mathf.MoveTowards(huntW, hunting ? 1f : 0f, dt * 1.8f);
            recoilW = Mathf.MoveTowards(recoilW, blinded ? 1f : 0f, dt * (blinded ? 5f : 1.2f));

            // Tốc độ phát clip khớp tốc độ di chuyển (AI tính) — không còn khựng hình / random mỗi khung
            float play = ai != null ? ai.LocomotionPlayback : 1f;
            animator.speed = Mathf.Lerp(animator.speed, blinded ? 0.45f : play, 1f - Mathf.Exp(-8f * dt));

            // ---- Giật cổ (eased)
            if (t >= nextTwitch)
            {
                twitchRot = Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(-32f, 32f), Random.Range(-26f, 26f));
                twitchTarget = 1f; twitchHold = t + Random.Range(0.12f, 0.3f);
                nextTwitch = t + Mathf.Lerp(Random.Range(2.5f, 6f), Random.Range(0.9f, 2.2f), huntW);
            }
            if (t > twitchHold) twitchTarget = 0f;
            twitchW = Mathf.Lerp(twitchW, twitchTarget, 1f - Mathf.Exp(-(twitchTarget > twitchW ? 22f : 6f) * dt));

            // ---- Đầu / cổ: nghiêng lệch; khi dõi theo người chơi thì bớt nghiêng để IK nhìn thẳng vào mặt người chơi
            float lookW = bodyIK != null ? bodyIK.LookWeight : 0f;
            float baseTilt = Mathf.Lerp(22f, 30f, huntW) * (1f - 0.45f * lookW);
            var tilt = Quaternion.Euler(8f + Mathf.Sin(t * 0.7f) * 4f, Mathf.Sin(t * 0.37f) * 10f * (1f - lookW), baseTilt + Mathf.Sin(t * 1.3f) * 5f);
            // Bị rọi đèn: ngoảnh mặt sang một bên, cúi xuống
            var away = Quaternion.Euler(18f * shield, -32f * shield, 0f);
            head.localRotation = head.localRotation * Quaternion.Slerp(Quaternion.identity, tilt, 1f - 0.6f * shield) * away
                                 * Quaternion.Slerp(Quaternion.identity, twitchRot, twitchW);
            if (neck != null) neck.localRotation = neck.localRotation * Quaternion.Euler(10f, 0f, 6f);

            // ---- Miệng há như đang gào; săn thì há rộng và run nhẹ
            if (jaw != null)
            {
                float gapeT = blinded ? 34f : Mathf.Lerp(14f, 26f, huntW);
                gapeW = Mathf.Lerp(gapeW, gapeT, 1f - Mathf.Exp(-5f * dt));
                float tremble = Mathf.Sin(t * 23f) * 3f * Mathf.Max(huntW, recoilW) + Mathf.Sin(t * 0.9f) * 4f * (1f - huntW);
                jaw.localRotation = jawInit * Quaternion.Euler(0f, 0f, -(gapeW + tremble)); // đặt tuyệt đối: không cộng dồn
            }
            if (lipsUp != null)
                for (int i = 0; i < lipsUp.Length; i++) lipsUp[i].localPosition = lipsInit[i] + new Vector3(0f, Mathf.Lerp(0.0025f, 0.004f, huntW), 0f);

            // ---- Thân: gù lưng; ngả ra sau khi che mặt; run khi choáng
            if (spine != null)
            {
                float hunch = Mathf.Lerp(12f, 18f, huntW) - 20f * shield - 10f * recoilW;
                float shake = (stunned ? Mathf.Sin(t * 40f) * 6f : 0f) + recoilW * Mathf.Sin(t * 33f) * 2.5f;
                spine.localRotation = spine.localRotation * Quaternion.Euler(hunch, 0f, shake);
            }
            float armOut = Mathf.Lerp(8f, 18f, huntW) * (1f - shield);
            if (lArm != null) lArm.localRotation = lArm.localRotation * Quaternion.Euler(0f, 0f, -armOut);
            if (rArm != null) rArm.localRotation = rArm.localRotation * Quaternion.Euler(0f, 0f, armOut);

            // ---- Khập khiễng: pha tăng theo quãng đường đi được (không theo thời gian) nên khớp bước chân
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            if (hips != null)
            {
                limpPhase += speed * dt * 2.4f;
                float limp = Mathf.Clamp01(speed / 1.3f) * Mathf.Max(0f, Mathf.Sin(limpPhase));
                hips.localPosition += new Vector3(0f, -0.05f * limp, 0f);
                hips.localRotation = hips.localRotation * Quaternion.Euler(0f, 0f, 6f * limp);
            }

            // ---- Nghiêng người vào cua + chúi tới khi lao nhanh (AI tự đặt visualRoot khi choáng do flash/lược)
            if (visualRoot != null && !stunned)
            {
                float yaw = transform.eulerAngles.y;
                float yawRate = dt > 0f ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
                lastYaw = yaw;
                lean = Mathf.Lerp(lean, Mathf.Clamp(-yawRate * 0.045f, -9f, 9f) * Mathf.Clamp01(speed / 2f), 1f - Mathf.Exp(-5f * dt));
                float pitch = Mathf.Clamp01(speed / 6f) * 7f * huntW - 12f * recoilW;
                var want = Quaternion.Euler(pitch, 0f, lean);
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, want, 1f - Mathf.Exp(-9f * dt));
            }

            // ---- Mắt: lóe trắng khi bị rọi, rực đỏ khi đang nhờn đèn (MaterialPropertyBlock — không sửa material dùng chung)
            if (eyes != null && eyes.Length > 0)
            {
                Color target = eyeBase;
                if (ai != null && ai.LightImmune && !blinded) target = new Color(2.6f, 0.12f, 0.05f) * (0.8f + 0.2f * Mathf.Sin(t * 6f));
                else if (ai != null && ai.BeingLit) target = Color.Lerp(eyeBase, new Color(2.2f, 2.2f, 2.0f), ai.LightExposure01);
                eyeNow = Color.Lerp(eyeNow, target, 1f - Mathf.Exp(-6f * dt));
                for (int i = 0; i < eyes.Length; i++)
                {
                    eyes[i].GetPropertyBlock(mpb);
                    mpb.SetColor(EmissionId, eyeNow);
                    eyes[i].SetPropertyBlock(mpb);
                }
            }
        }
    }
}
