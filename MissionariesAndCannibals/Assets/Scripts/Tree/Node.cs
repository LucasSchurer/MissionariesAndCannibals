using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class that handles the information about a certain problem state
/// and the visual representation of a given node.
/// </summary>
public class Node : MonoBehaviour
{
	/// <summary>
	/// Describe a problem state and offers auxiliary functions to better work and compare states.
	/// <remarks>
	/// A problem state can be represented in the following way:
	/// [cannibals on left side, missionaries on left side, cannibals on right side, missionaries on right side, boat side].
	/// The problem's solution can be represented as the following state: [0, 0, 3, 3, 1] .
	/// </remarks>
	/// </summary>
	public struct State
	{
		public enum BoatSide { Left, Right };

		public int cannibalsLeft;
		public int missionariesLeft;
		public int cannibalsRight;
		public int missionariesRight;
		public BoatSide boatSide;

		public BoatSide OppositeBoatSide => boatSide == BoatSide.Left ? BoatSide.Right : BoatSide.Left;

		public State(int cannibalsLeft, int missionariesLeft, int cannibalsRight, int missionariesRight, BoatSide boatSide)
		{
			this.cannibalsLeft = cannibalsLeft;
			this.missionariesLeft = missionariesLeft;
			this.cannibalsRight = cannibalsRight;
			this.missionariesRight = missionariesRight;
			this.boatSide = boatSide;
		}

		public override string ToString()
        {
			return $"{boatSide} [{cannibalsLeft}, {missionariesLeft}, {cannibalsRight}, {missionariesRight}]";
		}

		public static bool operator ==(State a, State b)
        {
			return a.cannibalsLeft == b.cannibalsLeft && a.missionariesLeft == b.missionariesLeft && a.cannibalsRight == b.cannibalsRight && a.missionariesRight == b.missionariesRight && a.boatSide == b.boatSide;
        }

		public static bool operator !=(State a, State b)
		{
			return a.cannibalsLeft != b.cannibalsLeft && a.missionariesLeft != b.missionariesLeft && a.cannibalsRight != b.cannibalsRight && a.missionariesRight != b.missionariesRight && a.boatSide != b.boatSide;
        }

		public bool IsValid => (cannibalsLeft <= missionariesLeft || missionariesLeft == 0) &&
							   (cannibalsRight <= missionariesRight || missionariesRight == 0);

		public bool IsSolution => cannibalsLeft == 0 && missionariesLeft == 0;
	}
	
	private struct Flags
    {
		public bool isClosed;
		public bool isOpen;
		public bool isValid;
		public bool isSolution;
		public bool isRoot;
		public bool isCopy;
	}

	[Header("References")]
	[SerializeField]
	private TextMeshProUGUI _configurationTMP;
	[SerializeField]
    private Transform _childrenContainer;
	[SerializeField]
	private LineRenderer _parentLineRenderer;
	[SerializeField]
	private LineRenderer _sameNodeLineRenderer;

	[Header("Node Properties")]
	[SerializeField]
	private float horizontalSpacing = 1.5f;

	[Header("Colors")]
	[ColorUsage(true, true)]
	[SerializeField]
	private Color _invalidColor;
	[ColorUsage(true, true)]
	[SerializeField]
	private Color _openColor;
	[ColorUsage(true, true)]
	[SerializeField]
	private Color _closedColor;
	[ColorUsage(true, true)]
	[SerializeField]
	private Color _solutionColor;
	[ColorUsage(true, true)]
	[SerializeField]
	private Color _rootColor;

	private delegate void OnFlagsChanged(Flags flags);
	private OnFlagsChanged onFlagsChanged;

	public State state;
	private Flags _flags;
	private Node _parent;
	private Node _original;
	private List<Node> _children;

	private SpriteRenderer _spriteRenderer;
	private float _horizontalSize;
	public bool IsCopy => _flags.isCopy;
	public Transform ChildrenContainer => _childrenContainer;

	public Node Initialize(State state, Node parent = null)
    {
		_parent = parent;

		_horizontalSize = GetComponent<CircleCollider2D>().bounds.size.x;
		_spriteRenderer = GetComponent<SpriteRenderer>();

		_flags = new Flags { isClosed = false, isOpen = false, isSolution = false, isValid = true, isCopy = false};

		if (_parent != null)
		{
			_parent.AddChild(this);
			_parent.RearrangeChildren();
		} else
        {
			_flags.isRoot = true;
        }

		_children = new List<Node>();
		this.state = state;

		SetUIElements();

		return this;
    }
    private void Update()
    {
		UpdateEdges();
	}

	/// <summary>
	/// Change the position of all of the node's children while increasing it's own horizontal size.
	/// After all of the node's children have been 
	/// rearranged, call the method on the node's parent.
	/// </summary>
	public void RearrangeChildren()
    {
		_horizontalSize = 0;

		foreach (Node child in _children)
        {
			_horizontalSize += child._horizontalSize + horizontalSpacing;
        }

		_horizontalSize -= horizontalSpacing;

		float xPosition = -_horizontalSize / 2;

		foreach (Node child in _children)
        {
			xPosition += child._horizontalSize / 2;
			StartCoroutine(child.MoveToPosition(new Vector2(xPosition, 0), 0.15f));
			xPosition += child._horizontalSize / 2 + horizontalSpacing;
        }

		if (_parent != null)
        {
			_parent.RearrangeChildren();
        }
	}

	/// <summary>
	/// If the given node isn't a child of the node, add it.
	/// </summary>
	/// <param name="child"></param>
	/// <returns></returns>
    public Node AddChild(Node child)
	{
		if (child != null)
		{
			if (!_children.Contains(child))
			{
				_children.Add(child);
				return child;
			}
		}

		return null;
	}

	/// <summary>
	/// Set all UI elements, such as labels, panels, etc...
	/// </summary>
	private void SetUIElements()
    {
		_configurationTMP.text = state.ToString();
    }

	/// <summary>
	/// Update the position of the lines linking the node to it's parent and also 
	/// to the original node, if the node is a copy.
	/// </summary>
	private void UpdateEdges()
    {
		if (_parent != null)
		{
			_parentLineRenderer.SetPosition(0, transform.position);
			_parentLineRenderer.SetPosition(1, _parent.transform.position);
		}

		if (_original != null)
		{
			_sameNodeLineRenderer.SetPosition(0, transform.position);
			_sameNodeLineRenderer.SetPosition(1, _original.transform.position);
		}
	}

	/// <summary>
	/// Iterate through the node and it's parents, marking them as the solution
	/// and updating their color.	
	/// </summary>
	/// <param name="cameraController"></param>
	/// <param name="timeBetweenSteps"></param>
	/// <returns></returns>
	public IEnumerator RetraceSteps(CameraController cameraController, float timeBetweenSteps)
	{
		Node current = this;

		while (current != null)
		{
			cameraController.SetTarget(current.transform);

			current._flags.isSolution = true;
			current.UpdateColor();
			current = current._parent;

			yield return new WaitForSeconds(timeBetweenSteps * 2);
		}

		yield return null;
	}

	/// <summary>
	/// Update a node's color to match its flags.
	/// </summary>
	public void UpdateColor()
    {
		if (_flags.isSolution)
		{
			_spriteRenderer.material.SetColor("_Color", _solutionColor);

			if (!_flags.isCopy)
            {
				_parentLineRenderer.material.SetColor("_Color", _solutionColor);
			}

			return;
		}

		if (_flags.isRoot)
        {
			_spriteRenderer.material.SetColor("_Color", _rootColor);
			return;
		}

		if (!_flags.isValid)
        {
			_spriteRenderer.material.SetColor("_Color", _invalidColor);
			return;
        }

		if (_flags.isClosed)
		{
			_spriteRenderer.material.SetColor("_Color", _closedColor);
			return;
		}

		if (_flags.isOpen)
		{
			_spriteRenderer.material.SetColor("_Color", _openColor);
			return;
		}
	}

	/// <summary>
	/// Create the node's chidren, with each one of them representing a different option
	/// to the current state.
	/// </summary>
	/// <returns></returns>
	public List<Node> GenerateChildren()
    {
		List<Node> generatedChildren = new List<Node>();
		Node child;
		int cannibalsOnSide = state.boatSide == State.BoatSide.Left ? state.cannibalsLeft : state.cannibalsRight;
		int missionariesOnSide = state.boatSide == State.BoatSide.Left ? state.missionariesLeft : state.missionariesRight;

		if (cannibalsOnSide > 0)
        {
			child = TreeController.Instance.SpawnNode(Move(1, 0), this);
			if (child != null)
            {
				generatedChildren.Add(child);
			}

			if (cannibalsOnSide > 1)
            {
				child = TreeController.Instance.SpawnNode(Move(2, 0), this);
				if (child != null)
				{
					generatedChildren.Add(child);
				}
			}
        }

		if (missionariesOnSide > 0)
		{
			child = TreeController.Instance.SpawnNode(Move(0, 1), this);
			if (child != null)
			{
				generatedChildren.Add(child);
			}

            if (missionariesOnSide > 1)
            {
                child = TreeController.Instance.SpawnNode(Move(0, 2), this);
                if (child != null)
                {
                    generatedChildren.Add(child);
                }
            }
        }

		if (cannibalsOnSide > 0 && missionariesOnSide > 0)
        {
			child = TreeController.Instance.SpawnNode(Move(1, 1), this);
			if (child != null)
			{
				generatedChildren.Add(child);
			}
		}

		return generatedChildren;
	}

	/// <summary>
	/// Returns a state that represents a movement of missionaries and/or cannibals from the current state.
	/// </summary>
	/// <example>
	/// Current state: left side [3, 3, 0, 0]
	/// Move(1, 1)
	/// Returned state: right side [2, 2, 1, 1]
	/// </example>
	/// <param name="cannibals"></param>
	/// <param name="missionaries"></param>
	/// <returns></returns>
	private State Move(int cannibals, int missionaries)
    {
		if (state.boatSide == State.BoatSide.Left)
        {
			return new State
			{
				cannibalsLeft = state.cannibalsLeft - cannibals,
				cannibalsRight = state.cannibalsRight + cannibals,
				missionariesLeft = state.missionariesLeft - missionaries,
				missionariesRight = state.missionariesRight + missionaries,
				boatSide = state.OppositeBoatSide
			};
        } else
        {
			return new State
			{
				cannibalsLeft = state.cannibalsLeft + cannibals,
				cannibalsRight = state.cannibalsRight - cannibals,
				missionariesLeft = state.missionariesLeft + missionaries,
				missionariesRight = state.missionariesRight - missionaries,
				boatSide = state.OppositeBoatSide
			};
		}
    }

	/// <summary>
	/// Mark the node as closed, invoke onFlagsChanged and update the node's color.
	/// </summary>
	public void CloseNode()
    {
		_flags.isClosed = true;
		_flags.isOpen = false;

		onFlagsChanged?.Invoke(_flags);

		UpdateColor();
    }

	/// <summary>
	/// Mark the node as open, invoke onFlagsChanged and update the node's color.
	/// </summary>
	public void OpenNode()
    {
		_flags.isOpen = true;
		_flags.isClosed = false;

		onFlagsChanged?.Invoke(_flags);

		UpdateColor();
    }

	/// <summary>
	/// Return if the node's is currently the problem's solution and update it's flag. Also invoke onFlagsChanged and update the node's color.
	/// </summary>
	public bool IsSolution()
	{
		_flags.isSolution = state.IsSolution;

		onFlagsChanged?.Invoke(_flags);

		UpdateColor();

		return _flags.isSolution;
	}

	/// <summary>
	/// Return if the node's state is a valid state update it's flag. Also invoke onFlagsChanged and update the node's color.
	/// </summary>
	public bool IsValid()
    {
		_flags.isValid = state.IsValid;

		onFlagsChanged?.Invoke(_flags);

		UpdateColor();

		return _flags.isValid;
	}

	/// <summary>
	/// Set the given node as the node's original, update the flag and invoke onFlagsChanged.
	/// </summary>
	/// <param name="node"></param>
	public void SetOriginal(Node node)
    {
		_flags = node._flags;
		_flags.isCopy = true;

		_original = node;

		_original.onFlagsChanged += SetFlags;

		UpdateColor();
    }

	/// <summary>
	/// Callback function to update the node's flags based on another node.
	/// </summary>
	/// <param name="flags"></param>
	private void SetFlags(Flags flags)
    {
		_flags.isValid = flags.isValid;
		_flags.isSolution = flags.isSolution;
		_flags.isClosed = flags.isClosed;
		_flags.isOpen = flags.isOpen;

		UpdateColor();
	}

    private void OnDisable()
    {
		if (_original != null)
        {
			_original.onFlagsChanged -= SetFlags;
		}
    }

	/// <summary>
	/// Interpolate the current position to target position in a given time.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="time"></param>
	/// <returns></returns>
    private IEnumerator MoveToPosition(Vector2 position, float time)
    {
		Vector2 initialPosition = transform.localPosition;

		float elapsedTime = 0;

		while (elapsedTime < time)
        {
			transform.localPosition = Vector2.Lerp(initialPosition, position, elapsedTime/time);
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		transform.localPosition = position;

		yield return null;
    }
}
