using UnityEngine;

public class AutoPlayVoiceAfterDelay : MonoBehaviour
{
    public AudioClip voiceClip;
    public float delayTime = 5f; // 5秒后自动播放（对应列车到站+人物挥手的时间）
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = voiceClip;
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;

        // 延迟5秒播放
        Invoke("PlayVoice", delayTime);
    }

    void PlayVoice()
    {
        Debug.Log("✅ 延迟播放触发！");
        audioSource.Play();
    }
}