using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kylin.FSM
{
    public class TransitionEdge : Edge
    {
        public TransitionEntry transitionData { get; }
        public int FromStateId => transitionData.FromStateId;
        public int ToStateId => transitionData.ToStateId;

        private FSMEditorWindow _editorWindow;

        public TransitionEdge(TransitionEntry transitionData, Port output, Port input)
        {
            this.transitionData = transitionData;
            this.output = output;
            this.input = input;

            // ��Ÿ�� ���� �ɼ�
            this.edgeControl.edgeWidth = 2;

            // ���� ���� ǥ�ø� ���� ��
            var infoLabel = new Label();
            if (transitionData.RequiredTriggers != null && transitionData.RequiredTriggers.Length > 0)
            {
                infoLabel.text = string.Join(", ", transitionData.RequiredTriggers);
            }
            else
            {
                infoLabel.text = "No Triggers";
            }

            // �׵θ� ������ ���� Ŭ���� �߰�
            this.AddToClassList("transition-edge");

            // ��Ŭ�� �޴� ����
            this.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Delete Transition", _ => OnDeleteTransition());
            });

            // ���� �� �ð��� ǥ�� ������ ���� �̺�Ʈ ���
            // Unity 21.3.30���� OnSelected/OnUnselected �̺�Ʈ�� ���� �� �����Ƿ� 
            // ���� ���� ���¸� �����ϴ� ������� ����
            this.RegisterCallback<MouseEnterEvent>(evt => {
                if (this.selected)
                {
                    this.edgeControl.edgeWidth = 4;
                    this.edgeControl.inputColor = Color.yellow;
                    this.edgeControl.outputColor = Color.yellow;
                }
                else
                {
                    this.edgeControl.edgeWidth = 3; // ���콺 ���� �� �ణ ����
                }
            });

            this.RegisterCallback<MouseLeaveEvent>(evt => {
                if (this.selected)
                {
                    this.edgeControl.edgeWidth = 4;
                    this.edgeControl.inputColor = Color.yellow;
                    this.edgeControl.outputColor = Color.yellow;
                }
                else
                {
                    this.edgeControl.edgeWidth = 2; // �⺻ �β��� ����
                    this.edgeControl.inputColor = Color.white;
                    this.edgeControl.outputColor = Color.white;
                }
            });
            _editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
        }
        public override void OnSelected()
        {
            base.OnSelected();

            Debug.Log($"TransitionEdge ���õ� (OnSelected): {FromStateId}->{ToStateId}");

            // �ð��� ��Ÿ�� ����
            this.edgeControl.edgeWidth = 4;
            this.edgeControl.inputColor = Color.yellow;
            this.edgeControl.outputColor = Color.yellow;

            // �ν����� ������Ʈ (lazy �ʱ�ȭ)
            var editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
            if (editorWindow != null)
            {
                editorWindow.ShowTransitionInspector(this);
            }
        }
        // ���� ������ �� ȣ��Ǵ� �޼��� �������̵�
        public override void OnUnselected()
        {
            base.OnUnselected();

            Debug.Log($"TransitionEdge ���� ������ (OnUnselected): {FromStateId}->{ToStateId}");

            // �ð��� ��Ÿ�� ����
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = Color.white;
            this.edgeControl.outputColor = Color.white;
        }

        private void OnDeleteTransition()
        {
            if (EditorUtility.DisplayDialog("Delete Transition",
                "Are you sure you want to delete this transition?",
                "Delete", "Cancel"))
            {
                var window = EditorWindow.GetWindow<FSMEditorWindow>();
                window.RemoveTransitionEntry(transitionData);
                window.RefreshGraph(false); // �׷��� �κ� ����
            }
        }
    }
}