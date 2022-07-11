using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that handles tree generation rules, 
/// and implements a BFS to solve the missionaries and cannibals 
/// problem.
/// </summary>
public class TreeController : MonoBehaviour
{   
    [Header("References")]
    [SerializeField]
    private Node _nodePrefab;
    [SerializeField]
    private Transform _treeContainer;
    [SerializeField]
    private CameraController _cameraController;
    [SerializeField]
    private TMP_InputField _cannibalsInputField;
    [SerializeField]
    private TMP_InputField _missionariesInputField;
    [SerializeField]
    private TMP_InputField _maxIterationsInputField;
    [SerializeField]
    private TMP_InputField _timeBetweenIterationsInputField;

    [Header("Search Properties")]
    [Tooltip("The time in seconds between each iteration of the BFS.")]
    [SerializeField]
    private float _timeBetweenIterations;
    [SerializeField]
    private int _startingCannibals = 3;
    [SerializeField]
    private int _startingMissionaries = 3;
    [Tooltip("The maximum amount of iterations the BFS will do. Can be used to prevent extremely long searchs or infinite loops.")]
    [SerializeField]
    private int _maxIterations = 30;

    private Node _root;
    private List<Node> _openList;
    private List<Node> _closedList;
    private int _nodeCount = 0;

    public static TreeController Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
        _openList = new List<Node>();
        _closedList = new List<Node>();
    }

    public void StartBFS()
    {
        StopAllCoroutines();

        _openList.Clear();
        _closedList.Clear();
        
        if (_root != null)
        {
            Destroy(_root.gameObject);
        }

        _nodeCount = 0;

        _startingCannibals = Mathf.Abs(int.Parse(_cannibalsInputField.text));
        _startingMissionaries = Mathf.Abs(int.Parse(_missionariesInputField.text));
        _maxIterations = Mathf.Abs(int.Parse(_maxIterationsInputField.text));
        _timeBetweenIterations = Mathf.Abs(float.Parse(_timeBetweenIterationsInputField.text));

        StartCoroutine(BFS());
    }

    /// <summary>
    /// Instantiate and initialize a node given a specific state.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Node SpawnNode(Node.State state, Node parent)
    {
        Transform parentGameObject;

        if (parent == null)
        {
            parentGameObject = _treeContainer;
        } else
        {
            parentGameObject = parent.ChildrenContainer;
        }

        Node node = Instantiate(_nodePrefab, parentGameObject).Initialize(state, parent);
        node.gameObject.name = _nodeCount.ToString();
        _nodeCount++;
        return node;
    }

    /// <summary>
    /// Generate the tree's root, using the initial cannibals and missionaries numbers
    /// on the left side of the river.
    /// </summary>
    private void GenerateRoot()
    {
        Node root = SpawnNode(new Node.State(_startingCannibals, _startingMissionaries, 0, 0, Node.State.BoatSide.Left), null);
        _root = root;
    }

    /// <summary>
    /// Get the first node in a list given a specific state. 
    /// </summary>
    /// <returns>
    /// Returns null if a node was not found.
    /// </returns>
    /// <param name="list"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public Node GetNodeInList(List<Node> list, Node.State state)
    {
        foreach (Node node in list)
        {
            if (node.state == state)
            {
                return node;
            }
        }

        return null;
    }

    private void AddNodesToOpenList(List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            AddNodeToOpenList(node);
        }
    }

    /// <summary>
    /// Add unique nodes to the open list.
    /// </summary>
    /// <remarks>
    /// Unique nodes are those who don't exist in the closed or open lists.
    /// This restriction prevents infinite loops while searching through the tree.
    /// </remarks>
    /// <param name="node"></param>
    private void AddNodeToOpenList(Node node)
    {
        SearchForOriginal(node);

        if (!node.IsCopy)
        {
            _openList.Add(node);
            node.OpenNode();
        }
    }

    /// <summary>
    /// Check if the node already exist in the open or closed list
    /// If a node is found, flag the passed node as a copy.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public void SearchForOriginal(Node node)
    {
        Node original = GetNodeInList(_closedList, node.state);

        if (original != null)
        {
            node.SetOriginal(original);
        } else
        {
            original = GetNodeInList(_openList, node.state);

            if (original != null)
            {
                node.SetOriginal(original);
            }
        }
    }

    /// <summary>
    /// Breadth-first search method.
    /// </summary>
    /// <returns></returns>
    private IEnumerator BFS()
    {
        int currentIteration = 0;
        GenerateRoot();
        Node currentNode = _root;
        _openList.Add(currentNode);

        while (_openList.Count > 0 && currentIteration < _maxIterations)
        {
            _cameraController.SetTarget(currentNode.transform);

            yield return new WaitForSeconds(_timeBetweenIterations * 0.3f);

            if (currentNode.IsSolution())
            {
                StartCoroutine(currentNode.RetraceSteps(_cameraController, _timeBetweenIterations));
                yield break;
            } else
            {
                if (currentNode.IsValid())
                {
                    AddNodesToOpenList(currentNode.GenerateChildren());
                }
            }

            _openList.Remove(currentNode);
            _closedList.Add(currentNode);
            currentNode.CloseNode();

            if (_openList.Count > 0)
            {
                currentNode = _openList[0];
            }

            currentIteration++;

            yield return new WaitForSeconds(_timeBetweenIterations * 0.7f);
        }

        yield return null;
    }
}

