using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DungeonMaster.Character.Enemy.FSM;
using UnityEngine;

namespace DungeonMaster.Character.Enemy
{
    public class Swampy : Enemy
    {
        [Header("슬라임 공격 스탯")]
        [SerializeField] private float _dashSpeed = 10f;    // 공격 시 대쉬 속도
        [SerializeField] private float _delayTimeAfterDash = 0.2f;    // 공격 후 딜레이 타임
        [SerializeField] private float _returnSpeed = 8f;  // 제자리로 돌아가는 속도
        [SerializeField] private float _dashDistance = 2f;  // 대쉬 거리
        
        [Header("넉백 설정")]
        [SerializeField] private float _knockbackSpeed = 15f;
        [SerializeField] private float _knockbackDistance = 1.5f;
        
        // 슬라임 공격 시작 위치(원래 위치)
        private Vector2 _originPosition;
        // 공격 상태 여부
        private bool _isAttacking = false;
        // 마지막 공격 시간 기록(프로퍼티)
        public float LastAttackTime { get; private set; }

        protected override void InitState()
        {
            // Indexer 방식 값 추가
            _states = new Dictionary<Type, IState>
            {
                [typeof(IdleState)] = new IdleState(),
                [typeof(ChaseState)] = new ChaseState(),
                [typeof(AttackState)] = new AttackState(),
            };
            Debug.Log($"Swampy::InitState() 상태 초기화 완료");
            
        }

        protected virtual void Start()
        {
            base.Start();
            
            // StartCoroutine("ExampleCoroutine"); // 예전 방식, 지금은 사용하지 않음
            // StartCoroutine(nameof(ExampleCoroutine));   // 만약 써야한다면 nameof()를 사용
            StartCoroutine(ExampleCoroutine()); // 함수 원형을 입력
        }
        
        #region 코루틴 예시

        public bool respawned = false;
        // 코루틴은 반드시 IEnumerator로 반환해야함
        private IEnumerator ExampleCoroutine()
        {
            Debug.Log("코루틴 시작");
            
            // yield : 양보하다
            // yield return null;  // 여기서 null의 의미는 다음 프레임까지 양보
            // yield return new WaitForSeconds(3.5f);   // 지정한 시간(3.5초)동안 메인 메시지 루프에게 제어권을 양보
            // Thread.Sleed(3500); // Block 방식, 위의 반대
            // yield return new WaitUntil(() => respawned == true); // respawned 값이 true가 되면 코루틴 종료
            // yield return new WaitWhile(() => !respawned);   // ~ 하는 동안 계속 제어권 양보(respawned 값이 false인 동안 제어)
            // yield return StartCoroutine(다른 코루틴); // 다른 코루틴이 완료될 때까지 제어권을 양보
            // 원칙적으로 만약 코루틴 함수 안에 while()문을 사용해야 한다면 while문 안에 yield 문을 반드시 넣어야함.
            yield return null;
            
            Debug.Log("코루틴 종료");
        }
        #endregion
        
        #region 공격 메서드

        public IEnumerator DashAttack()
        {
            _isAttacking = true;
            // 마지막 공격 시간 갱신
            LastAttackTime = Time.time;
            // 현재 위치 저장
            _originPosition = transform.position;
            // 현재 위치 Vector2
            Vector2 currPosition = new Vector2(transform.position.x, transform.position.y);
            
            // 목표 좌표 계산
            // 방향
            Vector2 dashDir = target.transform.position - transform.position;
            // 공격할 좌표를 계산 (목표 좌표 = 현재 위치 + 목표 방향 벡터 * 거리)
            Vector2 dashPos = currPosition + dashDir * _dashDistance;

            _spriteRenderer.flipX = dashDir.x < 0;

            // 실제로 이동한 시간(누적 시간)
            float dashTime = 0f;
            // 이동 시간 계산
            float dashDuration = _dashDistance / _dashSpeed;

            // 공격 사운드 재생
            AudioManager.Instance.EnemySFX(AudioManager.Instance.AudioDataSO.enemyAttackSFX);
            
            // while 루프로 대쉬 처리(앞으로 점진적으로 이동)
            while (dashTime < dashDuration)
            {
                // 플레이어가 공격할 때 _isAttacking 를 false로 바꾸면서, 코루틴을 중단함
                if (!_isAttacking) yield break;
                
                // 대쉬
                transform.position = Vector2.MoveTowards(transform.position, dashPos, Time.deltaTime * _dashSpeed);
                dashTime += Time.deltaTime;
                yield return null;
            }
            // 잠시 대기
            yield return new WaitForSeconds(_delayTimeAfterDash);
            
            // while : 원위치로 복귀
            float returnTime = 0f;
            float returnDistance = Vector2.Distance(transform.position, _originPosition);
            float returnDuration = returnDistance / _dashSpeed;
            
            while (returnTime < returnDuration)
            {
                transform.position = Vector2.MoveTowards(transform.position, _originPosition, Time.deltaTime * _returnSpeed);
                returnTime += Time.deltaTime;
                yield return null;
            }
            _isAttacking = false;
        }
        #endregion
        
        public override void TakeDamage(float damage)
        {
            if(_isAttacking) _isAttacking = false;
            base.TakeDamage(damage);
            
            ChangeState<IdleState>();
            
            // TODO: 넉백 처리
            StartCoroutine(Knockback());
        }
        
        // 넉백 처리 코루틴
        private IEnumerator Knockback()
        {
            IsKnockbacking = true;
            
            // 넉백 방향 벡터
            // 정규화 벡터, normalized vector == unit vector (단위 벡터)
            Vector2 dir = (transform.position - target.position).normalized;
            
            // 마지막 넉백 시간
            float knockbackTime = 0f;
            // 넉백 시간까지 걸리는 시간
            float knockbackDuration = _knockbackDistance / _knockbackSpeed;
            
            // while 루프로 대쉬 처리(앞으로 점진적으로 이동)
            while (knockbackTime < knockbackDuration)
            {
                // 넉백 Tanslate(방향 * 속도 * deltaTime, 기준 좌표계(default: 로컬 좌표계))
                transform.Translate(dir * _knockbackSpeed * Time.deltaTime);
                knockbackTime += Time.deltaTime;
                yield return null;
            }
            
            // 넉백 후 바로 공격하지 않도록 스턴 효과
            yield return new WaitForSeconds(1.5f);
            
            // 스턴 후 바로 공격하지 못하게 마지막 공격 시간 초기화
            LastAttackTime = Time.time;
            
            IsKnockbacking = false;
        }

    }
}
