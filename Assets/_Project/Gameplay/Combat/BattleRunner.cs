using System;
using Game.Core;
using Game.Gameplay.Progression;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 자동 전투를 진행한다. 몬스터는 한 번에 한 마리씩 등장하고, 플레이어는 고정 주기로 공격하며,
    /// <see cref="RequiredKills"/>만큼 처치하면 다음 층으로 넘어간다.
    /// 10층마다는 몬스터 대신 제한시간이 붙은 보스 한 마리가 등장한다.
    /// 엔진 의존이 없는 순수 로직이며, 표현 계층이 상태를 읽고 <see cref="Tick"/>을 호출한다.
    /// </summary>
    public sealed class BattleRunner
    {
        /// <summary>일반 층을 넘어가는 데 필요한 처치 수. 데이터 테이블이 생기면 그쪽으로 옮긴다.</summary>
        public const int KillsPerFloor = 10;

        /// <summary>이 배수에 해당하는 층이 보스 층이다.</summary>
        public const int BossFloorInterval = 10;

        /// <summary>보스를 잡는 데 주어지는 시간. 넘기면 시도가 처음부터 다시 시작된다.</summary>
        public const double BossTimeLimitSeconds = 30d;

        /// <summary>
        /// 보스 하나를 잡을 때 나오는 다이아. 층과 무관한 고정값이라 이른 보스도 값어치가 있다.
        /// 다이아의 유일한 획득 경로다.
        /// </summary>
        public const int BossDiamondReward = 5;

        private const double BossHealthMultiplier = 12d;

        private readonly FloorFormula _formula;
        private readonly CharacterStats _stats;
        private readonly IRandomSource _random;

        private double _secondsUntilNextAttack;

        public BattleRunner(FloorFormula formula, CharacterStats stats, IRandomSource random)
            : this(formula, stats, random, BattleProgress.Start) { }

        public BattleRunner(FloorFormula formula, CharacterStats stats, IRandomSource random, BattleProgress progress)
        {
            _formula = formula ?? throw new ArgumentNullException(nameof(formula));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            Floor = progress.Floor;
            KillsOnFloor = progress.KillsOnFloor;
            Gold = progress.Gold;
            Diamonds = progress.Diamonds;

            // 진행 중이던 몬스터의 남은 체력은 저장하지 않는다. 불러오면 항상 새 몬스터가 나온다.
            SpawnMonster();
        }

        /// <summary>타격이 발생할 때마다 발생. 표현 계층이 데미지 숫자를 띄우는 데 쓴다.</summary>
        public event Action<DamageResult> DamageDealt;

        /// <summary>현재 몬스터의 체력이 0이 되었을 때 발생.</summary>
        public event Action MonsterKilled;

        public int Floor { get; private set; }

        public int KillsOnFloor { get; private set; }

        public BigNumber MonsterHealth { get; private set; }

        public BigNumber MonsterMaxHealth { get; private set; }

        public BigNumber Gold { get; private set; }

        public int Diamonds { get; private set; }

        public bool IsBossFloor => Floor % BossFloorInterval == 0;

        /// <summary>보스에게 남은 시간. <see cref="IsBossFloor"/>일 때만 의미가 있다.</summary>
        public double BossSecondsRemaining { get; private set; }

        /// <summary>현재 층을 넘어가는 데 필요한 처치 수. 보스 층은 한 마리뿐이다.</summary>
        public int RequiredKills => IsBossFloor ? 1 : KillsPerFloor;

        /// <summary>저장에 쓰는 진행 상태 스냅샷.</summary>
        public BattleProgress Progress => new BattleProgress(Floor, KillsOnFloor, Gold, Diamonds);

        /// <summary>
        /// 골드가 충분하면 차감하고 <c>true</c>. 부족하면 아무것도 하지 않고 <c>false</c>.
        /// 확인과 차감이 갈라지면 잔액이 음수가 될 수 있어 한 번에 처리한다.
        /// </summary>
        public bool TrySpendGold(BigNumber amount)
        {
            if (Gold < amount) return false;

            Gold -= amount;
            return true;
        }

        /// <summary>
        /// <paramref name="deltaSeconds"/>만큼 전투를 진행한다. 델타가 길면 그 안에 들어가는 공격을
        /// 모두 처리하므로 프레임이 끊겨도 데미지가 유실되지 않는다.
        /// 다만 몇 시간 단위인 오프라인 보상에는 쓸 수 없다. 그쪽은 시뮬레이션이 아니라 수식으로 계산한다.
        /// </summary>
        public void Tick(double deltaSeconds)
        {
            if (IsBossFloor) TickBossTimer(deltaSeconds);

            _secondsUntilNextAttack -= deltaSeconds;

            double attackInterval = 1d / _stats.AttacksPerSecond;
            while (_secondsUntilNextAttack < 0d)
            {
                Attack();
                _secondsUntilNextAttack += attackInterval;
            }
        }

        private void TickBossTimer(double deltaSeconds)
        {
            BossSecondsRemaining -= deltaSeconds;
            if (BossSecondsRemaining > 0d) return;

            // 시간 초과. 보스가 체력을 회복하고 같은 층에서 다시 시작한다.
            SpawnMonster();
        }

        /// <summary>
        /// 현재 층을 즉시 클리어한다. 개발 중 진행을 건너뛰기 위한 것으로,
        /// 보상은 정상 처치와 똑같이 지급된다. 그래야 강화나 가챠를 바로 시험해볼 수 있다.
        /// </summary>
        public void ClearFloorImmediately()
        {
            int floorAtStart = Floor;
            while (Floor == floorAtStart) KillMonster();
        }

        private void Attack()
        {
            DamageResult damage = ResolveDamage();
            MonsterHealth -= damage.Amount;
            DamageDealt?.Invoke(damage);

            if (MonsterHealth > BigNumber.Zero) return;

            KillMonster();
        }

        private void KillMonster()
        {
            Gold += _formula.GoldReward(Floor) * _stats.GoldMultiplier;

            // 층이 오르기 전에 판정해야 방금 잡은 몬스터가 보스였는지 알 수 있다.
            if (IsBossFloor) Diamonds += BossDiamondReward;

            KillsOnFloor++;
            MonsterKilled?.Invoke();

            if (KillsOnFloor >= RequiredKills)
            {
                Floor++;
                KillsOnFloor = 0;
            }

            SpawnMonster();
        }

        private void SpawnMonster()
        {
            BigNumber health = _formula.MonsterHealth(Floor);
            if (IsBossFloor)
            {
                health *= BossHealthMultiplier;
                BossSecondsRemaining = BossTimeLimitSeconds;
            }

            MonsterMaxHealth = health;
            MonsterHealth = health;
        }

        private DamageResult ResolveDamage()
        {
            bool isCritical = _random.NextDouble() < _stats.CriticalChance;
            BigNumber amount = isCritical ? _stats.AttackPower * _stats.CriticalMultiplier : _stats.AttackPower;
            return new DamageResult(amount, isCritical);
        }
    }
}
