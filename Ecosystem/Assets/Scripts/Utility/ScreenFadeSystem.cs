using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Metatron.Utilities
{
    /// <summary>
    /// This class is a singleton class that lazy-loads itself when called from resources
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFadeSystem : MonoBehaviour
    {
        private static ScreenFadeSystem _instance;

        public enum State { Clear, Opaque }
        private static CanvasGroup _myCanvasGroup;

        public static async UniTask FadeAsync(State _type, float _duration)
        {
            PokeAwake();
            var startTime = Time.time;
            // before yield
            switch (_type)
            {
                case State.Opaque:
                    _myCanvasGroup.blocksRaycasts = true;
                    break;
            }
            // yield while
            while (Time.time < startTime + _duration)
            {
                if (_myCanvasGroup == null)
                {
                    Debug.LogWarning("ScreenFadeSystem.FadeAsync missing canvasGroup, exiting early");
                    return;
                }
                switch (_type)
                {
                    case State.Clear:
                        _myCanvasGroup.alpha = Mathf.Lerp(1, 0, Mathf.Clamp01((Time.time - startTime) / _duration));
                        break;
                    case State.Opaque:
                        _myCanvasGroup.alpha = Mathf.Lerp(0, 1, Mathf.Clamp01((Time.time - startTime) / _duration));
                        break;
                }
                await UniTask.Yield();
            }
            // after yield
            switch (_type)
            {
                case State.Clear:
                    _myCanvasGroup.alpha = 0;
                    _myCanvasGroup.blocksRaycasts = false;
                    break;
                case State.Opaque:
                    _myCanvasGroup.alpha = 1;
                    _myCanvasGroup.blocksRaycasts = true;
                    break;
            }
        }

        private static void PokeAwake()
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenFadeSystem");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenFadeSystem>();
                var canvas = (GameObject)GameObject.Instantiate(Resources.Load("CanvasScreenFade"), go.transform);
                _myCanvasGroup = canvas.GetComponentInChildren<CanvasGroup>();
            }
        }
    }
}