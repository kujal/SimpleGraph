/*
// THIS FILE IS NOT USED FOR BUILD, IT IS ONLY FOR EDITOR.
// The file builds the Graph Editor Window in Unity Editor, e.g. the visual representation of the graph.
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;


namespace SimpleGraph
{
    [System.Serializable]
    public class GraphEditorWindow : EditorWindow
    {
        [SerializeField]
        private Vector2 scrollPos;
        [SerializeField]
        private GameObject nodesParent;
        [SerializeField]
        private bool isConnecting = false;
        [SerializeField]
        private GraphNode startNode = null;
        [SerializeField]
        private float zoomFactor = 1.0f;
        [SerializeField]
        private Rect _zoomArea;
        [SerializeField]
        private const float zoomAreaScale = 10.0f;
        [SerializeField]
        private Vector2 panOffset = Vector2.zero;
        private static GameObject tempNodesParent;

        public static void ShowEditor(GameObject nodesParent)
        {
            tempNodesParent = nodesParent;
            GraphEditorWindow editor = EditorWindow.GetWindow<GraphEditorWindow>();
            editor.titleContent = new GUIContent(nodesParent.name);
        }

        private void OnEnable()
        {
            if (tempNodesParent != null)
            {
                nodesParent = tempNodesParent;
                tempNodesParent = null;
            }

            LoadGraph(nodesParent);
            GraphNode.OnSetStartNode += SetStartNode;
            GraphNode.OnSetEndNode += SetEndNode;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                if (nodesParent != null)
                {
                    LoadGraph(nodesParent);
                    Repaint();
                }
            }
        }

        private void SetStartNode(GraphNode node)
        {
            startNode = node;
            if (isConnecting) {
                isConnecting = false;
            } else {
                isConnecting = true;
            }   
        }

        private void SetEndNode(GraphNode node)
        {
            if (isConnecting && startNode != null)
            {
                if (!startNode.nextNodes.Contains(node))
                {
                    startNode.nextNodes.Add(node);
                }
                
                if (!node.previousNodes.Contains(startNode))
                {
                    node.previousNodes.Add(startNode);
                }
                
                isConnecting = false;
                startNode = null;
            }
            else {
                if (node.previousNodes.Count > 0)
                {
                    GraphNode startNode = node.previousNodes[0];
                    startNode.nextNodes.Remove(node);
                    node.previousNodes.Remove(startNode);
                }
                Repaint();
            }
        }

        private void OnGUI()
        {
            HandleZoomAndPan();

            // Somehow tricks the clipping window to extend beyond the initial view
            GUI.EndGroup();

            Rect zoomedArea = new Rect(0, 0, position.width * zoomFactor, position.height * zoomFactor);
            GUILayout.BeginArea(zoomedArea);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Draw the grid 
            /*
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);
            */


            // Save the current GUI matrix
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * zoomFactor, new Vector2(position.width / 2f, position.height / 2f));
            GUI.matrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one) * GUI.matrix;

            BeginWindows();



            // Draws the nodes
            for (int i = 0; i < GraphUtility.nodes.Count; i++)
            {
                GraphUtility.nodes[i].windowRect = GUI.Window(
                    i,
                    GraphUtility.nodes[i].windowRect,
                    GraphUtility.nodes[i].DrawNodeWindow,
                    GraphUtility.nodes[i].nodeName
                );
            }


            // Draws Bezier noodles
            foreach (GraphNode node in GraphUtility.nodes)
            {
                foreach (GraphNode nextNode in node.nextNodes)
                {
                    // Start from the right center
                    Vector3 startPos = new Vector3(node.windowRect.x + node.windowRect.width, node.windowRect.y + (node.windowRect.height / 2), 0); 
                    // End at the left center
                    Vector3 endPos = new Vector3(nextNode.windowRect.x, nextNode.windowRect.y + nextNode.windowRect.height / 2, 0); 
                    float horizontalNodesDist = endPos.x - startPos.x;
                    Vector3 startTangent = startPos + Vector3.right * (Mathf.Abs(horizontalNodesDist) / 2);
                    Vector3 endTangent = endPos + Vector3.left * (Mathf.Abs(horizontalNodesDist) / 2);

                    Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.white, null, 3f);
                }
            }

            EndWindows();
            GUI.matrix = oldMatrix;
            ProcessContextMenu(Event.current);
            GUI.BeginGroup(zoomedArea);
        }

        // NOT CURRENTLY IN USE --> FOR THE NEXT VARUSVEIJARI
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            float scaledGridSpacing = gridSpacing * zoomFactor;

            // Calculate the offset based on the pan offset and zoom factor
            Vector2 offset = new Vector2(panOffset.x % scaledGridSpacing, panOffset.y % scaledGridSpacing);

            // Calculate the center of the view
            Vector2 center = new Vector2(position.width / 2f, position.height / 2f);

            // Adjust the pan offset to keep the grid centered
            Vector2 adjustedPanOffset = panOffset * zoomFactor - center * (zoomFactor - 1);

            int widthDivs = Mathf.CeilToInt(position.width / scaledGridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / scaledGridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            for (int i = 0; i <= widthDivs; i++)
            {
                float x = i * scaledGridSpacing - offset.x + adjustedPanOffset.x;
                Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, position.height, 0));
            }

            for (int j = 0; j <= heightDivs; j++)
            {
                float y = j * scaledGridSpacing - offset.y + adjustedPanOffset.y;
                Handles.DrawLine(new Vector3(0, y, 0), new Vector3(position.width, y, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void HandleZoomAndPan()
        {
            Event e = Event.current;

            // Handle zooming with the scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                Vector2 mousePosition = e.mousePosition;
                float zoomDelta = -e.delta.y * 0.005f; // Adjust the zoom delta for smoother zooming
                float prevZoom = zoomFactor;
                zoomFactor = Mathf.Clamp(zoomFactor + zoomDelta, 0.5f, 2f);

                // Adjust panOffset to zoom around the mouse position
                //Vector2 offset = mousePosition - new Vector2(position.width / 2f, position.height / 2f);
               //panOffset -= offset * (zoomFactor - prevZoom) / zoomFactor;

                e.Use();
            }

            // Handle panning with middle mouse button drag
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                panOffset += e.delta;
                e.Use();
            }
        }
        public void LoadGraph(GameObject nodesParent)
        {
            GraphUtility.nodes.Clear();
            if (nodesParent == null)
            {
                return;
            }

            foreach (Transform child in nodesParent.transform)
            {
                GraphNode graphNode = child.GetComponent<GraphNode>();            

                if (graphNode != null)
                {
                    graphNode.windowRect = new Rect(graphNode.windowRect.x, graphNode.windowRect.y, 200, 170);
                    GraphUtility.nodes.Add(graphNode);
                }
            }
            
            Repaint();
        }

        private GraphNode CreateNode(NodeType nodeType, Vector2 position)
        {
            // Create a new GameObject
            GameObject nodeObject = new GameObject(nodeType.ToString());
            nodeObject.transform.SetParent(nodesParent.transform);
            nodeObject.transform.position = position;

            // Add the GraphNode component
            GraphNode newNode = nodeObject.AddComponent<GraphNode>();
            newNode.windowRect = new Rect(position.x, position.y, 200, 170);
            newNode.nodeName = nodeType.ToString();

            // Set the node type
            newNode.nodeType = nodeType;

            // Add the new node to the GraphUtility nodes list
            GraphUtility.nodes.Add(newNode);

            return newNode;
        }

        private void DestroyFailedConnections()
        {
            foreach (GraphNode node in GraphUtility.nodes)
            {
            // Check previous nodes
            for (int i = node.previousNodes.Count - 1; i >= 0; i--)
            {
                if (node.previousNodes[i] == null || !node.previousNodes[i].nextNodes.Contains(node))
                {
                node.previousNodes.RemoveAt(i);
                }
            }

            // Check next nodes
            for (int i = node.nextNodes.Count - 1; i >= 0; i--)
            {
                if (node.nextNodes[i] == null || !node.nextNodes[i].previousNodes.Contains(node))
                {
                node.nextNodes.RemoveAt(i);
                }
            }
            }
        }


        /*
        // This function creates the "context menu", which is the menu that appears when you right-click on the graph editor window.
        */
        private void ProcessContextMenu(Event e)
        {
            if (e.button == 1 && e.type == EventType.MouseDown)
            {
                GraphNode clickedNode = null;
                foreach (GraphNode node in GraphUtility.nodes)
                {
                    if (node.windowRect.Contains(e.mousePosition))
                    {
                        clickedNode = node;
                        break;
                    }
                }

                GenericMenu menu = new GenericMenu();

                if (clickedNode != null)
                {
                    menu.AddItem(new GUIContent("Remove Node"), false, () => GraphUtility.RemoveNode(clickedNode));
                }
                else
                {
                    menu.AddItem(new GUIContent("Add Start Node"), false, () => CreateNode(NodeType.StartNode, e.mousePosition));
                    menu.AddItem(new GUIContent("Add Inverter Node"), false, () => CreateNode(NodeType.InverterNode, e.mousePosition));
                    menu.AddItem(new GUIContent("Add Task Node"), false, () => CreateNode(NodeType.TaskNode, e.mousePosition));
                    menu.AddItem(new GUIContent("Add End Node"), false, () => CreateNode(NodeType.EndNode, e.mousePosition));
                    menu.AddItem(new GUIContent("Destroy failed connnections"), false, () => DestroyFailedConnections());
                }

                menu.ShowAsContext();
                e.Use();
            }
        }

    }
}