using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonMaster.UI
{
    // 아래에서 올라온 뒤 뒷면 -> 앞면으로 뒤집히는 카드
    [RequireComponent(typeof(RectTransform))]
    public class LevelUpCard : MonoBehaviour
    {
        [Header("앞/뒷면")]
        [SerializeField] private Image _cardImage;
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private GameObject _frontContent;  // Front 오브젝트

        [Header("올라오기")]
        [SerializeField] private float _riseDistance = 400f;
        [SerializeField] private float _riseDuration = 0.35f;
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("뒤집기")]
        [SerializeField] private float _flipDuration = 0.25f;

        private RectTransform _rect;
        private Vector2 _shownPosition;   // 인스펙터에서 잡아둔 최종 위치

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            // 연출로 움직이기 전에 캐싱해야 함.
            // 한 번이라도 이동한 뒤에 캐싱하면 카드 위치가 매번 어긋남
            _shownPosition = _rect.anchoredPosition;
        }

        // 화면 아래 + 뒷면 상태로 즉시 되돌린다
        public void ResetCard()
        {
            StopAllCoroutines();

            _rect.anchoredPosition = _shownPosition + Vector2.down * _riseDistance;
            _rect.localScale = Vector3.one;

            _cardImage.sprite = _backSprite;
            _frontContent.SetActive(false);
        }

        public Coroutine Rise(float delay) => StartCoroutine(RiseCo(delay));
        public Coroutine Flip() => StartCoroutine(FlipCo());

        private IEnumerator RiseCo(float delay)
        {
            // timeScale이 0이므로 WaitForSeconds는 영원히 끝나지 않음
            yield return new WaitForSecondsRealtime(delay);

            Vector2 from = _shownPosition + Vector2.down * _riseDistance;
            float t = 0f;

            while (t < _riseDuration)
            {
                t += Time.unscaledDeltaTime;   // 시간이 멈춰 있으므로 unscaled
                float k = _riseCurve.Evaluate(t / _riseDuration);

                // 커브가 1을 넘겨 튕기는 연출을 허용하려고 Unclamped
                _rect.anchoredPosition = Vector2.LerpUnclamped(from, _shownPosition, k);
                yield return null;
            }
            _rect.anchoredPosition = _shownPosition;
        }

        private IEnumerator FlipCo()
        {
            float half = _flipDuration * 0.5f;

            yield return ScaleXCo(1f, 0f, half);   // 납작해지며 사라짐

            // 폭이 0이라 아무것도 안 보이는 순간에 교체
            _cardImage.sprite = _frontSprite;
            _frontContent.SetActive(true);

            yield return ScaleXCo(0f, 1f, half);   // 다시 펼쳐짐
        }

        private IEnumerator ScaleXCo(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _rect.localScale = new Vector3(Mathf.Lerp(from, to, t / duration), 1f, 1f);
                yield return null;
            }
            _rect.localScale = new Vector3(to, 1f, 1f);
        }
    }
}