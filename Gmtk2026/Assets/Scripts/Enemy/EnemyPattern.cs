using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

// Author : Florian MAJCHER - Isart DIGITAL
// DATE : 00/00/2026 - Beginning of the class

namespace GMTK.Enemy
{
    
    public static class EnemyPattern
    {
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // VARIABLES
        public static UnityEvent<bool> OnItemUse;

        private const int NUMBER_SHOTS = 3;
        private const float DELAY_SHOTS = .15f;

        public static void ClassicShoot(this EnemyBehaviour pCurrentEnemy)
        {
            if (pCurrentEnemy == null) 
                return;

            pCurrentEnemy.RequestUseItem(true);
            pCurrentEnemy.RequestUseItem(false);
        }

        public static Coroutine TripleShootWithDelay(this EnemyBehaviour pCurrentEnemy, float pDelayBetweenShoots = DELAY_SHOTS) => pCurrentEnemy.StartCoroutine(TripleShootRoutine(pCurrentEnemy, pDelayBetweenShoots));
        
        private static IEnumerator TripleShootRoutine(EnemyBehaviour pEnemy, float pDelay)
        {
            WaitForSeconds lWait = new WaitForSeconds(pDelay);
            const float lSpreadAngle = 15f;

            for (int i = 0; i < NUMBER_SHOTS; i++)
            {
                if (!pEnemy || !pEnemy.gameObject.activeInHierarchy) 
                    yield break;
                
                float lCurrentAngle = (i - 1) * lSpreadAngle;
                
                pEnemy.ClassicShootWithAngle(lCurrentAngle);
                yield return lWait;
            }
        }
        
        public static void ClassicShootWithAngle(this EnemyBehaviour pCurrentEnemy, float pAngleOffset)
        {
            if (!pCurrentEnemy) return;

            pCurrentEnemy.transform.Rotate(0f, pAngleOffset, 0f, Space.World);
            pCurrentEnemy.ClassicShoot();
        }

        public static void StartContinuousShoot(this EnemyBehaviour pCurrentEnemy) => pCurrentEnemy?.RequestUseItem(true);
        
        public static void StopContinuousShoot(this EnemyBehaviour pCurrentEnemy) => pCurrentEnemy?.RequestUseItem(false);
        
        public static void ForwardAndBackwardMovement()
        {
            
        }

        public static void SinusMovementOnAnAxis(this EnemyBehaviour pCurrentEnemy, Vector3 pCurrentDirection, Vector3 pAxis)
        {
            
        }
        
        public static void JumpMovement()
        {
            
        }
    }
}