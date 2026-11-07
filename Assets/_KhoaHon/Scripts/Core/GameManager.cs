using System.Collections;
using System.Linq;
using KhoaHon.Notebook;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KhoaHon.Core
{
    /// <summary>Thành phần có trạng thái cần lưu/nạp.</summary>
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }

    /// <summary>
    /// Điều phối chiến dịch Khóa Hồn: cờ cốt truyện, giai đoạn, lưu/nạp, tham chiếu config dùng chung.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Dữ liệu dùng chung")]
        [SerializeField] private TextTable textTable;
        [SerializeField] private UIStyle uiStyle;
        [SerializeField] private KhoaHonDatabase database;
        [SerializeField] private Items.ToolsConfig toolsConfig;
        [Tooltip("Hồ sơ nhân vật: Evelyn, Alex (theo thứ tự enum PlayerCharacter)")]
        [SerializeField] private CharacterProfile[] characters = new CharacterProfile[2];

        [Header("Trạng thái")]
        [SerializeField] private GameStage stage = GameStage.Stage0_Gate;
        [Tooltip("Lý trí hiện tại (SanitySystem ở Phase 3 sẽ quản lý)")]
        [Range(0f, 100f)][SerializeField] private float sanity = 100f;
        [Tooltip("Tự nạp file lưu khi vào scene (dùng cho nút Tiếp tục)")]
        [SerializeField] private bool loadSaveOnStart = false;

        public StoryFlags Flags { get; } = new StoryFlags();
        public GameStage Stage => stage;
        public TextTable Text => textTable;
        public UIStyle Style => uiStyle;
        public KhoaHonDatabase Database => database;
        public Items.ToolsConfig Tools => toolsConfig;
        public float Sanity { get => sanity; set => sanity = Mathf.Clamp(value, 5f, 100f); }
        public bool IsRestoring { get; private set; }
        /// <summary>Nhân vật đang chơi.</summary>
        public PlayerCharacter Character { get; private set; } = PlayerCharacter.Evelyn;
        public CharacterProfile Profile => GetProfile(Character);
        public CharacterProfile[] Characters => characters;
        /// <summary>Phát khi nhân vật được áp dụng (lúc vào game hoặc nạp save) để đổi bộ công cụ/HUD.</summary>
        public static event System.Action<PlayerCharacter> CharacterApplied;

        public CharacterProfile GetProfile(PlayerCharacter c)
        {
            if (characters == null) return null;
            foreach (var p in characters) if (p != null && p.character == c) return p;
            return null;
        }

        private void Awake()
        {
            Instance = this;
            T.Table = textTable;
            Loc.Load();
            // Tiếp tục từ save: nhân vật lấy từ save đang chờ nạp (cần biết trước khi thanh đồ khởi tạo)
            if (SaveSystem.PendingLoad != null) GameFlow.PendingCharacter = SaveSystem.PendingLoad.CharacterValue;
            Character = GameFlow.PendingCharacter;
            ModalUI.Reset();
            Flags.Changed += OnFlagChanged;
        }

        private void OnDestroy()
        {
            Flags.Changed -= OnFlagChanged;
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            CharacterApplied?.Invoke(Character);
            var pending = SaveSystem.PendingLoad;
            if (pending == null && loadSaveOnStart) pending = SaveSystem.Read();
            SaveSystem.PendingLoad = null;
            if (pending == null) yield break;
            // Đợi 1 frame để mọi Start() khác chạy xong rồi mới áp dụng
            yield return null;
            ApplySave(pending);
        }

        private void OnFlagChanged(StoryFlag f, bool value)
        {
            if (value && !IsRestoring) GameEvents.RaiseFlagSet(f);
        }

        // ---------- Cờ & giai đoạn ----------
        public bool Has(StoryFlag f) => Flags.Has(f);
        public void SetFlag(StoryFlag f) => Flags.Set(f);

        public void SetStage(GameStage s)
        {
            if (stage == s) return;
            stage = s;
            if (!IsRestoring) GameEvents.RaiseStageChanged(s);
        }

        // ---------- Lưu / nạp ----------
        public SaveData CaptureSave()
        {
            var d = new SaveData
            {
                stage = stage.ToString(),
                flags = Flags.ToNames(),
                sanity = sanity,
                character = Character.ToString(),
            };
            foreach (var p in FindParticipants()) p.Capture(d);
            return d;
        }

        public void SaveNow(bool showToast = true)
        {
            SaveSystem.Write(CaptureSave());
            if (showToast) KhoaHonHUD.Toast(T.Get("toast.saved"));
        }

        public void ApplySave(SaveData d)
        {
            if (d == null) return;
            IsRestoring = true;
            try
            {
                Flags.LoadNames(d.flags);
                stage = d.StageValue;
                sanity = d.sanity;
                Character = d.CharacterValue;
                GameFlow.PendingCharacter = Character;
                CharacterApplied?.Invoke(Character);
                foreach (var p in FindParticipants()) p.Restore(d);
            }
            finally { IsRestoring = false; }
        }

        /// <summary>Nạp file lưu: nạp lại scene rồi áp dụng.</summary>
        public void LoadFromDisk()
        {
            var d = SaveSystem.Read();
            if (d == null) { KhoaHonHUD.Toast(T.Get("toast.no_save")); return; }
            SaveSystem.PendingLoad = d;
            GameFlow.PendingStart = StartMode.Continue;
            ReloadScene();
        }

        public void NewGame()
        {
            SaveSystem.Delete();
            SaveSystem.PendingLoad = null;
            GameFlow.PendingStart = StartMode.NewGame;
            ReloadScene();
        }

        private static void ReloadScene()
        {
            ModalUI.Reset();
            // Scene phải có trong Build Settings (KH_Main đã được thêm ở Phase 1)
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        private static ISaveParticipant[] FindParticipants() =>
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<ISaveParticipant>().ToArray();
    }
}
