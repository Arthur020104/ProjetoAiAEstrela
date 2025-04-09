using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _itemCountField, _popUpTextField;
    [SerializeField] private float _popupCharacterDisplayInterval = 0.1f, _durationBeforePopUpTextDestroy = 1f;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private List<string> _inicialText;
    [SerializeField] private GameObject _portal;
    private Queue<string> _queuePopUpText;
    private bool _isTypingOnPopup = false;
    private int _amountOfItems;
    private MapGenerator _mapGen;
    public bool gameOver = false;

    void Start()
    {
        if(!TryGetComponent<MapGenerator>(out _mapGen))
        {
            Debug.LogError("Could not get map generator");
        }
        _amountOfItems = _mapGen.AmountOfItems;
        _queuePopUpText = new Queue<string>();
        _popUpTextField.text = "";
        _popUpTextField.enabled = false;
        gameOver = false;
        _gameOverPanel.SetActive(false);
        _portal.SetActive(false);
        foreach(string text in _inicialText)
        {
            ShowText(text);
        }
        UpdateItemCount(0);

        Cursor.lockState = CursorLockMode.Locked;

    }

    void Update()
    {
        if(gameOver)
        {
            GameOverScreen();
            return;
        }
        PopUpQueue();
    }
    private void GameOverScreen()
    {
        if(!gameOver)
        {
            return;
        }
        Cursor.lockState = CursorLockMode.Confined;
        _gameOverPanel.SetActive(true);

        _popUpTextField.enabled = false;
        _itemCountField.enabled = false;
        foreach (Transform child in _itemCountField.transform)
        {
            child.gameObject.SetActive(false);
        }


    }
    public void QuitGame()
    {
        if (!gameOver)
        {
            return;
        }
        Application.Quit();
    }
    public void ReStartGame()
    {
        if (!gameOver)
        {
            return;
        }
        SceneManager.LoadScene("SampleScene");
    }
    private void PopUpQueue()
    {
        if(_queuePopUpText.Count > 0 && !_isTypingOnPopup)
        {
            StartCoroutine(TypingTextCoroutine(_queuePopUpText.Dequeue()));
        }
    }

    public void UpdateItemCount(int playerItems)
    {
        _itemCountField.text = $"{playerItems}/{_amountOfItems}";
        if(playerItems == _amountOfItems)
        {
            Debug.Log("Player won, open portal");
            _portal.SetActive(true);
            //Tell game Manager PLayer won and open a portal that will make a loud initial sound, and will keep imiting sound while use ist in it
        }
    }

    public void ShowText(string text)
    {
        _queuePopUpText.Enqueue(text);
    }

    IEnumerator TypingTextCoroutine(string text)
    {
        _isTypingOnPopup = true;
        string displayedText = "";
        _popUpTextField.enabled = true;
        
        foreach(char c in text)
        {
            displayedText += c;
            _popUpTextField.text = displayedText;
            yield return new WaitForSeconds(_popupCharacterDisplayInterval);
        }
        float timeToDestroy = _durationBeforePopUpTextDestroy - _popupCharacterDisplayInterval;
        if(timeToDestroy > 0)
            yield return new WaitForSeconds(timeToDestroy);

        _popUpTextField.text = "";
        _popUpTextField.enabled = false;
        _isTypingOnPopup = false;
    }
}
