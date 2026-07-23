using UnityEngine;
using UnityEngine.Events;

namespace GMTK.Inputs
{
    public partial class InputManager : PersistentSingleton<InputManager>
    {
        public static UnityEvent<Vector3> onMove { get; private set; }  = new();

        public static UnityEvent<Vector2> onLook { get; private set; } = new();

        public static UnityEvent<bool> onJump { get; private set; } = new();

        public static UnityEvent onInteract { get; private set; } = new();

        public static UnityEvent onAttack { get; private set; } = new();
    }
}