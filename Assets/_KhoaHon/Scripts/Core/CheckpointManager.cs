using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Quản lý checkpoint. Dời Transform "SpawnPoint" (mà DeathScreenManager dùng để hồi sinh)
    /// tới checkpoint gần nhất, nên không cần sửa DeathScreenManager.
    /// </summary>
    public class CheckpointManager : MonoBehaviour, ISaveParticipant
    {
        public static CheckpointManager Instance { get; private set; }

        [Tooltip("Transform SpawnPoint mà DeathScreenManager tìm theo tên")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Tự lưu game khi đến checkpoint mới")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private string startCheckpointId = "CP_Gate";

        private readonly Dictionary<string, Checkpoint> checkpoints = new Dictionary<string, Checkpoint>();
        private string currentId;

        public string CurrentId => currentId;
        public IEnumerable<string> AllIds => checkpoints.Keys;

        private void Awake()
        {
            Instance = this;
            foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(cp.Id)) checkpoints[cp.Id] = cp;
            if (spawnPoint == null)
            {
                var go = GameObject.Find("SpawnPoint");
                if (go != null) spawnPoint = go.transform;
            }
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(currentId) && checkpoints.TryGetValue(startCheckpointId, out var cp))
            {
                currentId = cp.Id;
                MoveSpawnPoint(cp);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Reach(Checkpoint cp)
        {
            if (cp == null || cp.Id == currentId) return;
            currentId = cp.Id;
            MoveSpawnPoint(cp);
            GameEvents.RaiseCheckpointReached(cp.Id);
            if (autoSave && GameManager.Instance != null && !GameManager.Instance.IsRestoring)
                GameManager.Instance.SaveNow();
        }

        public void ReachById(string id)
        {
            if (checkpoints.TryGetValue(id, out var cp)) Reach(cp);
        }

        /// <summary>Dịch chuyển Vy tới checkpoint (cheat, nạp game).</summary>
        public bool TeleportTo(string id, bool markCurrent = true)
        {
            if (!checkpoints.TryGetValue(id, out var cp)) return false;
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return false;
            var cc = pc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            pc.transform.SetPositionAndRotation(cp.transform.position, Quaternion.Euler(0f, cp.transform.eulerAngles.y, 0f));
            pc.SetPitch(0f);
            if (cc != null) cc.enabled = true;
            if (markCurrent) { currentId = cp.Id; MoveSpawnPoint(cp); }
            return true;
        }

        private void MoveSpawnPoint(Checkpoint cp)
        {
            if (spawnPoint == null) return;
            spawnPoint.SetPositionAndRotation(cp.transform.position, Quaternion.Euler(0f, cp.transform.eulerAngles.y, 0f));
        }

        public void Capture(SaveData data) => data.checkpointId = currentId ?? "";

        public void Restore(SaveData data)
        {
            if (!string.IsNullOrEmpty(data.checkpointId)) TeleportTo(data.checkpointId);
        }
    }
}
