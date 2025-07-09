using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class ScreenFader : MonoBehaviour
{
        #region Singleton µ¥Àý
        public static ScreenFader I;
        private void Awake()
        {
            I = this;
        }
        #endregion

        [SerializeField] private Image fadeImage;
        public const float fadeOutTime = 1f;
        public const float fadeInTime = 1f;

        public IEnumerator FadeOut(float duration = fadeOutTime)
        {
            Debug.Log("µ­³ö");
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
            Color color = fadeImage.color;
            color.a = Mathf.Lerp(0f, 1f, time / duration);
            fadeImage.color = color;
            yield return null;
            }
        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;
    }

        public IEnumerator FadeIn(float duration = fadeInTime)
        {
            Debug.Log("µ­Èë");
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
            Color color = fadeImage.color;
            color.a = Mathf.Lerp(1f, 0f, time / duration);
            fadeImage.color = color;
            yield return null;
            }
            Color finalColor = fadeImage.color;
            finalColor.a = 0f;
            fadeImage.color = finalColor;
    }
}
