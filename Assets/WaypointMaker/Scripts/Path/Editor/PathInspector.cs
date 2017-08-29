using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WaypointMaker;


namespace WaypointMakerEd
{
	[CustomEditor(typeof(Path))]
	public class PathInspector : Editor
	{
		private static int s_PathInspectorHash = "PathInspectorHash".GetHashCode();

		[System.NonSerialized] private Plane plane = new Plane(Vector3.up, Vector3.zero);
		[System.NonSerialized] private Path Target = null;
		[System.NonSerialized] private PathNode activeNode = null;
		[System.NonSerialized] private bool dirty = false;
		[System.NonSerialized] private float d;
		[System.NonSerialized] private Ray ray;
		[System.NonSerialized] private SceneView lastSceneView = null;
		[System.NonSerialized] private Vector2 selectionStart;
		[System.NonSerialized] private Vector2 selectionMousePos;
		[System.NonSerialized] private List<PathNode> selectedNodes = new List<PathNode>();

		private bool dragSelecting = false;
		private bool autoSelectPlaced = true;
		private static bool moveAll = false;
		private static int xNodes = 10;
		private static int yNodes = 10;
		private static float nodeSpacing = 2f;
		private static float innerSpacing = 0.5f;
		private static bool streetGrid = false;

		private static GUIStyle _SelectionRect = null;
		private static GUIStyle SelectionRect { get { return (_SelectionRect ?? (_SelectionRect = GUI.skin.FindStyle("selectionRect"))); } }

		// ----------------------------------------------------------------------------------------------------------------

		public override void OnInspectorGUI()
		{
			Target = target as Path;

			GUILayout.Label("[Ctrl+Shift Click] add node");
			GUILayout.Label("[Ctrl Click] connect selected to click target");
			GUILayout.Label("[Del] delete selected");
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Nodes: ", Target.nodes.Count.ToString());
			EditorGUILayout.LabelField("Selected Node Id: ", (activeNode == null ? "-" : activeNode.id.ToString()));
			if (activeNode == null) EditorGUILayout.LabelField("Node Position: ", "-");
			else activeNode.position = EditorGUILayout.Vector3Field("Node Position: ", activeNode.position);
			EditorGUILayout.Space();

			GUILayout.Label("Tools", EditorStyles.boldLabel);
			autoSelectPlaced = EditorGUILayout.Toggle("Auto-select Placed", autoSelectPlaced);
			moveAll = EditorGUILayout.Toggle("Move all", moveAll);
			EditorGUILayout.Space();
			if (GUILayout.Button("Remove All Nodes"))
			{
				Undo.RecordObject(Target, "Remove all nodes");
				dirty = true;
				Target.RemoveAllNodes();
				if (lastSceneView != null) lastSceneView.Repaint();
			}
			EditorGUILayout.Space();


			AutoCreateNodes();
			EditorGUILayout.Space();

			if (GUI.changed || dirty)
			{
				dirty = false;
				GUI.changed = false;
				EditorSceneManager.MarkSceneDirty(Target.gameObject.scene);
			}
		}

		private void OnSceneGUI()
		{
			Target = target as Path;
			Event ev = Event.current;

			if (ev.type == EventType.Repaint) lastSceneView = SceneView.currentDrawingSceneView;

			DrawNodes(ev);
			HandleEvents(ev);

			if (dirty)
			{
				dirty = false;
				EditorSceneManager.MarkSceneDirty(Target.gameObject.scene);
			}
		}

		private void HandleEvents(Event ev)
		{
			int controlID = GUIUtility.GetControlID(s_PathInspectorHash, FocusType.Passive);
			EventType type = ev.GetTypeForControl(controlID);
			switch (type)
			{
				case EventType.MouseDown:
					{
						if (HandleUtility.nearestControl == controlID && ev.button == 0)
						{
							// add a new node to scene and add to the "active" node's list of out-nodes
							if (ev.modifiers == (EventModifiers.Shift | EventModifiers.Control))
							{
								ev.Use();
								ray = HandleUtility.GUIPointToWorldRay(ev.mousePosition);
								if (plane.Raycast(ray, out d))
								{
									Undo.RecordObject(Target, "Add node");
									PathNode node = new PathNode
									{
										id = Target.GetNewNodeId(),
										position = ray.GetPoint(d)
									};

									node.position.y = Target.transform.position.y;
									Target.nodes.Add(node);
									dirty = true;

									if (activeNode != null) activeNode.outNodeIds.Add(node.id);
									if (activeNode == null || autoSelectPlaced) activeNode = node;
								}
							}

							// start selection of objects
							else if (ev.modifiers == EventModifiers.Shift)
							{
								GUIUtility.hotControl = controlID;
								activeNode = null;
								dragSelecting = true;
								selectionMousePos = selectionStart = ev.mousePosition;
								ev.Use();

								if (lastSceneView != null) lastSceneView.Repaint();
							}
						}

					}
					break;

				case EventType.MouseDrag:
					{
						if (GUIUtility.hotControl == controlID && lastSceneView != null)
						{
							selectionMousePos = new Vector2(Mathf.Max(ev.mousePosition.x, 0f), Mathf.Max(ev.mousePosition.y, 0f));
							lastSceneView.Repaint();
						}
					}
					break;

				case EventType.MouseUp:
					{
						if (GUIUtility.hotControl == controlID && ev.button == 0 && dragSelecting)
						{
							// end selection drag
							GUIUtility.hotControl = 0;
							dragSelecting = false;
							selectionMousePos = new Vector2(Mathf.Max(ev.mousePosition.x, 0f), Mathf.Max(ev.mousePosition.y, 0f));
							activeNode = null;
							ev.Use();

							Rect r = new Rect(selectionStart.x, selectionStart.y, selectionMousePos.x - selectionStart.x, selectionMousePos.y - selectionStart.y);
							if (r.width < 0f) { r.x += r.width; r.width = -r.width; }
							if (r.height < 0f) { r.y += r.height; r.height = -r.height; }

							foreach (PathNode n in Target.nodes)
							{
								if (r.Contains(HandleUtility.WorldToGUIPoint(n.position))) selectedNodes.Add(n);
							}

							if (lastSceneView != null) lastSceneView.Repaint();
						}
					}
					break;

				case EventType.KeyDown:
					{
						if (activeNode != null)
						{
							if (ev.keyCode == KeyCode.Escape)
							{
								ev.Use();
								selectedNodes.Clear();
								activeNode = null;
								Repaint();
								return;
							}
							if (ev.keyCode == KeyCode.Delete)
							{
								ev.Use();
								Undo.RecordObject(Target, "Delete node");
								int id = activeNode.id;
								Target.UnlinkAllFrom(id);
								Target.nodes.Remove(activeNode);
								activeNode = null;
								dirty = true;
								Repaint();
								return;
							}
						}

						else if (selectedNodes.Count > 0)
						{
							if (ev.keyCode == KeyCode.Escape)
							{
								ev.Use();
								selectedNodes.Clear();
								activeNode = null;
								Repaint();
								return;
							}
							if (ev.keyCode == KeyCode.Delete)
							{
								ev.Use();
								Undo.RecordObject(Target, "Delete node");
								foreach (PathNode n in selectedNodes)
								{
									int id = n.id;
									Target.UnlinkAllFrom(id);
									Target.nodes.Remove(n);
								}
								selectedNodes.Clear();
								activeNode = null;
								dirty = true;
								Repaint();
								return;
							}
						}
					}
					break;

				case EventType.ValidateCommand:
					{
						if (ev.commandName == "FrameSelected")
						{
							if (activeNode != null || selectedNodes.Count > 0)
							{
								ev.Use();
							}
						}
					}
					break;

				case EventType.ExecuteCommand:
					{
						if (ev.commandName == "FrameSelected")
						{
							if (activeNode != null || selectedNodes.Count > 0)
							{
								ev.Use();
								SceneView.currentDrawingSceneView.LookAt(activeNode != null ? activeNode.position : selectedNodes[0].position);
							}
						}
					}
					break;

				case EventType.Repaint:
					{
						if (dragSelecting && GUIUtility.hotControl == controlID)
						{
							Handles.BeginGUI();
							SelectionRect.Draw(FromToRect(selectionStart, selectionMousePos), GUIContent.none, false, false, false, false);
							Handles.EndGUI();
						}
					}
					break;

				case EventType.Layout:
					{
						HandleUtility.AddDefaultControl(controlID);
					}
					break;
			}			
		}

		private void DrawNodes(Event ev)
		{
			float sz = 0.1f;

			for (int i = 0; i < Target.nodes.Count; i++)
			{
				PathNode n = Target.nodes[i];
				Handles.color = (n == activeNode || selectedNodes.Contains(n) ? Color.yellow : Color.white);

				if (ev.type == EventType.Repaint)
				{
					foreach (int id in n.outNodeIds)
					{
						PathNode n2 = Target.GetNode(id);
						Vector3 dir = (n2.position - n.position).normalized;
						Handles.DrawDottedLine(n.position, n2.position, 2f);
						Handles.ConeHandleCap(0, n.position + Vector3.ClampMagnitude(dir, 0.3f), Quaternion.LookRotation(dir), sz * 0.3f, EventType.Repaint);
					}
				}

				if (Handles.Button(n.position, Quaternion.identity, sz, sz * 1.2f, Handles.SphereHandleCap))
				{
					if (activeNode != null && ev.modifiers == EventModifiers.Control)
					{
						if (!activeNode.outNodeIds.Contains(n.id))
						{
							Undo.RecordObject(Target, "Create node Link");
							activeNode.outNodeIds.Add(n.id);
						}
					}
					else
					{
						activeNode = n;
						selectedNodes.Clear();
					}

					Repaint();
				}

				if (!(ev.modifiers == EventModifiers.Control || ev.modifiers == (EventModifiers.Shift | EventModifiers.Control)))
				{
					if (selectedNodes.Count > 0 && n == selectedNodes[0])
					{
						EditorGUI.BeginChangeCheck();
						Vector3 newPos = Handles.PositionHandle(n.position, Quaternion.identity);
						if (EditorGUI.EndChangeCheck())
						{
							Vector3 delta = newPos - n.position;
							Undo.RecordObject(Target, "Move node(s)");
							foreach (PathNode sn in selectedNodes) sn.position += delta;
							dirty = true;
						}
					}
					else if (activeNode != null && n == activeNode)
					{
						EditorGUI.BeginChangeCheck();
						Vector3 newPos = Handles.PositionHandle(n.position, Quaternion.identity);
						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(Target, "Move node(s)");
							n.position = newPos;
							dirty = true;
						}
					}
				}

			}

			if (activeNode != null && ev.type == EventType.Repaint && (ev.modifiers == EventModifiers.Control || ev.modifiers == (EventModifiers.Shift | EventModifiers.Control)))
			{
				ray = HandleUtility.GUIPointToWorldRay(ev.mousePosition);
				if (plane.Raycast(ray, out d))
				{
					Handles.color = Color.yellow;
					Vector3 p = ray.GetPoint(d);
					Vector3 dir = (p - activeNode.position).normalized;
					Handles.DrawDottedLine(activeNode.position, p, 2f);
					Handles.ArrowHandleCap(0, activeNode.position, Quaternion.LookRotation(dir), sz * 3f, EventType.Repaint);
					SceneView.currentDrawingSceneView.Repaint();
				}
			}
		}

		private void AutoCreateNodes()
		{
			EditorGUILayout.Space();
			GUILayout.Label("Grid Maker", EditorStyles.boldLabel);
			xNodes = EditorGUILayout.IntField("xNodes", xNodes);
			yNodes = EditorGUILayout.IntField("yNodes", yNodes);
			nodeSpacing = EditorGUILayout.FloatField("Spacing", nodeSpacing);
			streetGrid = EditorGUILayout.Toggle("Street Grid", streetGrid);
			if (streetGrid) innerSpacing = EditorGUILayout.FloatField("Inner Spacing", innerSpacing);

			if (GUILayout.Button("Auto Create Nodes"))
			{
				Undo.RecordObject(Target, "Create Grid");
				dirty = true;
				Target.RemoveAllNodes();
				float yy = Target.transform.position.y;

				for (int x = 0; x < xNodes; x++)
				{
					for (int y = 0; y < yNodes; y++)
					{
						if (streetGrid)
						{
							PathNode[] n = { new PathNode(), new PathNode(), new PathNode(), new PathNode() };

							n[0].id = Target.GetNewNodeId();
							n[1].id = Target.GetNewNodeId();
							n[2].id = Target.GetNewNodeId();
							n[3].id = Target.GetNewNodeId();

							Target.nodes.Add(n[0]);
							Target.nodes.Add(n[1]);
							Target.nodes.Add(n[2]);
							Target.nodes.Add(n[3]);

							int[] nIdx = { Target.nodes.Count - 4, Target.nodes.Count - 3, Target.nodes.Count - 2, Target.nodes.Count - 1 };

							n[0].position = new Vector3(x * nodeSpacing, yy, y * nodeSpacing);
							n[1].position = n[0].position + new Vector3(0f, 0f, innerSpacing);
							n[2].position = n[0].position + new Vector3(innerSpacing, 0f, innerSpacing);
							n[3].position = n[0].position + new Vector3(innerSpacing, 0f, 0f);

							// link the two nodes with each other (bi-directional)
							n[0].outNodeIds.Add(n[1].id);
							n[1].outNodeIds.Add(n[2].id);
							n[2].outNodeIds.Add(n[3].id);
							n[3].outNodeIds.Add(n[0].id);

							// link with the previous nodes in column
							if (y > 0)
							{
								Target.nodes[nIdx[0] - 3].outNodeIds.Add(nIdx[0]);
								Target.nodes[nIdx[3]].outNodeIds.Add(nIdx[0] - 2);
							}

							// link with the previous nodes in row
							if (x > 0)
							{
								Target.nodes[nIdx[2] - (yNodes * 4)].outNodeIds.Add(nIdx[1]);
								Target.nodes[nIdx[0]].outNodeIds.Add(nIdx[3] - (yNodes * 4));
							}
						}

						else
						{
							PathNode n = new PathNode() { id = Target.GetNewNodeId() };

							Target.nodes.Add(n);
							n.position = new Vector3(x * nodeSpacing, yy, y * nodeSpacing);

							int nIdx = Target.nodes.Count - 1;
							if (y > 0)
							{
								Target.nodes[nIdx - 1].outNodeIds.Add(nIdx);
								Target.nodes[nIdx].outNodeIds.Add(nIdx - 1);
							}
							if (x > 0)
							{
								Target.nodes[nIdx].outNodeIds.Add(nIdx - yNodes);
								Target.nodes[nIdx - yNodes].outNodeIds.Add(nIdx);
							}
						}
					}
				}

				if (lastSceneView != null) lastSceneView.Repaint();
			}
		}

		private Rect FromToRect(Vector2 start, Vector2 end)
		{
			Rect result = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
			if (result.width < 0f)
			{
				result.x += result.width;
				result.width = -result.width;
			}
			if (result.height < 0f)
			{
				result.y += result.height;
				result.height = -result.height;
			}
			return result;
		}

		// ----------------------------------------------------------------------------------------------------------------
	}
}