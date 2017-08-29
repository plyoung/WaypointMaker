using System.Collections.Generic;
using UnityEngine;


namespace WaypointMaker
{
	[System.Serializable]
	public class PathNode
	{
		public int id = 0; // this is not an index into the Path list of nodes. it is unique among all nodes in the list.
		public Vector3 position = Vector3.zero;
		public List<int> outNodeIds = new List<int>();

		// runtime helper so that can skip slower Id lookups
		public int Idx { get; set; }
		public List<int> OutNodeIdx { get; set; }
	}
}