using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KhoaHon.DebugTools
{
    /// <summary>
    /// Công cụ kiểm thử (Editor/Dev): điều khiển CharacterController của Vy đi qua danh sách điểm,
    /// dùng đường NavMesh nếu có, nếu không thì đi thẳng. Ghi log kẹt để nghiệm thu "đi hết nhà không kẹt".
    /// </summary>
    public class KhoaHonAutoWalker : MonoBehaviour
    {
        public static readonly List<string> Log = new List<string>();
        public static bool Running { get; private set; }
        public static bool Success { get; private set; }

        [SerializeField] private float speed = 3.5f;
        [SerializeField] private float stuckTime = 2.0f;

        private CharacterController cc;
        private PlayerController pc;

        /// <summary>straight[i] = true: đi thẳng tới điểm i (không dùng NavMesh).</summary>
        public static KhoaHonAutoWalker Begin(Vector3[] points, bool[] straight, float walkSpeed = 3.5f)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) { Log.Add("No player"); return null; }
            var w = player.GetComponent<KhoaHonAutoWalker>();
            if (w == null) w = player.gameObject.AddComponent<KhoaHonAutoWalker>();
            w.speed = walkSpeed;
            w.StopAllCoroutines();
            w.StartCoroutine(w.Run(points, straight));
            return w;
        }

        private IEnumerator Run(Vector3[] points, bool[] straight)
        {
            Running = true; Success = false; Log.Clear();
            cc = GetComponent<CharacterController>();
            pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false; // tự điều khiển CC, tránh input
            float t0 = Time.time;
            bool ok = true;
            for (int i = 0; i < points.Length && ok; i++)
            {
                var corners = new List<Vector3>();
                bool useStraight = straight != null && i < straight.Length && straight[i];
                if (!useStraight)
                {
                    var path = new NavMeshPath();
                    NavMesh.SamplePosition(transform.position, out var a, 2f, NavMesh.AllAreas);
                    NavMesh.SamplePosition(points[i], out var b, 2f, NavMesh.AllAreas);
                    if (NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                        corners.AddRange(path.corners);
                    else { Log.Add($"[{i}] no complete navmesh path → straight"); corners.Add(points[i]); }
                }
                else corners.Add(points[i]);

                foreach (var c in corners)
                {
                    float lastProgress = Time.time;
                    float best = float.MaxValue;
                    while (true)
                    {
                        var to = c - transform.position; to.y = 0;
                        float dist = to.magnitude;
                        if (dist < 0.35f) break;
                        if (dist < best - 0.05f) { best = dist; lastProgress = Time.time; }
                        if (Time.time - lastProgress > stuckTime)
                        {
                            Log.Add($"STUCK at {transform.position:F1} heading {c:F1} (wp {i})");
                            ok = false; break;
                        }
                        var dir = to / dist;
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                        cc.Move((dir * speed + Vector3.down * 6f) * Time.deltaTime);
                        yield return null;
                    }
                    if (!ok) break;
                }
                if (ok) Log.Add($"[{i}] reached {transform.position:F1} t={Time.time - t0:F1}s");
            }
            Success = ok;
            Running = false;
            if (pc != null) pc.enabled = true;
            Log.Add(ok ? "DONE OK" : "FAILED");
        }
    }
}
