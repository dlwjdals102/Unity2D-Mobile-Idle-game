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

        /// <summary>세이브 파일을 지운다. 파일이 없으면 아무 일도 하지 않는다.</summary>
        public void Delete()
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
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

            if (!TryMigrate(data))
            {
                data = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 옛 버전 세이브를 현재 스키마로 한 단계씩 끌어올린다.
        /// 각 단계는 바로 다음 버전으로만 옮기므로, 버전이 몇 개 밀려 있어도 순서대로 통과한다.
        /// 앞으로 스키마를 바꿀 때마다 여기에 case를 하나씩 추가한다.
        /// </summary>
        private static bool TryMigrate(SaveData data)
        {
            // 이 빌드보다 새 버전의 세이브는 무엇이 들었는지 알 수 없으므로 손대지 않는다.
            if (data.version > SaveData.CurrentVersion) return false;

            while (data.version < SaveData.CurrentVersion)
            {
                switch (data.version)
                {
                    case 1:
                        // v1에는 강화 단계가 없었다. 새 필드는 0단계로 두면 되고,
                        // 역직렬화가 이미 0으로 채워두므로 버전만 올린다.
                        data.version = 2;
                        break;

                    case 2:
                        // v2에는 다이아가 없었다. 0에서 시작한다.
                        data.version = 3;
                        break;

                    default:
                        // 알 수 없는 버전. 새 게임으로 시작하는 편이 잘못된 상태로 복원하는 것보다 낫다.
                        return false;
                }
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
