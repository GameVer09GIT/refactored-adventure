using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Characters
{
    /// <summary>
    /// Ngoại hình cơ thể người chơi theo nhân vật: Evelyn mặc áo khoác len dài tay, Alex áo nỉ xám than + găng tay da.
    /// Đổi vật liệu của lưới thân/tay (kể cả tay áo/găng 3D gắn thêm), bật/tắt găng và thân 3D riêng của Alex (BlackwoodAlexBody) — không đụng xương, IK, tư thế hay đèn.
    /// Vật liệu do công cụ BlackwoodPlayerLook tạo trong Assets/BlackwoodManor/Materials/Player, lưới tay áo/găng do BlackwoodPlayerGarments.
    /// </summary>
    [DefaultExecutionOrder(-140)]
    public class PlayerLook : MonoBehaviour
    {
        [Tooltip("Vật liệu quần áo (thay cho 'body') theo thứ tự enum PlayerCharacter")]
        [SerializeField] private Material[] bodyMaterials = new Material[2];
        [Tooltip("Vật liệu da/tay (thay cho 'body_skin') theo thứ tự enum PlayerCharacter")]
        [SerializeField] private Material[] skinMaterials = new Material[2];
        [Tooltip("Renderer chỉ hiện khi chơi Alex (găng tay)")]
        [SerializeField] private Renderer[] alexOnly = new Renderer[0];
        [Tooltip("Renderer chỉ hiện khi chơi Evelyn (thân/đầu gốc — Alex có thân 3D riêng)")]
        [SerializeField] private Renderer[] evelynOnly = new Renderer[0];

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
            if (alexOnly != null)
                foreach (var r in alexOnly)
                    if (r != null) r.enabled = c == PlayerCharacter.Alex;
            if (evelynOnly != null)
                foreach (var r in evelynOnly)
                    if (r != null) r.enabled = c != PlayerCharacter.Alex;
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
