using UnityEngine;
using DungeonMaster.Character.Player;

namespace DungeonMaster.Weapon
{
    // 모든 무기의 공통 부모.
    //
    // 스탯 처리 방식은 Player.cs 와 동일하다.
    // 인스펙터의 [SerializeField] 값은 "기본값"으로 그대로 두고, 강화분만 별도 필드에 누적한 뒤
    // 프로퍼티에서 합쳐서 돌려준다. 그래서 씬/프리팹에 저장된 값이 리팩터링으로 날아가지 않는다.
    //
    // 최종 피해량 = (기본 + 가산) * 무기배율 * 플레이어 전역배율
    // 마지막 항이 있어서 "모든 무기 피해량 +10%" 같은 패시브 카드가 가능해진다.
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("무기 공통 스탯")]
        [SerializeField] protected float _damage = 10f;

        protected float _addDamage;
        protected float _mulDamage = 1f;

        // 이 무기를 들고 있는 플레이어. 전역 배율을 읽어오는 용도
        protected RoguelikePlayer _owner;

        // 해금 여부. 잠긴 무기의 "강화" 카드는 후보에 오르지 않는다
        protected bool _unlocked;

        public abstract WeaponId Id { get; }
        public virtual bool IsUnlocked => _unlocked;

        public float Damage => (_damage + _addDamage) * _mulDamage * GlobalDamageMul;

        protected float GlobalDamageMul => _owner != null ? _owner.WeaponDamageMul : 1f;
        protected float GlobalAreaMul => _owner != null ? _owner.WeaponAreaMul : 1f;
        protected float GlobalRateMul => _owner != null ? _owner.WeaponRateMul : 1f;

        protected virtual void Awake()
        {
            _owner = GetComponentInParent<RoguelikePlayer>();
        }

        // 여러 번 불려도 안전(멱등). 해금 카드와 강화 카드가 같은 경로를 타기 때문
        public void Unlock()
        {
            if (IsUnlocked) return;

            _unlocked = true;
            OnUnlock();
            Rebuild();
        }

        protected virtual void OnUnlock() { }

        // 카드가 호출하는 강화 진입점.
        // percent 는 가산 비율이다. 0.1f = +10% (PlayerStatUpgradeSO 와 같은 규약)
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
        public virtual void Rebuild() { }
    }
}
