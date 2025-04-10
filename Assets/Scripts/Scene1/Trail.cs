using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trail : Mover
{
    private bool _startedMoving = false;
    [SerializeField]private float _timeToDestoyAftherReachTarget = 3.0f;
    
    protected override void Update()
    {
        base.Update();
        if(OnDestination() && _startedMoving)
        {
            Destroy(this.gameObject, _timeToDestoyAftherReachTarget);
        }
    }
    public override void GoTo(Vector2 destination)
    {
        base.GoTo(destination);
        _startedMoving = true;
    }
}
