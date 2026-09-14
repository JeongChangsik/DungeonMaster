using UnityEngine;
using DungeonMaster.Character.Player;

namespace DungeonMaster.Weapon
{
    // 모든 무기의 공통 부모.
    //
    // [하는 일] 공전 칼날, 투척 단검, 화염 오라 같은 무기들이 똑같이 가져야 하는 것을 한곳에 모았다.
    //           피해량 계산, 해금(처음 얻기), 강화 카드 받기, 다시 배치하기(Rebuild)가 여기 있다.
    // [붙이는 곳] 이 클래스는 abstract(추상)라서 직접 붙일 수 없다.
    //           WeaponOrbit / WeaponProjectile / WeaponAura 처럼 이걸 물려받은 자식 스크립트를
    //           Player > WeaponRoot 아래 오브젝트에 붙인다.
    // [연결] WeaponUpgradeSO(무기 카드)가 WeaponManager.Get(id) 로 무기를 찾은 뒤 Unlock() 과 AddStat() 을 부른다.
    //        RoguelikePlayer 의 전역 배율(WeaponDamageMul 등)을 읽어서 피해량에 곱한다.
    //        전역 배율이 바뀌면 WeaponManager.RebuildAll() 이 모든 무기의 Rebuild() 를 부른다.
    // [설계] 스탯 처리 방식은 Player.cs 와 동일하다.
    //        인스펙터의 [SerializeField] 값은 "기본값"으로 그대로 두고, 강화분만 별도 필드에 누적한 뒤
    //        프로퍼티에서 합쳐서 돌려준다. 그래서 씬/프리팹에 저장된 값이 리팩터링으로 날아가지 않는다.
    //        기본값을 직접 바꿔 버리면, 플레이 중에 올린 값과 원래 값이 섞여서 되돌릴 수 없게 된다.
    //
    //        최종 피해량 = (기본 + 가산) * 무기배율 * 플레이어 전역배율
    //        마지막 항이 있어서 "모든 무기 피해량 +10%" 같은 패시브 카드가 가능해진다.
    //
    //        무기마다 다른 부분(개수, 범위, 속도 등)은 virtual 메서드를 자식이 override(덮어쓰기)해서 채운다.
    //        virtual = "자식이 바꿔도 되는 함수", override = "부모 함수를 내 방식으로 바꾼다" 는 뜻이다.
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("무기 공통 스탯")]
        [SerializeField] protected float _damage = 10f;

        // 강화 카드로 쌓인 값. _addDamage 는 더하는 양, _mulDamage 는 곱하는 배율(1 = 그대로)
        protected float _addDamage;
        protected float _mulDamage = 1f;

        // 이 무기를 들고 있는 플레이어. 전역 배율을 읽어오는 용도
        protected RoguelikePlayer _owner;

        // 해금 여부. 잠긴 무기의 "강화" 카드는 후보에 오르지 않는다
        protected bool _unlocked;

        // 이 무기가 어떤 종류인지. 자식마다 반드시 정해야 해서 abstract 로 뒀다.
        // 카드 SO 는 이 값으로 "내가 강화할 무기"를 찾는다
        public abstract WeaponId Id { get; }

        // 공전 칼날처럼 "해금"의 기준이 다른 무기는 override 해서 바꾼다
        public virtual bool IsUnlocked => _unlocked;

        public float Damage => (_damage + _addDamage) * _mulDamage * GlobalDamageMul;

        // 플레이어가 없으면(테스트용으로 무기만 씬에 둔 경우 등) 1 을 돌려서 배율이 없는 것처럼 동작한다
        protected float GlobalDamageMul => _owner != null ? _owner.WeaponDamageMul : 1f;
        protected float GlobalAreaMul => _owner != null ? _owner.WeaponAreaMul : 1f;
        protected float GlobalRateMul => _owner != null ? _owner.WeaponRateMul : 1f;

        // 오브젝트가 깨어날 때 한 번 불린다. 부모 쪽(Player > WeaponRoot > 이 무기)을 거슬러 올라가 플레이어를 찾는다.
        // Awake 는 모든 Start 보다 먼저 끝나므로, 자식의 Start(예: WeaponOrbit 의 첫 Rebuild)에서는 _owner 가 이미 채워져 있다.
        // 자식 클래스가 Awake 를 또 쓰고 싶으면 override 하고 base.Awake() 를 꼭 불러야 _owner 가 채워진다.
        protected virtual void Awake()
        {
            _owner = GetComponentInParent<RoguelikePlayer>();
        }

        // 무기를 처음 얻을 때 부른다. WeaponUpgradeSO.Apply 가 카드 적용 직전에 부른다.
        // 여러 번 불려도 안전(멱등). 해금 카드와 강화 카드가 같은 경로를 타기 때문
        // (멱등 = 한 번 하든 열 번 하든 결과가 같다는 뜻)
        public void Unlock()
        {
            if (IsUnlocked) return;

            _unlocked = true;
            OnUnlock();
            Rebuild();
        }

        // 해금되는 순간 무기마다 따로 할 일이 있으면 자식이 채운다(예: 오라 그림 켜기)
        protected virtual void OnUnlock() { }

        // 카드가 호출하는 강화 진입점.
        // percent 는 가산 비율이다. 0.1f = +10% (PlayerStatUpgradeSO 와 같은 규약)
        // +10% 카드를 두 장 먹으면 1.1 * 1.1 = 1.21 이 아니라 1 + 0.1 + 0.1 = 1.2 가 된다.
        // 곱으로 쌓으면 뒤로 갈수록 너무 세지므로 더하는 방식으로 막았다.
        //
        // 자식은 자기 스탯(개수, 범위...)을 먼저 처리한 뒤 base.AddStat() 을 불러
        // 피해량 처리와 Rebuild 를 여기에 맡긴다.
        public virtual void AddStat(WeaponStat stat, float flat, float percent)
        {
            if (stat == WeaponStat.Damage)
            {
                _addDamage += flat;
                _mulDamage += percent;
            }

            Rebuild();
        }

        // 스탯이 바뀌었을 때 실제 오브젝트에 반영한다.
        // 전역 배율이 바뀔 때도 WeaponManager.RebuildAll() 을 통해 불린다.
        // 매번 Damage 같은 프로퍼티를 새로 읽는 무기(투척 단검, 오라)는 할 일이 없어서 비워 두고,
        // 칼날을 미리 만들어 두는 공전 칼날처럼 "만들어 둔 것"이 있는 무기만 override 한다.
        public virtual void Rebuild() { }
    }
}
