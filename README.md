# WaypointMaker

This is a simple tool for laying out and using waypoints in Unity. It also features an option to quickly lay out a simple grid of connected nodes or a street type grid layout of nodes.

### Use

Add `Component > Navigation > WaypointMaker Path` component to a GameObject.

Now you can hold `Ctrl+Help` and click in the scene to add nodes to the scene. You can select an existing node in the scene and then click on another while holding `Ctrl` to make a link from the one node to the other.

Hold `Shift` and drag over several nods to select them all. Use the movement gizmo to move selected nodes. Use `Del` to delete selected nodes.


### Example

The sample scene shows the use of a grid of nodes and includes path finding in the `vehicle` component (script) to show how you might possibl use and navigate the grid of nodes.


### Code

The `Path` component simply holds a list of all the nodes. To find a node (PathNode) you may use `Path.GetNode(node_id)` where `node_id` would be the same as the `id` presented in the Inspector when you have a node selected.

The `PathNode` has the node's `id` and `position` information. It also has a list of node IDs in `outNodeIds`. This list tells you what links this node has towards other nodes. Note that this is a list of IDs, not indices into the list of nodes in the `Path` object. So to get the actual node you would still use `Path.GetNode()`.

There is however also a list of indices generated at runtime during Path's `Awake()`. This is stored in `PathNode.OutNodeIdx` for each node and could be used as a faster way of getting node info than using the ID lookup.

The node ID will never change, even if you add or remove more nodes to the path. The IDX will differ depending on how many nodes are in the scene and in what order they appear in the `Path.nodes` list.

![screenshot](https://user-images.githubusercontent.com/837362/29813875-b7c22ddc-8cab-11e7-8a95-1737ffe0f691.png)



