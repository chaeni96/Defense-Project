using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private static EventManager _instance;
    public static EventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("EventManager");
                    _instance = obj.AddComponent<EventManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    // 이벤트 고유ID랑 실제 이벤트 객체 매핑
    private Dictionary<string, IEvent> registeredEvents = new Dictionary<string, IEvent>();

    // 특정 오브젝트에 발생하는 이벤트들 관리, 트리거로 넣어두기
    private Dictionary<GameObject, List<KeyValuePair<EventTriggerType, string>>> objectEvents = new Dictionary<GameObject, List<KeyValuePair<EventTriggerType, string>>>();

    // 이벤트 등록
    public void RegisterEvent(string eventId, IEvent gameEvent)
    {
        if (!registeredEvents.ContainsKey(eventId))
        {
            registeredEvents.Add(eventId, gameEvent);
        }
        else
        {
            registeredEvents[eventId] = gameEvent;
        }
    }

    // 객체에 이벤트 연결
    public void AssignEventToObject(GameObject obj, EventTriggerType triggerType, string eventId)
    {
        if (!objectEvents.ContainsKey(obj))
        {
            objectEvents[obj] = new List<KeyValuePair<EventTriggerType, string>>();
        }

        objectEvents[obj].Add(new KeyValuePair<EventTriggerType, string>(triggerType, eventId));
    }

    // 이벤트 해제
    public void UnassignEventsFromObject(GameObject obj)
    {
        if (objectEvents.ContainsKey(obj))
        {
            objectEvents.Remove(obj);
        }
    }

    // 이벤트 트리거
    public void TriggerEvent(GameObject obj, EventTriggerType triggerType, Vector3 position)
    {
        if (objectEvents.TryGetValue(obj, out var events))
        {
            foreach (var eventPair in events)
            {
                if (eventPair.Key == triggerType &&
                    registeredEvents.TryGetValue(eventPair.Value, out var gameEvent))
                {
                    gameEvent.StartEvent(obj, position);
                }
            }
        }
    }

    // 이벤트 데이터로부터 이벤트 클래스 생성
    public IEvent CreateEventFromData(D_EventDummyData eventData)
    {
        if (eventData is D_SpawnEnemyEventData spawnData)
        {
            return new SpawnEnemyEvent(spawnData);
        }
        else if(eventData is D_DropItemEventData itemData)
        {
            return new DropItemEvent(itemData);
        }

        return null;
    }
}