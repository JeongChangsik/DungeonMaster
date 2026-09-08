using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonMaster.Spawn
{
    // 타일맵 가장자리(바닥이 끊기는 셀)에서 몬스터를 무작위로 스폰
    // 빈 오브젝트에 붙여서 사용
    public class EnemySpawner : MonoBehaviour
    {
        [Header("타일맵")]
        [SerializeField] private Tilemap _groundTilemap;    // 바닥(Base)
        [SerializeField] private Tilemap _wallTilemap;      // 벽(Wall). 없으면 비워둬도 됨

        [Header("스폰 대상")]
        [SerializeField] private GameObject[] _enemyPrefabs;
        [SerializeField] private Transform _player;

        [Header("스폰 규칙")]
        [SerializeField] private float _interval = 1f;              // 스폰 주기(초)
        [Min(1)]
        [SerializeField] private int _countPerSpawn = 1;            // 한 번에 몇 마리
        [Min(1)]
        [SerializeField] private int _maxAlive = 50;                // 동시 생존 상한
        [SerializeField] private float _minDistanceFromPlayer = 8f; // 이보다 가까우면 스폰 안 함

        // 스폰 가능한 월드 좌표
        // 매번 타일맵을 훑으면 비싸므로(110x56 = 6160칸) 시작할 때 한 번만 계산해 둠
        private readonly List<Vector3> _spawnPoints = new List<Vector3>();

        // 살아 있는 적 추적용(상한 검사에 필요)
        private readonly List<GameObject> _alive = new List<GameObject>();

        private float _lastSpawnTime;

        #region 유니티 생명주기
        private void Start()
        {
            CacheSpawnPoints();
        }

        private void Update()
        {
            if (Time.time < _lastSpawnTime + _interval) return;
            _lastSpawnTime = Time.time;

            // 죽어서 파괴된 적을 목록에서 제거
            // 유니티의 == 오버로딩 덕분에 파괴된 오브젝트는 null로 판정됨
            _alive.RemoveAll(enemy => enemy == null);

            for (int i = 0; i < _countPerSpawn; i++)
            {
                if (_alive.Count >= _maxAlive) return;
                SpawnOne();
            }
        }
        #endregion

        #region 스폰 지점 계산
        // 바닥이면서, 상하좌우 중 하나라도 바닥이 아닌 셀 = 가장자리
        private void CacheSpawnPoints()
        {
            _spawnPoints.Clear();

            if (_groundTilemap == null)
            {
                Debug.LogError($"EnemySpawner::CacheSpawnPoints() _groundTilemap이 비어 있습니다.");
                return;
            }

            // cellBounds는 타일이 실제로 칠해진 범위만 반환함
            foreach (Vector3Int cell in _groundTilemap.cellBounds.allPositionsWithin)
            {
                if (!IsWalkable(cell)) continue;
                if (!IsEdge(cell)) continue;

                // 셀 좌표 -> 셀 중앙의 월드 좌표
                _spawnPoints.Add(_groundTilemap.GetCellCenterWorld(cell));
            }

            Debug.Log($"EnemySpawner::CacheSpawnPoints() 가장자리 스폰 지점 {_spawnPoints.Count}개");
        }

        // 바닥이 있고 벽이 없는 칸만 서 있을 수 있음
        private bool IsWalkable(Vector3Int cell)
        {
            if (!_groundTilemap.HasTile(cell)) return false;
            if (_wallTilemap != null && _wallTilemap.HasTile(cell)) return false;
            return true;
        }

        // 네 방향 중 하나라도 막혀 있으면 가장자리로 판정
        // 맵이 직사각형이 아니어도(구멍, 굴곡) 그대로 동작함
        private bool IsEdge(Vector3Int cell)
        {
            return !IsWalkable(cell + Vector3Int.up)
                || !IsWalkable(cell + Vector3Int.down)
                || !IsWalkable(cell + Vector3Int.left)
                || !IsWalkable(cell + Vector3Int.right);
        }
        #endregion

        #region 스폰
        private void SpawnOne()
        {
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0)
            {
                Debug.LogError($"EnemySpawner::SpawnOne() _enemyPrefabs가 비어 있습니다.");
                return;
            }

            if (!TryGetSpawnPosition(out Vector3 position)) return;

            GameObject prefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
            _alive.Add(Instantiate(prefab, position, Quaternion.identity));
        }

        // 조건을 만족하는 지점을 뽑되, 못 찾으면 이번 턴은 건너뜀
        // 플레이어가 구석에 붙어 있으면 조건을 만족하는 지점이 아예 없을 수 있으므로
        // 시도 횟수를 제한하지 않으면 무한 루프가 됨
        private bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_spawnPoints.Count == 0) return false;

            float sqrMin = _minDistanceFromPlayer * _minDistanceFromPlayer;
            const int maxAttempts = 20;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidate = _spawnPoints[Random.Range(0, _spawnPoints.Count)];

                // 루트 연산을 피하려고 제곱끼리 비교
                if (_player != null && (candidate - _player.position).sqrMagnitude < sqrMin) continue;

                position = candidate;
                return true;
            }

            return false;
        }
        #endregion

        #region 디버그
        // 플레이 중에 이 오브젝트를 선택하면 스폰 지점이 표시됨
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            foreach (Vector3 point in _spawnPoints)
            {
                Gizmos.DrawCube(point, Vector3.one * 0.5f);
            }
        }
        #endregion
    }
}
