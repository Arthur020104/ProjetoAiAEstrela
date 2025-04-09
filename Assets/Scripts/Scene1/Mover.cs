using UnityEngine;
using System.Collections.Generic;
using AStarPath;
public class Mover : MonoBehaviour
{
    protected Stack<(int, int)> _path = new Stack<(int, int)>();
    protected MapGenerator _mapGen;
    protected Vector2 _movingTowards;

    [SerializeField] protected float _moveSpeed = 10.0f;
    [SerializeField] protected float _minDistanceToPos = 0.1f;

    protected bool onTarget;

    protected Vector2 lasTTargetPosition;


    protected virtual void Awake()
    {
        onTarget = true;
        _mapGen = GameObject.FindAnyObjectByType<MapGenerator>();
        if (_mapGen == null)
        {
            Debug.LogError("Could not find reference to map generator script");
            throw new System.Exception("Could not find reference to map generator script");
        }
        lasTTargetPosition = new Vector2(transform.position.x, transform.position.z);
        _movingTowards = lasTTargetPosition;
    }
    protected virtual void Start(){}
    protected virtual void Update()
    {
        MovingTowards();
        
    }

    protected void MovingTowards()
    {
        if(_path == null)
        {
            Debug.LogWarning("path null");
            return;
        }
        Vector2 position2d = new Vector2(transform.position.x, transform.position.z);

        // If _movingTowards is not set (default(Vector2)) or we have reached the target, set the next waypoint
        if (_movingTowards == new Vector2(-1,-1) || HasReachedTarget())
        {
            if (_path.Count == 0)
            {
                if(OnDestination())
                    onTarget = true;
                return;
            }

            var nextPosition = _path.Pop();
            _movingTowards = new Vector2(nextPosition.Item1, nextPosition.Item2);
        }

        // Move towards target
        Vector2 direction = (_movingTowards - position2d).normalized;
        Vector2 movement = direction * Time.deltaTime * _moveSpeed;
        transform.position += new Vector3(movement.x, 0.0f, movement.y);
    }

    public bool HasReachedTarget()
    {
        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(currentPosition, _movingTowards) <= _minDistanceToPos;
    }
    public virtual void GoTo(Vector2 destination)
    {
        // Round the current position for pathfinding
        onTarget = false;
        lasTTargetPosition = destination;
        //consider start the position is it moving to if different from default
        Vector2 start = _movingTowards == new Vector2(-1,-1)? new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z)): _movingTowards;
        Stack<(int, int)> newPath = AStar.AStarPathfinding(start, destination, _mapGen);
        if (newPath != null)
        {
            _path = newPath;
            return;
        }
        Debug.LogWarning("Can't go to null path");
    }
    protected virtual bool OnDestination()
    {
        if(lasTTargetPosition == null)
        {
            return false;
        }
        return Vector2.Distance(lasTTargetPosition , new Vector2(transform.position.x, transform.position.z)) <= _minDistanceToPos;
    }
    public virtual void GoToRandomPos()
    {
        Vector2 destination;
        do
        {
            destination.x = Random.Range(0, _mapGen.gridWidth);
            destination.y = Random.Range(0, _mapGen.gridHeight);
        } while (_mapGen.IsBlocked(Mathf.RoundToInt(destination.x), Mathf.RoundToInt(destination.y)));
        GoTo(destination);
    }
}
