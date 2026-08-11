using System;
using System.Globalization;
using System.IO;

namespace Game.Core.Save
{
    /// <summary>
    /// 세이브 파일의 읽기와 쓰기.
    /// 엔진 의존이 없도록 파일 경로와 JSON 변환을 밖에서 주입받는다.
    /// 덕분에 이 클래스는 씬도 에디터도 없이 테스트할 수 있다.
    /// </summary>
    public sealed class SaveStore
    {
        private readonly string _filePath;
        private readonly Func<SaveData, string> _serialize;
        private readonly Func<string, SaveData> _deserialize;

        public SaveStore(string filePath, Func<SaveData, string> serialize, Func<string, SaveData> deserialize)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("경로가 비어 있다.", nameof(filePath));

            _filePath = filePath;
            _serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            data.version = SaveData.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            string payload = _serialize(data);
            string content = Checksum(payload) + "\n" + payload;

            // 임시 파일에 먼저 쓰고 교체한다.
            // 저장 도중 앱이 강제 종료돼도 반쯤 쓰인 파일이 기존 세이브를 덮어쓰지 않는다.
            string temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, content);

            if (File.Exists(_filePath)) File.Replace(temporaryPath, _filePath, null);
            else File.Move(temporaryPath, _filePath);
        }

        /// <summary>
        /// 세이브를 읽는다. 파일이 없거나, 손상됐거나, 모르는 버전이면 <c>false</c>를 돌려준다.
        /// 이 경우 호출자는 새 게임으로 시작하면 된다.
        /// </summary>
        public bool TryLoad(out SaveData data)
        {
            data = null;

            if (!File.Exists(_filePath)) return false;

            string content = File.ReadAllText(_filePath);

            int separator = content.IndexOf('\n');
            if (separator <= 0) return false;

            string storedChecksum = content.Substring(0, separator);
            string payload = content.Substring(separator + 1);
            if (storedChecksum != Checksum(payload)) return false;

            data = _deserialize(payload);
            if (data == null) return false;

            // 버전이 다르면 아직은 포기한다. 스키마가 처음 바뀌는 시점에
            // 여기에 마이그레이션을 넣는다(예: v1 -> v2에서 스탯 강화 단계 추가).
            if (data.version != SaveData.CurrentVersion)
            {
                data = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 손상과 단순 변조를 감지하기 위한 체크섬. 암호학적 강도는 필요 없다.
        /// 파일을 열어 골드를 고치면 검증이 깨지는 정도면 충분하다.
        /// <para>
        /// FNV-1a를 직접 구현한 이유는 <c>string.GetHashCode</c>가 실행할 때마다 값이 달라져
        /// 저장된 체크섬과 비교할 수 없기 때문이다.
        /// </para>
        /// </summary>
        private static string Checksum(string payload)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char character in payload)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }
    }
}
