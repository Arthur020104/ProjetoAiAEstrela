using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform _castRayFrom, _playerTransform;
    [SerializeField] private float _raySize = 2.0f;
    [SerializeField] private LayerMask _layerToHit;
    [SerializeField] private float _timeToRayCast = 0.1f;
    private float _timer = 0;
    void Start()
    {
        if(_castRayFrom == null)
        {
            Debug.LogError("Tranform reference for ray was not assigned in the inspector");
        }
        _timer = _timeToRayCast;
    }

    // Update is called once per frame
    void Update()
    {
        if(_timer <= 0.0f)
        {
            _timer = _timeToRayCast;
            Vector3 direction = (_playerTransform.position - _castRayFrom.position).normalized;
            Debug.Log("Casting");
            RaycastHit hitInfo;
            if (Physics.Raycast(_castRayFrom.position, direction, out hitInfo, _raySize, _layerToHit))
            {
                Debug.Log("hit a");
                if (hitInfo.collider.CompareTag("Player"))
                {
                    Debug.Log("hit player");
                    SceneManager.LoadScene("Scene2");

                }
            }
        }
        _timer -= Time.deltaTime;

    }
}
