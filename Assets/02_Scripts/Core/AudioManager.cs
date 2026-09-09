using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    // 싱글턴(Singleton) 디자인 패턴

    // 오디오 데이터SO
    public AudioDataSO AudioDataSO;

    // 오디오 소스 컴포넌트 변수
    private AudioSource _bgmSource;
    private AudioSource _sfxPlayerSource;
    private AudioSource _sfxEnemySource;
    private AudioSource _sfxSource;     // 뱀서라이크용 공용 원샷 소스

    [Header("효과음 겹침 방지")]
    [Tooltip("같은 효과음이 이 간격 안에 다시 울리지 않게 한다. 적 20마리를 동시에 때리면 소음이 되기 때문")]
    [SerializeField] private float _sfxMinInterval = 0.05f;

    // 클립별 마지막 재생 시각. 시간이 멈춰도(레벨업 카드) 동작해야 하므로 unscaledTime 을 쓴다
    private readonly Dictionary<AudioClip, float> _lastPlay = new Dictionary<AudioClip, float>();

    void Start()
    {
        // "this." 빼도 됨
        _bgmSource = this.gameObject.AddComponent<AudioSource>();
        _sfxPlayerSource = this.gameObject.AddComponent<AudioSource>();
        _sfxEnemySource = gameObject.AddComponent<AudioSource>();
        _sfxSource = gameObject.AddComponent<AudioSource>();

        _bgmSource.loop = true;
        _sfxPlayerSource.loop = false;
        _sfxEnemySource.loop = false;
        _sfxSource.loop = false;

        if (AudioDataSO == null)
        {
            Debug.LogError("AudioManager::Start() AudioDataSO 가 비어 있습니다.");
            return;
        }

        // 게임 시작 시 BGM 재생
        PlayBGM(AudioDataSO.mainBGM);
    }

    #region 공통 메서드
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;

        _bgmSource.clip = clip;
        _bgmSource.volume = AudioDataSO.volume;
        _bgmSource.Play();
    }

    public void PlayerSFX(AudioClip clip)
    {
        _sfxPlayerSource.PlayOneShot(clip, AudioDataSO.volume);
    }

    public void EnemySFX(AudioClip clip)
    {
        _sfxEnemySource.PlayOneShot(clip, AudioDataSO.volume);
    }

    // 호출부를 짧게 쓰기 위한 정적 헬퍼.
    // 사용 예: AudioManager.Play(AudioManager.Data?.enemyHitSFX);
    public static AudioDataSO Data { get { return Instance != null ? Instance.AudioDataSO : null; } }

    public static void Play(AudioClip clip, float pitchJitter = 0.08f)
    {
        if (Instance != null) Instance.PlaySFX(clip, pitchJitter);
    }

    // 뱀서라이크용 공용 효과음.
    // 같은 클립이 짧은 간격에 반복되면 무시하고, 피치를 조금씩 흔들어 기계적으로 들리지 않게 한다.
    public void PlaySFX(AudioClip clip, float pitchJitter = 0.08f)
    {
        if (clip == null || _sfxSource == null) return;

        float last;
        if (_lastPlay.TryGetValue(clip, out last) && Time.unscaledTime < last + _sfxMinInterval) return;
        _lastPlay[clip] = Time.unscaledTime;

        _sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        _sfxSource.PlayOneShot(clip, AudioDataSO != null ? AudioDataSO.volume : 1f);
    }

    #endregion
}
