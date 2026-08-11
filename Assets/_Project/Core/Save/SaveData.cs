using System;

namespace Game.Core.Save
{
    /// <summary>
    /// 세이브 파일에 그대로 직렬화되는 데이터.
    /// JsonUtility가 다루므로 필드는 public이어야 하고 프로퍼티는 쓸 수 없다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>
        /// 현재 스키마 버전. 저장 항목이 바뀌면 올리고 <c>SaveStore</c>에 마이그레이션을 추가한다.
        /// <para>v1 → v2: 스탯 강화 단계 추가</para>
        /// </summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        /// <summary>
        /// 마지막 저장 시각(UTC, ISO 8601 왕복 형식).
        /// 오프라인 보상은 아직 없지만, 지금 기록해두지 않으면 이미 저장된 파일에는 나중에 채워 넣을 수 없다.
        /// </summary>
        public string savedAtUtc;

        public int floor = 1;
        public int killsOnFloor;

        // BigNumber는 구조체라 JsonUtility가 다루지 못하므로 가수와 지수로 나눠 저장한다.
        // 이 두 값은 BigNumber의 내부 표현 그대로라 왕복해도 값이 정확히 보존된다.
        public double goldMantissa;
        public int goldExponent;

        // v2에서 추가. 강화 수치가 아니라 단계만 저장한다.
        // 수치는 단계에서 계산되므로, 밸런싱을 바꿔도 기존 세이브가 그대로 새 수식을 따른다.
        public int attackPowerLevel;
        public int criticalMultiplierLevel;
    }
}
