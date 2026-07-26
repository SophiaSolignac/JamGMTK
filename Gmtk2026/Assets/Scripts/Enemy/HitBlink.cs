using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author : Florian MAJCHER - Isart DIGITAL
// DATE : 00/00/2026 - Beginning of the class

namespace GMTK.Enemy
{
    
    public class HitBlink : MonoBehaviour
    {
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // VARIABLES
        [Header("Color Blink Settings")]
        [SerializeField] private Color _BlinkColor = Color.white;
        [SerializeField] private float _BlinkDuration = .1f;

        [Header("Punch Scale Settings")]
        [SerializeField] private bool _UseScalePunch = true;
        [SerializeField] private Vector3 _PunchScaleMultiplier = new Vector3(1.15f, 1.15f, 1.15f); // +15% de taille

        private Renderer[] _Renderers;
        private MaterialPropertyBlock _PropertyBlock;
        private Coroutine _BlinkCoroutine;
        private Vector3 _OriginalScale;

        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor"); 

        private void Awake()
        {
            _Renderers = GetComponentsInChildren<Renderer>();
            _PropertyBlock = new MaterialPropertyBlock();
            _OriginalScale = transform.localScale;
        }

        public void TriggerBlink()
        {
            if (_BlinkCoroutine != null)
            {
                StopCoroutine(_BlinkCoroutine);
                transform.localScale = _OriginalScale; 
            }

            _BlinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private IEnumerator BlinkRoutine()
        {
            if (_UseScalePunch)
                transform.localScale = Vector3.Scale(_OriginalScale, _PunchScaleMultiplier);
            
            _PropertyBlock.SetColor(ColorProperty, _BlinkColor);
            foreach (Renderer lT in _Renderers)
            {
                if (lT)
                    lT.SetPropertyBlock(_PropertyBlock);
            }

            yield return new WaitForSeconds(_BlinkDuration);

            _PropertyBlock.Clear();
            foreach (Renderer lT in _Renderers)
            {
                if (lT)
                    lT.SetPropertyBlock(_PropertyBlock);
            }

            if (_UseScalePunch)
                transform.localScale = _OriginalScale;

            _BlinkCoroutine = null;
        }
    }
}