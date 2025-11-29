using Moths.FSM;
using Moths.Graphs.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moths.FSM.Graphs.Editor
{
    using Object = UnityEngine.Object;
    using FSM = Moths.FSM.FSM;


    public class FSMGraph : BasicGraph
    {
        private FSM _currentFSM;
        private FSMGraphSidebar _sidebar;
        
        private FSMStateNode _viewingState;
        private Vector3 _viewingStateOffset;

        private GraphProperties Graph => (GraphProperties)_currentFSM.graphProperties;

        [OnOpenAsset]
        private static bool OpenFSMAsset(int instanceID, int line)
        {
            UnityEngine.Object asset = EditorUtility.InstanceIDToObject(instanceID);

            if (asset is FSM)
            {
                FSM fsm = (FSM)asset;
                if (!fsm.graphProperties)
                {
                    fsm.graphProperties = ScriptableObject.CreateInstance<GraphProperties>();
                    fsm.graphProperties.name = "Graph";
                    AssetDatabase.AddObjectToAsset(fsm.graphProperties, fsm);
                    EditorUtility.SetDirty(fsm);
                    AssetDatabase.SaveAssetIfDirty(fsm);
                }

                FSMGraph window = EditorWindow.CreateWindow<FSMGraph>();
                window.titleContent = new GUIContent(asset.name);
                window._currentFSM = fsm;
                window.UpdateView();
                return true;
            }

            return false;
        }

        protected override void Update()
        {
            base.Update();
        }


        protected override void OnEnable()
        {
            Button selectAssetButton = new Button(() =>
            {
                if (!_currentFSM) return;
                Selection.activeObject = _currentFSM;
            });
            selectAssetButton.text = "Select Asset";
            AddToolbarButton(selectAssetButton);

            Button createStateButton = new Button(OnCreate);
            createStateButton.text = "Create";
            AddToolbarButton(createStateButton);

            Button addChanceButton = new Button(AddChanceNode);
            addChanceButton.text = "Add Chance";
            AddToolbarButton(addChanceButton);

            Button saveButton = new Button(OnSave);
            saveButton.text = "Save";
            AddToolbarButton(saveButton);

            Button copyButton = new Button(OnCopy);
            copyButton.text = "Copy";
            AddToolbarButton(copyButton);

            Button refreshButton = new Button(() => UpdateView());
            refreshButton.text = "Refresh";
            AddToolbarButton(refreshButton);

            base.OnEnable();

            CreateSidebar();

            if (_currentFSM)
            {
                UpdateView();
            }

            //_graphView.style.scale = new Vector3(1, 0.95f, 1);

            OverlayRoot.styleSheets.Add(Resources.Load<StyleSheet>("styles"));

            rootVisualElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            rootVisualElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                DragAndDrop.AcceptDrag();

                Vector2 graphPos = _graphView.GetMousePositionInGraph(evt.mousePosition);

                if (DragAndDrop.paths.Length == DragAndDrop.objectReferences.Length)
                {
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        Object obj = DragAndDrop.objectReferences[i];
                        AddNode(obj, graphPos);
                    }
                }
            });
        }

        private void CreateSidebar()
        {
            OverlayRoot.style.flexDirection = FlexDirection.Row;
            
            _sidebar = new FSMGraphSidebar();
            _sidebar.Title = "States";

            OverlayRoot.Insert(0, _sidebar);
        }

        private void UpdateSidebar()
        {
            _sidebar.Clear();

            _sidebar.AddItem("All States",  ViewAllStates);
            for (int i = 0; i < Graph.stateNodes.Count; i++)
            {
                var state = Graph.stateNodes[i];
                _sidebar.AddItem(Graph.stateNodes[i].state.name, () => ViewState(state));
            }
        }

        private void AddNode(Object obj, Vector3? position)
        {
            if (obj is FSMState)
            {
                bool contains = false;
                foreach (var s in Graph.stateNodes)
                {
                    if (s.state == ((FSMState)obj))
                    {
                        contains = true;
                        break;
                    }
                }
                if (_graphView.Nodes.Where(x => x is FSMStateNode).Where(x => ((FSMStateNode)x).state == ((FSMState)obj)).Any())
                {
                    contains = true;
                }
                if (!contains)
                {
                    AddState((FSMState)obj, position);
                }
                else
                {
                    AddShortState((FSMState)obj, position);
                }
            }
            else if (obj is FSMTransition)
            {
                AddTransition((FSMTransition)obj, position);
            }
            else if (obj is IFSMPlugger)
            {
                AddPlugger((IFSMPlugger)obj, position);
            }
        }

        private void OnCreate()
        {
            List<GUIContent> content = new List<GUIContent>();

            var typesStates = TypeCache.GetTypesWithAttribute<FSMNodeAttribute>();

            List<Type> types = new List<Type>();

            foreach(var state in typesStates)
            {
                var attribute = state.GetCustomAttributes(typeof(FSMNodeAttribute), false).FirstOrDefault() as FSMNodeAttribute;
                content.Add(new GUIContent(attribute.path));
                types.Add(state);
            }


            EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition, Vector2.zero), 
                content.ToArray(), -1, CreateMenuItemCallback, types);

        }

        private void CreateMenuItemCallback(object userData, string[] options, int selected)
        {
            if (!_currentFSM) return;

            List<Type> typesStates = (List<Type>)userData;

            var selectedType = typesStates[selected];

            ScriptableObject nodeInstance = null;

            var currentStates = Graph.stateNodes;
            foreach(var state in currentStates)
            {
                if (state.state.GetType() == selectedType)
                {
                    int choice = EditorUtility.DisplayDialogComplex("FSM Graph", "State already exists, do you want to create new?", "Yes", "Cancel", "No");
                    if (choice == 2)
                    {
                        nodeInstance = state.state;
                    }
                    break;
                }
            }

            if (!nodeInstance)
            {
                nodeInstance = ScriptableObject.CreateInstance(selectedType.FullName);
                nodeInstance.name = selectedType.Name;
                AssetDatabase.AddObjectToAsset(nodeInstance, _currentFSM);
                EditorUtility.SetDirty(_currentFSM);
                AssetDatabase.SaveAssetIfDirty(_currentFSM);
            }

            Vector2 graphCenter = _graphView.contentContainer.WorldToLocal(
                _graphView.worldBound.center
            );
            AddNode(nodeInstance, graphCenter);
        }

        private void AddState(FSMState state, Vector3? position)
        {
            FSMStateNode node = new FSMStateNode(state);
            if (position != null) node.position = position.Value;
            _graphView.AddNode(node, position == null);
            UpdateSidebar();
        }

        private void AddShortState(FSMState state, Vector3? position)
        {
            FSMShortStateNode node = new FSMShortStateNode(state);
            if (position != null) node.position = position.Value;
            _graphView.AddNode(node, position == null);
        }

        private void AddTransition(FSMTransition transition, Vector3? position)
        {
            FSMTransitionNode node = new FSMTransitionNode(transition);
            if (position != null) node.position = position.Value;
            _graphView.AddNode(node, position == null);
        }

        private void AddPlugger(IFSMPlugger plugger, Vector3? position)
        {
            FSMPluggerNode node = new FSMPluggerNode(plugger);
            if (position != null) node.position = position.Value;
            _graphView.AddNode(node, position == null);
        }

        private void AddChanceNode()
        {
            FSMChanceNode node = new FSMChanceNode(new List<FSMChanceNode.Chance>() { new FSMChanceNode.Chance(1), new FSMChanceNode.Chance(1) });
            _graphView.AddNode(node, true);
        }


        private void ProcessStateTransition(Transition transition, FSMTransitionNode transitionNode)
        {
            var edge = _graphView.Edges.Where(y => y.output.node == transitionNode).FirstOrDefault();
            if (edge != null)
            {
                if (edge.input.node is FSMStateNode)
                {
                    transition.newState = ((FSMStateNode)edge.input.node).state;
                    AddTransitionLink(transitionNode.GUID, ((FSMStateNode)edge.input.node).GUID, 0, "");
                }
                else if (edge.input.node is FSMShortStateNode)
                {
                    transition.newState = ((FSMShortStateNode)edge.input.node).state;
                    AddTransitionLink(transitionNode.GUID, ((FSMShortStateNode)edge.input.node).GUID, 0, "");
                }
                else if (edge.input.node is FSMTransitionNode)
                {
                    transition.linked = new Transition();
                    transition.linked.transition = ((FSMTransitionNode)edge.input.node).transition;
                    AddTransitionLink(transitionNode.GUID, ((FSMTransitionNode)edge.input.node).GUID, 0, "");
                    ProcessStateTransition(transition.linked, ((FSMTransitionNode)edge.input.node));
                }else if (edge.input.node is FSMChanceNode)
                {
                    AddTransitionLink(transitionNode.GUID, ((BasicNode)edge.input.node).GUID, 0, "");
                    ProcessChanceTransitions(transition, ((FSMChanceNode)edge.input.node));
                }
            }
        }

        private void ProcessChanceTransitions(Transition transition, FSMChanceNode chanceNode)
        {
            for (int i = 0; i < chanceNode.outPorts.Count; i++)
            {
                var port = chanceNode.outPorts[i];
                var edge = _graphView.Edges.Where(y => y.output == port).FirstOrDefault();
                if (edge == null) continue;
                transition.chances.Add(new Chance
                {
                    newState = edge.input.node is FSMStateNode ? ((FSMStateNode)edge.input.node).state : ((FSMShortStateNode)edge.input.node).state,
                    value = chanceNode.chances[i].value,
                });
                AddTransitionLink(chanceNode.GUID, ((BasicNode)edge.input.node).GUID, i, "");
            }
        }

        private void OnSave()
        {
            var nodes = _graphView.nodes.ToList().Cast<BasicNode>().ToList();
            Graph.stateNodes.Clear();
            Graph.shortStateNodes.Clear();
            Graph.transitionNodes.Clear();
            Graph.pluggerNodes.Clear();
            Graph.chanceNodes.Clear();
            Graph.stateTransitionLinks.Clear();

            List<Object> includedNodes = new List<Object>();
            Dictionary<Transition, float> transitionYPositions = new Dictionary<Transition, float>();

            var viewingState = _viewingState;
            ViewAllStates();

            foreach (var n in nodes)
            {
                if (n is FSMStateNode)
                {
                    var state = (FSMStateNode)n;
                    state.state.inherit = null;
                    List<Transition> transitions = new List<Transition>();
                    List<Object> startPluggers = new List<Object>();
                    List<Object> updatePluggers = new List<Object>();
                    List<Object> exitPluggers = new List<Object>();

                    _graphView.Edges.Where(x => x.output.node == state).ToList().ForEach(edge =>
                    {
                        Node inputNode = edge.input.node;
                        if (inputNode is FSMTransitionNode)
                        {
                            var t = new Transition();
                            t.transition = ((FSMTransitionNode)inputNode).transition;
                            t.chances = new List<Chance>();
                            t.flag = state.FindPortFlag(edge.output);

                            AddTransitionLink(state.GUID, ((FSMTransitionNode)inputNode).GUID, transitions.Count, t.flag);

                            ProcessStateTransition(t, ((FSMTransitionNode)inputNode));

                            transitions.Add(t);
                            transitionYPositions[t] = inputNode.GetPosition().y;
                        }
                        else if (inputNode is FSMPluggerNode)
                        {
                            if (edge.output == state.startPluggerPort)
                            {
                                startPluggers.Add(((FSMPluggerNode)edge.input.node).pluggerObj);
                                AddTransitionLink(state.GUID, ((FSMPluggerNode)edge.input.node).GUID, 0, "");
                            }
                            else if (edge.output == state.updatePluggerPort)
                            {
                                updatePluggers.Add(((FSMPluggerNode)edge.input.node).pluggerObj);
                                AddTransitionLink(state.GUID, ((FSMPluggerNode)edge.input.node).GUID, 1, "");
                            }
                            else if (edge.output == state.exitPluggerPort)
                            {
                                exitPluggers.Add(((FSMPluggerNode)edge.input.node).pluggerObj);
                                AddTransitionLink(state.GUID, ((FSMPluggerNode)edge.input.node).GUID, 2, "");
                            }
                        }
                        else if (inputNode is FSMStateNode || inputNode is FSMShortStateNode)
                        {
                            if (edge.input.portName == "Start")
                            {
                                var t = new Transition();
                                t.transition = null;
                                t.flag = state.FindPortFlag(edge.output);
                                if (inputNode is FSMStateNode) t.newState = ((FSMStateNode)inputNode).state;
                                if (inputNode is FSMShortStateNode) t.newState = ((FSMShortStateNode)inputNode).state;
                                AddTransitionLink(state.GUID, ((BasicNode)inputNode).GUID, 1, state.FindPortFlag(edge.output));
                                transitions.Add(t);
                                transitionYPositions[t] = inputNode.GetPosition().y;
                            }
                        }
                        else if (inputNode is FSMChanceNode)
                        {
                            var t = new Transition();
                            t.transition = null;
                            t.chances = new List<Chance>();
                            t.flag = state.FindPortFlag(edge.output);

                            ProcessChanceTransitions(t, (FSMChanceNode)inputNode);
                            transitions.Add(t);
                            AddTransitionLink(state.GUID, ((BasicNode)inputNode).GUID, 0, state.FindPortFlag(edge.output));
                        }
                    });
                    _graphView.Edges.Where(x => x.input.node == state).ToList().ForEach(x =>
                    {
                        if (x.input.portName == "Inherit")
                        {
                            if (x.output.node is FSMStateNode)
                            {
                                state.state.inherit = ((FSMStateNode)x.output.node).state;
                                AddTransitionLink(((FSMStateNode)x.output.node).GUID, state.GUID, 0, "");
                            }
                            else if (x.output.node is FSMShortStateNode)
                            {
                                state.state.inherit = ((FSMShortStateNode)x.output.node).state;
                                AddTransitionLink(((FSMShortStateNode)x.output.node).GUID, state.GUID, 0, "");
                            }
                        }
                    });
                    transitions.Sort((t0, t1) =>
                    {
                        if (!transitionYPositions.ContainsKey(t0) || !transitionYPositions.ContainsKey(t1)) return 0;
                        return transitionYPositions[t0].CompareTo(transitionYPositions[t1]);
                    });
                    state.state.transitions = transitions.ToArray();
                    state.state.OnStartPluggers = startPluggers;
                    state.state.OnUpdatePluggers = updatePluggers;
                    state.state.OnExitPluggers = exitPluggers;

                    EditorUtility.SetDirty(state.state);
                    Graph.stateNodes.Add(state);

                    includedNodes.Add(state.state);
                }
                else if (n is FSMTransitionNode)
                {
                    Graph.transitionNodes.Add((FSMTransitionNode)n);
                    includedNodes.Add(((FSMTransitionNode)n).transition);
                }
                else if (n is FSMPluggerNode)
                {
                    Graph.pluggerNodes.Add((FSMPluggerNode)n);
                    includedNodes.Add(((FSMPluggerNode)n).pluggerObj);
                }
                else if (n is FSMShortStateNode)
                {
                    Graph.shortStateNodes.Add((FSMShortStateNode)n);
                    includedNodes.Add(((FSMShortStateNode)n).state);
                }else if (n is FSMChanceNode)
                {
                    Graph.chanceNodes.Add((FSMChanceNode)n);
                }

            }

            Object[] childNodes = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(_currentFSM));

            for (int i = childNodes.Length - 1; i >= 0; i--)
            {
                var obj = childNodes[i];
                if (obj == _currentFSM) continue;
                if (obj == Graph || includedNodes.Contains(obj)) continue;
                Undo.DestroyObjectImmediate(obj);
            }

            EditorUtility.SetDirty(_currentFSM);
            AssetDatabase.SaveAssets();

            UpdateView();

            if (viewingState != null)
            {
                ViewState(viewingState);
            }
        }

        private void OnCopy()
        {
            if (!EditorUtility.DisplayDialog("FSM Graph Copy", "Are you sure you want to copy this graph states?", "Yes"))
            {
                return;
            }

            string directory = AssetDatabase.GetAssetPath(_currentFSM);
            directory = directory.Substring(0, directory.Length - _currentFSM.name.Length - ".asset".Length - 1);
            directory += "/FSM/";
            System.IO.Directory.CreateDirectory(directory);
            foreach(var state in Graph.stateNodes)
            {
                var obj = ScriptableObject.Instantiate<FSMState>(state.state);
                AssetDatabase.CreateAsset(obj, directory + state.state.name + ".asset");
                state.state = obj;
            }
            foreach (var transition in Graph.transitionNodes)
            {
                var obj = ScriptableObject.Instantiate<FSMTransition>(transition.transition);
                AssetDatabase.CreateAsset(obj, directory + transition.transition.name + ".asset");
                transition.transition = obj;
            }
            foreach (var plugger in Graph.pluggerNodes)
            {
                var obj = ScriptableObject.Instantiate<ScriptableObject>(plugger.pluggerObj);
                AssetDatabase.CreateAsset(obj, directory + plugger.pluggerObj.name + ".asset");
                plugger.pluggerObj = obj;
            }
            AssetDatabase.Refresh();
        }
        //void OnSelectionChange()
        //{
        //    var obj = Selection.activeObject;
        //    var selectedProperties = (GraphProperties)null;
        //    if(obj is IFSMGraphContainer)
        //    {
        //        selectedProperties = (GraphProperties)(((IFSMGraphContainer)obj).graphProperties);
        //    }else if (obj is GameObject)
        //    {
        //        var components = ((GameObject)obj).GetComponentsInChildren<IFSMGraphContainer>();
        //        if (components == null) return;
        //        if (components.Length == 0) return;
        //        selectedProperties = (GraphProperties)((components[0]).graphProperties);
        //    }
        //    if (selectedProperties != _currentProperties && selectedProperties != null)
        //    {
        //        _currentProperties = selectedProperties;
        //        UpdateView();
        //    }
        //}

        private void AddTransitionLink(string output, string input, int index, string flag)
        {
            Graph.stateTransitionLinks.Add(new StateTransitionLink
            {
                outputGUID = output,
                inputGUID = input,
                index = index,
                flag = flag,
            });
        }

        private void ViewState(FSMStateNode state)
        {
            if (_viewingState == state) return;

            UnoffsetForViewingState();

            _viewingState = state;

            OffsetForViewingState();

            _sidebar.Title = state.state.name;

            string nodeGuid = state.GUID;

            HashSet<string> relatedGuids = new();
            Stack<(string output, string input)> links = new();
            Dictionary<string, string> linksDict = new();
            for (int i = 0; i < Graph.stateTransitionLinks.Count; i++)
            {
                var link = Graph.stateTransitionLinks[i];
                linksDict[link.outputGUID] = link.inputGUID;

                if (link.outputGUID != nodeGuid && link.inputGUID != nodeGuid) continue;
                links.Push((link.outputGUID, link.inputGUID));
            }

            while (links.Count > 0)
            {
                var link = links.Pop();
                if (relatedGuids.Contains(link.input)) continue;
                relatedGuids.Add(link.input);
                relatedGuids.Add(link.output);
                if (!linksDict.ContainsKey(link.input)) continue;
                links.Push((link.input, linksDict[link.input]));
            }

            relatedGuids.Add(state.GUID);

            _graphView.Nodes.ForEach(x =>
            {
                x.style.visibility = relatedGuids.Contains(x.GUID) ? Visibility.Visible : Visibility.Hidden;
                x.CanMove = x.GUID != _viewingState.GUID;
            });
            
            _graphView.Edges.ForEach(x => x.style.visibility = 
            (relatedGuids.Contains(((BasicNode)x.output.node).GUID) && relatedGuids.Contains(((BasicNode)x.input.node).GUID)) ? Visibility.Visible : Visibility.Hidden);
        }

        private void ViewAllStates()
        {
            if (_viewingState == null) return;

            UnoffsetForViewingState();

            var node = _graphView.Nodes.SingleOrDefault(x => x.GUID == _viewingState.GUID);
            if (node != null)
            {
                node.CanMove = true;
            }

            _viewingState = null;
            _sidebar.Title = "States";
            _graphView.Nodes.ForEach(x => x.style.visibility = Visibility.Visible);
            _graphView.Edges.ForEach(x => x.style.visibility = Visibility.Visible);
        }

        private void OffsetForViewingState()
        {
            if (_viewingState == null) return;
            Vector3 offset = _graphView.Nodes.SingleOrDefault(x => x.GUID == _viewingState.GUID).position;
            _graphView.Nodes.ForEach(x => x.SetPosition(x.position - offset));
            _viewingStateOffset = offset;
        }

        private void UnoffsetForViewingState()
        {
            if (_viewingState == null) return;
            Vector3 offset = _viewingStateOffset;
            _graphView.Nodes.ForEach(x => x.SetPosition(x.position + offset));
        }

        private void UpdateView()
        {
            _graphView.ClearNodes();
            _graphView.ClearEdges();

            foreach (var node in Graph.transitionNodes)
            {
                if (node.transition == null) continue;

                var n = new FSMTransitionNode(node.transition);
                n.GUID = node.GUID;
                n.position = node.position;
                _graphView.AddNode(n);
            }

            foreach (var node in Graph.stateNodes)
            {
                if (node.state == null) continue;

                var n = new FSMStateNode(node.state);
                n.GUID = node.GUID;
                n.position = node.position;
                _graphView.AddNode(n);
            }

            foreach (var node in Graph.shortStateNodes)
            {
                if (node.state == null) continue;

                var n = new FSMShortStateNode(node.state);
                n.GUID = node.GUID;
                n.position = node.position;
                _graphView.AddNode(n);
            }

            foreach (var node in Graph.pluggerNodes)
            {
                if (node.pluggerObj == null) continue;

                var n = new FSMPluggerNode((IFSMPlugger)node.pluggerObj);
                n.GUID = node.GUID;
                n.position = node.position;
                _graphView.AddNode(n);
            }

            foreach (var node in Graph.chanceNodes)
            {
                var n = new FSMChanceNode(node.chances);
                n.GUID = node.GUID;
                n.position = node.position;
                _graphView.AddNode(n);
            }

            foreach (var link in Graph.stateTransitionLinks)
            {
                var outputNode = _graphView.GetNodeByGUID(link.outputGUID);
                if (outputNode == null) continue;
                var inputNode = _graphView.GetNodeByGUID(link.inputGUID);
                if (inputNode == null) continue;

                Edge edge = null;

                if(outputNode is FSMStateNode)
                {
                    if (inputNode is FSMTransitionNode)
                    {
                        if (string.IsNullOrEmpty(link.flag))
                        {
                           edge = _graphView.LinkNodes(((FSMStateNode)outputNode).transitionPort, ((FSMTransitionNode)inputNode).inPort);
                        }
                        else
                        {
                            edge = _graphView.LinkNodes(((FSMStateNode)outputNode).GetPortByFlag(link.flag), ((FSMTransitionNode)inputNode).inPort);
                        }
                    }
                    else if(inputNode is FSMStateNode)
                    {
                        switch (link.index)
                        {
                            case 0:
                                edge = _graphView.LinkNodes(((FSMStateNode)outputNode).inheritOutPort, ((FSMStateNode)inputNode).inheritPort);
                                break;
                            case 1:
                                if (string.IsNullOrEmpty(link.flag))
                                {
                                    edge = _graphView.LinkNodes(((FSMStateNode)outputNode).transitionPort, ((FSMStateNode)inputNode).startPort);
                                }
                                else
                                {
                                    edge = _graphView.LinkNodes(((FSMStateNode)outputNode).GetPortByFlag(link.flag), ((FSMStateNode)inputNode).startPort);
                                }
                                break;
                        }
                    }
                    else if (inputNode is FSMShortStateNode)
                    {
                        switch (link.index)
                        {
                            case 1:
                                if (string.IsNullOrEmpty(link.flag))
                                {
                                    edge = _graphView.LinkNodes(((FSMStateNode)outputNode).transitionPort, ((FSMShortStateNode)inputNode).startPort);
                                }
                                else
                                {
                                    edge = _graphView.LinkNodes(((FSMStateNode)outputNode).GetPortByFlag(link.flag), ((FSMShortStateNode)inputNode).startPort);
                                }
                                break;
                        }
                    }
                    else if (inputNode is FSMPluggerNode)
                    {
                        switch (link.index)
                        {
                            case 0:
                                edge = _graphView.LinkNodes(((FSMStateNode)outputNode).startPluggerPort, ((FSMPluggerNode)inputNode).inPort);
                                break;
                            case 1:
                                edge = _graphView.LinkNodes(((FSMStateNode)outputNode).updatePluggerPort, ((FSMPluggerNode)inputNode).inPort);
                                break;
                            case 2:
                                edge = _graphView.LinkNodes(((FSMStateNode)outputNode).exitPluggerPort, ((FSMPluggerNode)inputNode).inPort);
                                break;
                        }
                    }
                    else if (inputNode is FSMChanceNode)
                    {
                        if (string.IsNullOrEmpty(link.flag))
                        {
                            edge = _graphView.LinkNodes(((FSMStateNode)outputNode).transitionPort, ((FSMChanceNode)inputNode).inPort);
                        }
                        else
                        {
                            edge = _graphView.LinkNodes(((FSMStateNode)outputNode).GetPortByFlag(link.flag), ((FSMChanceNode)inputNode).inPort);
                        }
                    }
                }
                if (outputNode is FSMShortStateNode)
                {
                    if (inputNode is FSMStateNode)
                    {
                        edge = _graphView.LinkNodes(((FSMShortStateNode)outputNode).inheritOutPort, ((FSMStateNode)inputNode).inheritPort);
                    }
                }
                else if (outputNode is FSMTransitionNode)
                {
                    if (inputNode is FSMStateNode)
                    {
                        edge = _graphView.LinkNodes(((FSMTransitionNode)outputNode).outPort, ((FSMStateNode)inputNode).startPort);
                    }
                    else if(inputNode is FSMShortStateNode)
                    {
                        edge = _graphView.LinkNodes(((FSMTransitionNode)outputNode).outPort, ((FSMShortStateNode)inputNode).startPort);
                    }
                    else if (inputNode is FSMTransitionNode)
                    {
                        edge = _graphView.LinkNodes(((FSMTransitionNode)outputNode).outPort, ((FSMTransitionNode)inputNode).inPort);
                    }
                    else if (inputNode is FSMChanceNode)
                    {
                        edge = _graphView.LinkNodes(((FSMTransitionNode)outputNode).outPort, ((FSMChanceNode)inputNode).inPort);
                    }
                }
                else if (outputNode is FSMChanceNode)
                {
                    if (inputNode is FSMStateNode)
                    {
                        edge = _graphView.LinkNodes(((FSMChanceNode)outputNode).outPorts[link.index], ((FSMStateNode)inputNode).startPort);
                    }
                    else if (inputNode is FSMShortStateNode)
                    {
                        edge = _graphView.LinkNodes(((FSMChanceNode)outputNode).outPorts[link.index], ((FSMShortStateNode)inputNode).startPort);
                    }
                }
            }


            UpdateSidebar();
        }

        //private void DrawState(FSMState state, int row, int column, int rowOffset, int columnOffset, List<ScriptableObject> addedObjects)
        //{
        //    _iterations++;
        //    if (_iterations >= 100) return;
        //    addedObjects.Add(state);

        //    while (state.inherit != null && !addedObjects.Contains(state.inherit))
        //    {
        //        DrawState(state.inherit, row, column, rowOffset + 1, columnOffset - 1, addedObjects);
        //    }

        //    row += rowOffset;
        //    column += columnOffset;
        //    _graphView.AddNode(new FSMStateNode(state), row + rowOffset, column + columnOffset);
        //    //return;
        //    if(state.transitions != null)
        //    {
        //        int rowIndex = 0;
        //        column++;
        //        foreach(var t in state.transitions)
        //        {
        //            if (!addedObjects.Contains(t.transition))
        //            {
        //                DrawTransition(t.transition, row - (rowIndex), column, addedObjects);
        //            }
        //            if (!addedObjects.Contains(t.newState))
        //            {
        //                DrawState(t.newState, row - (rowIndex++), column, rowOffset, columnOffset, addedObjects);
        //            }
        //        }
        //    }
        //}

        //private void DrawTransition(FSMTransition transition, int row, int column, List<ScriptableObject> addedObjects)
        //{
        //    _iterations++;
        //    if (_iterations >= 100) return;
        //    addedObjects.Add(transition);

        //    _graphView.AddNode(new FSMTransitionNode(transition), row, column);
        //}
    }
}