using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioSource openNewCharacterSource;
    [SerializeField] private AudioSource clickSource;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayOpenCharacter()
    {
        if (openNewCharacterSource != null)
        {
            openNewCharacterSource.Play();
            Debug.Log("Вызвали звук открытия персонажа");
        }
    }

    public void PlayClick()
    {
        if (clickSource != null)
        {
            clickSource.Play();
        }
    }
}