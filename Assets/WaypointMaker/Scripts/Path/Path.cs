using System.Collections.Generic;
using UnityEngine;


namespace WaypointMaker
{
	[AddComponentMenu("Navigation/WaypointMaker Path")]
	public class Path : MonoBehaviour
	{
		public List<PathNode> nodes = new List<PathNode>();
		[SerializeField] private int nextId = 0;

		private void Awake()
		{
			// init the IDX cache
			for (int i = 0; i < nodes.Count; i++)
			{
				nodes[i].Idx = i;
			}

			foreach (PathNode n in nodes)
			{
				n.OutNodeIdx = new List<int>(n.outNodeIds.Count);
				for (int i = 0; i < n.outNodeIds.Count; i++)
				{
					n.OutNodeIdx.Add(GetNode(n.outNodeIds[i]).Idx);
				}
			}
		}

		public void RemoveAllNodes()
		{
			nodes.Clear();
			nextId = 0;
		}

		public void UnlinkAllFrom(int id)
		{
			foreach (PathNode n in nodes)
			{
				for (int i = n.outNodeIds.Count - 1; i >= 0; i--)
				{
					if (n.outNodeIds[i] == id)
					{
						n.outNodeIds.RemoveAt(i);
					}
				}
			}
		}

		public int GetNewNodeId()
		{
			return nextId++;
		}

		public PathNode GetNode(int id)
		{
			foreach (PathNode n in nodes)
			{
				if (n.id == id) return n;
			}
			return null;
		}

		// ----------------------------------------------------------------------------------------------------------------
	}
}