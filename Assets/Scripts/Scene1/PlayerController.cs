using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("ViewConfig")]
    [SerializeField]private Transform _viewPoint;
    [SerializeField]private float _mouseSensX = 10.0f, _mouseSensY = 10.0f, _clampVerticalRot = 60.0f;
    [SerializeField]private bool _invertLook = false;
    private const float MOUSE_SENS_MULT = 100.0f;
    private float _verticalRotStore = 0.0f;
    private Vector2 _mouseInput;
    private Camera _cam;

    [Header("MovementConfig")]
    [SerializeField]private float _moveSpeed = 10.0f, _runMulti = 1.6f;
    private float _activeSpeed;
    private CharacterController _charCon;
    private Vector3 _move;

    [Header("Resorces")]
    [SerializeField]private GameObject _trailPrefab;
    [SerializeField]private float timeToSendTrail = 0.3f;

    [SerializeField]private int _itemCount = 0;
    [SerializeField]private LayerMask _itemInteractionLayer;
    [SerializeField]private float _pickupItemRadius = 1;

    //IA
    private AI _AIScript;
    private MapGenerator _mapGen;

    [Header("Audio")]
    [SerializeField]private AudioSource _stepsSource;
    private Vector3 _lastPosition;

    [Header("UI")]
    [SerializeField]private UIManager _uiManager;

    public bool _isFrozen = false;

    void Start()
    {
        _cam = Camera.main;
        if(_cam == null) 
            Debug.LogError("Camera can't be null.");
        if(_viewPoint == null) 
            Debug.LogError("ViewPoint can't be null.");
        if(!TryGetComponent<CharacterController>(out _charCon)) 
            Debug.LogError("Character needs to have CharacterController.");
        _AIScript = GameObject.FindAnyObjectByType<AI>();
        if(_AIScript == null) 
            Debug.LogError("Could not find the AI");
        
        _mapGen = GameObject.FindAnyObjectByType<MapGenerator>();
        if (_mapGen == null) 
            Debug.LogError("Could not find reference to map generator script");

        if (_stepsSource == null) 
            Debug.LogError("StepsSource is null");
        if(_uiManager == null) 
            Debug.LogError("uiManager is null");

        _lastPosition = transform.position;
        _activeSpeed = _moveSpeed;
    }

    void Update()
    {
        if(_isFrozen)
        {
            if(_stepsSource.enabled) _stepsSource.enabled = false;
            return;
        }
        HandleCameraInput();
        HandleMovement();
        LocateItems();
        CollectItem();
        HandleAudio();
    }
    void LocateItems()
    {
        if(Input.GetKeyDown(KeyCode.F) && _AIScript != null)
        {
            _AIScript.GoTo(new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z)));
            SpawnTrailsToTargets(_trailPrefab, transform.position);
        }
    }
    void CollectItem()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            Collider[] items = Physics.OverlapSphere(transform.position, _pickupItemRadius, _itemInteractionLayer);
            foreach(Collider item in items)
            {
                _mapGen.itemsPosition.Remove(new Vector2(Mathf.RoundToInt(item.transform.position.x), Mathf.RoundToInt(item.transform.position.z)));
                Destroy(item.gameObject);
                _itemCount++;
                _uiManager.UpdateItemCount(_itemCount);
            }
        }
    }
    // Manages footstep sounds based on player movement speed
    void HandleAudio()
    {
        Vector3 currentHorizontalPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 lastPosHorizontal = new Vector3(_lastPosition.x, 0.0f, _lastPosition.z);
        _lastPosition = transform.position;

        float normalizedSpeed = ((currentHorizontalPos - lastPosHorizontal).magnitude / Time.deltaTime)/ _moveSpeed;
        if(normalizedSpeed > 0)
        {
            _stepsSource.enabled = true;
            _stepsSource.pitch = Mathf.Clamp(normalizedSpeed, 0.5f, 2.0f);
        }
        else
        {
            _stepsSource.enabled = false;
        }
    }
    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    // Controls player movement, including running with left shift
    private void HandleMovement()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            _activeSpeed *= _runMulti;
        }
        else if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            _activeSpeed /= _runMulti;
        }
        float yVel = _move.y;
        _move = _activeSpeed * (transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical")).normalized;
        _move.y = yVel;

        if(_charCon.isGrounded) _move.y = 0.0f;
        _charCon.Move(_move * Time.deltaTime);
    }

    // Processes mouse input for first-person camera control
    private void HandleCameraInput()
    {
        _mouseInput = new Vector2(Input.GetAxisRaw("Mouse X") * _mouseSensX, Input.GetAxisRaw("Mouse Y") * _mouseSensY) * Time.deltaTime * MOUSE_SENS_MULT;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + _mouseInput.x, transform.eulerAngles.z);
        _verticalRotStore = Mathf.Clamp(_verticalRotStore + (_invertLook ? _mouseInput.y : -_mouseInput.y), -_clampVerticalRot, _clampVerticalRot);
        _viewPoint.rotation = Quaternion.Euler(_verticalRotStore, _viewPoint.eulerAngles.y, _viewPoint.eulerAngles.z);
    }

    // Syncs the camera position and rotation with the view point
    private void UpdateCameraPosition()
    {
        _cam.transform.position = _viewPoint.position;
        _cam.transform.rotation = _viewPoint.rotation;
    }

    // Creates trails from current position to all item positions on the map
    public void SpawnTrailsToTargets(GameObject prefab, Vector3 pos)
    {
        List<Vector2> targets = _mapGen.itemsPosition;
        float timeToSend = Time.time + timeToSendTrail;
        for (int i = 0; i < targets.Count; i++)
        {
            StartCoroutine(SendTrail(prefab, pos, targets[i], timeToSend));
        }
    }

    // Coroutine to animate a trail to a specific target position
    IEnumerator SendTrail(GameObject prefab, Vector3 pos, Vector2 target, float time)
    {
        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
        if (!instance.TryGetComponent<Trail>(out Trail trailScript))
        {
            Debug.LogError("Could not find trail script on prefab");
            yield break;
        }
        yield return new WaitForSeconds(time - Time.time);
        trailScript.GoTo(target);
    }
}
