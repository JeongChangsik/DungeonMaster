using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonMaster.UI
{
    // 아래에서 올라온 뒤 뒷면 -> 앞면으로 뒤집히는 카드
    //
    // 인스펙터 연결 규칙:
    //  - 같은 오브젝트에 붙어 있는 것(Image, RectTransform)은 코드가 직접 찾는다
    //  - 자식 오브젝트(Front, Item, Name...)만 인스펙터로 받는다
    // 이렇게 나눠야 "Card Image 칸에 Item을 넣는" 류의 실수가 아예 불가능해진다
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class LevelUpCard : MonoBehaviour
    {
        [Header("자식 오브젝트 연결")]
        [SerializeField] private GameObject _frontContent;  // Front
        [SerializeField] private Image _image;              // Front > Boder > Item
        [SerializeField] private TMP_Text _nameText;        // Front > Boder > Name
        [SerializeField] private TMP_Text _levelText;       // Front > Boder > Level
        [SerializeField] private TMP_Text _descText;        // Front > Boder > Description

        [Header("카드 앞/뒷면 그림")]
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _frontSprite;

        [Header("올라오기")]
        [SerializeField] private float _riseDistance = 700f;
        [SerializeField] private float _riseDuration = 0.35f;
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("뒤집기")]
        [SerializeField] private float _flipDuration = 0.25f;

        private RectTransform _rect;
        private Image _cardImage;         // 카드 몸통. 자기 자신의 Image라 인스펙터에 안 내놓는다
        private Vector2 _shownPosition;   // 인스펙터에서 잡아둔 최종 위치
        private bool _ready;              // 연결이 다 됐는지. 아니면 아무것도 하지 않는다

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _cardImage = GetComponent<Image>();

            // 연출로 움직이기 전에 캐싱해야 함.
            // 한 번이라도 이동한 뒤에 캐싱하면 카드 위치가 매번 어긋남
            _shownPosition = _rect.anchoredPosition;

            _ready = CheckReferences();
            if (_ready) ClearTexts();

        }

        #region 연결 검사
        // 빈 칸을 한 번에 전부 알려준다.
        // 하나 고치고 실행 -> 또 에러 -> 또 고치고 실행을 반복하지 않기 위함
        private bool CheckReferences()
        {
            bool ok = true;

            ok &= Require(_frontContent, "Front Content", "Front");
            ok &= Require(_image,        "Image",         "Front > Boder > Item");
            ok &= Require(_nameText,     "Name Text",     "Front > Boder > Name");
            ok &= Require(_levelText,    "Level Text",    "Front > Boder > Level");
            ok &= Require(_descText,     "Desc Text",     "Front > Boder > Description");

            return ok;
        }

        private bool Require(UnityEngine.Object target, string slotName, string whatToPutIn)
        {
            if (target != null) return true;

            Debug.LogError(
                $"[{name}] Level Up Card 의 '{slotName}' 칸이 비어 있습니다. " +
                $"{name} 아래의 '{whatToPutIn}' 을 끌어다 놓으세요.", this);
            return false;
        }
        #endregion

        #region 카드 내용
        // 카드가 올라오기 전(= 아직 뒷면일 때) 미리 채워둬야
        // 뒤집히는 순간 완성된 앞면이 나타난다
        public void SetData(UpgradeSO upgrade, int currentLevel)
        {
            if (!_ready) return;

            _nameText.text = upgrade.Title;
            _descText.text = upgrade.Description;
            _levelText.text = $"Lv.{currentLevel}";

            // 쪽지에 그림이 없으면 아예 안 그린다.
            // 그냥 넣으면 흰 네모가 보이거나 직전 카드의 그림이 남는다
            _image.sprite = upgrade.Image;
            _image.enabled = upgrade.Image != null;
        }

        private void ClearTexts()
        {
            // 에디터에서 자리를 잡으려고 넣어둔 임시 글자를 지운다
            _nameText.text = string.Empty;
            _levelText.text = string.Empty;
            _descText.text = string.Empty;
        }
        #endregion

        #region 연출
        // 화면 아래 + 뒷면 상태로 즉시 되돌린다
        public void ResetCard()
        {
            if (!_ready) return;

            StopAllCoroutines();

            _rect.anchoredPosition = _shownPosition + Vector2.down * _riseDistance;
            _rect.localScale = Vector3.one;

            if (_backSprite != null) _cardImage.sprite = _backSprite;
            _frontContent.SetActive(false);
        }

        public Coroutine Rise(float delay) => _ready ? StartCoroutine(RiseCo(delay)) : null;
        public Coroutine Flip() => _ready ? StartCoroutine(FlipCo()) : null;

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
            if (_frontSprite != null) _cardImage.sprite = _frontSprite;
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
        #endregion
    }
}
