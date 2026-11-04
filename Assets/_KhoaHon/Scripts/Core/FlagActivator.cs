using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Bật/tắt các object theo một cờ cốt truyện (ví dụ: nắp hầm biến mất khi có CellarOpened).
    /// Tự đồng bộ cả sau khi nạp game.
    /// </summary>
    public class FlagActivator : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private StoryFlag flag = StoryFlag.CellarOpened;
        [Tooltip("Bật khi có cờ")]
        [SerializeField] private GameObject[] activateWhenSet = new GameObject[0];
        [Tooltip("Tắt khi có cờ")]
        [SerializeField] private GameObject[] deactivateWhenSet = new GameObject[0];

        public void Configure(StoryFlag f, GameObject[] onSet, GameObject[] offSet)
        {
            flag = f; activateWhenSet = onSet ?? new GameObject[0]; deactivateWhenSet = offSet ?? new GameObject[0];
        }

        private void OnEnable() => GameEvents.FlagSet += OnFlagSet;
        private void OnDisable() => GameEvents.FlagSet -= OnFlagSet;
        private void Start() => Apply();

        private void OnFlagSet(StoryFlag f) { if (f == flag) Apply(); }

        public void Apply()
        {
            bool set = GameManager.Instance != null && GameManager.Instance.Has(flag);
            foreach (var go in activateWhenSet) if (go != null) go.SetActive(set);
            foreach (var go in deactivateWhenSet) if (go != null) go.SetActive(!set);
        }

        public void Capture(SaveData data) { }
        public void Restore(SaveData data) => Apply();
    }
}
