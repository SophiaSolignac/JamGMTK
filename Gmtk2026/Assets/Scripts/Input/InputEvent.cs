using UnityEngine;
using UnityEngine.Events;

namespace GMTK.Inputs
{
    public partial class InputManager : MonoBehaviour
    {
        public static UnityEvent<Vector3> onMove { get; private set; }  = new();

        public static UnityEvent<Vector2> onLook { get; private set; } = new();

        public static UnityEvent<bool> onJump { get; private set; } = new();

        public static UnityEvent onInteract { get; private set; } = new();
        
        public static UnityEvent onDrop { get; private set; } = new();

        public static UnityEvent<bool> onAttack { get; private set; } = new();
    }
}