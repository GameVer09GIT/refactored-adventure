using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Blackwood.Core;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Quét lỗi vặt không cần chơi: (1) khóa chữ dùng trong code nhưng thiếu trong TextTable hoặc thiếu bản tiếng Anh;
    /// (2) vật thể trong BW_Main lơ lửng hoặc lún (không chạm sàn, không áp tường, không treo trần). Kết quả: Console + Temp/Blackwood/audit_report.txt.
    /// </summary>
    public static class BlackwoodAudit
    {
        [MenuItem("Blackwood/Test/Audit (text keys + floating props)")]
        public static void Menu() => Debug.Log(Run());

        public static string Run()
        {
            var sb = new StringBuilder();
            TextKeys(sb);
            Floating(sb);
            Directory.CreateDirectory("Temp/Blackwood");
            File.WriteAllText("Temp/Blackwood/audit_report.txt", sb.ToString());
            return "AUDIT\n" + sb;
        }

        // ------------------------------------------------------------------ khóa chữ
        static readonly Regex Call = new Regex(@"\bT\.(?:Get|Format)\(\s*""([a-z0-9_.]+)""", RegexOptions.Compiled);
        static readonly Regex Literal = new Regex(@"""((?:prompt|msg|ui|item|sub|hint|obj|tip|altar|ritual|lord|end|ach|char|key|chat|doc|notebook|safe|evp|mind|tape|ctrl|intro|cred|death|load)\.[a-z0-9_.]+)""", RegexOptions.Compiled);

        static void TextKeys(StringBuilder sb)
        {
            var guid = AssetDatabase.FindAssets("t:TextTable").FirstOrDefault();
            var table = guid != null ? AssetDatabase.LoadAssetAtPath<TextTable>(AssetDatabase.GUIDToAssetPath(guid)) : null;
            if (table == null) { sb.AppendLine("TEXT: không tìm thấy TextTable"); return; }
            var used = new Dictionary<string, string>();
            foreach (var dir in new[] { "Assets/BlackwoodManor/Scripts", "Assets/BlackwoodManor/Editor" })
                foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (f.Contains("BlackwoodContent")) continue; // file định nghĩa nội dung
                    var src = File.ReadAllText(f);
                    foreach (Match m in Call.Matches(src)) used[m.Groups[1].Value] = Path.GetFileName(f);
                    foreach (Match m in Literal.Matches(src)) if (!m.Groups[1].Value.EndsWith(".")) used[m.Groups[1].Value] = Path.GetFileName(f);
                }
            var missing = used.Where(kv => !table.Has(kv.Key)).OrderBy(kv => kv.Key).ToList();
            var noEn = table.Entries.Where(e => !string.IsNullOrEmpty(e.text) && string.IsNullOrEmpty(e.en)).Select(e => e.key).ToList();
            var emptyVi = table.Entries.Where(e => string.IsNullOrEmpty(e.text)).Select(e => e.key).ToList();
            sb.AppendLine($"TEXT: {used.Count} khóa dùng trong code, {table.Entries.Count} khóa trong bảng");
            sb.AppendLine($"  thiếu trong bảng ({missing.Count}): " + string.Join(", ", missing.Select(kv => kv.Key + " [" + kv.Value + "]")));
            sb.AppendLine($"  thiếu bản tiếng Anh ({noEn.Count}): " + string.Join(", ", noEn.Take(60)));
            sb.AppendLine($"  trống tiếng Việt ({emptyVi.Count}): " + string.Join(", ", emptyVi.Take(60)));
        }

        // ------------------------------------------------------------------ vật lơ lửng
        static readonly string[] Hanging = { "Lamp", "Light", "Chandelier", "Painting", "Frame", "Plaque", "Chain", "Hook", "MocTreo", "Curtain",
            "Nail", "Flame", "Candle", "Moon", "Fog", "Dust", "Shaft", "Decal", "Blood", "Web", "Rope", "Ghost", "Lord", "Aura", "Sign",
            "Window", "Door", "Plank", "Ink", "Footprint", "Scratch", "Wall", "Ceiling", "Roof", "Beam", "Rain", "Vapor", "Particle",
            "Trigger", "CP_", "Xich", "Hatch", "NapHam", "Clock", "Mirror" };

        static void Floating(StringBuilder sb)
        {
            var root = GameObject.Find("BW_Conversion");
            if (root == null) { sb.AppendLine("FLOAT: không thấy BW_Conversion (mở BW_Main trước)"); return; }
            Physics.SyncTransforms();
            var found = new List<string>();
            int checkedN = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (r is ParticleSystemRenderer || !r.enabled) continue;
                var b = r.bounds;
                if (b.size.magnitude < 0.03f || b.size.magnitude > 3.5f) continue;
                string path = PathOf(r.transform);
                if (Hanging.Any(h => path.Contains(h))) continue;
                checkedN++;
                // Bỏ qua chính nó và các vật cùng gốc (thân + chân của một món)
                var top = TopUnder(r.transform, root.transform);
                bool Self(Collider c) => c.transform.IsChildOf(top);
                bool Hit(Vector3 o, Vector3 d, float len) =>
                    Physics.RaycastAll(o, d, len, ~0, QueryTriggerInteraction.Ignore).Any(h => !Self(h.collider));
                var c0 = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);
                bool ground = Hit(c0, Vector3.down, 0.1f)
                              || Hit(new Vector3(b.min.x + 0.02f, b.min.y + 0.02f, b.min.z + 0.02f), Vector3.down, 0.1f)
                              || Hit(new Vector3(b.max.x - 0.02f, b.min.y + 0.02f, b.max.z - 0.02f), Vector3.down, 0.1f);
                if (ground) continue;
                bool wall = Hit(b.center, Vector3.left, b.extents.x + 0.08f) || Hit(b.center, Vector3.right, b.extents.x + 0.08f)
                            || Hit(b.center, Vector3.forward, b.extents.z + 0.08f) || Hit(b.center, Vector3.back, b.extents.z + 0.08f);
                bool ceil = Hit(new Vector3(b.center.x, b.max.y - 0.02f, b.center.z), Vector3.up, 0.1f);
                if (wall || ceil) continue;
                RaycastHit gh;
                float gap = Physics.Raycast(c0, Vector3.down, out gh, 20f, ~0, QueryTriggerInteraction.Ignore) ? gh.distance : -1f;
                found.Add($"{path} @ {b.center:F2} cách mặt dưới {(gap < 0 ? "?" : gap.ToString("F2") + " m")}");
            }
            sb.AppendLine($"FLOAT: kiểm {checkedN} vật, nghi lơ lửng {found.Count}");
            foreach (var f in found.Take(80)) sb.AppendLine("  " + f);
        }

        static Transform TopUnder(Transform t, Transform root)
        {
            while (t.parent != null && t.parent != root && t.parent.parent != root) t = t.parent;
            return t;
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (int i = 0; t != null && i < 5; i++, t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
