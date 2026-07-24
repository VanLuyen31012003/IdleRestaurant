using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    [SerializeField] private float loadingTime = 2f;
    [SerializeField] private Text progressText;
    [SerializeField] private Image progressFill;

    private IEnumerator Start()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("Game");
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (timer < loadingTime)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / loadingTime);

            progressText.text = Mathf.RoundToInt(progress * 100) + "%";
            progressFill.fillAmount = progress;

            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}