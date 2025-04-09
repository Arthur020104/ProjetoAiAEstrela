using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

public class UiToggle : MonoBehaviour
{
    [SerializeField] private GameObject _wonGameUi;
    [SerializeField] private PlayerController _player;
    private bool _isActive = false;
    private void Start()
    {
        _isActive = true;
        _wonGameUi.SetActive(_isActive);
        _player._isFrozen = _isActive;
        Cursor.lockState = CursorLockMode.Confined;
    }
    private void Update()
    {
        if (Input.GetButtonUp("Cancel"))
        {
            _isActive = !_isActive;
            _player._isFrozen = _isActive;
            _wonGameUi.SetActive(_isActive);
            if (_isActive)
            {
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

    }
    public void ReStartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
