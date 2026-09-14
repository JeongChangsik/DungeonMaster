using UnityEngine;

// [하는 일] 게임에서 쓰는 소리 파일(BGM, 효과음)과 기본 볼륨을 한곳에 모아 둔 데이터 묶음.
// [붙이는 곳] 오브젝트에 붙이지 않는다. ScriptableObject(씬 밖에 .asset 파일로 저장되는 데이터)라서
//          Project 창 우클릭 > Create > DungeonMaster > AudioDataSO 로 만든다. 지금은 Core/Data/AudioDataSO.asset 하나를 쓴다.
//          그 에셋을 Audio Manager 인스펙터의 Audio Data SO 칸에 넣는다.
// [연결] AudioManager 가 읽는다. 다른 스크립트는 AudioManager.Data.enemyHitSFX 처럼 AudioManager 를 거쳐 꺼낸다.
// [설계] 소리 파일을 코드가 아니라 에셋에 두어서, 소리를 바꿀 때 코드를 고치지 않아도 된다.
//        volume 은 효과음과 BGM 에 곱하는 "기본 비율"이다. 옵션 메뉴의 소리 크기는 이 값을 바꾸지 않는다.
//        에디터에서 플레이 중에 SO 값을 바꾸면 플레이를 멈춰도 에셋 파일에 그대로 남기 때문이다(PauseMenuUI 참고).
[CreateAssetMenu(fileName = "AudioDataSO", menuName = "DungeonMaster/AudioDataSO")]
public class AudioDataSO : ScriptableObject
{
    [Header("Volume Scale")]
    public float volume;

    [Header("BGM Clips")]
    public AudioClip mainBGM;
    public AudioClip battleBGM;

    [Header("SFX Clips")]
    public AudioClip playerAttackSFX;
    public AudioClip enemyAttackSFX;

    [Header("SFX Items")]
    public AudioClip itemPickupSFX;

    [Header("뱀서라이크 전용 SFX")]
    public AudioClip enemyHitSFX;       // 적이 맞을 때
    public AudioClip enemyDeathSFX;     // 적이 죽을 때
    public AudioClip coinPickupSFX;     // 경험치 코인 획득
    public AudioClip levelUpSFX;        // 레벨업
    public AudioClip cardSelectSFX;     // 카드 선택
    public AudioClip weaponThrowSFX;    // 단검 투척
    public AudioClip playerHurtSFX;     // 플레이어 피격
    public AudioClip enemyChargeSFX;    // 돌진형 적이 기를 모을 때 (피할 타이밍을 소리로 알림)
    public AudioClip bombExplodeSFX;    // 폭탄이 터질 때
    public AudioClip playerDeathSFX;    // 플레이어 사망
}
