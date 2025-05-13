using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameWaveProgressInfo : MonoBehaviour
{
    [SerializeField] private Transform progressInfoLayout; // HorizontalLayoutGroup
    [SerializeField] private GameObject gameProgressPrefab; // GameProgressObject 프리팹

    private List<GameProgressObject> progressObjects = new List<GameProgressObject>();
    private int maxProgressObjects = 6; // 한 번에 표시할 최대 객체 수
    private int currentGroupStartIndex = 0; // 현재 표시 중인 웨이브 그룹의 시작 인덱스

    private List<WaveBase> allWaves = new List<WaveBase>(); // 전체 웨이브 목록
    private int currentWaveIndex = 0; // 현재 진행 중인 웨이브 인덱스

    private void Awake()
    {
        // 프로그레스 오브젝트 미리 생성
        CreateProgressObjects();
    }

    private void Start()
    {
        // StageManager가 초기화된 후 웨이브 정보 가져오기
        if (StageManager.Instance != null)
        {
            // 웨이브 인덱스 변경 이벤트 구독
            StageManager.Instance.OnWaveIndexChanged += OnWaveIndexChanged;

            // 현재 웨이브 정보로 초기화
            InitializeWithCurrentWaves();
        }
    }

    // StageManager에서 현재 웨이브 정보 가져와 초기화
    private void InitializeWithCurrentWaves()
    {
        allWaves = new List<WaveBase>(StageManager.Instance.GetWaveList());
        currentWaveIndex = StageManager.Instance.currentWaveIndex;

        // 그룹 시작 인덱스 계산
        currentGroupStartIndex = (currentWaveIndex / maxProgressObjects) * maxProgressObjects;

        // UI 업데이트
        UpdateProgressUI();
    }

    // 프로그레스 오브젝트 생성
    private void CreateProgressObjects()
    {
        // 기존 객체 정리
        foreach (var obj in progressObjects)
        {
            if (obj != null)
                Destroy(obj.gameObject);
        }
        progressObjects.Clear();

        // 새 객체 생성
        for (int i = 0; i < maxProgressObjects; i++)
        {
            GameObject newObj = Instantiate(gameProgressPrefab, progressInfoLayout);
            GameProgressObject progressObj = newObj.GetComponent<GameProgressObject>();
            progressObjects.Add(progressObj);
        }
    }

    // 웨이브 변경 이벤트 핸들러
    private void OnWaveIndexChanged(int newIndex, int totalWaves)
    {
        currentWaveIndex = newIndex - 1; // StageManager는 1부터 시작하므로 0 기반 인덱스로 변환

        // 그룹 시작 인덱스 계산
        int newGroupStartIndex = (currentWaveIndex / maxProgressObjects) * maxProgressObjects;

        // 그룹이 변경되었으면 UI 전체 업데이트
        if (newGroupStartIndex != currentGroupStartIndex)
        {
            currentGroupStartIndex = newGroupStartIndex;
            UpdateProgressUI();
        }
        else
        {
            // 같은 그룹 내에서는 활성화 상태만 업데이트
            UpdateActiveState();
        }
    }

    // 프로그레스 UI 전체 업데이트
    private void UpdateProgressUI()
    {
        for (int i = 0; i < maxProgressObjects; i++)
        {
            int waveIndex = currentGroupStartIndex + i;

            // 웨이브 인덱스가 유효한지 확인
            if (waveIndex < allWaves.Count)
            {
                // 아이콘 및 활성화 상태 설정
                progressObjects[i].gameObject.SetActive(true);
                progressObjects[i].SetIcon(allWaves[waveIndex]);
                progressObjects[i].SetActive(waveIndex == currentWaveIndex);
            }
            else
            {
                // 남은 슬롯은 비활성화
                progressObjects[i].gameObject.SetActive(false);
            }
        }
    }

    // 활성화 상태만 업데이트 (같은 그룹 내 웨이브 변경 시)
    private void UpdateActiveState()
    {
        for (int i = 0; i < maxProgressObjects; i++)
        {
            int waveIndex = currentGroupStartIndex + i;
            if (waveIndex < allWaves.Count)
                progressObjects[i].SetActive(waveIndex == currentWaveIndex);
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnWaveIndexChanged -= OnWaveIndexChanged;
    }
}
