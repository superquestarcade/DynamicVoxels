using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Metatron.Utilities
{
    public class RuntimeInitialiser : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Debug.Log("RuntimeInitialiser Started");

            foreach (var i in Resources.LoadAll("RuntimeInit"))
            {
                var go = GameObject.Instantiate((GameObject)i);
                Debug.Log($"RuntimeInitialiser Instantiated: {go.name}");
                go.GetComponent<IIRuntimeInitable>()?.Init();
            }

            Debug.Log("RuntimeInitialiser Finished");
        }
    }

    public interface IIRuntimeInitable
    {
        public void Init();
    }
}