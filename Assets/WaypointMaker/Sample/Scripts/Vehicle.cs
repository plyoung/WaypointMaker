using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaypointMaker;
using Priority_Queue;


public class Vehicle : MonoBehaviour
{
	private const float MoveSpeed = 1f;
	private Vector3[] path = null;
	private Path roadGrid;
	private Transform tr;
	private int pathIdx = -1;
	private Vector3 startPos;
	private Vector3 destPos;
	private Vector3 startDir;
	private Vector3 destDir;
	private float startMoveTime;
	private float startTurnTime;
	private float totalMoveTime;
	private float totalTurnTime;
	private bool canTurn;
	private bool turning;
	private PathNode lastNode;
	private bool buildingPath = false;

	private void Start()
	{
		tr = GetComponent<Transform>();

		// get reference to the waypoint system. there is only one in this scene.
		roadGrid = FindObjectOfType<Path>();

		// choose a random node to start at
		lastNode = roadGrid.nodes[Random.Range(0, roadGrid.nodes.Count)];
		tr.position = lastNode.position;

		// and go..
		StartCoroutine(CreatePath());
	}

	private void Update()
	{
		if (pathIdx < 0)
		{
			if (!buildingPath) StartCoroutine(CreatePath());
			return;
		}

		float t = Time.time - startMoveTime;
		float distanceCompleted = t / totalMoveTime;
		if (startPos != destPos)
		{
			tr.position = Vector3.Lerp(startPos, destPos, distanceCompleted);
		}

		if (turning)
		{
			t = Time.time - startTurnTime;
			float turningCompleted = t / totalTurnTime;
			tr.forward = Vector3.Lerp(startDir, destDir, turningCompleted);
			if (turningCompleted >= 1.0f) turning = false;
		}

		// check if should start turning
		if (canTurn)
		{
			// should check against actual distance from target rather than "time remaining" since longer paths would throw this off
			float d = (path[pathIdx] - tr.position).sqrMagnitude;
			if (d <= 0.1f)
			{
				turning = true;
				canTurn = false;
				if (pathIdx + 1 < path.Length)
				{
					startTurnTime = Time.time;
					startDir = tr.forward;
					destDir = (path[pathIdx + 1] - path[pathIdx]).normalized;
					totalTurnTime = (destDir - startDir).magnitude / 3f;
				}
			}
		}

		// check if reached destination
		if (distanceCompleted >= 1.0f)
		{
			NextPoint();
		}
	}

	private IEnumerator CreatePath()
	{
		buildingPath = true;

		// choose a random node to reach
		PathNode endNode = roadGrid.nodes[Random.Range(0, roadGrid.nodes.Count)];

		// find a path form start to end
		int count = 0;
		path = new Vector3[0];
		float totalDistance = 0f;
		int start = lastNode.Idx;
		int end = endNode.Idx;
		lastNode = endNode;

		//Debug.LogFormat("{0} => {1}", start, end);

		List<int> neighbors = roadGrid.nodes[start].OutNodeIdx;
		if (!neighbors.Contains(end))
		{
			int current = -1;
			int next = -1;
			FastPriorityQueue<PQNode> frontier = new FastPriorityQueue<PQNode>(roadGrid.nodes.Count);
			Dictionary<int, int> came_from = new Dictionary<int, int>();
			Dictionary<int, float> cost_so_far = new Dictionary<int, float>();
			float new_cost = 0f;
			float priority = 0f;

			frontier.Enqueue(new PQNode() { idx = start }, 0);
			came_from.Add(start, -1);
			cost_so_far.Add(start, 0);

			while (frontier.Count > 0)
			{
				current = frontier.Dequeue().idx;
				if (current == end) break;

				neighbors = roadGrid.nodes[current].OutNodeIdx;
				for (int i = 0; i < neighbors.Count; i++)
				{
					next = neighbors[i];
					new_cost = cost_so_far[current] + 1;
					if (false == cost_so_far.ContainsKey(next)) cost_so_far.Add(next, new_cost + 1);
					if (new_cost < cost_so_far[next])
					{
						cost_so_far[next] = new_cost;
						priority = new_cost + Heuristic(roadGrid.nodes[next], roadGrid.nodes[end]);
						frontier.Enqueue(new PQNode() { idx = next }, priority);
						if (false == came_from.ContainsKey(next)) came_from.Add(next, current);
						else came_from[next] = current;
					}
				}

				count++; if (count >= 10) { count = 0; yield return null; }
			}

			// build path
			List<PathNode> pathNodes = new List<PathNode>();
			next = end;
			while (came_from.ContainsKey(next))
			{
				if (came_from[next] == -1) break;
				if (came_from[next] == start) break;
				pathNodes.Add(roadGrid.nodes[came_from[next]]);
				next = came_from[next];

				count++; if (count >= 10) { count = 0; yield return null; }
			}

			if (pathNodes.Count > 0)
			{
				path = new Vector3[pathNodes.Count + 2];
				int idx = 1;
				path[0] = tr.position;

				Vector3 prev = roadGrid.nodes[end].position;
				for (int i = pathNodes.Count - 1; i >= 0; i--)
				{
					path[idx] = pathNodes[i].position;
					idx++;

					totalDistance += (pathNodes[i].position - prev).magnitude;
					prev = pathNodes[i].position;
				}

				path[idx] = roadGrid.nodes[end].position;
			}
			else
			{
				path = null;
			}
		}

		// let vehicle follow path
		if (path != null && path.Length > 1)
		{
			destDir = (path[1] - path[0]); destDir.y = 0;
			destDir.Normalize();
			tr.forward = destDir;
			NextPoint();
		}

		buildingPath = false;
	}

	private float Heuristic(PathNode node, PathNode goal)
	{
		float dx = Mathf.Abs(node.position.x - goal.position.x);
		float dy = Mathf.Abs(node.position.y - goal.position.y);
		return (dx + dy);
	}

	private void NextPoint()
	{
		pathIdx++;
		if (pathIdx >= path.Length)
		{
			pathIdx = -1;
			return;
		}

		canTurn = true;
		startPos = tr.position;
		destPos = path[pathIdx];
		startMoveTime = Time.time;
		totalMoveTime = (destPos - startPos).magnitude / MoveSpeed;
	}

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		if (path == null || path.Length == 0) return;
		Gizmos.color = Color.magenta;
		for (int i = 0; i < path.Length - 1; i++)
		{
			Gizmos.DrawLine(path[i], path[i + 1]);
		}
	}
#endif

	//	// ----------------------------------------------------------------------------------------------------------------
}
