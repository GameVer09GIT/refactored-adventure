using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Characters
{
    /// <summary>
    /// Ngoại hình cơ thể người chơi theo nhân vật: Evelyn mặc áo khoác dài tay, Alex áo thun tay trần.
    /// Chỉ đổi vật liệu của lưới thân/tay (kể cả bộ tay góc nhìn thứ nhất) — không đụng xương, IK, tư thế hay đèn.
    /// Vật liệu do công cụ BlackwoodPlayerLook tạo trong Assets/BlackwoodManor/Materials/Player.
    /// </summary>
    [DefaultExecutionOrder(-140)]
    public class PlayerLook : MonoBehaviour
    {
        [Tooltip("Vật liệu quần áo (thay cho 'body') theo thứ tự enum PlayerCharacter")]
        [SerializeField] private Material[] bodyMaterials = new Material[2];
        [Tooltip("Vật liệu da/tay (thay cho 'body_skin') theo thứ tự enum PlayerCharacter")]
        [SerializeField] private Material[] skinMaterials = new Material[2];

        private Renderer[] bodyRenderers, skinRenderers;
        private int[] bodySlots, skinSlots;

        public void Configure(Material[] body, Material[] skin)
        {
            bodyMaterials = body;
            skinMaterials = skin;
        }

        private void Awake()
        {
            Collect();
            Apply(GameManager.Instance != null ? GameManager.Instance.Character : PlayerCharacter.Evelyn);
        }

        private void OnEnable() => GameManager.CharacterApplied += Apply;
        private void OnDisable() => GameManager.CharacterApplied -= Apply;

        /// <summary>Ghi nhớ renderer/ô vật liệu nào đang dùng vật liệu thân và da gốc (so theo tên, làm một lần).</summary>
        private void Collect()
        {
            var body = new System.Collections.Generic.List<Renderer>(); var bs = new System.Collections.Generic.List<int>();
            var skin = new System.Collections.Generic.List<Renderer>(); var ss = new System.Collections.Generic.List<int>();
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name;
                    if (n == "body" || n.StartsWith("M_PL_") && n.EndsWith("_Body")) { body.Add(r); bs.Add(i); }
                    else if (n == "body_skin" || n.StartsWith("M_PL_") && n.EndsWith("_Skin")) { skin.Add(r); ss.Add(i); }
                }
            }
            bodyRenderers = body.ToArray(); bodySlots = bs.ToArray();
            skinRenderers = skin.ToArray(); skinSlots = ss.ToArray();
        }

        public void Apply(PlayerCharacter c)
        {
            if (bodyRenderers == null) Collect();
            int k = (int)c;
            Swap(bodyRenderers, bodySlots, bodyMaterials != null && k < bodyMaterials.Length ? bodyMaterials[k] : null);
            Swap(skinRenderers, skinSlots, skinMaterials != null && k < skinMaterials.Length ? skinMaterials[k] : null);
        }

        private static void Swap(Renderer[] rs, int[] slots, Material m)
        {
            if (m == null || rs == null) return;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                var mats = rs[i].sharedMaterials;
                if (slots[i] >= mats.Length || mats[slots[i]] == m) continue;
                mats[slots[i]] = m;
                rs[i].sharedMaterials = mats;
            }
        }
    }
}
