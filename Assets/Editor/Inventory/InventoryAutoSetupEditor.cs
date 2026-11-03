using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Inventory.Editor
{
    /// <summary>
    /// Menu tự động thiết lập toàn bộ hệ thống Hotbar, Đèn pin, Radar (Texture, Prefab, ItemData, Scene wiring)
    /// </summary>
    public static class InventoryAutoSetupEditor
    {
        private const string IconsFolder = "Assets/UI/Icons";
        private const string TexturesFolder = "Assets/UI/Textures";
        private const string ItemsDataFolder = "Assets/Items";
        private const string PrefabsItemsFolder = "Assets/Prefabs/Items";
        private const string PrefabsUIFolder = "Assets/Prefabs/UI";
        private const string MaterialsFolder = "Assets/Prefabs/Items/Materials";

        [MenuItem("Tools/Inventory/Auto Setup Hotbar, Flashlight, Radar")]
        public static void RunFullSetup()
        {
            EnsureDirectories();

            // 1. Tạo âm thanh vật phẩm
            GenerateItemSoundsEditor.GenerateAllSounds();

            // 2. Tạo hình ảnh Icon và Texture cho Radar/Hotbar
            CreateIconsAndTextures();

            // 3. Tạo vật liệu (Materials) cho đèn pin và radar
            CreateMaterials(out Material matFlashlightBody, out Material matFlashlightGlass, out Material matRadarBody, out Material matRadarScreen);

            // 4. Tạo Prefab mô hình 3D cầm tay (Flashlight và Radar)
            GameObject flashlightPrefab = CreateFlashlightPrefab(matFlashlightBody, matFlashlightGlass);
            GameObject radarPrefab = CreateRadarPrefab(matRadarBody, matRadarScreen);

            // 5. Tạo ScriptableObject ItemData
            ItemData flashlightData = CreateOrUpdateItemData("Item_Flashlight", "Đèn pin", ItemType.Flashlight, $"{IconsFolder}/icon_flashlight.png", flashlightPrefab);
            ItemData radarData = CreateOrUpdateItemData("Item_Radar", "Radar", ItemType.Radar, $"{IconsFolder}/icon_radar.png", radarPrefab);

            // Gán ItemData vào Prefab cầm tay
            AssignItemDataToPrefab(flashlightPrefab, flashlightData);
            AssignItemDataToPrefab(radarPrefab, radarData);

            // 6. Tạo Prefab UI (HotbarUI và RadarHUD)
            GameObject hotbarUIPrefab = CreateHotbarUIPrefab();
            GameObject radarHUDPrefab = CreateRadarHUDPrefab();

            // 7. Cài đặt vào Scene đang mở
            SetupSceneInstances(hotbarUIPrefab, radarHUDPrefab, flashlightData, radarData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[InventoryAutoSetup] ĐÃ HOÀN TẤT THIẾT LẬP HOTBAR, ĐÈN PIN VÀ RADAR THÀNH CÔNG 100%!</b></color>");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { IconsFolder, TexturesFolder, ItemsDataFolder, PrefabsItemsFolder, PrefabsUIFolder, MaterialsFolder };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
            AssetDatabase.Refresh();
        }

        #region 1. Tạo Textures & Icons

        private static void CreateIconsAndTextures()
        {
            // A. Icon Đèn pin (128x128)
            CreateFlashlightIcon($"{IconsFolder}/icon_flashlight.png");

            // B. Icon Radar (128x128)
            CreateRadarIcon($"{IconsFolder}/icon_radar.png");

            // C. Radar Dial (256x256)
            CreateRadarDialTexture($"{TexturesFolder}/radar_dial.png");

            // D. Radar Sweep Cone (256x256)
            CreateRadarSweepTexture($"{TexturesFolder}/radar_sweep.png");

            // E. Radar Blip Dot (32x32)
            CreateRadarBlipTexture($"{TexturesFolder}/radar_blip.png");

            // F. Hotbar Slot Background (64x64 bo góc)
            CreateSlotBgTexture($"{TexturesFolder}/hotbar_slot_bg.png");

            AssetDatabase.Refresh();

            // Cấu hình import Sprite cho các file vừa tạo
            ConfigureSpriteImporter($"{IconsFolder}/icon_flashlight.png");
            ConfigureSpriteImporter($"{IconsFolder}/icon_radar.png");
            ConfigureSpriteImporter($"{TexturesFolder}/radar_dial.png");
            ConfigureSpriteImporter($"{TexturesFolder}/radar_sweep.png");
            ConfigureSpriteImporter($"{TexturesFolder}/radar_blip.png");
            ConfigureSpriteImporter($"{TexturesFolder}/hotbar_slot_bg.png");
        }

        private static void CreateFlashlightIcon(string path)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Vẽ hình đèn pin góc nghiêng 45 độ
            // Thân đèn: hình chữ nhật xoay
            Color bodyColor = new Color(0.85f, 0.85f, 0.88f, 0.95f);
            Color headColor = new Color(1.0f, 0.92f, 0.65f, 1.0f);
            Color beamColor = new Color(1.0f, 0.95f, 0.75f, 0.35f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Tọa độ tương đối xoay 45 độ quanh tâm (64, 64)
                    float dx = x - 64f;
                    float dy = y - 64f;
                    float u = (dx + dy) * 0.7071f; // Dọc thân đèn (-50 đến +50)
                    float v = (-dx + dy) * 0.7071f; // Ngang thân đèn (-20 đến +20)

                    // Thân đèn pin: u từ -32 đến 16, |v| <= 10
                    if (u >= -34f && u <= 14f && Mathf.Abs(v) <= 9f)
                    {
                        tex.SetPixel(x, y, bodyColor);
                    }
                    // Đầu đèn loe: u từ 14 đến 32, v nở từ 9 đến 18
                    else if (u > 14f && u <= 32f)
                    {
                        float maxV = Mathf.Lerp(9f, 18f, (u - 14f) / 18f);
                        if (Mathf.Abs(v) <= maxV)
                        {
                            tex.SetPixel(x, y, headColor);
                        }
                    }
                    // Vòng lens trước: u từ 32 đến 36, |v| <= 18
                    else if (u > 32f && u <= 36f && Mathf.Abs(v) <= 18f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    // Nón ánh sáng phát ra: u từ 36 đến 56, góc loe
                    else if (u > 36f && u <= 56f)
                    {
                        float maxV = Mathf.Lerp(18f, 32f, (u - 36f) / 20f);
                        if (Mathf.Abs(v) <= maxV)
                        {
                            float alpha = Mathf.Lerp(0.45f, 0.05f, (u - 36f) / 20f);
                            tex.SetPixel(x, y, new Color(1.0f, 0.95f, 0.7f, alpha));
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRadarIcon(string path)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color greenLine = new Color(0.2f, 1.0f, 0.35f, 0.95f);
            Color greenFaint = new Color(0.15f, 0.75f, 0.25f, 0.35f);
            Color centerDot = new Color(0.3f, 1.0f, 0.5f, 1.0f);

            float center = 64f;
            float maxR = 52f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > maxR + 1f)
                    {
                        tex.SetPixel(x, y, transparent);
                        continue;
                    }

                    // Vòng ngoài cùng
                    if (Mathf.Abs(dist - maxR) <= 1.8f)
                    {
                        tex.SetPixel(x, y, greenLine);
                    }
                    // Vòng giữa (2/3)
                    else if (Mathf.Abs(dist - maxR * 0.66f) <= 1.2f)
                    {
                        tex.SetPixel(x, y, greenFaint);
                    }
                    // Vòng trong (1/3)
                    else if (Mathf.Abs(dist - maxR * 0.33f) <= 1.2f)
                    {
                        tex.SetPixel(x, y, greenFaint);
                    }
                    // Trục chữ thập
                    else if ((Mathf.Abs(x - center) <= 1.0f || Mathf.Abs(y - center) <= 1.0f) && dist <= maxR)
                    {
                        tex.SetPixel(x, y, greenFaint);
                    }
                    // Chấm tâm người chơi
                    else if (dist <= 3.5f)
                    {
                        tex.SetPixel(x, y, centerDot);
                    }
                    // Tia quét 45 độ
                    else
                    {
                        float angle = Mathf.Atan2(y - center, x - center) * Mathf.Rad2Deg;
                        if (angle >= 30f && angle <= 60f)
                        {
                            float alpha = Mathf.Lerp(0.45f, 0.05f, (angle - 30f) / 30f);
                            tex.SetPixel(x, y, new Color(0.2f, 1f, 0.3f, alpha));
                        }
                        else
                        {
                            tex.SetPixel(x, y, transparent);
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRadarDialTexture(string path)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color bgDark = new Color(0.04f, 0.07f, 0.05f, 0.82f);
            Color gridLine = new Color(0.18f, 0.72f, 0.32f, 0.40f);
            Color outerRing = new Color(0.22f, 0.88f, 0.40f, 0.75f);
            Color centerDot = new Color(0.4f, 1.0f, 0.5f, 1.0f);

            float center = 128f;
            float maxR = 112f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > maxR + 2f)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else if (Mathf.Abs(dist - maxR) <= 2.2f)
                    {
                        tex.SetPixel(x, y, outerRing);
                    }
                    else if (dist <= maxR)
                    {
                        // Vòng 2/3
                        if (Mathf.Abs(dist - maxR * 0.66f) <= 1.4f)
                        {
                            tex.SetPixel(x, y, gridLine);
                        }
                        // Vòng 1/3
                        else if (Mathf.Abs(dist - maxR * 0.33f) <= 1.4f)
                        {
                            tex.SetPixel(x, y, gridLine);
                        }
                        // Trục chữ thập
                        else if (Mathf.Abs(x - center) <= 1.2f || Mathf.Abs(y - center) <= 1.2f)
                        {
                            tex.SetPixel(x, y, gridLine);
                        }
                        // Chấm tâm
                        else if (dist <= 4.0f)
                        {
                            tex.SetPixel(x, y, centerDot);
                        }
                        // Nền tròn
                        else
                        {
                            tex.SetPixel(x, y, bgDark);
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRadarSweepTexture(string path)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);

            float center = 128f;
            float maxR = 112f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > maxR || dist < 2f)
                    {
                        tex.SetPixel(x, y, transparent);
                        continue;
                    }

                    // Góc tính từ trục Y thẳng đứng (0 = thẳng đứng trên)
                    // Hướng kim đồng hồ
                    float angle = Mathf.Atan2(x - center, y - center) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;

                    // Kim quét sắc ở góc 0 độ, vệt đuôi mờ dần về góc 40 độ
                    if (angle <= 40f)
                    {
                        float t = angle / 40f; // 0 = mép quét sắc, 1 = đuôi
                        float alpha = Mathf.Lerp(0.85f, 0.0f, t);
                        tex.SetPixel(x, y, new Color(0.25f, 1.0f, 0.45f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRadarBlipTexture(string path)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            float center = 16f;
            float radius = 13f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                    else
                    {
                        float falloff = 1f - (dist / radius);
                        float alpha = Mathf.SmoothStep(0f, 1f, falloff);
                        tex.SetPixel(x, y, new Color(0.3f, 1.0f, 0.45f, alpha));
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSlotBgTexture(string path)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color darkBg = new Color(0.08f, 0.08f, 0.10f, 0.78f);
            Color border = new Color(0.35f, 0.35f, 0.40f, 0.50f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < 2 || x >= size - 2 || y < 2 || y >= size - 2);
                    tex.SetPixel(x, y, isBorder ? border : darkBg);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        #endregion

        #region 2. Tạo Materials

        private static void CreateMaterials(out Material matFlashlightBody, out Material matFlashlightGlass, out Material matRadarBody, out Material matRadarScreen)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Thân đèn pin: xám đen kim loại
            string pathFb = $"{MaterialsFolder}/M_FlashlightBody.mat";
            matFlashlightBody = AssetDatabase.LoadAssetAtPath<Material>(pathFb);
            if (matFlashlightBody == null)
            {
                matFlashlightBody = new Material(litShader);
                matFlashlightBody.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.20f, 1f));
                matFlashlightBody.SetFloat("_Metallic", 0.65f);
                matFlashlightBody.SetFloat("_Smoothness", 0.55f);
                AssetDatabase.CreateAsset(matFlashlightBody, pathFb);
            }

            // 2. Mặt kính đèn pin: phát sáng emission
            string pathFg = $"{MaterialsFolder}/M_FlashlightGlass.mat";
            matFlashlightGlass = AssetDatabase.LoadAssetAtPath<Material>(pathFg);
            if (matFlashlightGlass == null)
            {
                matFlashlightGlass = new Material(litShader);
                matFlashlightGlass.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.95f, 1f));
                matFlashlightGlass.EnableKeyword("_EMISSION");
                matFlashlightGlass.SetColor("_EmissionColor", Color.black);
                matFlashlightGlass.SetFloat("_Smoothness", 0.9f);
                AssetDatabase.CreateAsset(matFlashlightGlass, pathFg);
            }

            // 3. Thân Radar: xanh quân đội sẫm mờ
            string pathRb = $"{MaterialsFolder}/M_RadarBody.mat";
            matRadarBody = AssetDatabase.LoadAssetAtPath<Material>(pathRb);
            if (matRadarBody == null)
            {
                matRadarBody = new Material(litShader);
                matRadarBody.SetColor("_BaseColor", new Color(0.12f, 0.15f, 0.13f, 1f));
                matRadarBody.SetFloat("_Metallic", 0.2f);
                matRadarBody.SetFloat("_Smoothness", 0.35f);
                AssetDatabase.CreateAsset(matRadarBody, pathRb);
            }

            // 4. Màn hình Radar: phát sáng xanh nhẹ
            string pathRs = $"{MaterialsFolder}/M_RadarScreen.mat";
            matRadarScreen = AssetDatabase.LoadAssetAtPath<Material>(pathRs);
            if (matRadarScreen == null)
            {
                matRadarScreen = new Material(litShader);
                matRadarScreen.SetColor("_BaseColor", new Color(0.04f, 0.12f, 0.05f, 1f));
                matRadarScreen.EnableKeyword("_EMISSION");
                matRadarScreen.SetColor("_EmissionColor", new Color(0.05f, 0.25f, 0.08f));
                matRadarScreen.SetFloat("_Smoothness", 0.85f);
                AssetDatabase.CreateAsset(matRadarScreen, pathRs);
            }
        }

        #endregion

        #region 3. Tạo Prefabs mô hình cầm tay

        private static GameObject CreateFlashlightPrefab(Material matBody, Material matGlass)
        {
            string prefabPath = $"{PrefabsItemsFolder}/Flashlight.prefab";

            GameObject root = new GameObject("Flashlight");
            var sway = root.AddComponent<ViewModelSway>();
            sway.DefaultPosition = new Vector3(0.22f, -0.20f, 0.42f);
            sway.DefaultRotation = new Vector3(2.5f, -3.0f, 1.0f);

            var flashlightItem = root.AddComponent<FlashlightItem>();

            // A. Thân đèn (Cylinder)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.07f, 0.08f, 0.07f); // Bán kính 0.035m, dài 0.16m
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var bodyRend = body.GetComponent<MeshRenderer>();
            bodyRend.sharedMaterial = matBody;
            bodyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // B. Đầu đèn loe (Cylinder lớn hơn ở phía trước)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            head.transform.localScale = new Vector3(0.10f, 0.025f, 0.10f); // Bán kính 0.05m
            Object.DestroyImmediate(head.GetComponent<Collider>());
            var headRend = head.GetComponent<MeshRenderer>();
            headRend.sharedMaterial = matBody;
            headRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // C. Mặt kính phía trước (Cylinder mỏng đè lên đầu đèn)
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.name = "GlassLens";
            glass.transform.SetParent(head.transform, false);
            glass.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            glass.transform.localRotation = Quaternion.identity;
            glass.transform.localScale = new Vector3(0.92f, 0.08f, 0.92f);
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            var glassRend = glass.GetComponent<MeshRenderer>();
            glassRend.sharedMaterial = matGlass;
            glassRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // D. Nút bấm (Cube nhỏ trên thân)
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = "SwitchButton";
            btn.transform.SetParent(root.transform, false);
            btn.transform.localPosition = new Vector3(0f, 0.038f, 0.08f);
            btn.transform.localScale = new Vector3(0.018f, 0.012f, 0.03f);
            Object.DestroyImmediate(btn.GetComponent<Collider>());
            var btnRend = btn.GetComponent<MeshRenderer>();
            btnRend.sharedMaterial = matBody;
            btnRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // E. Spot Light thực tế
            GameObject lightGo = new GameObject("SpotLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.22f);
            lightGo.transform.localRotation = Quaternion.identity;
            Light spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.957f, 0.839f);
            spot.range = 22.0f;
            spot.spotAngle = 55.0f;
            spot.innerSpotAngle = 30.0f;
            spot.intensity = 2.8f;
            spot.shadows = LightShadows.Soft;

            // F. Nối biến vào FlashlightItem
            SerializedObject so = new SerializedObject(flashlightItem);
            so.FindProperty("spotLight").objectReferenceValue = spot;
            so.FindProperty("glassRenderer").objectReferenceValue = glassRend;
            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Items/flashlight_click.wav");
            so.FindProperty("toggleClickClip").objectReferenceValue = clickClip;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRadarPrefab(Material matBody, Material matScreen)
        {
            string prefabPath = $"{PrefabsItemsFolder}/Radar.prefab";

            GameObject root = new GameObject("Radar");
            var sway = root.AddComponent<ViewModelSway>();
            sway.DefaultPosition = new Vector3(0.05f, -0.16f, 0.38f);
            sway.DefaultRotation = new Vector3(15.0f, -5.0f, 0f);

            var radarItem = root.AddComponent<RadarItem>();

            // A. Thân thiết bị (Hộp chữ nhật 0.09 x 0.14 x 0.02m)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.09f, 0.14f, 0.022f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var bodyRend = body.GetComponent<MeshRenderer>();
            bodyRend.sharedMaterial = matBody;
            bodyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // B. Màn hình radar (Quad/Cube mỏng trên mặt trước)
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(body.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.12f, 0.52f);
            screen.transform.localScale = new Vector3(0.82f, 0.65f, 0.05f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var screenRend = screen.GetComponent<MeshRenderer>();
            screenRend.sharedMaterial = matScreen;
            screenRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // C. Ăng-ten (Cylinder nhỏ trên đỉnh góc trái)
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "Antenna";
            antenna.transform.SetParent(body.transform, false);
            antenna.transform.localPosition = new Vector3(-0.38f, 0.65f, 0f);
            antenna.transform.localScale = new Vector3(0.06f, 0.32f, 0.06f);
            Object.DestroyImmediate(antenna.GetComponent<Collider>());
            var antRend = antenna.GetComponent<MeshRenderer>();
            antRend.sharedMaterial = matBody;
            antRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // D. Nối biến vào RadarItem
            SerializedObject so = new SerializedObject(radarItem);
            so.FindProperty("screenRenderer").objectReferenceValue = screenRend;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        #endregion

        #region 4. Tạo ItemData ScriptableObjects

        private static ItemData CreateOrUpdateItemData(string assetName, string displayName, ItemType itemType, string iconPath, GameObject heldPrefab)
        {
            string path = $"{ItemsDataFolder}/{assetName}.asset";
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(data, path);
            }

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            data.SetData(displayName, itemType, icon, heldPrefab);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AssignItemDataToPrefab(GameObject prefab, ItemData data)
        {
            if (prefab == null || data == null) return;
            var held = prefab.GetComponent<HeldItem>();
            if (held != null)
            {
                var so = new SerializedObject(held);
                so.FindProperty("itemData").objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
            }
        }

        #endregion

        #region 5. Tạo UI Prefabs

        private static GameObject CreateHotbarUIPrefab()
        {
            string prefabPath = $"{PrefabsUIFolder}/HotbarUI.prefab";

            GameObject root = new GameObject("HotbarUI", typeof(RectTransform), typeof(CanvasGroup), typeof(HotbarUI));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 28f);
            rootRt.sizeDelta = new Vector2(356f, 80f);

            CanvasGroup rootCg = root.GetComponent<CanvasGroup>();
            rootCg.blocksRaycasts = false;
            rootCg.interactable = false;

            HotbarUI hotbarComp = root.GetComponent<HotbarUI>();
            SerializedObject so = new SerializedObject(hotbarComp);

            Sprite slotBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesFolder}/hotbar_slot_bg.png");

            // Tạo 4 ô con
            float startX = -138f;
            float stepX = 92f;

            for (int i = 0; i < 4; i++)
            {
                GameObject slotGo = new GameObject($"Slot_{i + 1}", typeof(RectTransform));
                RectTransform slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.SetParent(rootRt, false);
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                slotRt.anchoredPosition = new Vector2(startX + i * stepX, 0f);
                slotRt.sizeDelta = new Vector2(80f, 80f);

                // Nền ô
                GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                RectTransform bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.SetParent(slotRt, false);
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
                Image bgImg = bgGo.GetComponent<Image>();
                bgImg.sprite = slotBgSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = new Color(0.08f, 0.08f, 0.10f, 0.75f);
                bgImg.raycastTarget = false;

                // Viền ô (Border)
                GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
                RectTransform borderRt = borderGo.GetComponent<RectTransform>();
                borderRt.SetParent(slotRt, false);
                borderRt.anchorMin = Vector2.zero;
                borderRt.anchorMax = Vector2.one;
                borderRt.sizeDelta = Vector2.zero;
                Image borderImg = borderGo.GetComponent<Image>();
                borderImg.sprite = slotBgSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = (i == 0) ? new Color(1.0f, 0.96f, 0.88f, 1.0f) : new Color(0.35f, 0.35f, 0.35f, 0.45f);
                borderImg.raycastTarget = false;

                // Icon vật phẩm
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.SetParent(slotRt, false);
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(56f, 56f);
                Image iconImg = iconGo.GetComponent<Image>();
                iconImg.raycastTarget = false;
                iconGo.SetActive(false);

                // Số thứ tự 1-4 ở góc trên-trái
                GameObject numGo = new GameObject("NumberText", typeof(RectTransform), typeof(TextMeshProUGUI));
                RectTransform numRt = numGo.GetComponent<RectTransform>();
                numRt.SetParent(slotRt, false);
                numRt.anchorMin = new Vector2(0f, 1f);
                numRt.anchorMax = new Vector2(0f, 1f);
                numRt.pivot = new Vector2(0f, 1f);
                numRt.anchoredPosition = new Vector2(6f, -4f);
                numRt.sizeDelta = new Vector2(24f, 24f);
                TextMeshProUGUI numText = numGo.GetComponent<TextMeshProUGUI>();
                numText.text = (i + 1).ToString();
                numText.fontSize = 14f;
                numText.fontStyle = FontStyles.Bold;
                numText.alignment = TextAlignmentOptions.TopLeft;
                numText.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
                numText.raycastTarget = false;

                // Lưu vào SerializedObject
                SerializedProperty slotProp = so.FindProperty("slots").GetArrayElementAtIndex(i);
                slotProp.FindPropertyRelative("rootRect").objectReferenceValue = slotRt;
                slotProp.FindPropertyRelative("backgroundImage").objectReferenceValue = bgImg;
                slotProp.FindPropertyRelative("borderImage").objectReferenceValue = borderImg;
                slotProp.FindPropertyRelative("iconImage").objectReferenceValue = iconImg;
                slotProp.FindPropertyRelative("numberText").objectReferenceValue = numText;
            }

            // Dòng tên vật phẩm (ItemNameText) nằm ngay phía trên thanh Hotbar
            GameObject nameGo = new GameObject("ItemNameText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(rootRt, false);
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.anchoredPosition = new Vector2(0f, 105f); // Cách mép trên hotbar ~25px
            nameRt.sizeDelta = new Vector2(300f, 32f);

            CanvasGroup nameCg = nameGo.GetComponent<CanvasGroup>();
            nameCg.alpha = 0f;
            nameCg.blocksRaycasts = false;

            TextMeshProUGUI nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.text = "";
            nameText.fontSize = 20f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            so.FindProperty("itemNameText").objectReferenceValue = nameText;
            so.FindProperty("itemNameCanvasGroup").objectReferenceValue = nameCg;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRadarHUDPrefab()
        {
            string prefabPath = $"{PrefabsUIFolder}/RadarHUD.prefab";

            GameObject root = new GameObject("RadarHUD", typeof(RectTransform), typeof(CanvasGroup), typeof(RadarHUD));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(1f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0f);
            rootRt.pivot = new Vector2(1f, 0f);
            rootRt.anchoredPosition = new Vector2(-40f, 40f);
            rootRt.sizeDelta = new Vector2(220f, 220f);

            CanvasGroup rootCg = root.GetComponent<CanvasGroup>();
            rootCg.alpha = 0f;
            rootCg.blocksRaycasts = false;
            rootCg.interactable = false;

            RadarHUD radarComp = root.GetComponent<RadarHUD>();
            SerializedObject so = new SerializedObject(radarComp);

            Sprite dialSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesFolder}/radar_dial.png");
            Sprite sweepSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesFolder}/radar_sweep.png");
            Sprite blipSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesFolder}/radar_blip.png");

            // Nền mặt số Radar (Dial)
            GameObject dialGo = new GameObject("RadarDial", typeof(RectTransform), typeof(Image));
            RectTransform dialRt = dialGo.GetComponent<RectTransform>();
            dialRt.SetParent(rootRt, false);
            dialRt.anchorMin = Vector2.zero;
            dialRt.anchorMax = Vector2.one;
            dialRt.sizeDelta = Vector2.zero;
            Image dialImg = dialGo.GetComponent<Image>();
            dialImg.sprite = dialSprite;
            dialImg.raycastTarget = false;

            // Kim quét (SweepHand)
            GameObject sweepGo = new GameObject("SweepHand", typeof(RectTransform), typeof(Image));
            RectTransform sweepRt = sweepGo.GetComponent<RectTransform>();
            sweepRt.SetParent(rootRt, false);
            sweepRt.anchorMin = Vector2.zero;
            sweepRt.anchorMax = Vector2.one;
            sweepRt.sizeDelta = Vector2.zero;
            Image sweepImg = sweepGo.GetComponent<Image>();
            sweepImg.sprite = sweepSprite;
            sweepImg.raycastTarget = false;

            // Khu vực chứa Blip
            GameObject blipContainerGo = new GameObject("BlipContainer", typeof(RectTransform));
            RectTransform blipContainerRt = blipContainerGo.GetComponent<RectTransform>();
            blipContainerRt.SetParent(rootRt, false);
            blipContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            blipContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            blipContainerRt.pivot = new Vector2(0.5f, 0.5f);
            blipContainerRt.anchoredPosition = Vector2.zero;
            blipContainerRt.sizeDelta = Vector2.zero;

            // Pool 4 Blip Images
            SerializedProperty blipPoolProp = so.FindProperty("blipPool");
            for (int i = 0; i < 4; i++)
            {
                GameObject blipGo = new GameObject($"Blip_{i + 1}", typeof(RectTransform), typeof(Image));
                RectTransform blipRt = blipGo.GetComponent<RectTransform>();
                blipRt.SetParent(blipContainerRt, false);
                blipRt.anchorMin = new Vector2(0.5f, 0.5f);
                blipRt.anchorMax = new Vector2(0.5f, 0.5f);
                blipRt.pivot = new Vector2(0.5f, 0.5f);
                blipRt.anchoredPosition = Vector2.zero;
                blipRt.sizeDelta = new Vector2(14f, 14f);
                Image blipImg = blipGo.GetComponent<Image>();
                blipImg.sprite = blipSprite;
                blipImg.color = new Color(0.2f, 1.0f, 0.45f, 0f);
                blipImg.raycastTarget = false;
                blipGo.SetActive(false);

                blipPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = blipImg;
            }

            // Dòng chữ trạng thái (StatusText)
            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.SetParent(rootRt, false);
            statusRt.anchorMin = new Vector2(0.5f, 0f);
            statusRt.anchorMax = new Vector2(0.5f, 0f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -8f);
            statusRt.sizeDelta = new Vector2(180f, 24f);
            TextMeshProUGUI statusText = statusGo.GetComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 13f;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = new Color(0.3f, 1.0f, 0.5f, 0.9f);
            statusText.raycastTarget = false;

            // Âm thanh
            AudioClip scanClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Items/radar_scan.wav");
            AudioClip blipClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Items/radar_blip.wav");

            so.FindProperty("sweepTransform").objectReferenceValue = sweepRt;
            so.FindProperty("blipContainer").objectReferenceValue = blipContainerRt;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("scanSound").objectReferenceValue = scanClip;
            so.FindProperty("blipSound").objectReferenceValue = blipClip;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        #endregion

        #region 6. Gắn vào Scene

        private static void SetupSceneInstances(GameObject hotbarPrefab, GameObject radarHUDPrefab, ItemData flashlightData, ItemData radarData)
        {
            // 1. Tìm Canvas có sẵn trong Scene (Canvas sortingOrder=0 chứa StaminaUI)
            Canvas targetCanvas = null;
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.gameObject.name == "Canvas")
                {
                    targetCanvas = c;
                    break;
                }
            }
            if (targetCanvas == null && allCanvases.Length > 0) targetCanvas = allCanvases[0];

            if (targetCanvas == null)
            {
                Debug.LogError("[InventoryAutoSetup] Không tìm thấy Canvas nào trong Scene!");
                return;
            }

            // Gắn HotbarUI vào Canvas nếu chưa có
            Transform existingHotbar = targetCanvas.transform.Find("HotbarUI");
            HotbarUI hotbarUIInstance;
            if (existingHotbar != null)
            {
                hotbarUIInstance = existingHotbar.GetComponent<HotbarUI>();
            }
            else
            {
                GameObject hotbarGo = (GameObject)PrefabUtility.InstantiatePrefab(hotbarPrefab, targetCanvas.transform);
                hotbarGo.name = "HotbarUI";
                hotbarUIInstance = hotbarGo.GetComponent<HotbarUI>();
                Undo.RegisterCreatedObjectUndo(hotbarGo, "Create HotbarUI in Canvas");
            }

            // Gắn RadarHUD vào Canvas nếu chưa có
            Transform existingRadarHUD = targetCanvas.transform.Find("RadarHUD");
            RadarHUD radarHUDInstance;
            if (existingRadarHUD != null)
            {
                radarHUDInstance = existingRadarHUD.GetComponent<RadarHUD>();
            }
            else
            {
                GameObject radarGo = (GameObject)PrefabUtility.InstantiatePrefab(radarHUDPrefab, targetCanvas.transform);
                radarGo.name = "RadarHUD";
                radarHUDInstance = radarGo.GetComponent<RadarHUD>();
                Undo.RegisterCreatedObjectUndo(radarGo, "Create RadarHUD in Canvas");
            }

            // 2. Cài đặt vào Player
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[InventoryAutoSetup] Không tìm thấy PlayerController trong Scene!");
                return;
            }

            HotbarInventory hotbarInv = player.GetComponent<HotbarInventory>();
            if (hotbarInv == null)
            {
                hotbarInv = Undo.AddComponent<HotbarInventory>(player.gameObject);
            }

            // Gắn slots vào HotbarInventory
            SerializedObject invSo = new SerializedObject(hotbarInv);
            SerializedProperty slotsProp = invSo.FindProperty("slots");
            slotsProp.GetArrayElementAtIndex(0).objectReferenceValue = flashlightData;
            slotsProp.GetArrayElementAtIndex(1).objectReferenceValue = radarData;
            slotsProp.GetArrayElementAtIndex(2).objectReferenceValue = null;
            slotsProp.GetArrayElementAtIndex(3).objectReferenceValue = null;

            AudioClip switchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Items/hotbar_switch.wav");
            invSo.FindProperty("switchSound").objectReferenceValue = switchClip;
            invSo.FindProperty("hotbarUI").objectReferenceValue = hotbarUIInstance;

            // Đảm bảo ViewmodelHolder dưới Camera
            Transform camTransform = player.playerCamera != null ? player.playerCamera : player.transform.Find("Camera");
            if (camTransform != null)
            {
                Transform holder = camTransform.Find("ViewmodelHolder");
                if (holder == null)
                {
                    GameObject holderGo = new GameObject("ViewmodelHolder");
                    Undo.RegisterCreatedObjectUndo(holderGo, "Create ViewmodelHolder under Camera");
                    holderGo.transform.SetParent(camTransform, false);
                    holderGo.transform.localPosition = Vector3.zero;
                    holderGo.transform.localRotation = Quaternion.identity;
                    holder = holderGo.transform;
                }
                invSo.FindProperty("viewmodelHolder").objectReferenceValue = holder;
            }

            invSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(player.gameObject);
            EditorUtility.SetDirty(targetCanvas.gameObject);

            // Lưu scene
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        }

        #endregion
    }
}
