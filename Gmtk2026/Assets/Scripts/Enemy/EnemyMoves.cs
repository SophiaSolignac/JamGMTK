using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author : Florian MAJCHER - Isart DIGITAL
// DATE : 00/00/2026 - Beginning of the class

namespace GMTK.Enemy
{
    
    public static class EnemyMoves
    {
        // ----------------~~~~~~~~~~~~~~~~~~~==========================# // VARIABLES
        
        public static Vector3 LerpBetweenPositions(Vector3 pInitialPos, Vector3 pTarget, float pTime) 
            => Vector3.Lerp(pInitialPos, pTarget, pTime);
        
        public static Vector3 CircularMovement(Vector3 pCenter, float pAngle, float pRadius)
        {
            Vector3 lOffset = PolarToCartesianOnZAxis(pAngle, pRadius);
            return pCenter + lOffset;
        }

        public static Vector3 PolarToCartesianOnZAxis(float pAngle, float pRadius)
            => new Vector3(Mathf.Cos(pAngle), 0f, Mathf.Sin(pAngle)) * pRadius;
    }
}