using System;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
	[Header("Path Meshes")]
	public Mesh ArrowMesh;
	public Mesh NodeMesh;
	public Material ArrowMat;
	public Material OriginNodeMat;
	public Material RefNodeMat;
	public Material NodeMat;

	[Header("Wall Meshes")]
	public Mesh WallMesh;
	public Mesh DoorMesh;
	public Material WallMat;
	public Material DoorMat;

	[Header("Properties")]
	public int Width;
	public int Height;
	public int MaxIterationsPerFrame;


	[Serializable]
	public struct node
	{
		public bool Visited;
		public enum connection { None, East, North, West, South }
		public connection Connection;
	}
	public node[][] Nodes;

	[Header("Generation State")]
	public int OriginX;
	public int OriginY;
	public int PositionX;
	public int PositionY;
	public int NodesLeft;

	[Header("Extra Objects")]
	public GameObject EntranceDoor;
	public GameObject ExitDoor;

	[Header("State Control")]
	public float GenerateSleepTime = 0.016666666666f;
	public float ShiftSleepTime = 0.4f;
	public bool RefreshNodes;
	public bool OriginShift;


	void RefreshArrowMesh()
	{
		Vector3[] vertices = new Vector3[7];
		vertices[0] = new Vector3(0.1f, 0.03f, -0.2f);
		vertices[1] = new Vector3(0.1f, -0.03f, -0.2f);
		vertices[2] = new Vector3(0.7f, 0.03f, -0.2f);
		vertices[3] = new Vector3(0.7f, -0.03f, -0.2f);
		vertices[4] = new Vector3(0.7f, 0.1f, -0.2f);
		vertices[5] = new Vector3(0.7f, -0.1f, -0.2f);
		vertices[6] = new Vector3(0.9f, 0.0f, -0.2f);

		int[] indices = new int[9];
		indices[0] = 0;
		indices[1] = 2;
		indices[2] = 1;
		indices[3] = 2;
		indices[4] = 3;
		indices[5] = 1;
		indices[6] = 4;
		indices[7] = 6;
		indices[8] = 5;

		ArrowMesh = new Mesh();
		ArrowMesh.name = "Arrow";
		ArrowMesh.vertices = vertices;
		ArrowMesh.SetIndices(indices, MeshTopology.Triangles, 0);
	}

	void RefreshNodeMesh()
	{
		float pi2 = Mathf.PI * 2.0f;
		Vector3[] vertices = new Vector3[20];
		for (int i = 0; i < 20; i++)
		{
			float theta = (float)i / 20.0f * pi2;
			theta = -theta;
			vertices[i] = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta)) * 0.1f;
		}

		int[] indices = new int[54];
		for (int i = 0, j = 1; i < 54; i += 3, j++)
		{
			indices[i + 0] = 0;
			indices[i + 1] = j;
			indices[i + 2] = j + 1;
		}

		NodeMesh = new Mesh();
		NodeMesh.name = "Node";
		NodeMesh.vertices = vertices;
		NodeMesh.SetIndices(indices, MeshTopology.Triangles, 0);	
	}

	void RefreshWallMesh()
	{
		Vector3[] vertices = new Vector3[8];
		vertices[0] = new Vector3(-0.5f, -0.5f);
		vertices[1] = new Vector3(0.5f, -0.5f);
		vertices[2] = new Vector3(-0.5f, 0.5f);
		vertices[3] = new Vector3(0.5f, 0.5f);
		vertices[4] = new Vector3(-0.475f, -0.475f);
		vertices[5] = new Vector3(0.475f, -0.475f);
		vertices[6] = new Vector3(-0.475f, 0.475f);
		vertices[7] = new Vector3(0.475f, 0.475f);

		int[] indices = new int[24];
		indices[0] = 0;
		indices[1] = 5;
		indices[2] = 1;
		indices[3] = 0;
		indices[4] = 4;
		indices[5] = 5;
		indices[6] = 1;
		indices[7] = 5;
		indices[8] = 7;
		indices[9] = 1;
		indices[10] = 7;
		indices[11] = 3;
		indices[12] = 3;
		indices[13] = 7;
		indices[14] = 6;
		indices[15] = 3;
		indices[16] = 6;
		indices[17] = 2;
		indices[18] = 2;
		indices[19] = 6;
		indices[20] = 4;
		indices[21] = 2;
		indices[22] = 4;
		indices[23] = 0;

		WallMesh = new Mesh();
		WallMesh.name = "Wall";
		WallMesh.vertices = vertices;
		WallMesh.SetIndices(indices, MeshTopology.Triangles, 0);
	}

	void RefreshDoorMesh()
	{
		Vector3[] vertices = new Vector3[4];
		vertices[0] = new Vector3(0.45f, -0.475f, -0.1f);
		vertices[1] = new Vector3(0.55f, -0.475f, -0.1f);
		vertices[2] = new Vector3(0.45f, 0.475f, -0.1f);
		vertices[3] = new Vector3(0.55f, 0.475f, -0.1f);

		int[] indices = new int[6];
		indices[0] = 0;
		indices[1] = 3;
		indices[2] = 1;
		indices[3] = 0;
		indices[4] = 2;
		indices[5] = 3;

		DoorMesh = new Mesh();
		DoorMesh.name = "Door";
		DoorMesh.vertices = vertices;
		DoorMesh.SetIndices(indices, MeshTopology.Triangles, 0);
	}

	public void DoMeshRefresh()
	{
		RefreshArrowMesh();
		RefreshNodeMesh();
		RefreshWallMesh();
		RefreshDoorMesh();
	}

	public void DoNodeRefresh()
	{
		if (!RefreshNodes) return;
		RefreshNodes = false;

		for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);

		float halfWidth = Width % 2 == 1 ? Mathf.Floor(Width / 2.0f) : Width / 2.0f - 0.5f;
		float halfHeight = Height % 2 == 1 ? Mathf.Floor(Height / 2.0f) : Height / 2.0f - 0.5f;

		float startX = -halfWidth;
		float startY = -halfHeight;

		if (!EntranceDoor)
		{
			EntranceDoor = new GameObject("Entrance", typeof(MeshFilter), typeof(MeshRenderer));
			EntranceDoor.GetComponent<MeshFilter>().mesh = DoorMesh;
			EntranceDoor.GetComponent<MeshRenderer>().material = DoorMat;

		}
		EntranceDoor.transform.localPosition = new Vector3(-halfWidth - 1.0f, halfHeight);
		if (!ExitDoor)
		{
			ExitDoor = new GameObject("Entrance", typeof(MeshFilter), typeof(MeshRenderer));
			ExitDoor.GetComponent<MeshFilter>().mesh = DoorMesh;
			ExitDoor.GetComponent<MeshRenderer>().material = DoorMat;

		}
		ExitDoor.transform.localPosition = new Vector3(halfWidth + 1.0f, -halfHeight);
		ExitDoor.transform.eulerAngles = new Vector3(0.0f, 0.0f, 180.0f);

		Nodes = new node[Height][];
		for (int y = 0; y < Height; y++)
		{
			Nodes[y] = new node[Width];
			for (int x = 0; x < Width; x++)
			{
				float currX = startX + 1.0f * x;
				float currY = startY + 1.0f * y;

				GameObject newChild = new GameObject("Node " + y + "_" + x, typeof(MeshFilter), typeof(MeshRenderer), typeof(Cell));
				newChild.layer = 6;
				newChild.transform.parent = transform;
				newChild.transform.localPosition = new Vector3(currX, currY);
				newChild.GetComponent<MeshFilter>().mesh = WallMesh;
				newChild.GetComponent<MeshRenderer>().material = WallMat;
				newChild.GetComponent<Cell>().Maze = this;

				GameObject doorChild = new GameObject("Door", typeof(MeshFilter), typeof(MeshRenderer));
				doorChild.layer = 6;
				doorChild.transform.SetParent(newChild.transform, false);
				doorChild.GetComponent<MeshFilter>().mesh = DoorMesh;
				doorChild.GetComponent<MeshRenderer>().material = DoorMat;
				doorChild.SetActive(false);

				GameObject arrowChild = new GameObject("Arrow", typeof(MeshFilter), typeof(MeshRenderer));
				arrowChild.layer = 7;
				arrowChild.transform.SetParent(doorChild.transform, false);
				arrowChild.transform.localPosition = new Vector3(0.0f, 0.0f, -0.1f);
				arrowChild.GetComponent<MeshFilter>().mesh = ArrowMesh;
				arrowChild.GetComponent<MeshRenderer>().material = ArrowMat;

				GameObject nodeChild = new GameObject("Node", typeof(MeshFilter), typeof(MeshRenderer));
				nodeChild.layer = 7;
				nodeChild.transform.SetParent(newChild.transform, false);
				nodeChild.GetComponent<MeshFilter>().mesh = NodeMesh;
				nodeChild.GetComponent<MeshRenderer>().material = NodeMat;

				GameObject refChild = new GameObject("Reference", typeof(MeshFilter), typeof(MeshRenderer));
				refChild.layer = 7;
				refChild.transform.SetParent(nodeChild.transform, false);
				refChild.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
				refChild.transform.localPosition = new Vector3(0.0f, 0.0f, 0.1f);
				refChild.GetComponent<MeshFilter>().mesh = NodeMesh;
				refChild.GetComponent<MeshRenderer>().material = RefNodeMat;
				refChild.SetActive(false);

				Nodes[y][x] = new node();
				Nodes[y][x].Visited = false;
				Nodes[y][x].Connection = node.connection.None;
			}
		}

		OriginX = UnityEngine.Random.Range(0, Width);
		OriginY = UnityEngine.Random.Range(0, Height);
		PositionX = OriginX;
		PositionY = OriginY;
		Nodes[OriginY][OriginX].Visited = true;
		Cell originCell = transform.GetChild(OriginY * Width + OriginX).GetComponent<Cell>();
		originCell.SetAsOrigin(true);
		originCell.SetAsReference(true);
		NodesLeft = Width * Height - 1;
	}

	public struct direction_data
	{
		public node.connection Direction;
		public int XInc;
		public int YInc;
	}

	public direction_data SelectDirection(int currX, int currY, bool avoidVisitedCells)
	{
		direction_data[] directions = new direction_data[5];
		directions[0] = new direction_data();

		int maxIndex = 1;

		// See if we can go East
		if (currX < Width - 1)
		{
			if (avoidVisitedCells && !Nodes[currY][currX + 1].Visited || !avoidVisitedCells)
			{
				directions[maxIndex] = new direction_data();
				directions[maxIndex].Direction = node.connection.East;
				directions[maxIndex].XInc = 1;
				maxIndex++;
			}
		}
		// See if we can go North
		if (currY < Height - 1)
		{
			if (avoidVisitedCells && !Nodes[currY + 1][currX].Visited || !avoidVisitedCells)
			{
				directions[maxIndex] = new direction_data();
				directions[maxIndex].Direction = node.connection.North;
				directions[maxIndex].YInc = 1;
				maxIndex++;
			}
		}
		// See if we can go West
		if (currX > 0)
		{
			if (avoidVisitedCells && !Nodes[currY][currX - 1].Visited || !avoidVisitedCells)
			{
				directions[maxIndex] = new direction_data();
				directions[maxIndex].Direction = node.connection.West;
				directions[maxIndex].XInc = -1;
				maxIndex++;
			}
		}
		// See if we can go South
		if (currY > 0)
		{
			if (avoidVisitedCells && !Nodes[currY - 1][currX].Visited || !avoidVisitedCells)
			{
				directions[maxIndex] = new direction_data();
				directions[maxIndex].Direction = node.connection.South;
				directions[maxIndex].YInc = -1;
				maxIndex++;
			}
		}
		// Initialize remaining entries to zero.
		for (int i = maxIndex; i < 5; i++) directions[i] = new direction_data();

		// Backtrack time
		if (maxIndex == 1)
		{
			direction_data result = new direction_data();
			result.Direction = Nodes[currY][currX].Connection;
			switch (result.Direction)
			{
			case node.connection.East:
				result.XInc = 1;
				break;
			case node.connection.North:
				result.YInc = 1;
				break;
			case node.connection.West:
				result.XInc = -1;
				break;
			case node.connection.South:
				result.YInc = -1;
				break;
			}
			return result;
		}
		// Select a direction to go in
		int direction = UnityEngine.Random.Range(1, maxIndex);
		return directions[direction];
	}

	bool Generating = true;
	bool Paused = false;
	float Timer = 0.0f;

	public void Start()
	{
		DoMeshRefresh();
	}


	public void Update()
	{
		// Generate new maze
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Timer = 0.0f;
			Generating = true;
			RefreshNodes = true;

			// Switch generation mode
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				OriginShift = !OriginShift;
			}
		}
		// Pause the game
		if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Pause))
		{
			Paused = !Paused;
		}
		// Wall Layer
		if (Input.GetKeyDown(KeyCode.Alpha6))
		{
			Camera camera = Camera.main;
			bool renderLayer = ((camera.cullingMask >> 6) & 1) == 1;
			if (renderLayer) camera.cullingMask &= ~(1 << 6);
			else camera.cullingMask |= (1 << 6);
		}
		// Node and Path Layer
		if (Input.GetKeyDown(KeyCode.Alpha7))
		{
			Camera camera = Camera.main;
			bool renderLayer = ((camera.cullingMask >> 7) & 1) == 1;
			if (renderLayer) camera.cullingMask &= ~(1 << 7);
			else camera.cullingMask |= (1 << 7);
		}

		if (Paused) return;

		if (RefreshNodes)
		{
			DoNodeRefresh();
			return;
		}

		float maxTime = Generating ? GenerateSleepTime : ShiftSleepTime;

		if (Timer > maxTime)
		{
			Timer -= maxTime;
			for (int i = 0; i < MaxIterationsPerFrame; i++)
			{
				// Get the origin and reference cells
				Cell origin = transform.GetChild(OriginY * Width + OriginX).GetComponent<Cell>();
				Cell reference = transform.GetChild(PositionY * Width + PositionX).GetComponent<Cell>();
				reference.SetAsReference(false);

				// Select the direction to move in
				direction_data direction;
				// We are still generating the maze
				if (Generating)
				{
					if (OriginShift) direction = SelectDirection(OriginX, OriginY, false);
					else direction = SelectDirection(PositionX, PositionY, true);
				}
				// We are modifying a finished maze
				else direction = SelectDirection(OriginX, OriginY, false);

				// We're done generating this maze
				if (direction.Direction == node.connection.None)
				{
					reference.SetAsReference(false);
					Generating = false;
					Timer = 0.0f;
					break;
				}

				PositionX += direction.XInc;
				PositionY += direction.YInc;
				// Generating using Origin Shift or modifying generated maze
				if (OriginShift || !Generating)
				{
					// Update the connections
					Nodes[OriginY][OriginX].Connection = direction.Direction;
					OriginX += direction.XInc;
					OriginY += direction.YInc;
					Nodes[OriginY][OriginX].Connection = node.connection.None;

					// Update the visuals
					origin.SetConnection(direction.Direction);
					origin.SetAsOrigin(false);
					origin = transform.GetChild(OriginY * Width + OriginX).GetComponent<Cell>();
					origin.SetConnection(node.connection.None);
					origin.SetAsOrigin(true);
				}
				// Generating using Depth-First Search
				else if (Generating && !OriginShift && !(PositionX == OriginX && PositionY == OriginY))
				{
					reference = transform.GetChild(PositionY * Width + PositionX).GetComponent<Cell>();
					reference.SetAsReference(true);

					// Only modify connection if there is no existing connection
					if (Nodes[PositionY][PositionX].Connection == node.connection.None)
					{
						// Update connections
						switch (direction.Direction)
						{
						case node.connection.East:
							Nodes[PositionY][PositionX].Connection = node.connection.West;
							break;
						case node.connection.North:
							Nodes[PositionY][PositionX].Connection = node.connection.South;
							break;
						case node.connection.West:
							Nodes[PositionY][PositionX].Connection = node.connection.East;
							break;
						case node.connection.South:
							Nodes[PositionY][PositionX].Connection = node.connection.North;
							break;
						}

						// Update visuals
						reference.SetConnection(direction.Direction, true);
					}
				}

				// Update visited flag and decrement the counter
				if (!Nodes[PositionY][PositionX].Visited)
				{
					Nodes[PositionY][PositionX].Visited = true;
					NodesLeft--;
				}

				// Done generating the maze
				if (Generating && OriginShift && NodesLeft == 0)
				{
					Generating = false;
					Timer = 0.0f;
				}
			}
		}
		Timer += Time.unscaledDeltaTime;
	}
}
