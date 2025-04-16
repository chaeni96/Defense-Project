using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioClipInfo
{
    public string addressableKey; // 어드레서블 키 = 오디오 클립 이름(사운드 이름)
    public float volume = 1.0f;
    public AudioClip clip;
    public AsyncOperationHandle<AudioClip>? loadOperation; // 어드레서블 로드 핸들

    public AudioClipInfo(string addressableKey, float volume = 1f)
    {
        this.addressableKey = addressableKey;
        this.volume = volume;
    }
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager _instance;

    // 로드된 모든 오디오 클립 정보 관리
    public Dictionary<string, AudioClipInfo> audioClips = new Dictionary<string, AudioClipInfo>();

    // 현재 재생 중인 모든 효과음 관리 (여러 개의 동일한 효과음 지원을 위해 List로 변경)
    public Dictionary<string, List<AudioSource>> activeAudioSources = new Dictionary<string, List<AudioSource>>();

    // 동시에 재생 가능한 같은 효과음의 최대 개수 
    //이것도 AudioClipInfo의 변수로 넣을지 고민중
    [SerializeField] private int maxConcurrentSameSounds = 5;


    // BGM 관련 변수
    private AudioSource bgmAudioSource1;
    private AudioSource bgmAudioSource2;
    private AudioSource currentBgmSource;
    private AudioSource nextBgmSource;
    private string currentBgmKey = "";
    [SerializeField] private float bgmVolume = 0.5f;
    [SerializeField] private float bgmFadeDuration = 1.0f;


    [SerializeField] private float sfxMasterVolume = 1.0f; // SFX 마스터 볼륨


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

            // BGM 오디오 소스 초기화
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

    // BGM 재생 메서드
    public void PlayBGM(string addressableKey, float fadeTime = -1)
    {
        // 이미 같은 BGM이 재생 중이면 무시
        if (currentBgmKey == addressableKey && currentBgmSource.isPlaying)
            return;

        currentBgmKey = addressableKey;
        float actualFadeTime = fadeTime >= 0 ? fadeTime : bgmFadeDuration;
        StartCoroutine(PlayBGMAsync(addressableKey, actualFadeTime));
    }

    private IEnumerator PlayBGMAsync(string addressableKey, float fadeTime)
    {
        // 오디오 클립 정보가 없으면 생성
        if (!audioClips.ContainsKey(addressableKey))
        {
            audioClips[addressableKey] = new AudioClipInfo(addressableKey, bgmVolume);
        }

        AudioClipInfo info = audioClips[addressableKey];

        // 클립이 로드되지 않았으면 어드레서블로 로드
        if (info.clip == null && !info.loadOperation.HasValue)
        {
            // 어드레서블 비동기 로드 시작
            info.loadOperation = Addressables.LoadAssetAsync<AudioClip>(info.addressableKey);
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("BGM 어드레서블 로드 실패: " + info.addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }
        else if (info.loadOperation.HasValue && !info.loadOperation.Value.IsDone)
        {
            // 이미 로드 중이면 완료될 때까지 대기
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("BGM 로드 실패: " + addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }

        // 새 BGM 설정
        nextBgmSource.clip = info.clip;
        nextBgmSource.volume = 0;
        nextBgmSource.Play();

        // 크로스페이드 실행
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

        // 페이드 완료 후 이전 BGM 중지
        currentBgmSource.Stop();
        currentBgmSource.clip = null;

        // 소스 교체
        AudioSource temp = currentBgmSource;
        currentBgmSource = nextBgmSource;
        nextBgmSource = temp;
    }

    // BGM 정지
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

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (currentBgmSource.isPlaying)
        {
            currentBgmSource.volume = bgmVolume;
        }
    }

    // 현재 재생 중인 BGM 이름 반환
    public string GetCurrentBGM()
    {
        return currentBgmKey;
    }

    // 효과음 비동기로 로드하고 재생하는 코루틴
    public void PlaySoundEffect(string addressableKey, float volume)
    {
        StartCoroutine(PlaySoundEffectAsync(addressableKey, volume));
    }

    private IEnumerator PlaySoundEffectAsync(string soundAddressableKey, float volume)
    {
        // 오디오 클립 정보가 없으면 생성
        if (!audioClips.ContainsKey(soundAddressableKey))
        {
            audioClips[soundAddressableKey] = new AudioClipInfo(soundAddressableKey, volume);
        }

        AudioClipInfo info = audioClips[soundAddressableKey];

        // 클립이 로드되지 않았으면 어드레서블로 로드
        if (info.clip == null && !info.loadOperation.HasValue)
        {
            // 어드레서블 비동기 로드 시작
            info.loadOperation = Addressables.LoadAssetAsync<AudioClip>(info.addressableKey);
            yield return info.loadOperation;

            if (info.loadOperation.Value.Status == AsyncOperationStatus.Succeeded)
            {
                info.clip = info.loadOperation.Value.Result;
            }
            else
            {
                Debug.LogWarning("어드레서블 로드 실패: " + info.addressableKey);
                info.loadOperation = null;
                yield break;
            }
        }
        else if (info.loadOperation.HasValue && !info.loadOperation.Value.IsDone)
        {
            // 이미 로드 중이면 완료될 때까지 대기
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

        // 활성 소스 리스트가 없으면 초기화
        if (!activeAudioSources.ContainsKey(soundAddressableKey))
        {
            activeAudioSources[soundAddressableKey] = new List<AudioSource>();
        }

        List<AudioSource> sourcesForThisSound = activeAudioSources[soundAddressableKey];

        // 재생 중인 동일 효과음이 최대 개수에 도달했는지 확인
        if (sourcesForThisSound.Count >= maxConcurrentSameSounds)
        {
            // 가장 오래된 효과음(리스트의 첫 번째 항목)을 중지하고 제거
            AudioSource oldestSource = sourcesForThisSound[0];
            sourcesForThisSound.RemoveAt(0);
            Destroy(oldestSource);
        }

        // 새 오디오 소스 생성
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = info.clip;
        newSource.volume = info.volume * sfxMasterVolume;
        newSource.Play();

        // 활성 소스 리스트에 추가
        sourcesForThisSound.Add(newSource);

        // 재생 완료 후 처리를 위한 코루틴 시작
        StartCoroutine(CleanupAfterPlay(soundAddressableKey, newSource, info.clip.length));
    }

    private IEnumerator CleanupAfterPlay(string addressableKey, AudioSource source, float length)
    {
        yield return new WaitForSeconds(length);

        // 소스가 여전히 활성 상태인지 확인
        if (activeAudioSources.ContainsKey(addressableKey) &&
            activeAudioSources[addressableKey].Contains(source))
        {
            activeAudioSources[addressableKey].Remove(source);
            Destroy(source);

            // 이 효과음의 모든 인스턴스가 제거되었다면 딕셔너리에서 키도 제거
            if (activeAudioSources[addressableKey].Count == 0)
            {
                activeAudioSources.Remove(addressableKey);
            }
        }
    }

    // 특정 효과음 모두 재생 중지
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

    // 모든 효과음 재생 중지
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

    // 효과음 볼륨 조절 메서드
    public void SetSFXVolume(float volume)
    {
        sfxMasterVolume = Mathf.Clamp01(volume);

        // 현재 재생 중인 모든 효과음에도 적용
        foreach (var sources in activeAudioSources.Values)
        {
            foreach (var source in sources)
            {
                // 원래 볼륨 (클립 정보에서) * 마스터 볼륨
                string key = activeAudioSources.FirstOrDefault(x => x.Value.Contains(source)).Key;
                if (!string.IsNullOrEmpty(key) && audioClips.ContainsKey(key))
                {
                    source.volume = audioClips[key].volume * sfxMasterVolume;
                }
            }
        }

        // 설정 저장
        PlayerPrefs.SetFloat("SFXVolume", sfxMasterVolume);
        PlayerPrefs.Save();
    }

    // 지정된 효과음 제외한 모든 효과음 언로드하기 -> 아직 쓰는곳 없음 나중에 최적화위해서
    // 씬전환할때 필요없는 오디오 에셋을 메모리에서 해제하기위함
    // ignoreList = 보존할 목록
    public void UnloadAllSoundsExcept(List<string> ignoreList)
    {
        // 제거할 소리들만 중지
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

        // 캐시된 클립 정보에서 제거
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
            // 어드레서블 리소스 해제
            var info = audioClips[key];
            if (info.loadOperation.HasValue && info.loadOperation.Value.IsValid())
            {
                Addressables.Release(info.loadOperation.Value);
            }
            audioClips.Remove(key);
        }
    }

    // 추가: 씬 전환 등에서 모든 리소스 정리
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

    // OnDestroy에서 리소스 정리
    private void OnDestroy()
    {
        ReleaseAllResources();
    }
}