using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject scrollPanel;
    public void PlayBtn()
    {
        SceneManager.LoadScene(1);
    }

    public void ScrollBtn()
    {
        scrollPanel.SetActive(true);
    }

    public void BackBtn()
    {
        scrollPanel.SetActive(false);
    }
}
