using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public class InputManager : MonoBehaviourSingleton<InputManager>
    {
        public Action<Vector2> OnMove;
        public Action<Vector2> OnLook;
        public Action<bool> OnJump;
        
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Move(InputAction.CallbackContext _callbackContext)
        {
            OnMove?.Invoke(_callbackContext.ReadValue<Vector2>());
        }
    
        public void Look(InputAction.CallbackContext _callbackContext)
        {
            OnLook?.Invoke(_callbackContext.ReadValue<Vector2>());
        }
    
        public void Jump(InputAction.CallbackContext _callbackContext)
        {
            if(_callbackContext.started) OnJump?.Invoke(true);
            if(_callbackContext.canceled) OnJump?.Invoke(false);
        }
    }
}
