using UnityEngine;

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
}
