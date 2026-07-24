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
        [Header("Shooting Settings")]
        [SerializeField] private float _ShootInterval = 2f;
        [SerializeField] private float _InitialShootDelay = 0.5f;
        
        [Header("General Settings")]
        [SerializeField] private float _RadiusRange;
        [SerializeField] private float _RotationSpeed = 5f;
        [SerializeField] private float _DetectionCheckInterval = .1f;
        [SerializeField] private LayerMask _PlayerMask, _ObstacleMask;
        [SerializeField] private E_EnemyType _EnemyType;
        [SerializeField] private E_ShootType _ShootType;

        private const float MIN_MAGNITUDE = .0001f;
        
        private SphereCollider _CurrentSphereCollider;
        private PlayerController _PlayerController;
        
        private bool _IsPlayerInZone;
        private bool _HasDetectedPlayer;
        
        private Transform _PlayerTransform, _ObstacleTransform;
        private Coroutine _CheckSightCoroutine, _LookAtCoroutine, _ShootingCoroutine;

        private ParticleSystem _ExplosionParticles;

        public Action onPlayerDetected, onPlayerLost;
        public Action<bool> onTryUseItem;

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // READY
        private void Awake()
        {
            _CurrentSphereCollider = GetComponent<SphereCollider>();
            _CurrentSphereCollider.isTrigger = true;
            _CurrentSphereCollider.radius = _RadiusRange;
            
            onPlayerDetected += StartShootingLoop;
            onPlayerLost += StopShootingLoop;
        }
        
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // SHOOT LOOP
        private void StartShootingLoop() => _ShootingCoroutine ??= StartCoroutine(ShootingRoutine(_ShootType));
        
        private void StopShootingLoop()
        {
            if (_ShootingCoroutine == null) 
                return;
            
            StopCoroutine(_ShootingCoroutine);
            _ShootingCoroutine = null;
        }

        private IEnumerator ShootingRoutine(E_ShootType pShootType)
        {
            yield return new WaitForSeconds(_InitialShootDelay);
            
            WaitForSeconds lWaitInterval = new WaitForSeconds(Mathf.Max(.05f, _ShootInterval));

            while (_HasDetectedPlayer)
            {
                switch (pShootType)
                {
                    case E_ShootType.CLASSIC:
                        this.ClassicShoot();
                        break;
                    case E_ShootType.TRIPLE:
                        yield return this.TripleShootWithDelay();
                        break;
                    case E_ShootType.SPIRAL:
                        yield return this.CorkscrewShoot();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(pShootType), pShootType, null);
                }
                yield return lWaitInterval;
            }

            _ShootingCoroutine = null;
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
                Vector3 lDirection = _PlayerTransform.position - transform.position;

                if (lDirection.sqrMagnitude > MIN_MAGNITUDE)
                {
                    Quaternion lTargetRotation = Quaternion.LookRotation(lDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lTargetRotation, Time.deltaTime * _RotationSpeed);
                }

                yield return null;
            }

            _LookAtCoroutine = null;
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

        private void OnDisable() => StopAllDetectionCoroutines();
        
        private void OnDestroy()
        {
            onPlayerDetected -= StartShootingLoop;
            onPlayerLost -= StopShootingLoop;
            
            StopAllDetectionCoroutines();
        }
        
        public void OnHit()
        {
            _ExplosionParticles?.Play();
            Destroy(gameObject);
        }
    }
}