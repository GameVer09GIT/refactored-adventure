using System;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>Số kiểu phim còn lại của máy ảnh.</summary>
    public class FilmStock : MonoBehaviour, ISaveParticipant
    {
        public static FilmStock Instance { get; private set; }

        [SerializeField] private int current = 8;

        public event Action<int> Changed;
        public int Current => current;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            if (cfg != null) current = cfg.filmStart;
        }

        public bool TryConsume()
        {
            if (current <= 0) return false;
            current--;
            Changed?.Invoke(current);
            return true;
        }

        public void Add(int n)
        {
            if (n <= 0) return;
            current += n;
            Changed?.Invoke(current);
        }

        public void Capture(SaveData data) => data.film = current;
        public void Restore(SaveData data) { current = Mathf.Max(0, data.film); Changed?.Invoke(current); }
    }
}
