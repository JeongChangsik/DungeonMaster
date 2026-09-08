using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonMaster.Character.Player;
using MoreMountains.Feedbacks;
using System.Collections;

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

            // TODO: 업그레이드 후보 3개를 뽑아 카드에 표시

            _panel.SetActive(true);
            Time.timeScale = 0f;

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowCardsCo());
        }

        private IEnumerator ShowCardsCo()
        {
            // 뒷면인 동안 눌러버리면 안 되니까 잠가둔다
            SetCardsInteractable(false);

            Coroutine last = null;
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].ResetCard();
                last = _cards[i].Rise(i * _riseInterval);   // 가장 늦게 출발 = 가장 늦게 도착
            }
            yield return last;

            // 한 장씩 순서대로. 앞 카드가 끝나야 다음 카드가 시작됨
            for (int i = 0; i < _cards.Length; i++)
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
            // TODO: 선택한 업그레이드를 플레이어에 적용

            Debug.Log($"카드 {index} 선택");
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