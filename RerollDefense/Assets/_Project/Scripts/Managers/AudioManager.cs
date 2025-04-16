using System.Collections;
using System.Collections.Generic;
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
        }
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
        newSource.volume = info.volume;
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

    // ������ ȿ���� ������ ��� ȿ���� ��ε��ϱ�
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