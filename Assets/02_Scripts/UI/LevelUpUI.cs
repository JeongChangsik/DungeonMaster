using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonMaster.Character.Player;
using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;

namespace DungeonMaster.UI
{
    // 레벨업 -> 시간 정지 -> 카드 선택 -> 시간 재개
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private RoguelikePlayer _player;
        [SerializeField] private GameObject _panel;      // 켜고 끌 대상
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Button[] _cardButtons;
        [SerializeField] private MMF_Player _levelUpFeedback;
        [SerializeField] private float _riseInterval = 0.08f;   // 카드끼리 올라오는 시차

        [Header("업그레이드")]
        [SerializeField] private List<UpgradeSO> _pool = new();

        // 게임 시작과 동시에 Lv.1로 적용할 업그레이드.
        // 플레이어가 고른 적이 없는데 무기를 들고 시작해야 하므로,
        // 시작 장비도 카드와 똑같은 경로(Apply)로 넣어준다.
        // 이렇게 해야 "레벨 = 개수"가 저절로 맞는다
        [SerializeField] private List<UpgradeSO> _startingUpgrades = new();

        // 업그레이드별 현재 레벨. SO 에셋에 저장하면 플레이를 멈춰도 값이 남으므로 런타임에만 들고 있는다
        private readonly Dictionary<UpgradeSO, int> _levels = new();
        private readonly List<UpgradeSO> _drawn = new();
        private readonly List<UpgradeSO> _candidates = new();

        private LevelUpCard[] _cards;
        private Coroutine _showRoutine;

        // 카드를 고르는 도중에 또 레벨업이 터질 수 있으므로 쌓아뒀다가 순서대로 처리
        private int _pending;

        private void OnEnable()  => _player.OnLevelUpAction += HandleLevelUp;
        private void OnDisable() => _player.OnLevelUpAction -= HandleLevelUp;

        private void Start()
        {
            _panel.SetActive(false);
            _cards = new LevelUpCard[_cardButtons.Length];

            for (int i = 0; i < _cardButtons.Length; i++)
            {
                _cards[i] = _cardButtons[i].GetComponent<LevelUpCard>();

                int index = i;
                _cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }

            ApplyStartingUpgrades();
        }

        private void ApplyStartingUpgrades()
        {
            foreach (UpgradeSO upgrade in _startingUpgrades)
            {
                if (upgrade == null) continue;
                LevelUpUpgrade(upgrade);
            }
        }

        // 업그레이드 하나를 1단계 올린다.
        // 레벨 갱신과 실제 적용이 항상 함께 일어나야 하므로 반드시 이 함수를 거친다.
        // 시작 무기든 카드 선택이든 전부 여기로 들어온다
        private void LevelUpUpgrade(UpgradeSO upgrade)
        {
            int level = GetLevel(upgrade) + 1;

            _levels[upgrade] = level;
            upgrade.Apply(_player, level);

            Debug.Log($"업그레이드 적용: {upgrade.Title} Lv.{level}");
        }

        private void HandleLevelUp(int level)
        {
            _pending++;
            if (_levelText != null) _levelText.text = $"LEVEL {level}";

            _levelUpFeedback?.PlayFeedbacks();   // 추가

            if (_panel.activeSelf) return;
            ShowNext();
        }

        private void ShowNext()
        {
            if (_pending <= 0)
            {
                Close();
                return;
            }
            _pending--;

            Draw(_cards.Length);

            // 남은 후보가 하나도 없으면(전부 만렙) 카드를 띄우지 않는다
            if (_drawn.Count == 0)
            {
                Close();
                return;
            }

            // 순서 주의: 반드시 패널을 먼저 켜야 한다.
            // 부모가 꺼져 있으면 자식 카드도 꺼진 것으로 취급되어 Awake가 실행되지 않고,
            // 그 상태에서 SetData를 부르면 초기화 전이라 그냥 무시된다
            _panel.SetActive(true);
            Time.timeScale = 0f;

            for (int i = 0; i < _cards.Length; i++)
            {
                bool used = i < _drawn.Count;
                _cards[i].gameObject.SetActive(used);

                if (used) _cards[i].SetData(_drawn[i], GetLevel(_drawn[i]));
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowCardsCo());
        }

        // 아직 한 번도 안 고른 업그레이드는 0레벨
        private int GetLevel(UpgradeSO upgrade)
            => _levels.TryGetValue(upgrade, out int level) ? level : 0;

        // 만렙이 아닌 것들 중에서 중복 없이 count개.
        // 앞에서부터 무작위 원소와 자리를 바꿔가는 방식(Fisher-Yates)이라 중복이 구조적으로 불가능하다
        private void Draw(int count)
        {
            _drawn.Clear();
            _candidates.Clear();

            foreach (UpgradeSO upgrade in _pool)
            {
                if (upgrade == null) continue;
                if (GetLevel(upgrade) < upgrade.MaxLevel && upgrade.IsAvailable(_player)) _candidates.Add(upgrade);
            }

            int drawCount = Mathf.Min(count, _candidates.Count);
            for (int i = 0; i < drawCount; i++)
            {
                int index = Random.Range(i, _candidates.Count);
                (_candidates[i], _candidates[index]) = (_candidates[index], _candidates[i]);
                _drawn.Add(_candidates[i]);
            }
        }

        private IEnumerator ShowCardsCo()
        {
            // 뒷면인 동안 눌러버리면 안 되니까 잠가둔다
            SetCardsInteractable(false);

            Coroutine last = null;
            for (int i = 0; i < _drawn.Count; i++)
            {
                _cards[i].ResetCard();
                last = _cards[i].Rise(i * _riseInterval);   // 가장 늦게 출발 = 가장 늦게 도착
            }
            yield return last;

            // 한 장씩 순서대로. 앞 카드가 끝나야 다음 카드가 시작됨
            for (int i = 0; i < _drawn.Count; i++)
            {
                yield return _cards[i].Flip();
            }

            SetCardsInteractable(true);
        }

        private void SetCardsInteractable(bool value)
        {
            foreach (Button button in _cardButtons) button.interactable = value;
        }

        private void OnCardSelected(int index)
        {
            if (index < _drawn.Count) LevelUpUpgrade(_drawn[index]);
            ShowNext();   // 대기 중인 레벨업이 남아 있으면 이어서, 없으면 Close()
        }

        private void Close()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            _panel.SetActive(false);
            Time.timeScale = 1f;
            _player.ResumeExpGain();
        }
    }
}