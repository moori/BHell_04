using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using DG.Tweening;
using System;

public class Player : MonoBehaviour
{
    private HealthComponent healthComponent;

    [Header("Movement")]
    public float speed=9f;
    public float slowSpeed=3f;
    public float currentSpeed;
    private Vector3 inputDir;
    public float horizontalBoundarie;
    public float verticalBoundarie;

    [Header("Weapon")]
    //public BulletPool bulletPool;
    public float delayBetweenShots; //0.0375f
    public static float BASE_INV_FIRERATE = 0.05f;
    public static int MAX_FIRERATE_UNLOCKS = 5;
    public static float INV_FIRERATE_UPGRADE_FACTOR = 0.8f;
    private float timeLastShot;
    public float minAngleError = 15;
    public Transform turret;
    public List<Vector3> lastMoveDirections = new List<Vector3>();
    public int lastMoveDirectionsAmount = 10;
    private int moveDirIndex = 0;
    private bool isShooting;
    public static int BASE_MAX_AMMO = 500;
    public static int ABSOLUTE_MAX_AMMO = 4000;
    public int maxAmmo = 500;
    public int ammo;
    Collider2D[] hitsBuffer = new Collider2D[10];
    public int shotsFiredLastSecond;
    private int shotsFiredBonus;
    public int firerateUpgradeRef=8;
    private int evenShots;
    private Vector3 aimReference;
    public float aimReferenceMagnitude;

    public UnityEvent<int> OnShoot;
    public UnityEvent<int,int> OnAmmoChange;
    public UnityEvent<float> OnRecharge;
    public UnityEvent OnDie;

    [Header("Bubble")]
    public float bubbleRadius;

    internal void ApplyUpgrade(UpgradeShip.UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeShip.UpgradeType.Firerate:
                UpgradeFirerate();
                break;
            case UpgradeShip.UpgradeType.MaxHP:
                UpgradeMaxHP();
                break;
            case UpgradeShip.UpgradeType.ShieldSlot:
                UpgradeShield();
                break;
            default:
                break;
        }
    }

    public float bubbleDuration;
    public float shieldRechargeDuration;
    public bool isBubbling;
    public SpriteRenderer bubbleSprite;
    public SpriteRenderer bubbleShadowSprite;
    public LayerMask collectibleLayerMask;
    public List<ShieldCounter> shieldCounters;
    public GameObject shieldIndicator;
    private float bubbleStartTime;
    public UnityEvent OnShieldReady;
    private bool canActivateShield => shieldCounters.Any(x => x.isFull);

    [Header("Coins")]
    public float coinAttractionRadius;
    public float coinCollectionRadius;
    public float coinAttractionSpeed;
    public LayerMask coinCollectibleLayerMask;
    Collider2D[] coinHitsBuffer = new Collider2D[20];
    public int coins = 0;
    public UnityEvent<int> OnCollectCoin;

    [Header("Health")]
    public float timeLastDamage;
    public float damageCooldown=1f;
    private bool canTakeDamage;

    [Header("Audio")]
    public AudioSource fireSrc;
    public AudioSource audidoSrc;
    public AudioClip collectCoinClip;
    public AudioClip damageClip;


    [Header("Upgrades")]
    private float maxHPUpgradePercent = .2f;
    public int maxHP_UpgradesUnlocked = 0;
    public static int max_maxHP_UpgradesUnlocked = 8;
    public int fireRate_UpgradeUnlocks = 0;
    public static int max_fireRate_UpgradeUnlocks = 5;
    public int bubbleSlot_UpgradeUnlocks = 1;
    public static int max_bubbleSlot_UpgradeUnlocks = 5;

    private void Awake()
    {
        for (int i = 0; i < lastMoveDirectionsAmount; i++)
        {
            lastMoveDirections.Add(Vector3.down);
        }

        ammo = maxAmmo; 
        bubbleSprite.transform.localScale = Vector3.zero;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 90;

        healthComponent = GetComponent<HealthComponent>(); 
        SetShieldCounter();

        aimReference = transform.position + (Vector3.up * aimReferenceMagnitude);

        //InvokeRepeating("Measure", 1f, 1f);
    }


    void Update()
    {
        HandleTurret();
        HandleMovement();
        HandleBubble();
        HandleShot();

        if(Time.time-timeLastDamage >= damageCooldown)
        {
            canTakeDamage = true;
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            UpgradeMaxHP();
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            UpgradeFirerate();
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            UpgradeShield();
        }
#endif
    }
    public void Measure()
    {
        Debug.Log(shotsFiredLastSecond);
        shotsFiredLastSecond = 0;
    }

    private void FixedUpdate()
    {
        if (isBubbling)
        {
            HandleFixedBubble();
        }

        HandleFixedCoins();
    }

    [ContextMenu("UpgradeMaxHP")]
    public void UpgradeMaxHP()
    {
        maxHP_UpgradesUnlocked++;
        maxAmmo = Mathf.RoundToInt(BASE_MAX_AMMO * (1f + (maxHP_UpgradesUnlocked * maxHPUpgradePercent)));
        maxAmmo = Mathf.Clamp(maxAmmo, BASE_MAX_AMMO, ABSOLUTE_MAX_AMMO);
        Recharge(1f);
    }

    [ContextMenu("UpgradeFirerate")]
    public void UpgradeFirerate()
    {
        fireRate_UpgradeUnlocks = Mathf.Clamp(fireRate_UpgradeUnlocks + 1, 0, 5);
    }
    [ContextMenu("UpgradeShield")]
    public void UpgradeShield()
    {
        bubbleSlot_UpgradeUnlocks = Mathf.Clamp(bubbleSlot_UpgradeUnlocks + 1, 1, shieldCounters.Count-1);

        for (int i = 1; i < bubbleSlot_UpgradeUnlocks+1; i++)
        {
            shieldCounters[i].gameObject.SetActive(true);
        }
    }


    private void HandleFixedBubble()
    {
        int numHits = Physics2D.OverlapCircleNonAlloc(transform.position, bubbleRadius, hitsBuffer, collectibleLayerMask.value);
        for (int i = 0; i < numHits; i++)
        {
            if (hitsBuffer[i].gameObject.TryGetComponent<Battery>(out var battery))
            {
                Recharge(battery.percentCharge * BASE_MAX_AMMO/maxAmmo);
                battery.Collect();
            }

            if (hitsBuffer[i].gameObject.TryGetComponent<Bullet>(out var enemyBullet))
            {
                enemyBullet.Kill();
                var p = PoolManager.instance.GetShieldHitParticle();
                p.transform.position = enemyBullet.transform.position;
                p.transform.forward = -enemyBullet.transform.up;
                p.gameObject.SetActive(true);
                p.Play();
            }
        }
    }
    private void HandleFixedCoins()
    {
        int numHits = Physics2D.OverlapCircleNonAlloc(transform.position, coinAttractionRadius, coinHitsBuffer, coinCollectibleLayerMask.value);
        for (int i = 0; i < numHits; i++)
        {
            if (coinHitsBuffer[i].gameObject.TryGetComponent<Coin>(out var coin))
            {
                var distance = Vector3.Distance(transform.position, coin.transform.position);
                if (distance <= coinCollectionRadius)
                {
                    coin.Kill();
                    CollectCoin();
                    continue;
                }

                var attractionDir = (transform.position - coin.transform.position).normalized;
                coin.transform.position += attractionDir * coinAttractionSpeed * Mathf.Lerp(3,1, distance/coinAttractionRadius ) * Time.fixedDeltaTime;
            }
        }
    }

    private void CollectCoin()
    {
        coins++;
        audidoSrc.PlayOneShot(collectCoinClip);
        OnCollectCoin?.Invoke(coins);
    }

    public void Recharge(float percent)
    {
        ammo = Mathf.Clamp(ammo + Mathf.RoundToInt(maxAmmo * percent), 0, maxAmmo);
        OnRecharge?.Invoke(percent);
        OnAmmoChange?.Invoke(ammo,maxAmmo);
    }

    private void HandleMovement()
    {
        inputDir = new Vector2(PlayerInputs.Horizontal, PlayerInputs.Vertical).normalized;
        currentSpeed = PlayerInputs.Slow ? slowSpeed : speed;
        var moveVec = inputDir * currentSpeed * Time.deltaTime;
        var pos = transform.position + new Vector3(moveVec.x, moveVec.y, 0);

        //pos = new Vector3(Mathf.Clamp(pos.x, -horizontalBoundarie, horizontalBoundarie), Mathf.Clamp(pos.y, -verticalBoundarie, verticalBoundarie), 0);

        transform.position = GameController.GetClosestPositionInsideBounds(pos);
    }

    private void HandleTurret()
    {
        if (isShooting) {
            aimReference = turret.TransformPoint(Vector3.up * aimReferenceMagnitude*0.66f);
            return; 
        }

        if (GameController.IsTouchingBounds(transform.position))
        {
            aimReference -= inputDir.normalized * speed * Time.deltaTime;
        }

        var cableVector = aimReference - transform.position;
        if (cableVector.magnitude > aimReferenceMagnitude)
        {
            aimReference = transform.position + (cableVector.normalized * aimReferenceMagnitude);
        }


        turret.rotation = Quaternion.LookRotation(Vector3.forward, aimReference - transform.position);

        return;
        if (PlayerInputs.Horizontal != 0 || PlayerInputs.Vertical != 0)
        {
            moveDirIndex = (moveDirIndex + 1) % lastMoveDirectionsAmount;
            lastMoveDirections[moveDirIndex] = inputDir;
            var turretForward = -new Vector3(lastMoveDirections.Average(x => x.x), lastMoveDirections.Average(x => x.y), lastMoveDirections.Average(x => x.y));
            turret.rotation = Quaternion.LookRotation(Vector3.forward, turretForward);
        }
    }

    private void HandleBubble()
    {
        if (isBubbling)
        {
            if (Time.time - bubbleStartTime >= bubbleDuration)
            {
                isBubbling = false;
                bubbleSprite.transform.DOScale(0f, 0.075f);
            }
        }
        else
        {
            if (PlayerInputs.Bubble && canActivateShield)
            {
                bubbleStartTime = Time.time;
                bubbleSprite.transform.localScale = Vector3.zero;
                bubbleSprite.transform.DOScale(bubbleRadius, 0.075f);
                isBubbling = true;

                var shieldCounter = shieldCounters.FirstOrDefault(x => x.isFull);
                shieldCounter.StartUsing(bubbleDuration);
                SetShieldCounter();
            }
        }

    }

    private void HandleShot()
    {
        if (PlayerInputs.Fire)
        {
            if(Time.time - timeLastShot >= delayBetweenShots)
            {
                Shoot();
                isShooting = true;

                evenShots++;
                if (evenShots >= 2)
                {
                    evenShots = 0;
                    Shoot();
                }

                //bonus
                shotsFiredBonus++;
                if(shotsFiredBonus >= 5)
                {
                    shotsFiredBonus = 0;
                    for (int i = 0; i < fireRate_UpgradeUnlocks; i++)
                    {
                        Shoot();
                    }
                }
            }
            if (!fireSrc.isPlaying && ammo>0)
            {
                fireSrc.DOKill();
                fireSrc.volume = 0.8f;
                fireSrc.Play();
            }
        }
        else
        {
            isShooting = false;
            if (fireSrc.isPlaying && !DOTween.IsTweening(fireSrc))
            {
                fireSrc.DOFade(0f, 0.1f).OnComplete(()=>{ fireSrc.Stop(); });
            }
        }
    }

    private void Shoot()
    {
        if (ammo <= 0) return;
        var b = PoolManager.instance.GetPlayerBullet();
        if (!b) return;
        timeLastShot = Time.time;
        b.transform.position = turret.transform.TransformPoint(Vector3.up * 0.25f);
        var shotDir = turret.transform.up + turret.transform.TransformVector(AddNoiseOnAngle(-minAngleError,minAngleError));
        b.Shoot(shotDir);
        OnShoot?.Invoke(1);
        ammo -= 1;
        OnAmmoChange?.Invoke(ammo, maxAmmo);

        shotsFiredLastSecond++;
    }

    public static Vector3 AddNoiseOnAngle(float min, float max)
    {
        float xNoise = UnityEngine.Random.Range(min, max);

        Vector3 noise = new Vector3(Mathf.Sin(2 * Mathf.PI * xNoise / 360), 0, 0);
        return noise;
    }

    public void TakeHit(int damage)
    {
        if (isBubbling || !canTakeDamage) return;
        canTakeDamage = false;
        timeLastDamage = Time.time;
        CameraController.instance.Shake(0.2f, 0.6f);
        Recharge(-.2f *BASE_MAX_AMMO/maxAmmo);
        if (ammo <= 0)
        {
            Die();
        }
        else
        {
            AudioManager.GetAudioSource().PlayOneShot(damageClip);
        }
    }

    public void Die()
    {
        Debug.Log("Game Over");

        OnDie?.Invoke();
        gameObject.SetActive(false);

        CameraController.instance.Shake(0.4f, 0.6f);
        var part = PoolManager.instance.GetExplosionParticles();
        part.transform.position = transform.position;
        part.gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bubbleRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coinAttractionRadius);
        Gizmos.DrawWireSphere(transform.position, coinCollectionRadius);


        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(aimReference, 0.1f);
    }

    public void SetShieldCounter()
    {
        var shieldCounter = shieldCounters.FirstOrDefault(x => x.gameObject.activeInHierarchy && !x.isFull && !x.isInUse);
        if (shieldCounter)
        {
            shieldCounter.StartCharging(shieldRechargeDuration);
        }
        shieldIndicator.SetActive(canActivateShield);

        if(shieldCounters.Count(x => x.gameObject.activeInHierarchy && x.isFull && !x.isInUse) == 1){
            bubbleShadowSprite.DOFade(.25f, .25f);
        }

        if (shieldCounters.Count(x => x.gameObject.activeInHierarchy && x.isFull && !x.isInUse) == 0)
        {
            bubbleShadowSprite.DOFade(0f, .25f);
        }
    }
}
