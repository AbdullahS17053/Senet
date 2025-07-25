using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject scrollPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [SerializeField] private GameObject teachingsPanel;
    
    [Header("Tap Effect Settings")]
    [SerializeField] private float scaleAmount = 1.1f;
    [SerializeField] private float scaleDuration = 0.1f;
    [SerializeField] private float afterEffectDelay = 0.1f;

    [SerializeField] private GameObject stickThrower;
    
    public static GameManager Instance;

    public void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // Prevent duplicates
    }

    public void PlayBtn()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            SceneManager.LoadScene(1);
        }));
    }

    public void ScrollBtn()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            scrollPanel.SetActive(true);
        }));
            
    }

    public void TeachingsBtn()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            teachingsPanel.SetActive(true);
        }));
    }

    public void BackBtn()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            scrollPanel.SetActive(false);
        }));
    }
    public IEnumerator PlayTapEffect(System.Action onComplete)
    {
        GameObject clicked = EventSystem.current.currentSelectedGameObject;
        if (clicked == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Transform transform = clicked.transform;

        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * scaleAmount;

        float timer = 0f;
        while (timer < scaleDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / scaleDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = targetScale;

        timer = 0f;
        while (timer < scaleDuration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / scaleDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = originalScale;

        yield return new WaitForSeconds(afterEffectDelay);

        onComplete?.Invoke();
    }

    public void CloseTeachingsPanel()
    {
        StartCoroutine(PlayTapEffect(() =>
        {
            teachingsPanel.SetActive(false);
        }));
    }

    public void MenuBtn()
    {
        SceneManager.LoadScene(0);
    }

    public void ReplayBtn()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void CheckForWinCondition()
    {
        int playerCount = 0;
        int aiCount = 0;

        PieceMover[] allPieces = FindObjectsOfType<PieceMover>();
        foreach (var piece in allPieces)
        {
            if (piece.isAI) aiCount++;
            else playerCount++;
        }

        if (aiCount == 1)
        {
            Debug.Log("AI Wins!");
            if (losePanel != null)
            {
                losePanel.SetActive(true);
                stickThrower.SetActive(false);
            }
        }
        else if (playerCount == 1)
        {
            Debug.Log("Player Wins!");
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                stickThrower.SetActive(false);
            }
        }
    }
}



