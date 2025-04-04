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
    public class FSMStateNode : BasicNode
    {
        //public abstract FSMState GetState();

        public FSMState state;

        public Port transitionPort;
        public Port[] flagPorts;
        public Port startPort;
        public Port inheritPort;
        public Port inheritOutPort;

        public Port startPluggerPort;
        public Port updatePluggerPort;
        public Port exitPluggerPort;

        private VisualElement _inspectorContainer = new VisualElement();

        public FSMStateNode(FSMState state)
        {
            this.state = state;


            base.title = this.state.name;

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

            //Selection.activeObject = state;
        }

        private void UpdatePortsView()
        {
            inputContainer.Clear();
            outputContainer.Clear();

            inheritPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(FSMState));
            inheritPort.portName = "Inherit";
            inputContainer.Add(inheritPort);



            startPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FSMTransition));
            startPort.portName = "Start";
            inputContainer.Add(startPort);


            inheritOutPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(FSMState));
            inheritOutPort.portName = "Inherit";
            outputContainer.Add(inheritOutPort);

            startPluggerPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(IFSMPlugger));
            startPluggerPort.portName = "Start Plugger";
            outputContainer.Add(startPluggerPort);

            updatePluggerPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(IFSMPlugger));
            updatePluggerPort.portName = "Update Plugger";
            outputContainer.Add(updatePluggerPort);

            exitPluggerPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(IFSMPlugger));
            exitPluggerPort.portName = "Exit Plugger";
            outputContainer.Add(exitPluggerPort);

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(FSMTransition));
            port.portName = "Transitions";
            outputContainer.Add(port);
            transitionPort = port;


            var flagsContainer = new VisualElement();

            var stateFlags = state.GetFlags();
            
            flagPorts = new Port[stateFlags.Length];

            for (int i = 0; i < stateFlags.Length; i++) 
            {
                var flagPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(FSMTransition));
                flagPort.portName = stateFlags[i].name;
                flagsContainer.Add(flagPort);
                flagPorts[i] = flagPort;
            }

            outputContainer.Add(flagsContainer);

            var togglePropsBtn = new Button(() =>
            {
                if (_inspectorContainer.childCount == 0)
                {
                    var inspector = new UnityEditor.UIElements.InspectorElement(state);
                    inspector.AddToClassList("inspector");
                    _inspectorContainer.Add(inspector);
                }
                else
                {
                    _inspectorContainer.RemoveAt(0);
                }
            }) { text = "Toggle Properties" };

            extensionContainer.Add(togglePropsBtn);
            extensionContainer.Add(_inspectorContainer);

            RefreshPorts();
            RefreshExpandedState();
        }


        public string FindPortFlag(Port port)
        {
            string flag = "";
            for (int i = 0; i < flagPorts.Length; i++)
            {
                if (port != flagPorts[i]) continue;
                flag = flagPorts[i].portName;
                break;
            }
            return flag;
        }

        public Port GetPortByFlag(string flagName)
        {
            for (int i = 0; i < flagPorts.Length; i++)
            {
                if (flagName != flagPorts[i].portName) continue;
                return flagPorts[i];
            }
            return null;
        }
    }
}