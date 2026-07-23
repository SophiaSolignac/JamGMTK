using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author : Florian MAJCHER - Isart DIGITAL
// DATE : 00/00/2026 - Beginning of the class

namespace GMTK.Enemy
{
    [RequireComponent(typeof(EnemyBehaviour))]
    public class EnemyItemHolder : ItemHolder
    {
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // READY
        private EnemyBehaviour _CurrentEnemy;
        
        protected override void Awake()
        {
            base.Awake();
            
            _CurrentEnemy = GetComponent<EnemyBehaviour>();
            
            if (_CurrentEnemy != null)
                _CurrentEnemy.onTryUseItem += OnUseItemRequested;
        }
        
        private void OnUseItemRequested(bool pStarted) => TryUseItem(pStarted);
        
        private void OnDestroy()
        {
            if (_CurrentEnemy != null)
                _CurrentEnemy.onTryUseItem -= OnUseItemRequested;
        }
        
    }
}