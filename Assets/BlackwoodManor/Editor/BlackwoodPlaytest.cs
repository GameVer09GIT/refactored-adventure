using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Blackwood.Core;
using Blackwood.Interaction;
using Blackwood.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Chơi thử tự động từ menu tới màn kết thúc, đi qua đúng các tương tác thật (IInteractable.Interact, nút UI, két sắt,
    /// bàn thờ nến…) theo thứ tự cốt truyện; kiểm tra cờ/chương/chìa sau từng bước, đếm lỗi Console, chụp ảnh các mốc.
    /// Ngoài ra: thử trốn tủ khi Mrs. Peake đứng ngay ngoài, và đi bộ thật (CharacterController) qua các chặng để bắt chỗ kẹt.
    ///
    /// An toàn dữ liệu: chạy trên slot lưu 2 (game tự lưu đè slot đang chơi và kết thúc sẽ xóa slot đó). Không đụng slot 0/1.
    /// Kết quả: Console (tiền tố PLAYTEST) + Temp/Blackwood/playtest_report.txt, ảnh Temp/Blackwood/pt_*.png.
    /// </summary>
    public static class BlackwoodPlaytest
    {
        public const int TestSlot = 2;
        const float GF = 11f, F2 = 18f;

        class Step
        {
            public string Name;
            public Action Enter;
            public Func<bool> Done;
            public float Timeout = 10f;
            public Func<string> Check;
            public bool Critical;
        }

        static readonly List<Step> steps = new List<Step>();
        static readonly List<string> report = new List<string>();
        static readonly List<string> errors = new List<string>();
        static int index, failures, deaths;
        static double stepStart;
        static bool entered, running;
        static string tag = "";

        public static bool Running => running;

        // ------------------------------------------------------------------ menu
        [MenuItem("Blackwood/Test/Auto Playthrough - Evelyn (Release)")]
        static void MenuEvelyn() => Start(PlayerCharacter.Evelyn, EndingId.Release, true);
        [MenuItem("Blackwood/Test/Auto Playthrough - Alex (New Master)")]
        static void MenuAlex() => Start(PlayerCharacter.Alex, EndingId.NewMaster, false);
        [MenuItem("Blackwood/Test/Auto Playthrough - Coward (window)")]
        static void MenuCoward() => Start(PlayerCharacter.Evelyn, EndingId.Coward, false);
        [MenuItem("Blackwood/Test/Walk Test (all doors open)")]
        static void MenuWalk() => StartWalk();

        /// <summary>Phải đang ở Play (màn menu). <paramref name="extra"/>: thêm bước trốn tủ.</summary>
        public static string Start(PlayerCharacter who, EndingId ending, bool extra)
        {
            if (!EditorApplication.isPlaying) return "Cần vào Play trước (đang ở menu chính).";
            steps.Clear();
            tag = who + "_" + ending;
            BuildCampaign(who, ending, extra);
            Begin();
            return "PLAYTEST bắt đầu: " + tag + " (" + steps.Count + " bước)";
        }

        public static string StartWalk()
        {
            if (!EditorApplication.isPlaying) return "Cần vào Play trước (đang ở menu chính).";
            steps.Clear();
            tag = "Walk";
            BuildWalk();
            Begin();
            return "PLAYTEST bắt đầu: Walk (" + steps.Count + " bước)";
        }

        static void Begin()
        {
            report.Clear(); errors.Clear();
            index = 0; failures = 0; deaths = 0; entered = false; running = true;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            DeathScreenManager.OnPlayerDied -= OnDeath; DeathScreenManager.OnPlayerDied += OnDeath;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Log("=== " + tag + " " + DateTime.Now.ToString("HH:mm:ss"));
        }

        static void OnDeath() { deaths++; Log("  ! người chơi chết"); }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (msg.StartsWith("Curl error") || msg.Contains("Licensing") || msg.Contains("WebSocket")) return;
            if (errors.Count < 40) errors.Add(type + ": " + msg.Split('\n')[0] + (string.IsNullOrEmpty(stack) ? "" : " @ " + stack.Split('\n')[0]));
        }

        static void Log(string s)
        {
            report.Add(s);
            Debug.Log("PLAYTEST " + s);
        }

        static void Tick()
        {
            if (!running) { EditorApplication.update -= Tick; return; }
            if (!EditorApplication.isPlaying) { Log("Play bị dừng giữa chừng"); Finish(); return; }
            if (index >= steps.Count) { Finish(); return; }
            var s = steps[index];
            double now = EditorApplication.timeSinceStartup;
            try
            {
                if (!entered) { entered = true; stepStart = now; s.Enter?.Invoke(); }
                bool done = s.Done == null || s.Done();
                bool timeout = now - stepStart > s.Timeout;
                if (!done && !timeout) return;
                string fail = !done ? "hết thời gian chờ (" + s.Timeout + " s)" : s.Check?.Invoke();
                if (fail == null) Log($"[{index + 1:00}] OK   {s.Name} ({now - stepStart:F1} s)");
                else { failures++; Log($"[{index + 1:00}] FAIL {s.Name}: {fail}"); if (s.Critical) { Log("Bước bắt buộc hỏng → dừng"); Finish(); return; } }
            }
            catch (Exception e)
            {
                failures++;
                Log($"[{index + 1:00}] EXC  {s.Name}: {e.GetType().Name}: {e.Message}");
                if (s.Critical) { Finish(); return; }
            }
            index++; entered = false;
        }

        static void Finish()
        {
            running = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            DeathScreenManager.OnPlayerDied -= OnDeath;
            Log($"=== KẾT QUẢ {tag}: {steps.Count - failures}/{steps.Count} bước OK, {errors.Count} lỗi Console, {deaths} lần chết");
            foreach (var e in errors) Log("  console " + e);
            Directory.CreateDirectory("Temp/Blackwood");
            File.AppendAllText("Temp/Blackwood/playtest_report.txt", string.Join("\n", report) + "\n\n");
            Debug.Log("PLAYTEST DONE " + tag + " failures=" + failures + " errors=" + errors.Count + " deaths=" + deaths);
        }

        // ------------------------------------------------------------------ helpers
        static PlayerController PC => Object.FindFirstObjectByType<PlayerController>();
        static GameManager GM => GameManager.Instance;
        static double Elapsed => EditorApplication.timeSinceStartup - stepStart;
        static Func<bool> After(float s) => () => Elapsed > s;

        static void Add(string name, Action enter, Func<bool> done, Func<string> check) => Add(name, enter, done, 10f, check);

        static void Add(string name, Action enter, Func<bool> done = null, float timeout = 10f, Func<string> check = null, bool critical = false)
            => steps.Add(new Step { Name = name, Enter = enter, Done = done, Timeout = timeout, Check = check, Critical = critical });

        static T Field<T>(object o, string name)
        {
            var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f != null ? (T)f.GetValue(o) : default;
        }

        static object Call(object o, string name, params object[] args)
        {
            var m = o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) throw new MissingMethodException(o.GetType().Name + "." + name);
            return m.Invoke(o, args);
        }

        static Transform Find(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);

        static void Teleport(Vector3 pos, float yaw, float pitch = 0f)
        {
            var pc = PC;
            var cc = pc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            pc.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            pc.SetPitch(pitch);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        /// <summary>Đứng cạnh vật (điểm NavMesh gần nhất trong 2 m, quay mặt và cúi/ngửa nhìn vào vật) như người chơi thật.</summary>
        static void StandNear(Transform t)
        {
            var target = t.GetComponentInChildren<Renderer>() is Renderer r ? r.bounds.center : t.position;
            Vector3 stand = target;
            bool found = false;
            for (float rad = 0.9f; rad <= 2.0f && !found; rad += 0.3f)
                for (int k = 0; k < 12 && !found; k++)
                {
                    var probe = target + Quaternion.Euler(0, k * 30f, 0) * Vector3.forward * rad;
                    if (NavMesh.SamplePosition(probe, out var hit, 1.2f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - target.y) < 2.2f
                        && !Physics.Linecast(hit.position + Vector3.up * 1.5f, target, ~0, QueryTriggerInteraction.Ignore) )
                    { stand = hit.position; found = true; }
                }
            if (!found && NavMesh.SamplePosition(target, out var h2, 2.5f, NavMesh.AllAreas)) stand = h2.position;
            var flat = target - stand; flat.y = 0f;
            float yaw = flat.sqrMagnitude > 0.001f ? Quaternion.LookRotation(flat).eulerAngles.y : 0f;
            var eye = stand + Vector3.up * 1.6f;
            float pitch = -Mathf.Atan2(target.y - eye.y, Mathf.Max(0.1f, flat.magnitude)) * Mathf.Rad2Deg;
            Teleport(stand, yaw, Mathf.Clamp(pitch, -70f, 70f));
        }

        static IInteractable Interactable(string name)
        {
            var t = Find(name);
            if (t == null) return null;
            return t.GetComponentsInChildren<MonoBehaviour>(true).OfType<IInteractable>().FirstOrDefault()
                   ?? t.GetComponentsInParent<MonoBehaviour>(true).OfType<IInteractable>().FirstOrDefault();
        }

        static string Use(string name)
        {
            var t = Find(name);
            if (t == null) return "không tìm thấy " + name;
            var it = Interactable(name);
            if (it == null) return name + " không có IInteractable";
            StandNear(t);
            if (!it.CanInteract()) return name + " CanInteract = false (prompt: " + it.Prompt + ")";
            it.Interact();
            return null;
        }

        static void CloseReader()
        {
            var r = DocumentReaderUI.Instance;
            if (r != null && r.IsOpen) r.Close();
        }

        static string Expect(params StoryFlag[] flags)
        {
            var miss = flags.Where(f => GM == null || !GM.Has(f)).ToArray();
            return miss.Length == 0 ? null : "thiếu cờ " + string.Join(", ", miss);
        }

        static void Shot(string name) => ScreenCapture.CaptureScreenshot("Temp/Blackwood/pt_" + name + ".png");

        static Button FindButton(string rootName, int index)
        {
            var root = Find(rootName);
            if (root == null) return null;
            var bs = root.GetComponentsInChildren<Button>(true);
            return index < bs.Length ? bs[index] : null;
        }

        static Button FindButtonByLabel(string label)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var t = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (t != null && t.text.Trim() == label) return b;
            }
            return null;
        }

        /// <summary>Giả lập nhấn/nhả Space qua Input System (sự kiện được xử lý ở vòng update kế tiếp của game).</summary>
        static void KeySpace(bool down)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            var st = down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space) : new UnityEngine.InputSystem.LowLevel.KeyboardState();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, st);
        }

        static void HousekeeperAway()
        {
            var hk = Object.FindFirstObjectByType<AI.HousekeeperAI>();
            if (hk == null) return;
            var ag = hk.GetComponent<NavMeshAgent>();
            var far = new Vector3(68f, F2, 20f);
            if (ag != null && ag.isOnNavMesh) ag.Warp(far); else hk.transform.position = far;
        }

        // ------------------------------------------------------------------ kịch bản chính
        static void BuildCampaign(PlayerCharacter who, EndingId ending, bool extra)
        {
            Add("Menu chính", null, () => GameFlow.State == FlowState.Menu && Elapsed > 1.5f, 60f, null, true);
            Add("Trò chơi mới (" + who + ", slot " + TestSlot + ")", () =>
            {
                SaveSystem.CurrentSlot = TestSlot;
                GameFlow.PendingCharacter = who;
                SaveSystem.PendingLoad = null;
                PlayerPrefs.SetInt("kh_deaths_run", 0);
                GameFlow.PendingStart = StartMode.NewGame;
                ModalUI.Reset();
                UI.LoadingScreen.Load();
            }, () => GameFlow.State == FlowState.Playing && PC != null, 300f,
                () => GM.Character != who ? "sai nhân vật " + GM.Character : Expect(StoryFlag.GateLoop3), true);
            Add("Ổn định, tắt AI khi đi cốt truyện", () =>
            {
                PC.mouseSensitivity = 0f;
                DebugTools.CheatSettings.AIEnabled = false;
                HousekeeperAway();
                Items.BatteryPool.Instance?.SetValue(100f);
                Shot(tag + "_00_gate");
            }, After(1.5f));

            // Giai đoạn 1 — cửa chính sập sau lưng
            Add("Vào nhà: cửa chính sập", () => Teleport(new Vector3(80.7f, GF, 10.4f), 0f), () => GM.Has(StoryFlag.EnteredHouse), 6f,
                () => GM.Stage != GameStage.Stage1_GroundFloor ? "chương " + GM.Stage : null);
            Add("Tờ biên bản gắn vào cánh cửa", () => Teleport(new Vector3(80.3f, GF, 9.4f), 180f, -5f), After(2.5f), () =>
            {
                Shot(tag + "_01_door_note");
                var doc = Find("Doc_BienBan2009");
                if (doc == null) return "không có Doc_BienBan2009";
                var door = doc.GetComponentInParent<DoorInteractable>();
                if (door == null) return "tờ giấy không gắn vào cánh cửa (parent = " + (doc.parent ? doc.parent.name : "-") + ")";
                if (door.IsOpen) return "cửa chính chưa đóng sau khi vào nhà";
                return null;
            });

            if (ending == EndingId.Coward)
            {
                Add("Nhặt ba lô của Julian (xà beng)", () => Use("Pickup_QuanBackpack"), After(1f), () => { CloseReader(); return Expect(StoryFlag.HasCrowbar); });
                Add("Cạy cửa sổ → hộp xác nhận", () => Use("HallWindow_Pryable"), After(1f), () =>
                {
                    Shot(tag + "_02_confirm");
                    if (FindButtonByLabel(T.Get("ui.yes")) == null) return "không thấy nút Có";
                    if (Cursor.lockState == CursorLockMode.Locked) return "chuột vẫn bị khóa khi hộp xác nhận hiện";
                    if (!ModalUI.IsAnyOpen) return "hộp xác nhận không chặn điều khiển (ModalUI)";
                    return null;
                });
                Add("Chọn Có → kết ẩn", () => FindButtonByLabel(T.Get("ui.yes"))?.onClick.Invoke(),
                    () => Find("BW_EndingCanvas") != null, 120f, () => GM.Has(StoryFlag.EndingSecret) ? null : "thiếu cờ EndingSecret");
                AddEndingTail();
                return;
            }

            Add("Đọc biên bản 2009", () => Use("Doc_BienBan2009"), After(0.8f), () =>
            {
                bool open = DocumentReaderUI.Instance != null && DocumentReaderUI.Instance.IsOpen;
                if (open) Shot(tag + "_02_reader");
                CloseReader();
                return Notebook.NotebookManager.Instance.Contains("DOC_2009_BienBan") ? null : "sổ tay chưa có DOC_2009_BienBan";
            });
            Add("Nhặt pin ở sảnh", () => Use("Pickup_Battery_Hall"), After(0.5f));
            Add("Đọc bảng đồng 1921", () => Use("Doc_BangDong1921"), After(0.8f), () => { CloseReader(); return null; });
            Add("Nhặt ba lô của Julian (xà beng)", () => Use("Pickup_QuanBackpack"), After(1f), () => { CloseReader(); return Expect(StoryFlag.GotQuanBackpack, StoryFlag.HasCrowbar); });
            foreach (var n in new[] { "Doc_ThiepTanGia1978", "Doc_DiThuLam1996", "Pickup_MauBao1953", "Pickup_Film_Study", "Pickup_Tape2" })
            {
                string nn = n;
                Add("Tài liệu/đồ: " + nn, () => { var e = Use(nn); if (e != null) Log("  " + e); }, After(0.8f), () => { CloseReader(); return null; });
            }

            Puzzles.SafeKeypad safe = null;
            Add("Két sắt: mở bảng số", () =>
            {
                safe = Object.FindFirstObjectByType<Puzzles.SafeKeypad>();
                StandNear(safe.transform);
                safe.Interact();
            }, After(0.8f), () => { Shot(tag + "_03_safe"); return Field<GameObject>(safe, "ui") != null ? null : "bảng số không mở"; });
            Add("Két sắt: nhập 1921", () =>
            {
                var d = Field<int[]>(safe, "digits");
                d[0] = 1; d[1] = 9; d[2] = 2; d[3] = 1;
                Call(safe, "TryOpen");
            }, After(2.5f), () => Field<bool>(safe, "opened") ? null : "két không mở với mã 1921");
            Add("Két sắt: lấy chìa", () => { if (safe.CanInteract()) safe.Interact(); }, After(1f), () =>
            {
                CloseReader();
                if (KeyRing.Instance == null || !KeyRing.Instance.Has("key_upstairs")) return "chưa có key_upstairs";
                return Expect(StoryFlag.SafeOpened, StoryFlag.HasUpstairsKey);
            });
            DoorInteractable stair = null;
            Add("Mở cửa cầu thang bằng chìa", () =>
            {
                stair = Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None).FirstOrDefault(d => Field<string>(d, "keyId") == "key_upstairs" || d.name.StartsWith("Door_95_"));
                StandNear(stair.transform);
                stair.Interact();
            }, After(1.2f), () => stair.IsOpen ? null : "cửa cầu thang không mở (locked=" + stair.IsLocked + ")");
            Add("Lên tầng 2", () => Teleport(new Vector3(103.5f, F2, 25.6f), 270f), () => GM.Has(StoryFlag.Floor2Entered), 6f,
                () => GM.Stage != GameStage.Stage2_Floor2 ? "chương " + GM.Stage : null);

            foreach (var h in new[] { "Moc", "Thuy", "Tho", "Kim", "Hoa" })
            {
                string hh = h;
                Add("Kỷ vật " + hh, () => { var e = Use("Relic_" + hh); if (e != null) Log("  " + e); }, After(1f), () =>
                {
                    CloseReader();
                    return Expect((StoryFlag)Enum.Parse(typeof(StoryFlag), "Relic_" + hh));
                });
            }

            if (extra) AddHidingTest();

            Add("Bàn thờ nến: đặt 5 trang bùa đúng bệ", () =>
            {
                HousekeeperAway();
                var altar = Puzzles.CandleAltar.Instance;
                foreach (var st in Object.FindObjectsByType<Puzzles.CandleStand>(FindObjectsSortMode.None).OrderBy(s => (int)s.Slot))
                {
                    StandNear(st.transform);
                    st.Interact();
                    if (Field<GameObject>(altar, "ui") == null) { Log("  bảng chọn trang không mở ở bệ " + st.Slot); continue; }
                    Call(altar, "Choose", st.Slot);
                }
            }, () => GM.Has(StoryFlag.CellarOpened), 25f, () =>
            {
                Shot(tag + "_04_altar");
                return GM.Stage != GameStage.Stage3_Cellar ? "chương " + GM.Stage : null;
            });
            Add("Nắp hầm đã mở", null, After(1f), () =>
            {
                var open = Find("CellarHatch_Open"); var closed = Find("CellarHatch_Closed");
                if (open == null || closed == null) return "không thấy nắp hầm";
                return open.gameObject.activeInHierarchy && !closed.gameObject.activeInHierarchy ? null : "nắp hầm chưa chuyển sang mở";
            });
            Add("Xuống hầm", () => CheckpointManager.Instance.TeleportTo("CP_CellarStairs", false), After(1.5f), () => { Shot(tag + "_05_cellar"); return null; });
            Add("Thi thể Julian", () =>
            {
                var j = Object.FindFirstObjectByType<Sequences.JulianBody>();
                StandNear(j.transform);
                j.Interact();
            }, After(2f), () => { CloseReader(); return Expect(StoryFlag.FoundQuan, StoryFlag.HasWhistle); });
            Add("Vào Sảnh Tế: Lord Blackwood đàm phán", () => CheckpointManager.Instance.TeleportTo("CP_RitualHall", false),
                () => Find("BW_RitualChoice") != null, 90f, () =>
                {
                    Shot(tag + "_06_ritual_choice");
                    var a = FindButton("BW_RitualChoice", 0); var b = FindButton("BW_RitualChoice", 1);
                    if (a == null || b == null) return "thiếu nút lựa chọn";
                    if (!a.interactable || !b.interactable) return "nút bị khóa dù đủ kỷ vật (release=" + a.interactable + ", claim=" + b.interactable + ")";
                    if (Cursor.lockState == CursorLockMode.Locked) return "chuột bị khóa ở màn chọn";
                    return Expect(StoryFlag.MetDaiQuan);
                });
            Add("Chọn kết " + ending, () => FindButton("BW_RitualChoice", ending == EndingId.Release ? 0 : 1).onClick.Invoke(),
                () => Find("BW_EndingCanvas") != null, 150f,
                () => Expect(ending == EndingId.Release ? StoryFlag.Ending1 : StoryFlag.Ending2));
            AddEndingTail();
        }

        static void AddEndingTail()
        {
            Add("Màn chữ kết thúc", null, After(4f), () => { Shot(tag + "_07_ending"); return null; });
            Add("Nhấn phím", () => KeySpace(true), After(0.3f));
            Add("Nhả phím → về menu, xóa save", () => KeySpace(false), () => GameFlow.State == FlowState.Menu, 90f,
                () => SaveSystem.SlotExists(TestSlot) ? "save slot " + TestSlot + " chưa bị xóa sau kết thúc" : null);
        }

        // ------------------------------------------------------------------ trốn tủ
        static void AddHidingTest()
        {
            AI.HidingSpot spot = null;
            Add("Trốn tủ: vào tủ", () =>
            {
                spot = Object.FindObjectsByType<AI.HidingSpot>(FindObjectsSortMode.None).FirstOrDefault(s => s.SpotKind == AI.HidingSpot.Kind.Wardrobe);
                Teleport(spot.OutPoint.position, spot.OutPoint.eulerAngles.y + 180f);
                spot.Interact();
            }, () => AI.HidingSpot.Current == spot && Elapsed > 1.5f, 6f, () => { Shot(tag + "_hide_in"); return null; });
            Add("Trốn tủ: Mrs. Peake đứng ngoài 10 s", () =>
            {
                DebugTools.CheatSettings.AIEnabled = true;
                var hk = Object.FindFirstObjectByType<AI.HousekeeperAI>();
                var p = spot.OutPoint.position + spot.OutPoint.forward * 2.5f;
                if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
                var ag = hk.GetComponent<NavMeshAgent>();
                if (ag != null && ag.isOnNavMesh) ag.Warp(p); else hk.transform.position = p;
            }, After(10f), () =>
            {
                Shot(tag + "_hide_wait");
                var hk = Object.FindFirstObjectByType<AI.HousekeeperAI>();
                if (deaths > 0) return "bị bắt khi đang trốn (không bị thấy lúc vào)";
                return AI.HidingSpot.Current != spot ? "bị lôi ra khỏi tủ" : null;
            });
            Add("Trốn tủ: ra khỏi tủ", () =>
            {
                DebugTools.CheatSettings.AIEnabled = false;
                HousekeeperAway();
                spot.Exit(); // trong game: phím E khi đang trốn (HidingSpot.LateUpdate → Exit)
            }, () => AI.HidingSpot.Current == null && Elapsed > 1f, 6f, () =>
            {
                float d = Vector3.Distance(PC.transform.position, spot.OutPoint.position);
                return d > 1.2f ? "ra tủ lệch điểm ra " + d.ToString("F2") + " m" : null;
            });
        }

        // ------------------------------------------------------------------ đi bộ thật
        static void BuildWalk()
        {
            Add("Menu chính", null, () => GameFlow.State == FlowState.Menu && Elapsed > 1.5f, 60f, null, true);
            Add("Trò chơi mới (slot " + TestSlot + ")", () =>
            {
                SaveSystem.CurrentSlot = TestSlot;
                GameFlow.PendingCharacter = PlayerCharacter.Evelyn;
                SaveSystem.PendingLoad = null;
                GameFlow.PendingStart = StartMode.NewGame;
                ModalUI.Reset();
                UI.LoadingScreen.Load();
            }, () => GameFlow.State == FlowState.Playing && PC != null, 300f, null, true);
            Add("Mở mọi cửa, mở hầm, tắt AI", () =>
            {
                PC.mouseSensitivity = 0f;
                DebugTools.CheatSettings.AIEnabled = false;
                HousekeeperAway();
                foreach (var d in Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None)) d.ForceOpen();
                GM.SetFlag(StoryFlag.CellarOpened);
            }, After(2f));
            // Cầu thang chữ U và dốc xuống hầm đi theo lối của người chơi (đi thẳng từng đoạn); các chặng còn lại theo NavMesh
            bool[] S(int n) => Enumerable.Repeat(true, n).ToArray();
            var legs = new (string name, Vector3[] pts, bool[] straight)[]
            {
                ("Cổng → cửa chính → sảnh", new[] { new Vector3(80.7f, GF, 4.5f), new Vector3(80.7f, GF, 11.5f) }, null),
                ("Sảnh → phòng thư viện / két", new[] { new Vector3(100.5f, GF, 10.5f) }, null),
                ("→ phòng ba lô", new[] { new Vector3(93.0f, GF, 22.6f) }, null),
                ("→ phòng khách trọ (băng 2)", new[] { new Vector3(108.0f, GF, 13.5f) }, null),
                ("→ bếp, nắp hầm", new[] { new Vector3(72.6f, GF, 23.5f) }, null),
                ("→ chân cầu thang", new[] { new Vector3(103.6f, GF, 24.4f) }, null),
                ("Cầu thang chữ U → tầng 2 (qua chiếu nghỉ)", new[] { new Vector3(103.6f, 14.75f, 18.2f), new Vector3(105.75f, 14.75f, 17.7f), new Vector3(105.75f, F2, 23.9f), new Vector3(103.5f, F2, 25.6f) }, S(4)),
                ("Tầng 2: phòng ngủ chủ nhân", new[] { new Vector3(105.2f, F2, 10.8f) }, null),
                ("Tầng 2: phòng khách trọ", new[] { new Vector3(106.5f, F2, 22.5f) }, null),
                ("Tầng 2: phòng thờ", new[] { new Vector3(80.7f, F2, 17.5f) }, null),
                ("Tầng 2: phòng gia nhân", new[] { new Vector3(71.0f, F2, 11.0f) }, null),
                ("Tầng 2: phòng trẻ em", new[] { new Vector3(69.0f, F2, 23.5f) }, null),
                ("Tầng 2 → xuống cầu thang (qua chiếu nghỉ)", new[] { new Vector3(103.5f, F2, 25.6f), new Vector3(105.75f, F2, 23.9f), new Vector3(105.75f, 14.75f, 17.7f), new Vector3(103.6f, 14.75f, 18.2f), new Vector3(103.6f, GF, 24.4f) }, new[] { false, true, true, true, true }),
                ("Bếp → nắp hầm → dốc đá → đáy hầm", new[] { new Vector3(74.0f, GF, 24.0f), new Vector3(74.0f, 10.9f, 25.5f), new Vector3(61.0f, 1.2f, 25.5f), new Vector3(58.7f, 1.0f, 24.3f) }, new[] { false, true, true, true }),
                ("Hầm: đáy → thi thể Julian → Sảnh Tế", new[] { new Vector3(58.7f, 1.0f, 15.6f) }, null),
            };
            foreach (var leg in legs)
            {
                var l = leg;
                Add("Đi: " + l.name, () => DebugTools.BlackwoodAutoWalker.Begin(l.pts, l.straight, 4f),
                    () => Elapsed > 0.5f && !DebugTools.BlackwoodAutoWalker.Running, 120f, () =>
                    {
                        var last = l.pts[l.pts.Length - 1];
                        var p = PC.transform.position;
                        string log = string.Join(" | ", DebugTools.BlackwoodAutoWalker.Log);
                        if (!DebugTools.BlackwoodAutoWalker.Success) return "kẹt: " + log;
                        if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(last.x, 0, last.z)) > 1.2f) return "không tới đích (" + p.ToString("F1") + ") " + log;
                        return null;
                    });
            }
            // HK không được đi tắt qua vách giữa cầu thang: đường NavMesh từ chân thang lên tầng 2 phải đi qua chiếu nghỉ
            Add("NavMesh cầu thang đi qua chiếu nghỉ", null, After(0.1f), () =>
            {
                var path = new NavMeshPath();
                NavMesh.SamplePosition(new Vector3(103.6f, GF, 23.0f), out var a, 1.5f, NavMesh.AllAreas);
                NavMesh.SamplePosition(new Vector3(105.75f, F2, 23.0f), out var b, 1.5f, NavMesh.AllAreas);
                if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return "không có đường NavMesh lên tầng 2";
                return path.corners.Any(c => c.z < 18.65f) ? null : "đường NavMesh trèo qua vách giữa (không qua chiếu nghỉ)";
            });
        }
    }
}
