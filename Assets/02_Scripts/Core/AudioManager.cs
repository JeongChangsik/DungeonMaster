using System.Collections.Generic;
using UnityEngine;

// [하는 일] 배경음(BGM)과 효과음(SFX)을 재생한다. 게임이 시작되면 mainBGM 을 튼다.
// [붙이는 곳] 씬의 Audio Manager 오브젝트(GamePlay, RogueLike 씬 둘 다 있다). 인스펙터 Audio Data SO 칸에 AudioDataSO 에셋을 넣는다.
//          AudioSource(실제로 소리를 내는 스피커 컴포넌트)는 미리 붙이지 않아도 Start 에서 코드가 붙인다.
// [연결] AudioDataSO : 소리 파일 목록과 기본 볼륨을 읽는다
//        뱀서라이크 코드(적, 무기, 코인, 레벨업 카드 등)는 AudioManager.Play(...) 한 줄로 효과음을 낸다
//        구 GamePlay 씬 코드(Warrior, Swampy)는 PlayerSFX / EnemySFX 를 쓴다
// [설계] Singleton<T> 를 물려받아 AudioManager.Instance 로 어디서든 부를 수 있다.
//        뱀서라이크는 같은 소리가 한순간에 수십 번 겹치므로, 클립마다 마지막 재생 시각을 기억해 너무 촘촘한 재생은 건너뛴다.
//        옵션 메뉴의 전체 소리 크기는 여기서 다루지 않는다. PauseMenuUI 가 AudioListener.volume 으로 한 번에 조절한다.
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

        // 에셋 칸이 비어 있으면 아래 PlayBGM 에서 오류가 나므로, 무엇이 빠졌는지 알리고 멈춘다
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
    // 실제 호출부는 ?. 대신 "Data != null ? Data.xxx : null" 로 쓴다.
    // 유니티 오브젝트는 ?. 로 검사하면 파괴된 오브젝트를 null 로 알아보지 못할 수 있기 때문이다
    public static AudioDataSO Data { get { return Instance != null ? Instance.AudioDataSO : null; } }

    // minInterval 을 넘기지 않으면(-1) 인스펙터에 설정된 기본 간격을 쓴다.
    // 자주 일어나는 사건(적 피격/사망, 코인)은 호출부에서 더 긴 간격을 준다.
    // static 이라 AudioManager.Instance.PlaySFX(...) 대신 AudioManager.Play(...) 로 짧게 부를 수 있다
    public static void Play(AudioClip clip, float pitchJitter = 0.08f, float minInterval = -1f)
    {
        if (Instance != null) Instance.PlaySFX(clip, pitchJitter, minInterval);
    }

    // 뱀서라이크용 공용 효과음.
    // 같은 클립이 짧은 간격에 반복되면 무시하고, 피치를 조금씩 흔들어 기계적으로 들리지 않게 한다.
    //
    // 간격을 사건마다 다르게 줄 수 있어야 한다.
    // 실측: 적 160마리와 싸울 때 피격음/사망음/코인음 세 개가 동시에 기본 간격(0.05초)에
    // 딱 붙어서 나고 있었다. 초당 60번이 겹치면 소리의 벽이 되어 아무것도 안 들린다.
    public void PlaySFX(AudioClip clip, float pitchJitter = 0.08f, float minInterval = -1f)
    {
        if (clip == null || _sfxSource == null) return;

        float interval = minInterval >= 0f ? minInterval : _sfxMinInterval;

        float last;
        if (_lastPlay.TryGetValue(clip, out last) && Time.unscaledTime < last + interval) return;
        _lastPlay[clip] = Time.unscaledTime;

        // 음 높이(pitch)를 1 근처에서 조금씩 바꾼다.
        // 볼륨은 AudioDataSO.volume(기본 비율)을 쓰고, 옵션 메뉴의 AudioListener.volume 이 그 위에 한 번 더 곱해진다
        _sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        _sfxSource.PlayOneShot(clip, AudioDataSO != null ? AudioDataSO.volume : 1f);
    }

    #endregion
}
