using System.Collections.Generic;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>Chùm chìa khóa Vy đang giữ (theo id).</summary>
    public class KeyRing : MonoBehaviour, ISaveParticipant
    {
        public static KeyRing Instance { get; private set; }

        private readonly HashSet<string> keys = new HashSet<string>();

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Has(string keyId) => !string.IsNullOrEmpty(keyId) && keys.Contains(keyId);
        public void Add(string keyId) { if (!string.IsNullOrEmpty(keyId)) keys.Add(keyId); }

        public void Capture(SaveData data) => data.keys = new List<string>(keys);

        public void Restore(SaveData data)
        {
            keys.Clear();
            foreach (var k in data.keys) keys.Add(k);
        }
    }
}
