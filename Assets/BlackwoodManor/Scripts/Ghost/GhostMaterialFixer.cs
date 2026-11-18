using UnityEngine;

/// <summary>
/// Tự động sửa và áp dụng vật liệu URP chuẩn cho con ma, đảm bảo không bao giờ bị màu hồng.
/// </summary>
[ExecuteAlways]
public class GhostMaterialFixer : MonoBehaviour
{
    [Header("Vật liệu URP")]
    [SerializeField] private Material clothMaterial;
    [SerializeField] private Material eyesMaterial;

    void Awake()
    {
        FixMaterials();
    }

    void Start()
    {
        FixMaterials();
    }

    void OnEnable()
    {
        FixMaterials();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        FixMaterials();
    }
#endif

    public void FixMaterials()
    {
        // 1. Tải vật liệu từ Resources nếu chưa gán
        if (clothMaterial == null)
        {
            clothMaterial = Resources.Load<Material>("M_GhostCloth");
        }
        if (eyesMaterial == null)
        {
            eyesMaterial = Resources.Load<Material>("M_GhostEyes");
        }

        // Nếu vẫn null thì tự động tạo material URP Lit dự phòng
        if (clothMaterial == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                clothMaterial = new Material(urpLit);
                clothMaterial.name = "M_GhostCloth_Runtime";
                clothMaterial.SetColor("_BaseColor", new Color(0.85f, 0.83f, 0.80f, 1f));
                Texture2D tex = Resources.Load<Texture2D>("burlap_cloth");
                if (tex != null) clothMaterial.SetTexture("_BaseMap", tex);
            }
        }

        if (eyesMaterial == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                eyesMaterial = new Material(urpLit);
                eyesMaterial.name = "M_GhostEyes_Runtime";
                eyesMaterial.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f, 1f));
            }
        }

        // 2. Gán vật liệu vào tất cả MeshRenderer của con ma
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            if (clothMaterial != null && eyesMaterial != null)
            {
                r.sharedMaterials = new Material[] { clothMaterial, eyesMaterial };
            }
            else if (clothMaterial != null)
            {
                r.sharedMaterials = new Material[] { clothMaterial };
            }
        }
    }
}
