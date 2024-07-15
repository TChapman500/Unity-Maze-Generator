using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
	public MazeGenerator Maze;
	public GameObject Node;
	public GameObject Arrow;
	public GameObject Door;
	public GameObject Highlight;

	bool IsOrigin;
	bool IsReference;

	void Start()
	{
		// Arrow and Door
		Door = transform.GetChild(0).gameObject;
		Arrow = Door.transform.GetChild(0).gameObject;

		// Node and Highlight
		Node = transform.GetChild(1).gameObject;
		Highlight = Node.transform.GetChild(0).gameObject;

		SetAsOrigin(IsOrigin);
		SetAsReference(IsReference);
	}

	public void SetAsReference(bool set)
	{
		IsReference = set;
		if (!Highlight || !Node) return;

		Highlight.SetActive(set);
		if (set) Node.transform.localScale = Vector3.one * 2.0f;
		else if (!IsOrigin) Node.transform.localScale = Vector3.one;
	}

	public void SetAsOrigin(bool set)
	{
		IsOrigin = set;
		if (!Node) return;

		if (set)
		{
			Node.transform.localScale = Vector3.one * 2.0f;
			Node.GetComponent<MeshRenderer>().material = Maze.OriginNodeMat;
		}
		else
		{
			if (!IsReference) Node.transform.localScale = Vector3.one;
			Node.GetComponent<MeshRenderer>().material = Maze.NodeMat;
		}
	}

	public void SetConnection(MazeGenerator.node.connection connection, bool opposite = false)
	{
		if (!Door) return;

		if (connection == MazeGenerator.node.connection.None)
		{
			Door.SetActive(false);
			return;
		}
		Door.SetActive(true);

		switch (connection)
		{
		case MazeGenerator.node.connection.East:
			Door.transform.localEulerAngles = opposite ? new Vector3(0.0f, 0.0f, 180.0f) : Vector3.zero;
			break;
		case MazeGenerator.node.connection.North:
			Door.transform.localEulerAngles = opposite ? new Vector3(0.0f, 0.0f, 270.0f) : new Vector3(0.0f, 0.0f, 90.0f);
			break;
		case MazeGenerator.node.connection.West:
			Door.transform.localEulerAngles = opposite ? Vector3.zero : new Vector3(0.0f, 0.0f, 180.0f);
			break;
		case MazeGenerator.node.connection.South:
			Door.transform.localEulerAngles = opposite ? new Vector3(0.0f, 0.0f, 90.0f) : new Vector3(0.0f, 0.0f, 270.0f);
			break;
		}
	}
}
