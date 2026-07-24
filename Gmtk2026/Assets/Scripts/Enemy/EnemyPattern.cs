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

            Quaternion lOriginalRotation = pCurrentEnemy.transform.rotation;
            pCurrentEnemy.transform.Rotate(0f, pAngleOffset, 0f, Space.World);
            pCurrentEnemy.ClassicShoot();
            pCurrentEnemy.transform.rotation = lOriginalRotation;
        }

        public static Coroutine CorkscrewShoot(this EnemyBehaviour pCurrentEnemy, int pTotalShots = 15, float pRadius = .6f, float pAngleStep = 24f, float pDelay = 0.05f) 
            => pCurrentEnemy.StartCoroutine(CorkscrewShootRoutine(pCurrentEnemy, pTotalShots, pRadius, pAngleStep, pDelay));

        public static IEnumerator CorkscrewShootRoutine(EnemyBehaviour pEnemy, int pTotalShots, float pRadius, float pAngleStep, float pDelay)
        {
            if (!pEnemy) yield break;

            WaitForSeconds lWait = new WaitForSeconds(pDelay);
            float lCurrentAngle = 0f;

            for (int i = 0; i < pTotalShots; i++)
            {
                if (!pEnemy || !pEnemy.gameObject.activeInHierarchy) 
                    yield break;

                Vector3 lTargetDirection = GetSpiralOffsetDirection(pEnemy.transform, lCurrentAngle, pRadius);
                Quaternion lOriginalRotation = pEnemy.transform.rotation;
                pEnemy.transform.rotation = Quaternion.LookRotation(lTargetDirection);
                pEnemy.ClassicShoot();
                pEnemy.transform.rotation = lOriginalRotation;
                lCurrentAngle += pAngleStep;
                yield return lWait;
            }
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

        private static Vector3 GetSpiralOffsetDirection(Transform pEnemyTransform, float pAngleInDegrees, float pRadius = 0.5f)
        {
            float lRad = pAngleInDegrees * Mathf.Deg2Rad;
            Vector3 lLocalDirection = new Vector3(Mathf.Cos(lRad) * pRadius, Mathf.Sin(lRad) * pRadius, 1f).normalized;
            return pEnemyTransform.TransformDirection(lLocalDirection);
        }
    }
}