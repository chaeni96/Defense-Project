using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameWaveProgressInfo : MonoBehaviour
{
    [SerializeField] private Transform progressInfoLayout; // HorizontalLayoutGroup
    [SerializeField] private GameObject gameProgressPrefab; // GameProgressObject ������

    private List<GameProgressObject> progressObjects = new List<GameProgressObject>();
    private int maxProgressObjects = 6; // �� ���� ǥ���� �ִ� ��ü ��
    private int currentGroupStartIndex = 0; // ���� ǥ�� ���� ���̺� �׷��� ���� �ε���

    private List<WaveBase> allWaves = new List<WaveBase>(); // ��ü ���̺� ���
    private int currentWaveIndex = 0; // ���� ���� ���� ���̺� �ε���

    private void Awake()
    {
        // ���α׷��� ������Ʈ �̸� ����
        CreateProgressObjects();
    }

    private void Start()
    {
        // StageManager�� �ʱ�ȭ�� �� ���̺� ���� ��������
        if (StageManager.Instance != null)
        {
            // ���̺� �ε��� ���� �̺�Ʈ ����
            StageManager.Instance.OnWaveIndexChanged += OnWaveIndexChanged;

            // ���� ���̺� ������ �ʱ�ȭ
            InitializeWithCurrentWaves();
        }
    }

    // StageManager���� ���� ���̺� ���� ������ �ʱ�ȭ
    private void InitializeWithCurrentWaves()
    {
        allWaves = new List<WaveBase>(StageManager.Instance.GetWaveList());
        currentWaveIndex = StageManager.Instance.currentWaveIndex;

        // �׷� ���� �ε��� ���
        currentGroupStartIndex = (currentWaveIndex / maxProgressObjects) * maxProgressObjects;

        // UI ������Ʈ
        UpdateProgressUI();
    }

    // ���α׷��� ������Ʈ ����
    private void CreateProgressObjects()
    {
        // ���� ��ü ����
        foreach (var obj in progressObjects)
        {
            if (obj != null)
                Destroy(obj.gameObject);
        }
        progressObjects.Clear();

        // �� ��ü ����
        for (int i = 0; i < maxProgressObjects; i++)
        {
            GameObject newObj = Instantiate(gameProgressPrefab, progressInfoLayout);
            GameProgressObject progressObj = newObj.GetComponent<GameProgressObject>();
            progressObjects.Add(progressObj);
        }
    }

    // ���̺� ���� �̺�Ʈ �ڵ鷯
    private void OnWaveIndexChanged(int newIndex, int totalWaves)
    {
        currentWaveIndex = newIndex - 1; // StageManager�� 1���� �����ϹǷ� 0 ��� �ε����� ��ȯ

        // �׷� ���� �ε��� ���
        int newGroupStartIndex = (currentWaveIndex / maxProgressObjects) * maxProgressObjects;

        // �׷��� ����Ǿ����� UI ��ü ������Ʈ
        if (newGroupStartIndex != currentGroupStartIndex)
        {
            currentGroupStartIndex = newGroupStartIndex;
            UpdateProgressUI();
        }
        else
        {
            // ���� �׷� �������� Ȱ��ȭ ���¸� ������Ʈ
            UpdateActiveState();
        }
    }

    // ���α׷��� UI ��ü ������Ʈ
    private void UpdateProgressUI()
    {
        for (int i = 0; i < maxProgressObjects; i++)
        {
            int waveIndex = currentGroupStartIndex + i;

            // ���̺� �ε����� ��ȿ���� Ȯ��
            if (waveIndex < allWaves.Count)
            {
                // ������ �� Ȱ��ȭ ���� ����
                progressObjects[i].gameObject.SetActive(true);
                progressObjects[i].SetIcon(allWaves[waveIndex]);
                progressObjects[i].SetActive(waveIndex == currentWaveIndex);
            }
            else
            {
                // ���� ������ ��Ȱ��ȭ
                progressObjects[i].gameObject.SetActive(false);
            }
        }
    }

    // Ȱ��ȭ ���¸� ������Ʈ (���� �׷� �� ���̺� ���� ��)
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
