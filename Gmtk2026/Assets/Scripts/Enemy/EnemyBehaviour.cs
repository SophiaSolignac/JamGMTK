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
        [SerializeField] private float _RotationSpeed = 5f;
        [SerializeField] private float _DetectionCheckInterval = .1f;
        [SerializeField] private LayerMask _PlayerMask, _ObstacleMask;
        [SerializeField] private E_EnemyType _EnemyType;
        [SerializeField] private E_ShootType _ShootType;
        
        [Header("Health & Defense Settings")]
        [SerializeField] private int _MaxHealth = 100;
        [SerializeField] private int _BaseDamage = 25;
        [SerializeField] [Range(0f, 2f)] private float _DamageTakenMultiplier = 1f;

        private const float MIN_MAGNITUDE = .0001f;
        
        private int _CurrentHealth;
        
        private SphereCollider _CurrentSphereCollider;
        private PlayerController _PlayerController;
        
        private bool _IsPlayerInZone;
        private bool _HasDetectedPlayer;
        
        private Transform _PlayerTransform, _ObstacleTransform;
        private Coroutine _CheckSightCoroutine, _LookAtCoroutine, _ShootingCoroutine;

        private ParticleSystem _ExplosionParticles;
        
        private bool _IsDead;

        public Action onPlayerDetected, onPlayerLost;
        public Action<bool> onTryUseItem;
        public Action<int, int> onHealthChanged;
        public Action onDeath;

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // READY
        private void Awake()
        {
            _CurrentHealth = _MaxHealth;
            
            _CurrentSphereCollider = GetComponent<SphereCollider>();
            _CurrentSphereCollider.isTrigger = true;
            
            onPlayerDetected += StartShootingLoop;
            onPlayerLost += StopShootingLoop;
            onDeath += HandleDeath;
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
                    case E_ShootType.TRIPLE_ARC_ANGLE:
                        yield return this.TripleShootWithDelay();
                        break;
                    case E_ShootType.SPIRAL:
                        yield return this.CorkscrewShoot();
                        break;
                    case E_ShootType.TRIPLE_SPRAY:
                        yield return this.TripleShootWithSpread();
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
        
        public void OnHit(int damage)
        {
            int lFinalDamage = Mathf.RoundToInt(_BaseDamage * _DamageTakenMultiplier);
            TakeDamage(lFinalDamage);
        }

        public void TakeDamage(int pAmount)
        {
            if (_CurrentHealth <= 0) return;

            _CurrentHealth -= pAmount;
            onHealthChanged?.Invoke(_CurrentHealth, _MaxHealth);
            
            if (_CurrentHealth > 0) 
                return;
            
            _CurrentHealth = 0;
            HandleDeath();
        }

        private void HandleDeath()
        {
            if (_IsDead) 
                return;
            
            StopAllDetectionCoroutines();
            StopShootingLoop(); 
            
            if (_CurrentSphereCollider) 
                _CurrentSphereCollider.enabled = false;

            _ExplosionParticles?.Play();
            Destroy(gameObject);
        }
    }
}