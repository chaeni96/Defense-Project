using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kylin.FSM
{    
    public class StateNode : Node
    {
        public StateEntry entry { get; }
        public int index { get; }
        public Port inputPort { get; }
        public Port outputPort { get; }
        public Action<StateNode> OnInspectClicked { get; set; }

        private Vector2 lastPosition;
        private FSMEditorWindow _editorWindow;

        public override void OnSelected()
        {
            base.OnSelected();
            var editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
            if (editorWindow != null)
            {
                editorWindow.ShowNodeInspector(this);
            }
        }
        public StateNode(StateEntry entry, int index)
        {
            this.entry = entry; 
            this.index = index;
            title = entry.stateTypeName.Split('.').Last();
            viewDataKey = entry.Id.ToString();
            inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            inputContainer.Add(inputPort); outputContainer.Add(outputPort);
            var btn = new Button(() => OnInspectClicked?.Invoke(this))
            {
                text = "Inspect"
            };
            titleButtonContainer.Add(btn);
            lastPosition = entry.position;
            this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            RefreshExpandedState(); RefreshPorts();

            _editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var currentPosition = GetPosition().position;

            if (Vector2.Distance(lastPosition, currentPosition) > 0.1f)
            {
                entry.position = currentPosition;
                lastPosition = currentPosition;

                if (_editorWindow != null)
                {
                    _editorWindow.UpdateNodePosition(entry.Id, currentPosition);
                }
            }
        }
    }
}
