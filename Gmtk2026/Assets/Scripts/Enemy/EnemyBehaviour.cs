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
        [SerializeField] private LayerMask _PlayerMask, _ObstacleMask;
        
        private SphereCollider _CurrentSphereCollider;
        private PlayerController _PlayerController;
        
        private bool _IsPlayerInZone;
        private bool _HasDetectedPlayer;
        
        private Transform _PlayerTransform, _ObstacleTransform;

        public Action onPlayerDetected;
        public Action onPlayerLost;

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // READY
        private void Awake()
        {
            _CurrentSphereCollider = GetComponent<SphereCollider>();
            _CurrentSphereCollider.isTrigger = true;
            _CurrentSphereCollider.radius = _RadiusRange;
        }

        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // PROCESS
        private void Update()
        {
            if (_IsPlayerInZone && !_HasDetectedPlayer)
                CheckLineOfSight();
            
            if (_HasDetectedPlayer && _PlayerTransform)
                LookAtPlayer();
            
            Debug.Log(_PlayerTransform);
        }

        private void OnTriggerEnter(Collider pOther)
        {
            if (((1 << pOther.gameObject.layer) & _PlayerMask) == 0) 
                return;
            
            _PlayerTransform = pOther.transform;
            _IsPlayerInZone = true;
            onPlayerDetected?.Invoke();
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

        private void CheckLineOfSight()
        {
            if (_PlayerTransform == null) 
                return;

            Vector3 lDirection = (_PlayerTransform.position - transform.position).normalized;
            float lDistance = Vector3.Distance(transform.position, _PlayerTransform.position);

            if (Physics.Raycast(transform.position, lDirection, lDistance, _ObstacleMask));
            _HasDetectedPlayer = true;
            onPlayerDetected?.Invoke();
        }

        private void LookAtPlayer()
        {
            Vector3 lDirection = (_PlayerTransform.position - transform.position).normalized;

            if (lDirection.magnitude <= .0001f) return;
            
            Quaternion lTargetRotation = Quaternion.LookRotation(lDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lTargetRotation, Time.deltaTime * _RotationSpeed);
        }
        
        public void OnHit()
        {
            throw new NotImplementedException();
        }
    }
}