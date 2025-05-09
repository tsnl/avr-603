using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyController : MonoBehaviour
{
    public GameObject projectileSpawnPoint;
    public GameObject projectilePrefab;
    public GameObject internalStateDisplay;
    public float attackCooldownSec = 1f;
    public float roarProbability = 0.8f;
    public float maxHealth = 100f;
    public float awareDistance = 15f;
    public int debugForcePhase = -1;
    public float phase1TurnSpeedFactor = 0.90f;
    public float phase2TurnSpeedFactor = 0.99f;
    public float phase1IsFacingThreshDeg = 5.0f;
    public float phase2IsFacingThreshDeg = 1.0f;
    public float roarJumpForce = 2.0f;


    private BehaviorTree bt;
    private float remainingHealth;
    private GameObject player;
    private float timeAccumulator = 0f;

    internal TMP_Text textDisplay;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("PlayerAvatar");
        remainingHealth = maxHealth;

        textDisplay = internalStateDisplay.GetComponent<TMP_Text>();

        bt = new Select
        {
            Children = new Select.Choice[]
            {
                new Select.Choice(
                    () => debugForcePhase == 1 || IsAwareOf(player) && remainingHealth >= maxHealth * 0.5f,
                    Phase1()
                ),
                new Select.Choice(
                    () => debugForcePhase == 2 || IsAwareOf(player) && remainingHealth < maxHealth * 0.5f,
                    Phase2()
                ),
            }
        };
    }

    BehaviorTree Phase1() =>
        new Select()
        {
            Children = new[] {
                new Select.Choice(
                    () => IsFacing(player, phase1IsFacingThreshDeg),
                    new Sequence(
                        new BehaviorTree[] {
                            // Briefly wait before trying to attack once facing the player.
                            new Idle(this, 0.15f),
                            new Random() {
                                Children = new Random.Choice[] {
                                    new(0.50f, NewAttackBT()),
                                    new(0.35f, new Idle(this, 1.25f)),
                                    new(1.00f, new Roar(this, () => roarJumpForce))
                                },
                            }
                        }
                    )
                ),
                new Select.Choice(
                    () => !IsFacing(player, phase1IsFacingThreshDeg),
                    new Random() {
                        Children = new Random.Choice[] {
                            new(0.2f, new Roar(this, () => roarJumpForce)),
                            new(0.1f, new Idle(this, 2.0f)),
                            new(0.8f, new FacePlayer(this, player, () => phase1TurnSpeedFactor, () => phase1IsFacingThreshDeg)),
                        },
                    }
                )
            },
        };

    BehaviorTree Phase2() =>
        new Select()
        {
            Children = new[] {
                new Select.Choice(
                    () => IsFacing(player, phase2IsFacingThreshDeg),
                    new Sequence(
                        new BehaviorTree[] {
                            // Briefly wait before trying to attack once facing the player.
                            new Idle(this, 0.1f),
                            new Random() {
                                Children = new Random.Choice[] {
                                    new(1.0f, NewAttackBT()),
                                    new(0.0f, new Roar(this, () => roarJumpForce))
                                },
                            }
                        }
                    )
                ),

                // Always rotate to face the player, even while attacking.
                new Select.Choice(
                    () => true,
                    new FacePlayer(this, player, () => phase2TurnSpeedFactor, () => phase2IsFacingThreshDeg)
                ),
            },
        };

    BehaviorTree NewAttackBT() =>
        new Sequence(
            new[] {
                new Attack(this, player, projectileSpawnPoint, projectilePrefab),
                new Attack(this, player, projectileSpawnPoint, projectilePrefab),
                new Attack(this, player, projectileSpawnPoint, projectilePrefab),
                new Attack(this, player, projectileSpawnPoint, projectilePrefab),
                new Attack(this, player, projectileSpawnPoint, projectilePrefab),
            }
        );

    void FixedUpdate()
    {
        bt.Tick(Time.fixedDeltaTime);
    }

    public bool IsAwareOf(GameObject target)
    {
        var distance = Vector3.Distance(transform.position, target.transform.position);
        return distance < awareDistance;
    }

    public bool IsFacing(GameObject target, float toleranceDeg)
    {
        var myHeading = new Vector2(transform.forward.x, transform.forward.z).normalized;
        var targetHeading = new Vector2(target.transform.position.x - transform.position.x, target.transform.position.z - transform.position.z).normalized;
        var angle = Vector2.SignedAngle(myHeading, targetHeading);
        return Mathf.Abs(angle) <= toleranceDeg;
    }
}


class FacePlayer : BehaviorTree
{
    private EnemyController self;
    private GameObject player;
    private Func<float> turnSpeedFactor;
    private Func<float> toleranceDeg;

    public FacePlayer(EnemyController enemyController, GameObject player, Func<float> turnSpeedFactor, Func<float> toleranceDeg)
    {
        this.self = enemyController;
        this.player = player;
        this.turnSpeedFactor = turnSpeedFactor;
        this.toleranceDeg = toleranceDeg;
    }

    public BehaviorStatus Tick(float dt)
    {
        self.textDisplay.text = "FacePlayer";

        if (self.IsFacing(player, toleranceDeg()))
        {
            return BehaviorStatus.Success;
        }
        else
        {
            Vector3 direction = (player.transform.position - self.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            var angle = Quaternion.Angle(self.transform.rotation, lookRotation);
            if (angle < toleranceDeg())
            {
                self.transform.rotation = Quaternion.Slerp(self.transform.rotation, lookRotation, 0.5f);
                return BehaviorStatus.Success;
            }
            else
            {
                self.transform.rotation = Quaternion.Slerp(self.transform.rotation, lookRotation, Mathf.Max(0.25f, turnSpeedFactor() * dt));
                return BehaviorStatus.Running;
            }
        }
    }
}

class Attack : BehaviorTree
{
    private EnemyController self;
    private GameObject player;
    private GameObject projectileSpawnPoint;
    private GameObject projectilePrefab;

    public Attack(EnemyController enemyController, GameObject player, GameObject projectileSpawnPoint, GameObject projectilePrefab)
    {
        this.self = enemyController;
        this.player = player;
        this.projectileSpawnPoint = projectileSpawnPoint;
        this.projectilePrefab = projectilePrefab;
    }

    public BehaviorStatus Tick(float dt)
    {
        self.textDisplay.text = "Attack";

        var projectile = GameObject.Instantiate(projectilePrefab, projectileSpawnPoint.transform);
        projectile.GetComponent<Rigidbody>().AddForce(projectileSpawnPoint.transform.forward * 15f, ForceMode.Impulse);
        GameObject.Destroy(projectile, 10f);
        return BehaviorStatus.Success;
    }
}

class Roar : BehaviorTree
{
    private EnemyController self;
    private bool jumpIssued = false;
    private Func<float> jumpForce;

    public Roar(EnemyController enemyController, Func<float> jumpForce)
    {
        this.self = enemyController;
        this.jumpForce = jumpForce;
    }

    public BehaviorStatus Tick(float dt)
    {
        self.textDisplay.text = "Roar";

        var grounded = Physics.CheckSphere(self.transform.position, 5.1f, LayerMask.GetMask("Ground"));

        if (grounded)
        {
            if (jumpIssued)
            {
                // Reset jumpIssued if the enemy is grounded and has already jumped before returning.
                jumpIssued = false;
                return BehaviorStatus.Success;
            }
            else
            {
                self.GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce(), ForceMode.Impulse);
                jumpIssued = true;
            }
        }

        return BehaviorStatus.Running;
    }
}


class Idle : BehaviorTree
{
    private EnemyController self;
    private float idleTime = 0.0f;
    private float idleAccumulator = 0.0f;

    public Idle(EnemyController enemyController, float idleTime)
    {
        this.self = enemyController;
        this.idleTime = idleTime;
    }

    public BehaviorStatus Tick(float dt)
    {
        self.textDisplay.text = "Idle";

        idleAccumulator += dt;
        if (idleAccumulator < idleTime)
        {
            return BehaviorStatus.Running;
        }

        idleAccumulator = 0.0f;
        return BehaviorStatus.Success;
    }
}
