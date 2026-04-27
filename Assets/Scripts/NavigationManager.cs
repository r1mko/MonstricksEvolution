using UnityEngine;
using UnityEngine.UI;
using YG;

public class NavigationManager : MonoBehaviour
{
    [SerializeField] private Button storeButton;
    [SerializeField] private Button collectionButton;
    [SerializeField] private GameObject storeScreen;
    [SerializeField] private GameObject collectionScreen;

    private void Awake()
    {
        if (storeButton != null)
            storeButton.onClick.AddListener(OpenStore);
        
        if (collectionButton != null)
            collectionButton.onClick.AddListener(OpenCollection);
    }

    private void OnDestroy()
    {
        if (storeButton != null)
            storeButton.onClick.RemoveAllListeners();

        if (collectionButton != null)
            collectionButton.onClick.RemoveAllListeners();
    }

    private void OpenStore()
    {
        YG2.InterstitialAdvShow();

        if (storeScreen != null) storeScreen.SetActive(true);
        if (collectionScreen != null) collectionScreen.SetActive(false);
    }

    private void OpenCollection()
    {
        YG2.InterstitialAdvShow();

        if (storeScreen != null) storeScreen.SetActive(false);
        if (collectionScreen != null) collectionScreen.SetActive(true);
    }
}