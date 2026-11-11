namespace KhoaHon.AI
{
    public enum HousekeeperState { Patrol, Suspicious, Hunt, Stunned, Kill, Inspect }

    /// <summary>Lý do vào trạng thái choáng (quyết định trạng thái sau khi hết choáng).</summary>
    public enum StunCause { None, Flash, Comb }

    /// <summary>Dữ liệu cảm nhận mỗi lần cập nhật (do HousekeeperAI điền).</summary>
    public struct Perception
    {
        public bool seesPlayer;
        public bool heardNoise;
        public bool flashlightOnFace;
        public bool playerReachable;     // Vy đang ở vùng Floor2 (bà đi tới được)
        public float distanceToPlayer;
        public bool playerHidden;
        public bool sawPlayerHide;
        public bool arrivedAtTarget;
    }

    /// <summary>
    /// Bảng chuyển trạng thái của bà Quản Gia — C# thuần, không phụ thuộc Unity để test EditMode.
    /// Thời gian truyền vào qua Tick(dt).
    /// </summary>
    public class HousekeeperBrain
    {
        public HousekeeperState State { get; private set; } = HousekeeperState.Patrol;
        public StunCause Stun { get; private set; }
        public bool Weakened { get; private set; }
        public float Timer { get; private set; }
        /// <summary>Đang khựng lại (chế độ yếu) trước khi lao tới.</summary>
        public bool Hesitating => State == HousekeeperState.Hunt && hesitateLeft > 0f;

        private readonly float loseSightTime, flashStun, combStun, hesitate, killRange, inspectTime;
        private readonly float searchMin, searchMax;
        private float notSeenFor;
        // Đã canh xong lượt trốn hiện tại → không quay lại canh cùng chỗ cho tới khi Vy ra khỏi chỗ trốn
        private bool hideHandled;
        private float hesitateLeft;
        private readonly System.Random rng;

        public event System.Action<HousekeeperState, HousekeeperState> Changed;

        public HousekeeperBrain(float loseSightTime, float searchMin, float searchMax, float flashStun, float combStun,
                                float hesitate, float killRange, float inspectTime, int seed = 1)
        {
            this.loseSightTime = loseSightTime; this.searchMin = searchMin; this.searchMax = searchMax;
            this.flashStun = flashStun; this.combStun = combStun; this.hesitate = hesitate;
            this.killRange = killRange; this.inspectTime = inspectTime;
            rng = new System.Random(seed);
        }

        public static HousekeeperBrain FromConfig(HousekeeperConfig c, int seed = 1) =>
            new HousekeeperBrain(c.loseSightTime, c.searchTime.x, c.searchTime.y, c.flashStunTime, c.combStunTime,
                                 c.weakenedHesitate, c.killRange, c.hidingInspectTime, seed);

        // ---------- Sự kiện ngoài
        /// <summary>Đèn flash máy ảnh vào mặt: choáng rồi chuyển thẳng sang Hunt.</summary>
        public void OnFlashed() { if (State != HousekeeperState.Kill) Enter(HousekeeperState.Stunned, flashStun, StunCause.Flash); }

        /// <summary>Vy nhặt chiếc lược: choáng 20 s rồi về Patrol, bật chế độ yếu vĩnh viễn.</summary>
        public void OnCombTaken()
        {
            Weakened = true;
            if (State != HousekeeperState.Kill) Enter(HousekeeperState.Stunned, combStun, StunCause.Comb);
        }

        public void SetWeakened(bool w) => Weakened = w;

        /// <summary>Reset sau khi Vy hồi sinh.</summary>
        public void ResetToPatrol() => Enter(HousekeeperState.Patrol, 0f, StunCause.None);

        // ---------- Cập nhật
        public HousekeeperState Tick(float dt, Perception p)
        {
            Timer -= dt;
            if (!p.playerHidden) hideHandled = false;
            bool sawHide = p.sawPlayerHide && !hideHandled;
            switch (State)
            {
                case HousekeeperState.Patrol:
                    if (p.seesPlayer && p.playerReachable && !p.playerHidden) EnterHunt();
                    else if (sawHide && p.playerReachable) Enter(HousekeeperState.Inspect, inspectTime + 60f, StunCause.None);
                    else if (p.heardNoise || p.flashlightOnFace) Enter(HousekeeperState.Suspicious, Search(), StunCause.None);
                    break;

                case HousekeeperState.Suspicious:
                    if (p.seesPlayer && p.playerReachable && !p.playerHidden) EnterHunt();
                    else if (sawHide && p.playerReachable) Enter(HousekeeperState.Inspect, inspectTime + 60f, StunCause.None);
                    else if (p.heardNoise || p.flashlightOnFace) Timer = System.Math.Max(Timer, Search() * 0.6f);
                    else if (Timer <= 0f) Enter(HousekeeperState.Patrol, 0f, StunCause.None);
                    break;

                case HousekeeperState.Hunt:
                    if (hesitateLeft > 0f) { hesitateLeft -= dt; break; }
                    if (p.playerHidden)
                    {
                        if (sawHide) Enter(HousekeeperState.Inspect, inspectTime + 60f, StunCause.None);
                        else Enter(HousekeeperState.Suspicious, Search(), StunCause.None);
                        break;
                    }
                    if (!p.playerReachable) { Enter(HousekeeperState.Suspicious, Search(), StunCause.None); break; }
                    if (p.seesPlayer) notSeenFor = 0f; else notSeenFor += dt;
                    if (p.seesPlayer && p.distanceToPlayer <= killRange) { Enter(HousekeeperState.Kill, 0f, StunCause.None); break; }
                    if (notSeenFor >= loseSightTime) Enter(HousekeeperState.Suspicious, Search(), StunCause.None);
                    break;

                case HousekeeperState.Inspect:
                    // Đi tới chỗ trốn; khi tới nơi đếm thời gian đứng sát (Timer đặt lại khi arrived lần đầu)
                    if (!p.playerHidden) { if (p.seesPlayer) EnterHunt(); else Enter(HousekeeperState.Suspicious, Search(), StunCause.None); break; }
                    if (p.arrivedAtTarget && Timer > inspectTime) Timer = inspectTime;
                    if (Timer <= 0f) { hideHandled = true; Enter(HousekeeperState.Patrol, 0f, StunCause.None); }
                    break;

                case HousekeeperState.Stunned:
                    if (Timer <= 0f)
                    {
                        if (Stun == StunCause.Flash) EnterHunt(skipHesitate: true);
                        else Enter(HousekeeperState.Patrol, 0f, StunCause.None);
                    }
                    break;

                case HousekeeperState.Kill:
                    break;
            }
            return State;
        }

        /// <summary>Nín thở thất bại khi đang bị kiểm tra chỗ trốn → bị lôi ra.</summary>
        public void OnBreathFailed() { if (State == HousekeeperState.Inspect) Enter(HousekeeperState.Kill, 0f, StunCause.None); }

        private void EnterHunt(bool skipHesitate = false)
        {
            Enter(HousekeeperState.Hunt, 0f, StunCause.None);
            hesitateLeft = Weakened && !skipHesitate ? hesitate : 0f;
        }

        private float Search() => (float)(searchMin + rng.NextDouble() * (searchMax - searchMin));

        private void Enter(HousekeeperState s, float timer, StunCause cause)
        {
            var old = State;
            State = s; Timer = timer; Stun = cause; notSeenFor = 0f;
            if (s != HousekeeperState.Hunt) hesitateLeft = 0f;
            if (old != s) Changed?.Invoke(old, s);
        }
    }
}
