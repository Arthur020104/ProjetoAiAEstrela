using System.Collections;
using System.Linq;
using UnityEngine;

public class AI : Mover
{
    [Header("Behavior")]
    private bool _isFrozen = false;
    [SerializeField] private float _timeToStartMoving = 10.0f;
    [SerializeField]private float _delayBeforeRandomMove = 7.0f;
    private float _lastMoved;
    private bool _timerOn = false;
    private float _lastSawPlayer;
    [SerializeField]private float _timeToDisregardPlayerPosition = 5.0f;
    [SerializeField]private float _maxDistanceToWalkFromLastSawPos = 30.0f, _minDistanceToWalkFromLastSawPos = 3.0f;
    [SerializeField] private float _maxTargetAngleDeviation = 30f;
    private float _maxTargetAngleDeviationCos;
    [SerializeField]private float _killPlayerDistance = 1.0f;

    [Header("Player")]
    [SerializeField] private Transform _playerPos;
    private Vector2 _lastSawPosition;

    [Header("Head Settings")]
    [SerializeField] private GameObject _head;
    [SerializeField] private float _turnRate = 10.0f;
    [SerializeField] private float _clampXZ = 50.0f;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask _layerToHit;
    [SerializeField] private float _raySize = 15.0f;

    [Header("Animator Settings")]
    [SerializeField] private Animator _animator;

    [SerializeField]private float _baseSpeedAnim = 0.5f;//basiclly the speed ia moves to match the animation 1x cycle

    private Vector2 _playerCoord;

    //vel
    private Vector3 lastPos;

    [Header("Audio Settings")]
    [SerializeField]private AudioSource _audioSourceLaugh;
    private float _lastPlayedLaugh;
    [SerializeField] private AudioSource _stepsSound;
    [SerializeField] private float _baseSpeedSound = 2.0f;

    [Header("UI")]
    [SerializeField] private UIManager _uiManager;


    private Quaternion _lastHeadRotation;
    protected override void Start()
    {
        base.Start();
        if (_playerPos == null)
        {
            Debug.LogError("Player position transform not assigned in inspector");
        }
        if (_animator == null)
        {
            Debug.LogError("Animator not assigned in inspector");
        }
        if(_uiManager == null)
        {
            Debug.LogError("uiManager not assigned in inspector");
        }
        _lastHeadRotation = Quaternion.identity;
        _audioSourceLaugh.enabled = false;
        _stepsSound.enabled = false;
        _timerOn = false;
        _lastMoved = Time.time;
        _lastSawPlayer = Time.time;
        _lastSawPosition = new Vector2(-1, -1);
        _maxTargetAngleDeviationCos = Mathf.Cos(_maxTargetAngleDeviation * Mathf.Deg2Rad);
        Debug.Log(_maxTargetAngleDeviationCos);
        StartCoroutine(FreezeForSeconds(_timeToStartMoving));
    }
    IEnumerator FreezeForSeconds(float duration)
    {
        _isFrozen = true;
        yield return new WaitForSeconds(duration);
        _isFrozen = false;
    }
    protected override void Update()
    {
        if (_isFrozen)
            return;
        base.Update();

        Vector3 currentVelocity = lastPos != null ? (transform.position - lastPos) / (Time.deltaTime) : new Vector3(0, 0, 0);

        _head.transform.rotation = _lastHeadRotation;
        RandomWalk();
        PursueTarget();
        Animation(currentVelocity);
        StepsSound(currentVelocity);
        lastPos = transform.position;

    }
    void RandomWalk()
    {
        if (OnDestination() && !_timerOn)
        {
            _lastMoved = Time.time;
            _timerOn = true;
        }
        if (Time.time - _lastMoved >= _delayBeforeRandomMove && _timerOn)
        {
            GoToRandomPos();
            _timerOn = false;
        }

        if (Time.time - _lastSawPlayer >= _timeToDisregardPlayerPosition)
        {
            _lastSawPosition = new Vector2(-1, -1);
        }
    }
    void StepsSound(Vector3 currentVelocity)
    {
        float speedClip = currentVelocity.magnitude / _baseSpeedSound;
        if (speedClip > 0)
        {
            _stepsSound.enabled = true;
            _stepsSound.pitch = Mathf.Clamp(speedClip, 0.5f, 2.0f);
        }
        else
        {
            _stepsSound.enabled = false;
        }
    }
    void Animation(Vector3 currentVelocity)
    {
        float animationMult = currentVelocity.magnitude/_baseSpeedAnim;
        if(animationMult > 0)
        {
            _animator.speed = animationMult;
        }

        _animator.SetFloat("animationMult", animationMult );
    }
    void LateUpdate()
    {
        AdjustHeadRotation();
    }
    void PursueTarget()
    {
        /*float distance = Vector3.Distance(_playerPos.position, transform.position);
        if(distance > _raySize)
            return;*/
        
        Vector3 direction = (_playerPos.position - transform.position).normalized;
        
        // Cast the ray and get the first hit.
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, _raySize, _layerToHit))
        {
            // Check if the hit objec is the player
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                Vector2 newCoord = new Vector2(Mathf.RoundToInt(_playerPos.position.x), Mathf.RoundToInt(_playerPos.position.z));

                _lastSawPosition = newCoord;
                _lastSawPlayer = Time.time;
                // If the coordinate is different, run to the player.
                if (lasTTargetPosition != newCoord)
                {
                    _playerCoord = newCoord;

                    
                    GoTo(newCoord);
                   
                }
                
                //audio wait audio finish playing for it to start again
                if(Time.time - _lastPlayedLaugh  >= _audioSourceLaugh.clip.length || Time.time < _audioSourceLaugh.clip.length)
                {
                    _audioSourceLaugh.enabled = true;
                    _lastPlayedLaugh = Time.time;
                }

                if(Vector3.Distance(_playerPos.position, transform.position) <= _killPlayerDistance)
                {
                    _stepsSound.enabled = false;
                    _animator.enabled = false;
                    hit.collider.GetComponent<PlayerController>().enabled = false;
                    hit.collider.GetComponent<AudioSource>().enabled = false;
                    
                    
                    _isFrozen = true;
                    
                    _uiManager.gameOver = true;
                    Debug.Log("killllll");
                }
            }
        }
         //wait audio finish playing for to stop
        if(Time.time - _lastPlayedLaugh  >= _audioSourceLaugh.clip.length)
            _audioSourceLaugh.enabled = false;
    }
    public override void GoToRandomPos()
    {
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 destination;
        bool lastSawPosValid = _lastSawPosition != new Vector2(-1, -1);
        Vector2 aiToLastSawDir = (_lastSawPosition - currentPos).normalized;
        Vector2 aiToDestDir;

        do
        {
            destination.x = Random.Range(0, _mapGen.gridWidth);
            destination.y = Random.Range(0, _mapGen.gridHeight);
            aiToDestDir = (destination - currentPos).normalized;
        }// Loop until the destination is unblocked and, if last seen player position is valid (is at an acceptable angle and distance).
        while (_mapGen.IsBlocked(Mathf.RoundToInt(destination.x), Mathf.RoundToInt(destination.y)) ||
            (lastSawPosValid && (Vector2.Dot(aiToLastSawDir, aiToDestDir) < _maxTargetAngleDeviationCos ||
                 Vector2.Distance(_lastSawPosition, destination) > _maxDistanceToWalkFromLastSawPos ||
                 Vector2.Distance(_lastSawPosition, destination) < _minDistanceToWalkFromLastSawPos))
              );

        GoTo(destination);
    }
    public override void GoTo(Vector2 destination)
    {
        base.GoTo(destination);
        _timerOn = false;
    }
    void AdjustHeadRotation()
    {
        _head.transform.rotation = _lastHeadRotation;
        // Calculate the vector from the head to the player position.
        Vector3 targetDelta = _playerPos.position - _head.transform.position;
        float angleToTarget = Vector3.Angle(_head.transform.forward, targetDelta);
        Vector3 turnAxis = Vector3.Cross(_head.transform.forward, targetDelta);

        // Apply rotation towards the target.
        _head.transform.RotateAround(_head.transform.position, turnAxis, Time.deltaTime * _turnRate * angleToTarget);

        // Clamp the head's x and z rotation values.
        Vector3 euler = _head.transform.eulerAngles;
        euler.x = (euler.x > 180) ? euler.x - 360 : euler.x;
        euler.z = (euler.z > 180) ? euler.z - 360 : euler.z;
        euler.x = Mathf.Clamp(euler.x, -_clampXZ, _clampXZ);
        euler.z = Mathf.Clamp(euler.z, -_clampXZ, _clampXZ);
        euler.x = (euler.x < 0) ? euler.x + 360 : euler.x;
        euler.z = (euler.z < 0) ? euler.z + 360 : euler.z;

        // Set the new clamped rotation.
        _head.transform.eulerAngles = new Vector3(euler.x, _head.transform.eulerAngles.y, euler.z);
        _lastHeadRotation = _head.transform.rotation;
    }
}
