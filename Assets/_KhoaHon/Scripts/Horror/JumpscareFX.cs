using System.Collections;
using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Jumpscare khi bị bắt: chớp đỏ/đen + nhiễu + rung camera + tiếng thét; giữ khuôn mặt bà Quản Gia sát camera
    /// (ẩn ảnh jumpscare cũ của bản demo). Thay chữ "YOU DIED" bằng "BẠN ĐÃ CHẾT".
    /// </summary>
    public class JumpscareFX : MonoBehaviour
    {
        [SerializeField] private AudioClip scareClip;
        [SerializeField] private float duration = 1.1f;

        private RawImage overlay;
        private Texture2D noiseTex;

        private void OnEnable() => GameEvents.PlayerCaught += OnCaught;
        private void OnDisable() => GameEvents.PlayerCaught -= OnCaught;

        private void Start()
        {
            var hud = KhoaHonHUD.Instance;
            if (hud == null) return;
            var rt = UIFactory.Stretch("KH_Jumpscare", hud.Root);
            overlay = rt.gameObject.AddComponent<RawImage>();
            overlay.raycastTarget = false;
            noiseTex = new Texture2D(256, 144, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            overlay.texture = noiseTex;
            overlay.color = new Color(1, 1, 1, 0);
            rt.gameObject.SetActive(false);
            ReskinDeathScreen(hud);
        }

        /// <summary>Ẩn ảnh demo (jumpscare + "YOU DIED"), thêm tiêu đề tiếng Việt kiểu máu.</summary>
        private static void ReskinDeathScreen(KhoaHonHUD hud)
        {
            var dsm = FindFirstObjectByType<DeathScreenManager>();
            if (dsm == null) return;
            foreach (var ri in dsm.GetComponentsInChildren<RawImage>(true))
            {
                if (ri.name.Contains("YouDied") || ri.transform.parent != null && ri.transform.parent.name.Contains("Jumpscare"))
                {
                    if (ri.name.Contains("YouDied"))
                    {
                        ri.color = new Color(0, 0, 0, 0);
                        var title = UIFactory.Text("KH_DeathTitle", ri.transform, hud.Style, true, 150, new Color(0.55f, 0.02f, 0.02f), TextAlignmentOptions.Center);
                        if (hud.Style != null && hud.Style.serifBoldFont != null) title.font = hud.Style.serifBoldFont;
                        title.text = T.Get("ui.you_died");
                        title.characterSpacing = 12f;
                        title.fontMaterial.EnableKeyword("UNDERLAY_ON");
                        title.fontMaterial.SetColor("_UnderlayColor", new Color(0.1f, 0, 0, 0.9f));
                        title.fontMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
                        title.fontMaterial.SetFloat("_UnderlaySoftness", 0.4f);
                    }
                    else ri.enabled = false; // tắt hẳn ảnh mặt ma cũ (DeathScreenManager gán lại màu mỗi frame): để khuôn mặt bà 3D hiện qua
                }
            }
            // Tắt tiếng hét cũ của DeathScreenManager — bà Quản Gia đã có tiếng thét riêng (hk_scream + sfx_jumpscare)
            var f = typeof(DeathScreenManager).GetField("audioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.GetValue(dsm) is AudioSource src) src.mute = true;
        }

        private void OnCaught() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            AmbientScares.Play2D(scareClip, 1.0f);
            HorrorPostFX.Instance?.Flash(1f);
            var pc = FindFirstObjectByType<PlayerController>();
            var cam = pc != null ? pc.playerCamera : null;
            var basePos = cam != null ? cam.localPosition : Vector3.zero;
            if (overlay != null) overlay.gameObject.SetActive(true);
            // Ẩn thanh đồ + tâm ngắm trong lúc bị bắt
            var oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null) oldCanvas.SetActive(false);
            var crosshair = KhoaHonHUD.Instance != null ? KhoaHonHUD.Instance.Root.Find("Crosshair") : null;
            if (crosshair != null) crosshair.gameObject.SetActive(false);
            var tool = KhoaHonHUD.Instance != null ? KhoaHonHUD.Instance.Root.Find("Tool") : null;
            if (tool != null) tool.gameObject.SetActive(false);
            float t = 0f;
            var px = new Color32[noiseTex.width * noiseTex.height];
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / duration;
                // Nhiễu đỏ đen
                for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(0, 255); px[i] = new Color32((byte)(v * 0.9f), (byte)(v * 0.05f), (byte)(v * 0.05f), (byte)(Random.value < 0.5f ? 255 : 120)); }
                noiseTex.SetPixels32(px); noiseTex.Apply(false);
                bool strobe = (int)(t * 22f) % 2 == 0;
                if (overlay != null) overlay.color = new Color(1, 1, 1, (strobe ? 0.55f : 0.15f) * k);
                if (cam != null) cam.localPosition = basePos + Random.insideUnitSphere * 0.06f * k;
                yield return null;
            }
            if (cam != null) cam.localPosition = basePos;
            if (overlay != null) overlay.gameObject.SetActive(false);
            // Bật lại sau khi màn chết đã che (hồi sinh sẽ cần thanh đồ)
            yield return new WaitForSecondsRealtime(2.5f);
            if (oldCanvas != null) oldCanvas.SetActive(true);
            if (crosshair != null) crosshair.gameObject.SetActive(true);
            if (tool != null) tool.gameObject.SetActive(true);
        }
    }
}
