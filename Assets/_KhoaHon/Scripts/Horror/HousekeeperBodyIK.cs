using KhoaHon.AI;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>
    /// IK thân trên của Mrs. Peake (gắn cùng GameObject với Animator; layer 0 phải bật IK Pass):
    /// quay đầu dõi theo người chơi khi đã thấy, và giơ hai tay che mặt về phía nguồn sáng khi bị rọi đèn pin.
    /// Chuyển tiếp bằng trọng số nên không giật.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HousekeeperBodyIK : MonoBehaviour
    {
        public HousekeeperAI ai;

        /// <summary>Mức giơ tay che mặt hiện tại 0..1 (HousekeeperCreep đọc để ngả người).</summary>
        public float ShieldWeight { get; private set; }
        public float LookWeight { get; private set; }

        private Animator anim;
        private Transform head;
        private Vector3 lookPos;
        private bool lookInit;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim != null && anim.isHuman) head = anim.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (ai == null || anim == null || head == null) return;
            float dt = Time.deltaTime;
            bool blinded = ai.BlindedStun;
            bool shield = blinded || (ai.BeingLit && !ai.LightImmune);
            float shieldTarget = !shield ? 0f : (blinded ? 1f : Mathf.Lerp(0.5f, 1f, ai.LightExposure01));
            ShieldWeight = Mathf.MoveTowards(ShieldWeight, shieldTarget, dt * (shield ? 3.2f : 1.4f));

            var st = ai.State;
            bool look = !shield && ai.SeesPlayer && (st == HousekeeperState.Hunt || st == HousekeeperState.Suspicious || st == HousekeeperState.Inspect);
            LookWeight = Mathf.MoveTowards(LookWeight, look ? 1f : 0f, dt * (look ? 2.2f : 1.2f));
            var eye = ai.PlayerEyePos;
            if (!lookInit) { lookPos = eye; lookInit = true; }
            lookPos = Vector3.Lerp(lookPos, eye, 1f - Mathf.Exp(-7f * dt));
            anim.SetLookAtWeight(LookWeight * 0.85f, 0.2f, 0.8f, 0f, 0.55f);
            anim.SetLookAtPosition(lookPos);

            float w = Mathf.SmoothStep(0f, 1f, ShieldWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, w); anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, w);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, w); anim.SetIKRotationWeight(AvatarIKGoal.RightHand, w);
            anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, w * 0.9f);
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, w * 0.9f);
            if (w <= 0.001f) return;

            // Hai bàn tay chắn trước mặt, lòng bàn tay hướng ra nguồn sáng, ngón chĩa lên; run rẩy
            var toLight = ai.LightSourcePos - head.position;
            if (toLight.sqrMagnitude < 0.01f) toLight = ai.transform.forward;
            toLight.Normalize();
            var right = Vector3.Cross(Vector3.up, toLight);
            if (right.sqrMagnitude < 0.001f) right = ai.transform.right;
            right.Normalize();
            var up = Vector3.Cross(toLight, right).normalized;
            float t = Time.time;
            var tremL = new Vector3(Mathf.PerlinNoise(t * 9f, 0.1f) - 0.5f, Mathf.PerlinNoise(t * 8f, 3.7f) - 0.5f, Mathf.PerlinNoise(t * 7f, 9.1f) - 0.5f) * 0.035f;
            var tremR = new Vector3(Mathf.PerlinNoise(t * 8.5f, 5.3f) - 0.5f, Mathf.PerlinNoise(t * 9.5f, 1.9f) - 0.5f, Mathf.PerlinNoise(t * 7.5f, 4.4f) - 0.5f) * 0.035f;
            var center = head.position + toLight * 0.27f + up * 0.02f;
            var lp = center - right * 0.085f + up * 0.035f + tremL;
            var rp = center + right * 0.10f - up * 0.02f + toLight * 0.04f + tremR;

            // Quy ước IK tay của Humanoid: tư thế chữ T — tay trái: ngón chĩa −X, lòng bàn tay −Y; tay phải: ngón +X, lòng −Y
            var fingersL = (up + right * 0.25f).normalized;
            var fingersR = (up - right * 0.25f).normalized;
            var rotL = Quaternion.LookRotation(Vector3.Cross(fingersL, toLight), -toLight);
            var rotR = Quaternion.LookRotation(Vector3.Cross(toLight, fingersR), -toLight);

            anim.SetIKPosition(AvatarIKGoal.LeftHand, lp); anim.SetIKRotation(AvatarIKGoal.LeftHand, rotL);
            anim.SetIKPosition(AvatarIKGoal.RightHand, rp); anim.SetIKRotation(AvatarIKGoal.RightHand, rotR);
            anim.SetIKHintPosition(AvatarIKHint.LeftElbow, head.position - right * 0.42f - up * 0.38f + toLight * 0.12f);
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, head.position + right * 0.42f - up * 0.38f + toLight * 0.12f);
        }
    }
}
