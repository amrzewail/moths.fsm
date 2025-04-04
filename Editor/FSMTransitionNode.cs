using Moths.FSM;
using Moths.Graphs.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moths.FSM.Graphs.Editor
{
    [System.Serializable]
    public class FSMTransitionNode : BasicNode
    {
        //public abstract FSMState GetState();

        public FSMTransition transition;

        public Port inPort;
        public Port outPort;
        public Port chancePort;

        private VisualElement _inspectorContainer = new VisualElement();

        public FSMTransitionNode(FSMTransition transition)
        {
            this.transition = transition;

            base.title = this.transition.name;

            var style = Resources.Load<StyleSheet>("styles");
            this.styleSheets.Add(style);
        }

        public override void GeneratePorts()
        {
            UpdatePortsView();
        }

        public override void OnSelected()
        {
            base.OnSelected();

            //Selection.activeObject = transition;
        }

        private void UpdatePortsView()
        {
            inputContainer.Clear();
            outputContainer.Clear();

            inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FSMTransition));
            inPort.portName = "In";
            inputContainer.Add(inPort);

            outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FSMTransition));
            outPort.portName = "Out";
            outputContainer.Add(outPort);

            var togglePropsBtn = new Button(() =>
            {
                if (_inspectorContainer.childCount == 0)
                {
                    var inspector = new UnityEditor.UIElements.InspectorElement(transition);
                    inspector.AddToClassList("inspector");
                    _inspectorContainer.Add(inspector);
                }
                else
                {
                    _inspectorContainer.RemoveAt(0);
                }
            })
            { text = "Toggle Properties" };

            extensionContainer.Add(togglePropsBtn);
            extensionContainer.Add(_inspectorContainer);

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}