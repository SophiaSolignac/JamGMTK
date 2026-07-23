using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK.Inputs
{
    public partial class InputManager : PersistentSingleton<InputManager>
    {
        public void OnMove(InputAction.CallbackContext context)
        {
            Vector3 direction = context.ReadValue<Vector2>();
            direction.z = direction.y;
            direction.y = 0;

            onMove.Invoke(direction);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            onLook.Invoke(context.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started || context.canceled)
                onJump.Invoke(context.started);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.started || context.canceled)
                onAttack.Invoke(context.started);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started) onInteract.Invoke();
        }

        public void OnDrop(InputAction.CallbackContext context)
        {
            if (context.started) onDrop.Invoke();
        }
    }
}