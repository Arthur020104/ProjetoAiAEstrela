using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    [SerializeField]private float _jumpForce = 12.0f, _gravityMult = 2.5f;

    [SerializeField]private Transform _groundCheckPoint;
    private bool _isGrounded;
    [SerializeField]private LayerMask _groundLayers;

    [SerializeField] private float _rayLenght =  0.2f;

    //[Header("Shooting")]//placeHolder
    [SerializeField]private GameObject _bulletImpact;
    [SerializeField]private float _timeToDestroyBulletImpact = 5.0f;

    [Header("Resorces")]
    [SerializeField]private GameObject _lantern;
    [SerializeField]private float _flashTime = 0.3f;
    private bool _lanternIsEnable = false;

    [SerializeField]private GameObject _trailPrefab;

    [SerializeField]private int _itemCount = 0;

    [SerializeField]private LayerMask _itemInteractionLayer;

    [SerializeField]private float _pickupItemRadius = 1;
    [SerializeField] private float timeToSendTrail = 0.3f;
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
        
        this._cam = Camera.main;
        if(this._cam == null)
        {
            Debug.LogError("Camera can't be null.");
        }

        if(this._viewPoint == null)
        {
            Debug.LogError("ViewPoint can't be null.");
        }
        if(!this.TryGetComponent<CharacterController>(out this._charCon))
        {
            Debug.LogError("Character needs to have CharacterController.");
        }
        _AIScript = GameObject.FindAnyObjectByType<AI>();
        if(_AIScript == null)
        {
            Debug.LogError("Could not find the AI");
        }
        if(_lantern == null)
        {
            Debug.LogError("Lantern is null");
        }
        if(_trailPrefab == null)
        {
            Debug.LogError("Trail is null");
        }
        _mapGen = GameObject.FindAnyObjectByType<MapGenerator>();
        if (_mapGen == null)
        {
            Debug.LogError("Could not find reference to map generator script");
        }
        if (_stepsSource == null)
        {
            Debug.LogError("StepsSource is null");
        }
        if(_uiManager == null)
        {
            Debug.LogError("uiManager is null");
        }
        
        _lastPosition = this.transform.position;
        _lanternIsEnable = false;
        _lantern.SetActive(false);

        this._activeSpeed = this._moveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if(_isFrozen)
        {
            if(_stepsSource.enabled)
                _stepsSource.enabled = false;
            return;
        }
        CheckGrounded();
        HandleCameraInput();
        HandleMovement();
        HandleCursor();

        if(Input.GetKeyDown(KeyCode.F) && _AIScript != null)
        {
            _AIScript.GoTo(new Vector2(Mathf.RoundToInt(gameObject.transform.position.x), Mathf.RoundToInt(gameObject.transform.position.z)));
            SpawnTrailsToTargets(_trailPrefab, transform.position);
            //if(!_lanternIsEnable)
              //  StartCoroutine(FlashLantern());
        }
        if(Input.GetKeyDown(KeyCode.E))
        {
            Collider[] items = Physics.OverlapSphere(gameObject.transform.position, _pickupItemRadius, _itemInteractionLayer);

            foreach(Collider item in items)
            {
                //remove item position from items pos on mapgen
                _mapGen.itemsPosition.Remove(new Vector2(Mathf.RoundToInt(item.gameObject.transform.position.x), Mathf.RoundToInt(item.gameObject.transform.position.z)));
                Destroy(item.gameObject);
                _itemCount++;
                Debug.Log($"Got item, item count: {_itemCount}");
                _uiManager.UpdateItemCount(_itemCount);
            }
        }
        HandleAudio();

    }
    void HandleAudio()
    {
        //Not playing audio if is off the ground
        Vector3 currentHorizontalPos = new Vector3(this.transform.position.x, 0.0f, this.transform.position.z);
        Vector3 lastPosHorizontal = new Vector3(_lastPosition.x, 0.0f, _lastPosition.z);
        _lastPosition = this.transform.position;

        float normalizedSpeed = ((currentHorizontalPos - lastPosHorizontal).magnitude / Time.deltaTime)/ _moveSpeed;
        
        if(normalizedSpeed > 0 && isGrounded())
        {
            _stepsSource.enabled = true;
            _stepsSource.pitch = Mathf.Clamp(normalizedSpeed, 0.5f, 2.0f);
        }
        else
        {
            _stepsSource.enabled = false;
        }
    }
    IEnumerator FlashLantern()
    {
        _lanternIsEnable = true;
        _lantern.SetActive(true);
        RenderSettings.fog = false;

        yield return new WaitForSeconds(_flashTime);

        _lanternIsEnable = false;
        _lantern.SetActive(false);
        RenderSettings.fog = true;
    }
    private static void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }
    private void Shoot()
    {
        Ray ray = this._cam.ViewportPointToRay(new Vector3(0.5f,0.5f,0.0f));
        ray.origin = this._cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log(hit.collider.gameObject.name);
            GameObject bulletImpactInstance = Instantiate(this._bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            Destroy(bulletImpactInstance, this._timeToDestroyBulletImpact);
        }
    }
    private void HandleMovement()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift) )
        {  
            this._activeSpeed *= this._runMulti;
        }
        else if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            this._activeSpeed /= this._runMulti;
        }
        //applying gravity using code to try using on my 3d(c++) lib
        //maybe will be replaced using the rb
        float yVel = this._move.y;
        this._move = this._activeSpeed * (this.gameObject.transform.right * Input.GetAxisRaw("Horizontal") +  this.gameObject.transform.forward * Input.GetAxisRaw("Vertical")).normalized;
        this._move.y = yVel;
        
        if(this._charCon.isGrounded)
        {
            this._move.y = 0.0f;
        }
        
        if(this._isGrounded && Input.GetButtonDown("Jump"))
        {
            this._move.y = this._jumpForce;
        }


        this._move.y += Physics.gravity.y * this._gravityMult * Time.deltaTime;

        
        this._charCon.Move(this._move * Time.deltaTime);

    }
    private void HandleCameraInput()
    {
        this._mouseInput = new Vector2(Input.GetAxisRaw("Mouse X") * this._mouseSensX, Input.GetAxisRaw("Mouse Y") * this._mouseSensY) * Time.deltaTime * MOUSE_SENS_MULT;
        
        //Applying horizontal rotation to the player and consequently to the ViewPoint, ViewPoint is a child(Inside unity) of the player GameObject
        gameObject.transform.rotation = Quaternion.Euler(this.gameObject.transform.eulerAngles.x, this.gameObject.transform.eulerAngles.y + this._mouseInput.x, this.gameObject.transform.eulerAngles.z);

        //Applying Vertical rotation only to the viewPoint, if player has head add some rotation to it
        this._verticalRotStore = Mathf.Clamp(this._verticalRotStore + (this._invertLook ? this._mouseInput.y : -this._mouseInput.y), -this._clampVerticalRot,this._clampVerticalRot);

        this._viewPoint.rotation = Quaternion.Euler(this._verticalRotStore,this._viewPoint.transform.eulerAngles.y, this._viewPoint.transform.eulerAngles.z);
    }
    private void CheckGrounded()
    {
        this._isGrounded = Physics.Raycast(this._groundCheckPoint.transform.position, -this.transform.up, this._rayLenght, this._groundLayers);
    }
    private bool isGrounded()
    {
        return this._isGrounded;
    }
    private void UpdateCameraPosition()
    {
        this._cam.transform.position = this._viewPoint.position;
        this._cam.transform.rotation = this._viewPoint.rotation;
    }



    public void SpawnTrailsToTargets(GameObject prefab, Vector3 pos)
    {
        //StartCoroutine(StartTrailsAndSend(prefab, pos));
        List<Vector2> targets = _mapGen.itemsPosition;
        float timeToSend = Time.time + timeToSendTrail;

        for (int i = 0; i < targets.Count; i++)
        {
            StartCoroutine(SendTrail(prefab, pos,targets[i], timeToSend));
        }
    }

    IEnumerator SendTrail(GameObject prefab, Vector3 pos, Vector2 target, float time)
    {
        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
        
        if (!instance.TryGetComponent<Trail>(out Trail trailScript))
        {
            Debug.LogError("Could not find trail script on prefab");
            yield break;
        }
        yield return new WaitForSeconds(time-Time.time);
        //Debug.Log($"Sending trail to target {target}");
        trailScript.GoTo(target);
        
    }
}
