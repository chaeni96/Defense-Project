#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Kylin.FSM
{
    [CustomEditor(typeof(FSMObjectBase), true)]
    public class FSMDebugInspector : Editor
    {
        private GUIStyle _boldLabelStyle;
        private GUIStyle _stateLabelStyle;
        private GUIStyle _triggerStyle;
        private GUIStyle _activeTriggerStyle;

        private FSMObjectBase _target;
        private Dictionary<Trigger, bool> _triggerToggles = new Dictionary<Trigger, bool>();

        private void OnEnable()
        {
            _target = (FSMObjectBase)target;

            // Ʈ���� ��� �ʱ�ȭ
            _triggerToggles.Clear();
            foreach (Trigger trigger in System.Enum.GetValues(typeof(Trigger)))
            {
                if (trigger == Trigger.None) continue;
                _triggerToggles[trigger] = false;
            }
        }

        public override void OnInspectorGUI()
        {
            // �⺻ �ν����� ǥ��
            DrawDefaultInspector();

            // ��Ÿ�� �ʱ�ȭ
            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                _boldLabelStyle.fontSize = 14;

                _stateLabelStyle = new GUIStyle(EditorStyles.helpBox);
                _stateLabelStyle.fontSize = 12;
                _stateLabelStyle.fontStyle = FontStyle.Bold;
                _stateLabelStyle.alignment = TextAnchor.MiddleCenter;

                _triggerStyle = new GUIStyle(EditorStyles.miniButton);
                _triggerStyle.margin = new RectOffset(4, 4, 2, 2);

                _activeTriggerStyle = new GUIStyle(_triggerStyle);
                _activeTriggerStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.8f, 0.6f, 0.8f));
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("FSM Debug", _boldLabelStyle);

            // ���� ���� ǥ��
            if (Application.isPlaying && _target.stateMachine != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_target.CurrentStateName, _stateLabelStyle, GUILayout.Height(30));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Send Triggers", EditorStyles.boldLabel);

                // Ʈ���� ��ư �׸��� ǥ��
                int col = 0;
                EditorGUILayout.BeginHorizontal();

                foreach (Trigger trigger in System.Enum.GetValues(typeof(Trigger)))
                {
                    if (trigger == Trigger.None) continue;

                    bool isActive = _triggerToggles.ContainsKey(trigger) && _triggerToggles[trigger];
                    GUIStyle style = isActive ? _activeTriggerStyle : _triggerStyle;

                    if (GUILayout.Button(trigger.ToString(), style))
                    {
                        if (Event.current.button == 0) // ���� Ŭ�� - �̺�Ʈ�� Ʈ����
                        {
                            _target.stateMachine?.RegisterTrigger(trigger);
                        }
                        else if (Event.current.button == 1) // ������ Ŭ�� - ���Ӽ� Ʈ���� ���
                        {
                            _triggerToggles[trigger] = !_triggerToggles[trigger];

                            if (_triggerToggles[trigger])
                                _target.stateMachine?.AddPersistentTrigger(trigger);
                            else
                                _target.stateMachine?.RemovePersistentTrigger(trigger);
                        }
                    }

                    col++;
                    if (col % 3 == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("Left-click: Send one-time trigger\nRight-click: Toggle persistent trigger", MessageType.Info);

                if (_target.stateMachine != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Persistent Mask", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"0x{_target.stateMachine.PersistentMask:X8}");

                    // Ȱ��ȭ�� ���Ӽ� Ʈ���� ��� ǥ��
                    int mask = _target.stateMachine.PersistentMask;
                    var activeTriggers = System.Enum.GetValues(typeof(Trigger))
                        .Cast<Trigger>()
                        .Where(t => t != Trigger.None && (mask & (int)t) != 0)
                        .ToList();

                    if (activeTriggers.Count > 0)
                    {
                        EditorGUILayout.LabelField("Active persistent triggers:");
                        EditorGUILayout.LabelField(string.Join(", ", activeTriggers));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No active persistent triggers");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter play mode to debug FSM", MessageType.Info);
            }
        }

        // �ؽ�ó ���� ��ƿ��Ƽ �Լ�
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
#endif