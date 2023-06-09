using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;
    public Pool hitParticlePool;
    public Pool playerBulletPool;
    public Pool enemyArrowBulletPool;
    public Pool enemyBatteryBulletPool;
    public Pool enemyMissileBulletPool;
    public Pool explosionParticletPool;
    public Pool smallRedExplosionParticletPool;
    public Pool dummyEnemyPool;
    public Pool simpleEnemyPool;
    public Pool stalkerEnemyPool;
    public Pool laserTurretEnemyPool; 
    public Pool missileTurretEnemyPool;
    public Pool pulsePool;
    public Pool batteryPool;
    public Pool coinPool;
    public Pool batteryShardPool;
    public Pool shieldHitParticlePool;
    public Pool trackingBulletPool; 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public ParticleSystem GetHitParticle()
    {
        return hitParticlePool.GetItem().GetComponent<ParticleSystem>();
    }
    public Bullet GetPlayerBullet()
    {
        return playerBulletPool.GetItem().GetComponent<Bullet>();
    }
    public Bullet GetEnemyArrowBullet()
    {
        return enemyArrowBulletPool.GetItem().GetComponent<Bullet>();
    }
    public Bullet GetEnemyBatteryBullet()
    {
        return enemyBatteryBulletPool.GetItem().GetComponent<Bullet>();
    }
    public Bullet GetEnemyMissileBullet()
    {
        return enemyMissileBulletPool.GetItem().GetComponent<Bullet>();
    }
    public Bullet GetTrackingBullet()
    {
        return trackingBulletPool.GetItem().GetComponent<Bullet>();
    }
    public ParticleSystem GetExplosionParticles()
    {
        return explosionParticletPool.GetItem().GetComponent<ParticleSystem>();
    }
    
    public ParticleSystem GetSmallRedExplosionParticles()
    {
        return smallRedExplosionParticletPool.GetItem().GetComponent<ParticleSystem>();
    }
    public Enemy GetDummyEnemy()
    {
        return dummyEnemyPool.GetItem().GetComponent<Enemy>();
    }
    public Enemy GetSimpleEnemy()
    {
        return simpleEnemyPool.GetItem().GetComponent<Enemy>();
    }
    public Enemy GetStalkerEnemy()
    {
        return stalkerEnemyPool.GetItem().GetComponent<Enemy>();
    }
    public Enemy GetLaserTurretEnemy()
    {
        return laserTurretEnemyPool.GetItem().GetComponent<Enemy>();
    }
    public Enemy GetMissileTurretEnemy()
    {
        return missileTurretEnemyPool.GetItem().GetComponent<Enemy>();
    }
    public Pulse GetPulse()
    {
        return pulsePool.GetItem().GetComponent<Pulse>();
    }
    public Battery GetBattery()
    {
        return batteryPool.GetItem().GetComponent<Battery>();
    }


    public Coin GetCoin()
    {
        return coinPool.GetItem().GetComponent<Coin>();
    }
    public Coin GetBatteryShard()
    {
        return batteryShardPool.GetItem().GetComponent<Coin>();
    }
    public ParticleSystem GetShieldHitParticle()
    {
        return shieldHitParticlePool.GetItem().GetComponent<ParticleSystem>();
    }
}

