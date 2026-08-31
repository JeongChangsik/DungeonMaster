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

}
