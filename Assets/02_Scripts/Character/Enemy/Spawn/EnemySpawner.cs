using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonMaster.Spawn
{
    // 뱀서라이크 스포너.
    // 빈 오브젝트에 붙여서 사용.
    //
    // 두 가지를 한다.
    //  1) 카메라 화면 바로 바깥에서 스폰 (플레이어를 항상 압박)
    //  2) 경과 시간에 따라 난이도를 올림 (무한 생존이므로 끝없이 어려워진다)
    public class EnemySpawner : MonoBehaviour
    {
        // 어떤 적을 언제부터 내보낼지
        [Serializable]
        public class EnemyEntry
        {
            public GameObject Prefab;

            [Tooltip("이 시간(초)이 지나야 등장하기 시작한다")]
            public float UnlockTime = 0f;

            [Tooltip("뽑기 가중치. 클수록 자주 나온다")]
            [Min(0.01f)] public float Weight = 1f;
        }

        [Header("타일맵")]
        [SerializeField] private Tilemap _groundTilemap;    // 바닥(Base)
        [SerializeField] private Tilemap _wallTilemap;      // 벽(Wall). 없으면 비워둬도 됨

        [Header("스폰 대상")]
        [SerializeField] private EnemyEntry[] _enemies;
        [SerializeField] private Transform _player;

        [Header("스폰 위치")]
        [Tooltip("화면 바깥 얼마나 떨어진 곳에서 스폰할지. 0이면 화면 경계에 딱 붙어서 튀어나온다")]
        [SerializeField] private float _ringPadding = 1.5f;
        [Tooltip("이보다 가까우면 스폰하지 않는다(눈앞에 튀어나오는 것 방지)")]
        [SerializeField] private float _minDistanceFromPlayer = 6f;

        [Header("난이도 상승 (무한 생존)")]
        [Tooltip("이 시간(초)에 걸쳐 아래 값들이 시작값 -> 최대값으로 올라간다")]
        [SerializeField] private float _rampDuration = 600f;
        [Tooltip("난이도가 올라가는 모양. 기본은 완만한 S자")]
        [SerializeField] private AnimationCurve _rampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("난이도 - 스폰 주기(초)")]
        [SerializeField] private float _intervalStart = 1.2f;
        [SerializeField] private float _intervalEnd = 0.18f;

        [Header("난이도 - 한 번에 몇 마리")]
        [Min(1)] [SerializeField] private int _countPerSpawnStart = 1;
        [Min(1)] [SerializeField] private int _countPerSpawnEnd = 4;

        [Header("난이도 - 동시 생존 상한")]
        [Min(1)] [SerializeField] private int _maxAliveStart = 30;
        [Min(1)] [SerializeField] private int _maxAliveEnd = 160;

        [Header("난이도 - 적 능력치 배율")]
        [SerializeField] private float _hpScaleEnd = 4f;
        [SerializeField] private float _damageScaleEnd = 2.5f;

        // 링 스폰이 실패했을 때 쓸 예비 지점(맵 가장자리).
        // 플레이어가 맵 구석에 붙어 있으면 링 위에 걸을 수 있는 칸이 없을 수 있다.
        private readonly List<Vector3> _fallbackPoints = new List<Vector3>();

        // 살아 있는 적 추적용(상한 검사에 필요)
        private readonly List<GameObject> _alive = new List<GameObject>();

        private float _lastSpawnTime;
        private float _startTime;
        private Camera _camera;

        // 0 = 게임 시작, 1 = 최대 난이도
        public float DifficultyT
        {
            get { return _rampCurve.Evaluate(Mathf.Clamp01((Time.time - _startTime) / Mathf.Max(1f, _rampDuration))); }
        }

        public float CurrentInterval { get { return Mathf.Lerp(_intervalStart, _intervalEnd, DifficultyT); } }
        public int CurrentCountPerSpawn { get { return Mathf.RoundToInt(Mathf.Lerp(_countPerSpawnStart, _countPerSpawnEnd, DifficultyT)); } }
        public int CurrentMaxAlive { get { return Mathf.RoundToInt(Mathf.Lerp(_maxAliveStart, _maxAliveEnd, DifficultyT)); } }
        public float CurrentHpScale { get { return Mathf.Lerp(1f, _hpScaleEnd, DifficultyT); } }
        public float CurrentDamageScale { get { return Mathf.Lerp(1f, _damageScaleEnd, DifficultyT); } }

        #region 유니티 생명주기
        private void Start()
        {
            _camera = Camera.main;
            _startTime = Time.time;
            CacheFallbackPoints();
        }

        private void Update()
        {
            if (Time.time < _lastSpawnTime + CurrentInterval) return;
            _lastSpawnTime = Time.time;

            // 죽어서 파괴된 적을 목록에서 제거.
            // 유니티의 == 오버로딩 덕분에 파괴된 오브젝트는 null로 판정된다.
            // !activeSelf 도 함께 보는 이유: 나중에 오브젝트 풀을 쓰면 적이 파괴되지 않고
            // 비활성화만 되는데, 그때 이 검사가 없으면 상한에 걸려 스폰이 영원히 멈춘다.
            _alive.RemoveAll(enemy => enemy == null || !enemy.activeSelf);

            int count = CurrentCountPerSpawn;
            int max = CurrentMaxAlive;

            for (int i = 0; i < count; i++)
            {
                if (_alive.Count >= max) return;
                SpawnOne();
            }
        }
        #endregion

        #region 스폰 위치
        // 카메라가 비추는 영역을 감싸는 원 위에서 뽑는다.
        // 맵 가장자리 고정 스폰과 달리, 플레이어가 어디 있든 화면 밖에서 바로 다가온다.
        private bool TryGetRingPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_camera == null || _player == null || _groundTilemap == null) return false;

            // 직교 카메라 기준. 화면 대각선 절반 + 여유만큼 떨어진 원
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + _ringPadding;

            float sqrMin = _minDistanceFromPlayer * _minDistanceFromPlayer;
            Vector3 center = _player.position;

            const int maxAttempts = 24;
            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                Vector3Int cell = _groundTilemap.WorldToCell(candidate);
                if (!IsWalkable(cell)) continue;                       // 맵 밖이거나 벽
                if ((candidate - center).sqrMagnitude < sqrMin) continue;

                position = _groundTilemap.GetCellCenterWorld(cell);
                return true;
            }
            return false;
        }

        // 링 위에 설 자리가 없을 때(플레이어가 맵 구석) 쓰는 예비 경로
        private bool TryGetFallbackPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_fallbackPoints.Count == 0) return false;

            float sqrMin = _minDistanceFromPlayer * _minDistanceFromPlayer;
            const int maxAttempts = 20;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidate = _fallbackPoints[UnityEngine.Random.Range(0, _fallbackPoints.Count)];
                if (_player != null && (candidate - _player.position).sqrMagnitude < sqrMin) continue;

                position = candidate;
                return true;
            }
            return false;
        }

        // 바닥이면서, 상하좌우 중 하나라도 바닥이 아닌 셀 = 가장자리
        private void CacheFallbackPoints()
        {
            _fallbackPoints.Clear();

            if (_groundTilemap == null)
            {
                Debug.LogError($"EnemySpawner::CacheFallbackPoints() _groundTilemap이 비어 있습니다.");
                return;
            }

            // cellBounds는 타일이 실제로 칠해진 범위만 반환함
            foreach (Vector3Int cell in _groundTilemap.cellBounds.allPositionsWithin)
            {
                if (!IsWalkable(cell)) continue;
                if (!IsEdge(cell)) continue;

                _fallbackPoints.Add(_groundTilemap.GetCellCenterWorld(cell));
            }

            Debug.Log($"EnemySpawner::CacheFallbackPoints() 예비 스폰 지점 {_fallbackPoints.Count}개");
        }

        // 바닥이 있고 벽이 없는 칸만 서 있을 수 있음
        private bool IsWalkable(Vector3Int cell)
        {
            if (!_groundTilemap.HasTile(cell)) return false;
            if (_wallTilemap != null && _wallTilemap.HasTile(cell)) return false;
            return true;
        }

        // 네 방향 중 하나라도 막혀 있으면 가장자리로 판정
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
            GameObject prefab = PickEnemy();
            if (prefab == null) return;

            Vector3 position;
            if (!TryGetRingPosition(out position) && !TryGetFallbackPosition(out position)) return;

            GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
            _alive.Add(enemy);

            // 경과 시간에 따른 능력치 배율 적용.
            // Instantiate 시점에 Awake 가 이미 끝났으므로 여기서 체력을 다시 세팅해도 안전하다.
            var re = enemy.GetComponent<DungeonMaster.Character.Enemy.RoguelikeEnemy>();
            if (re != null) re.ApplyDifficultyScale(CurrentHpScale, CurrentDamageScale);
        }

        // 해금 시간이 지난 적들 중에서 가중치로 하나 뽑는다
        private GameObject PickEnemy()
        {
            if (_enemies == null || _enemies.Length == 0)
            {
                Debug.LogError($"EnemySpawner::PickEnemy() _enemies가 비어 있습니다.");
                return null;
            }

            float elapsed = Time.time - _startTime;

            float total = 0f;
            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyEntry e = _enemies[i];
                if (e == null || e.Prefab == null) continue;
                if (elapsed < e.UnlockTime) continue;
                total += e.Weight;
            }
            if (total <= 0f) return null;   // 아직 아무도 해금되지 않음

            float roll = UnityEngine.Random.Range(0f, total);
            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyEntry e = _enemies[i];
                if (e == null || e.Prefab == null) continue;
                if (elapsed < e.UnlockTime) continue;

                roll -= e.Weight;
                if (roll <= 0f) return e.Prefab;
            }
            return null;
        }
        #endregion

        #region 디버그
        // 플레이 중에 이 오브젝트를 선택하면 스폰 링이 표시됨
        private void OnDrawGizmosSelected()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _player == null) return;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + _ringPadding;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(_player.position, radius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(_player.position, _minDistanceFromPlayer);
        }
        #endregion
    }
}
