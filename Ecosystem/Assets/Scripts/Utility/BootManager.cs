using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metatron.Utilities
{
    public class BootManager : MonoBehaviour
    {
        [FormerlySerializedAs("MainMenuSceneName")] public string mainMenuSceneName = "Menu";

        public void Awake()
        {
            SceneTransition.TransitionSceneAsync(mainMenuSceneName).Forget();
        }
    }
}