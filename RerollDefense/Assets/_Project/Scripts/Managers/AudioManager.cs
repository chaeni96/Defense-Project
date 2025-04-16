using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioClipInfo
{
    public string addressableKey; // ��巹���� Ű = ����� Ŭ�� �̸�(���� �̸�)
    public float volume = 1.0f;
    public AudioClip clip;
    public AsyncOperationHandle<AudioClip>? loadOperation; // ��巹���� �ε� �ڵ�

    public AudioClipInfo(string addressableKey, float volume = 1f)
    {
        this.addressableKey = addressableKey;
        this.volume = volume;
    }
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager _instance;

    // �ε�� ��� ����� Ŭ�� ���� ����
    public Dictionary<string, AudioClipInfo> audioClips = new Dictionary<string, AudioClipInfo>();

    // ���� ��� ���� ��� ȿ���� ���� (���� ���� ������ ȿ���� ������ ���� List�� ����)
    public Dictionary<string, List<AudioSource>> activeAudioSources = new Dictionary<string, List<AudioSource>>();

    // ���ÿ� ��� ������ ���� ȿ������ �ִ� ���� 
    //�̰͵� AudioClipInfo�� ������ ������ �����
    [SerializeField] private int maxConcurrentSameSounds = 5;


    // BGM ���� ����
    private AudioSource bgmAudioSource1;
    private AudioSource bgmAudioSource2;
    private AudioSource currentBgmSource;
    private AudioSource nextBgmSource;
    private string currentBgmKey = "";
    [SerializeField] private float bgmVolume = 0.5f;
    [SerializeField] private float bgmFadeDuration = 1.0f;


    [SerializeField] private float sfxMasterVolume = 1.0f; // SFX ������ ����


    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("AudioManager");
                    _instance = singleton.AddComponent<AudioManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            // BGM ����� �ҽ� �ʱ�ȭ
            bgmAudioSource1 = gameObject.AddComponent<AudioSource>();
            bgmAudioSource1.loop = true;
            bgmAudioSource1.volume = bgmVolume;

            bgmAudioSource2 = gameObject.AddComponent<AudioSource>();
            bgmAudioSource2.loop = true;
            bgmAudioSource2.volume = 0;

            currentBgmSource = bgmAudioSource1;
            nextBgmSource = bgmAudioSource2;
        }
    }

    // BGM ��� �޼���
    public void PlayBGM(string addressableKey, float fadeTime = -1)
    {
        // �̹� ���� BGM�� ��� ���̸� ����
        if (currentBgmKey == addressableKey && currentBgmSource.isPlaying)
            return;

        currentBgmKey = addressableKey;
        float actualFadeTime = fadeTime >= 0 ? fadeTime : bgmFadeDuration;
        StartCoroutine(PlayBGMAsync(addressableKey, actualFadeTime));
    }

    private IEnumerator PlayBGMAsync(string addressableKey, float fadeTime)
    {
        // ����� Ŭ�� ������ ������ ����
        if (!audioClips.ContainsKey(addressableKey))
        {
            audioClips[addressableKey] = new AudioClipInfo(addressableKey, bgmVolume);
        }

        AudioClipInfo info = audioClips[addressableKey];

        // Ŭ���� �ε���� �ʾ����� ��巹����� �ε�
        if (info.clip == null && !info.loadOperation.HasValue)
        {
            // ��巹���� �񵿱� �ε� ����
            info.loadOperation = Addressables.LoadAssetAsync<AudioClip>(info.addressableKey);
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("BGM ��巹���� �ε� ����: " + info.addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }
        else if (info.loadOperation.HasValue && !info.loadOperation.Value.IsDone)
        {
            // �̹� �ε� ���̸� �Ϸ�� ������ ���
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("BGM �ε� ����: " + addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }

        // �� BGM ����
        nextBgmSource.clip = info.clip;
        nextBgmSource.volume = 0;
        nextBgmSource.Play();

        // ũ�ν����̵� ����
        yield return StartCoroutine(CrossFadeBGM(fadeTime));
    }

    private IEnumerator CrossFadeBGM(float duration)
    {
        float currentTime = 0;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / duration;
            currentBgmSource.volume = Mathf.Lerp(bgmVolume, 0, t);
            nextBgmSource.volume = Mathf.Lerp(0, bgmVolume, t);
            yield return null;
        }

        // ���̵� �Ϸ� �� ���� BGM ����
        currentBgmSource.Stop();
        currentBgmSource.clip = null;

        // �ҽ� ��ü
        AudioSource temp = currentBgmSource;
        currentBgmSource = nextBgmSource;
        nextBgmSource = temp;
    }

    // BGM ����
    public void StopBGM(float fadeTime = -1)
    {
        float actualFadeTime = fadeTime >= 0 ? fadeTime : bgmFadeDuration;
        StartCoroutine(FadeOutBGM(actualFadeTime));
        currentBgmKey = "";
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float currentTime = 0;
        float startVolume = currentBgmSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / duration;
            currentBgmSource.volume = Mathf.Lerp(startVolume, 0, t);
            yield return null;
        }

        currentBgmSource.Stop();
        currentBgmSource.clip = null;
    }

    // BGM ���� ����
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (currentBgmSource.isPlaying)
        {
            currentBgmSource.volume = bgmVolume;
        }
    }

    // ���� ��� ���� BGM �̸� ��ȯ
    public string GetCurrentBGM()
    {
        return currentBgmKey;
    }

    // ȿ���� �񵿱�� �ε��ϰ� ����ϴ� �ڷ�ƾ
    public void PlaySoundEffect(string addressableKey, float volume)
    {
        StartCoroutine(PlaySoundEffectAsync(addressableKey, volume));
    }

    private IEnumerator PlaySoundEffectAsync(string soundAddressableKey, float volume)
    {
        // ����� Ŭ�� ������ ������ ����
        if (!audioClips.ContainsKey(soundAddressableKey))
        {
            audioClips[soundAddressableKey] = new AudioClipInfo(soundAddressableKey, volume);
        }

        AudioClipInfo info = audioClips[soundAddressableKey];

        // Ŭ���� �ε���� �ʾ����� ��巹����� �ε�
        if (info.clip == null && !info.loadOperation.HasValue)
        {
            // ��巹���� �񵿱� �ε� ����
            info.loadOperation = Addressables.LoadAssetAsync<AudioClip>(info.addressableKey);
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("��巹���� �ε� ����: " + info.addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }
        else if (info.loadOperation.HasValue && !info.loadOperation.Value.IsDone)
        {
            // �̹� �ε� ���̸� �Ϸ�� ������ ���
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("Failed to load Sound: " + soundAddressableKey);
                info.loadOperation = null;
                yield break;
            }
        }

        // Ȱ�� �ҽ� ����Ʈ�� ������ �ʱ�ȭ
        if (!activeAudioSources.ContainsKey(soundAddressableKey))
        {
            activeAudioSources[soundAddressableKey] = new List<AudioSource>();
        }

        List<AudioSource> sourcesForThisSound = activeAudioSources[soundAddressableKey];

        // ��� ���� ���� ȿ������ �ִ� ������ �����ߴ��� Ȯ��
        if (sourcesForThisSound.Count >= maxConcurrentSameSounds)
        {
            // ���� ������ ȿ����(����Ʈ�� ù ��° �׸�)�� �����ϰ� ����
            AudioSource oldestSource = sourcesForThisSound[0];
            sourcesForThisSound.RemoveAt(0);
            Destroy(oldestSource);
        }

        // �� ����� �ҽ� ����
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = info.clip;
        newSource.volume = info.volume * sfxMasterVolume;
        newSource.Play();

        // Ȱ�� �ҽ� ����Ʈ�� �߰�
        sourcesForThisSound.Add(newSource);

        // ��� �Ϸ� �� ó���� ���� �ڷ�ƾ ����
        StartCoroutine(CleanupAfterPlay(soundAddressableKey, newSource, info.clip.length));
    }

    private IEnumerator CleanupAfterPlay(string addressableKey, AudioSource source, float length)
    {
        yield return new WaitForSeconds(length);

        // �ҽ��� ������ Ȱ�� �������� Ȯ��
        if (activeAudioSources.ContainsKey(addressableKey) &&
            activeAudioSources[addressableKey].Contains(source))
        {
            activeAudioSources[addressableKey].Remove(source);
            Destroy(source);

            // �� ȿ������ ��� �ν��Ͻ��� ���ŵǾ��ٸ� ��ųʸ����� Ű�� ����
            if (activeAudioSources[addressableKey].Count == 0)
            {
                activeAudioSources.Remove(addressableKey);
            }
        }
    }

    // Ư�� ȿ���� ��� ��� ����
    public void StopSoundEffect(string addressableKey)
    {
        if (activeAudioSources.ContainsKey(addressableKey))
        {
            foreach (var source in activeAudioSources[addressableKey])
            {
                source.Stop();
                Destroy(source);
            }
            activeAudioSources.Remove(addressableKey);
        }
    }

    // ��� ȿ���� ��� ����
    public void StopAllSoundEffects()
    {
        foreach (var sources in activeAudioSources.Values)
        {
            foreach (var source in sources)
            {
                source.Stop();
                Destroy(source);
            }
        }
        activeAudioSources.Clear();
    }

    // ȿ���� ���� ���� �޼���
    public void SetSFXVolume(float volume)
    {
        sfxMasterVolume = Mathf.Clamp01(volume);

        // ���� ��� ���� ��� ȿ�������� ����
        foreach (var sources in activeAudioSources.Values)
        {
            foreach (var source in sources)
            {
                // ���� ���� (Ŭ�� ��������) * ������ ����
                string key = activeAudioSources.FirstOrDefault(x => x.Value.Contains(source)).Key;
                if (!string.IsNullOrEmpty(key) && audioClips.ContainsKey(key))
                {
                    source.volume = audioClips[key].volume * sfxMasterVolume;
                }
            }
        }

        // ���� ����
        PlayerPrefs.SetFloat("SFXVolume", sfxMasterVolume);
        PlayerPrefs.Save();
    }

    // ������ ȿ���� ������ ��� ȿ���� ��ε��ϱ� -> ���� ���°� ���� ���߿� ����ȭ���ؼ�
    // ����ȯ�Ҷ� �ʿ���� ����� ������ �޸𸮿��� �����ϱ�����
    // ignoreList = ������ ���
    public void UnloadAllSoundsExcept(List<string> ignoreList)
    {
        // ������ �Ҹ��鸸 ����
        List<string> keysToStopAndRemove = new List<string>();
        foreach (var key in activeAudioSources.Keys)
        {
            if (!ignoreList.Contains(key))
            {
                keysToStopAndRemove.Add(key);
            }
        }

        foreach (var key in keysToStopAndRemove)
        {
            foreach (var source in activeAudioSources[key])
            {
                source.Stop();
                Destroy(source);
            }
            activeAudioSources.Remove(key);
        }

        // ĳ�õ� Ŭ�� �������� ����
        List<string> clipKeysToRemove = new List<string>();
        foreach (var key in audioClips.Keys)
        {
            if (!ignoreList.Contains(key))
            {
                clipKeysToRemove.Add(key);
            }
        }

        foreach (var key in clipKeysToRemove)
        {
            // ��巹���� ���ҽ� ����
            var info = audioClips[key];
            if (info.loadOperation.HasValue && info.loadOperation.Value.IsValid())
            {
                Addressables.Release(info.loadOperation.Value);
            }
            audioClips.Remove(key);
        }
    }

    // �߰�: �� ��ȯ ��� ��� ���ҽ� ����
    public void ReleaseAllResources()
    {
        StopAllSoundEffects();

        foreach (var info in audioClips.Values)
        {
            if (info.loadOperation.HasValue && info.loadOperation.Value.IsValid())
            {
                Addressables.Release(info.loadOperation.Value);
            }
        }

        audioClips.Clear();
    }

    // OnDestroy���� ���ҽ� ����
    private void OnDestroy()
    {
        ReleaseAllResources();
    }
}