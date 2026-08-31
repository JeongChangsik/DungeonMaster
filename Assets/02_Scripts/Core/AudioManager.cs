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

    void Start()
    {
        // "this." 빼도 됨
        _bgmSource = this.gameObject.AddComponent<AudioSource>();
        _sfxPlayerSource = this.gameObject.AddComponent<AudioSource>();
        _sfxEnemySource = gameObject.AddComponent<AudioSource>();

        _bgmSource.loop = true;
        _sfxPlayerSource.loop = false;
        _sfxEnemySource.loop = false;

        // 게임 시작 시 BGM 재생
        PlayBGM(AudioDataSO.mainBGM);
    }

    #region 공통 메서드
    public void PlayBGM(AudioClip clip)
    {
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

    #endregion
}
