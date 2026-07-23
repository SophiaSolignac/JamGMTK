using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author : Florian MAJCHER - Isart DIGITAL
// DATE : 00/00/2026 - Beginning of the class

namespace GMTK.Enemy
{
    [RequireComponent(typeof(SphereCollider))]
    public class EnemyBehaviour : MonoBehaviour, I_BulletOrRaycastTarget
    {
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // VARIABLES
        [SerializeField] private float _RadiusRange;
        [SerializeField] private float _RotationSpeed = 5f;
        [SerializeField] private float _DetectionCheckInterval = .1f;
        [SerializeField] private LayerMask _PlayerMask, _ObstacleMask;

        private const float MIN_MAGNITUDE = .0001f;
        
        private SphereCollider _CurrentSphereCollider;
        private PlayerController _PlayerController;
        
        private bool _IsPlayerInZone;
        private bool _HasDetectedPlayer;
        
        private Transform _PlayerTransform, _ObstacleTransform;
        private Coroutine _CheckSightCoroutine, _LookAtCoroutine;

        public Action onPlayerDetected, onPlayerLost;
        public Action<bool> onTryUseItem;

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // READY
        private void Awake()
        {
            _CurrentSphereCollider = GetComponent<SphereCollider>();
            _CurrentSphereCollider.isTrigger = true;
            _CurrentSphereCollider.radius = _RadiusRange;
        }
        
        public void RequestUseItem(bool pStarted) => onTryUseItem?.Invoke(pStarted);
        
        private void OnTriggerEnter(Collider pOther)
        {
            if (((1 << pOther.gameObject.layer) & _PlayerMask) == 0) 
                return;
            
            _PlayerTransform = pOther.transform;
            _IsPlayerInZone = true;
            
            if (_CheckSightCoroutine == null && !_HasDetectedPlayer)
                _CheckSightCoroutine = StartCoroutine(CheckLineOfSightRoutine());
        }

        private void OnTriggerExit(Collider pOther)
        {
            if (pOther.transform != _PlayerTransform) 
                return;
            
            _IsPlayerInZone = false;
            _PlayerTransform = null;
            _HasDetectedPlayer = false;
            onPlayerLost?.Invoke();
        }
        
        private IEnumerator CheckLineOfSightRoutine()
        {
            WaitForSeconds lWait = new WaitForSeconds(_DetectionCheckInterval);

            while (_IsPlayerInZone && !_HasDetectedPlayer)
            {
                CheckLineOfSight();
                yield return lWait;
            }

            _CheckSightCoroutine = null;
        }

        private void CheckLineOfSight()
        {
            if (_PlayerTransform == null) 
                return;

            Vector3 lDirection = (_PlayerTransform.position - transform.position).normalized;
            float lDistance = Vector3.Distance(transform.position, _PlayerTransform.position);

            if (Physics.Raycast(transform.position, lDirection, lDistance, _ObstacleMask)) 
                return;
            
            _HasDetectedPlayer = true;
            onPlayerDetected?.Invoke();

            _LookAtCoroutine ??= StartCoroutine(LookAtPlayerCoroutine());
        }

        private IEnumerator LookAtPlayerCoroutine()
        {
            while (_HasDetectedPlayer && _PlayerTransform)
            {
                Vector3 lDirection = (_PlayerTransform.position - transform.position).normalized;

                if (!(lDirection.magnitude <= MIN_MAGNITUDE)) 
                    continue;
                
                Quaternion lTargetRotation = Quaternion.LookRotation(lDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lTargetRotation, Time.deltaTime * _RotationSpeed);
            }

            yield return null;
        }
        
        private void StopAllDetectionCoroutines()
        {
            if (_CheckSightCoroutine != null)
            {
                StopCoroutine(_CheckSightCoroutine);
                _CheckSightCoroutine = null;
            }

            if (_LookAtCoroutine != null)
            {
                StopCoroutine(_LookAtCoroutine);
                _LookAtCoroutine = null;
            }
        }

        private void OnDisable()
        {
            StopAllDetectionCoroutines();
        }

        public void OnHit()
        {
            throw new NotImplementedException();
        }
    }
}