using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField]private Transform _playerTransform, _enemyTransform;
    [SerializeField]private AudioSource _audioSourceTenseBuildUp;
    [SerializeField]private float _minimalDistanceToPlayTenseMusic = 12.0f, _buildUpStep = 4.0f;
    [SerializeField]private float _maxVolume = 1.0f, _minVolume = 0.2f;

    private bool _playingTenseMusic = false;
    private float _currentPitch = 1f;
    private float _targetPitch = 1f;
    private float _currentVolume = 0.5f;
    private float _targetVolume = 0.5f;

    void Start()
    {
        if (_playerTransform == null || _enemyTransform == null)
        {
            Debug.LogError("Player or Enemy Transform are not assigned in the Inspector.");
        }
        if(_audioSourceTenseBuildUp == null)
        {
            Debug.LogError("Tense Buildup Audio Source is not assigned in the Inspector.");
        }
        else
        {
            _audioSourceTenseBuildUp.enabled = false;
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(_playerTransform.position, _enemyTransform.position);
        
        if (distance <= _minimalDistanceToPlayTenseMusic && !_playingTenseMusic)
        {
            EnableTenseMusic();
        }
        else if (distance > _minimalDistanceToPlayTenseMusic && _playingTenseMusic)
        {
            DisableTenseMusic();
        }
        
        // Adjust playback speed based on distance for a more dynamic effect
        if (distance <= _minimalDistanceToPlayTenseMusic)
        {
            float pitchFactor = 1 - ((distance - _minimalDistanceToPlayTenseMusic) / _buildUpStep) * 0.1f;
            _targetPitch = Mathf.Clamp(pitchFactor, 0.8f, 1.6f);
            
            // Calculate volume - closer = louder
            float volumeFactor = 1 - (distance / _minimalDistanceToPlayTenseMusic);
            _targetVolume = Mathf.Lerp(_minVolume, _maxVolume, volumeFactor);
        }
        else
        {
            _targetPitch = 1f;
            _targetVolume = _minVolume;
        }
        
        // Smooth transitions
        _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, Time.deltaTime * 2f);
        _currentVolume = Mathf.Lerp(_currentVolume, _targetVolume, Time.deltaTime * 2f);
        
        if (_playingTenseMusic)
        {
            _audioSourceTenseBuildUp.pitch = _currentPitch;
            _audioSourceTenseBuildUp.volume = _currentVolume;
        }
    }
    
    private void EnableTenseMusic()
    {
        _audioSourceTenseBuildUp.enabled = true;
        _playingTenseMusic = true;
    }
    
    private void DisableTenseMusic()
    {
        _audioSourceTenseBuildUp.enabled = false;
        _playingTenseMusic = false;
    }
}
