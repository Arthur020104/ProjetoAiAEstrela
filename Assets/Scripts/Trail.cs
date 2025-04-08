using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trail : Mover
{
    private bool _startedMoving = false;
    
    protected override void Update()
    {
        base.Update();
        if(OnDestination() && _startedMoving)
        {
            Destroy(this.gameObject, 1.0f);
        }
    }
    public override void GoTo(Vector2 destination)
    {
        base.GoTo(destination);
        _startedMoving = true;
    }
}
