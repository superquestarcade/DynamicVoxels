using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metatron.Utilities
{
    public class SceneTransition : MonoBehaviour
    {
        public static Action GlobalPreFade;
        public static Action GlobalPreload;
        public static Action GlobalPostLoad;
        public static Action GlobalPostClear;

        /// <summary>
        /// This Init method warms the system for the first time load, actions invoked here are for the benefit of other subscribed systems
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private async void Init()
        {
            Debug.Log("SceneTransitionManager Init Start");
            await UniTask.Yield(); // give it a frame to get ready, helps a lot with the jumble of awake/start methods
            GlobalPreload?.Invoke();
            GlobalPreFade?.Invoke();
            GlobalPostLoad?.Invoke();
            await ScreenFadeSystem.FadeAsync(ScreenFadeSystem.State.Clear, 0.35f);
            GlobalPostClear?.Invoke();
            Debug.Log("SceneTransitionManager Init Finished");
        }

        public static async UniTask TransitionSceneAsync(string _primary, string[] _secondaries = null)
        {
            GlobalPreFade?.Invoke();
            await ScreenFadeSystem.FadeAsync(ScreenFadeSystem.State.Opaque, 0.5f);
            GlobalPreload?.Invoke();
            await SceneManager.LoadSceneAsync(_primary, LoadSceneMode.Single);
            if (_secondaries != null)
            {
                var additives = new UniTask[_secondaries.Length];
                for (var i = 0; i < _secondaries.Length; i++)
                    additives[i] = SceneManager.LoadSceneAsync(_secondaries[i], LoadSceneMode.Additive).ToUniTask();
                await UniTask.WhenAll(additives);
            }

            await UniTask.Delay(1000); // delay for 1 second, makes the audio transition less jarring
            GlobalPostLoad?.Invoke();
            await ScreenFadeSystem.FadeAsync(ScreenFadeSystem.State.Clear, 0.5f);
            GlobalPostClear?.Invoke();
        }
    }
}