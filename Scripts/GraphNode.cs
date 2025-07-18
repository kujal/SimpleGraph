/*
// This script creates the GraphNode class, which then is used to create nodes using the GraphUtility & GraphEditorWindow class.
*/

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using SimpleGraph; 
using UnityEditor;

namespace SimpleGraph
{
    [System.Serializable]
    public class GraphNode : MonoBehaviour
    {
        public Rect windowRect;
        public string nodeName;
        public NodeType nodeType;
        public List<GraphNode> previousNodes = new List<GraphNode>(); // Add list for previous nodes
        public List<GraphNode> nextNodes = new List<GraphNode>(); // Add list for next nodes

        [HideInInspector]
        public bool isCompleted = false; 
        [HideInInspector]
        public bool isActive = false; 
        [HideInInspector]
        public bool requireAllCompleted;
        [HideInInspector]
        public bool startOnBegin;
        


        public delegate void NodeEventHandler(GraphNode node);
        public static event NodeEventHandler OnSetStartNode;
        public static event NodeEventHandler OnSetEndNode;

        [System.Serializable]
        public class NodeEvent : UnityEvent<GraphNode> { }

        [HideInInspector]
        public NodeEvent onActiveEvent;
        public NodeEvent onDeactiveEvent;
        
        [HideInInspector]
        public NodeEvent onCompletionEvent;
        public NodeEvent onRevokeEvent;
       

        public void TriggerActiveEvent()
        {
            onActiveEvent?.Invoke(this);
        }

        public void TriggerDeactiveEvent() {
            onDeactiveEvent?.Invoke(this);
        }
        public void TriggerCompletionEvent()
        {
            onCompletionEvent?.Invoke(this);
        }

        public void TriggerRevokeEvent()
        {
            onRevokeEvent?.Invoke(this);
        }

        public void UpdateState(string activationType, bool state)
        {
            switch (activationType)
            {
            case "isActive":
                isActive = state;
                Debug.Log($"Node {nodeName} is now active: {isActive}");
                TriggerActiveEvent();
                break;
            case "isDeactive":
                isActive = state;
                Debug.Log($"Node {nodeName} is now deactive: {isActive}");
                TriggerDeactiveEvent();
                break;
            case "isComplete":
                isCompleted = state;
                Debug.Log($"Node {nodeName} is now completed: {isCompleted}");
                TriggerCompletionEvent();
                break;
            case "isRevoke":
                isCompleted = state;
                Debug.Log($"Node {nodeName} is now revoked: {isActive}");
                TriggerRevokeEvent();
                break;
            default:
                Debug.LogWarning($"Unknown activation type: {activationType}");
                break;
            }
        }

        public virtual void DrawConnectionPoints()
        {
            float buttonWidth = 20;
            float buttonHeight = 20;
            float inButtonX = windowRect.width - buttonWidth; // 10 pixels from the right edge
            float inButtonY = (windowRect.height / 2) - (buttonHeight / 2); // Centered vertically
            float outButtonX = 0; 
            float outButtonY = (windowRect.height / 2) - (buttonHeight / 2);

            // "In" button
            if (GUI.Button(new Rect(inButtonX, inButtonY, buttonWidth, buttonHeight), "•"))
            {
                OnSetStartNode?.Invoke(this);
            }
            
            // "Out" button
            if (GUI.Button(new Rect(outButtonX, outButtonY, buttonWidth, buttonHeight), "•"))
            {
                OnSetEndNode?.Invoke(this);
                
            }
        }

        public virtual void DrawNodeWindow(int id)
        {
            // The "x" button in the top-right corner
            if (GUI.Button(new Rect(windowRect.width - 20, 0, 20, 20), "x"))
            {
                OnSetEndNode?.Invoke(this); // Remove connections
                GraphUtility.RemoveNode(this);
            }

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20); 
            GUILayout.BeginVertical();
            GUILayout.Space(10); 

            switch (nodeType)
            {
                case NodeType.StartNode:
                case NodeType.EndNode:
                case NodeType.InverterNode:
                    GUILayout.Label("Node Type: " + nodeType);
                    GUILayout.Space(10); 
                    if (GUILayout.Button("Edit in Hierarchy", GUILayout.Height(40),  GUILayout.Width(150)))
                    {
                        #if UNITY_EDITOR
                        UnityEditor.Selection.activeGameObject = this.gameObject;
                        #endif
                    }
                    break;

                default:
                    GUILayout.Label("Node Name:");
                    string newNodeName = GUILayout.TextField(nodeName, GUILayout.Width(150));
                    if (newNodeName != nodeName)
                    {
                        nodeName = newNodeName;
                        UpdateNodeNameInHierarchy();
                    }

                    GUILayout.Space(10);

                    // Add the "Edit in Hierarchy" button
                    if (GUILayout.Button("Edit in Hierarchy", GUILayout.Height(40),  GUILayout.Width(150)))
                    {
                        #if UNITY_EDITOR
                        UnityEditor.Selection.activeGameObject = this.gameObject;
                        #endif
                    }
                    break;
            }

            GUILayout.Space(10);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            Color colorOriginal = GUI.color;

            GUI.color = isActive ? Color.green : Color.red;
            GUILayout.Label("Active: " + isActive);

            GUI.color = isCompleted ? Color.green : Color.red;
            GUILayout.Label("Completed: " + isCompleted);

            GUI.color = colorOriginal;
            GUILayout.Space(10);

            GUILayout.EndVertical();            
            DrawConnectionPoints();
            GUI.DragWindow();
        }

        private void UpdateNodeNameInHierarchy()
        {
            this.gameObject.name = nodeName;
        }
    }
    public enum NodeType
    {
        StartNode,
        InverterNode,
        TaskNode,
        EndNode
    }

    #if UNITY_EDITOR

    [CustomEditor(typeof(GraphNode))]
    public class GraphNodeEditor : Editor
    {
        private string customTextField = "Add the task node functionality below using UnityEvents.";
        private bool showDefaultInspector = false;
        private SerializedProperty requireAllCompleted;

        private void OnEnable()
        {
            requireAllCompleted = serializedObject.FindProperty("requireAllCompleted");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(customTextField, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            GraphNode graphNode = (GraphNode)target;

            if (graphNode.nodeName == "EndNode") {
                EditorGUILayout.PropertyField(requireAllCompleted, new GUIContent("Require all nodes to be completed"));
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.LabelField("Node Events", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("When node is Activated");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onActiveEvent"), new GUIContent("onActiveEvent"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("When node is Deactivated");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onDeactiveEvent"), new GUIContent("onDeactiveEvent"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("When node is Completed");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onCompletionEvent"), new GUIContent("onCompletionEvent"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("When node completion is Revoked");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onRevokeEvent"), new GUIContent("onRevokeEvent"));

            serializedObject.ApplyModifiedProperties();
            
            showDefaultInspector = EditorGUILayout.Foldout(showDefaultInspector, "Default Inspector / Advanced settings");
            if (showDefaultInspector)
            {
                DrawDefaultInspector();
            }
        }
    }
    #endif
}
