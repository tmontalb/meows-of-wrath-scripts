using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
	public float maxJumpHeight = 4;
	public float minJumpHeight = 0.5f;
	public float timeToJumpApex = .4f;
	float accelerationTimeAirborne = .2f;
	float accelerationTimeGrounded = .1f;
	public float moveSpeed = 6;
	public float moveSpeedMultiplier = 1;
	public float moveSpeedMultiplierLimit = 2;
	public float dashTime = 0;
	public bool dashingRight = true;
	int currentStartHealth = 5;
	public static int currentHealth = 5;
	public static int health;
	public int armor = 0;
	public float damageCoolDown = 1;
	public float damageDecreasingCoolDown = 0;
	public int damage = 1;
	public bool overlapDoor = false;
	public bool onLadder = false;
	public float ladderSpeed = 4f;
	public float ladderXPosition;
	public bool faceRight = true;

	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;

	public float wallSlideSpeedMax;
	public float wallStickTime;
	float timeToWallUnstick;
	public float bumpTime;

	float gravity;
	float maxJumpVelocity;
	float maxSecondJumpVelocity;
	float minJumpVelocity;
	float minSecondJumpVelocity;
	public Vector3 velocity;
	float velocityXSmoothing;

	public Controller2D controller;
	
	public Door door;

	public Vector2 directionalInput;
	public bool oldDirectionalFaceRight;
	public float oldHorizontalInput;
	bool wallSliding;
	int wallDirX;
	public static bool doubleJump;
	public bool secondJump;
	string attack = "punch";
	public GameObject[] punch;
    float punchDelay = 0;
	float initPunchDelay = 0.1f;
	public int punch2HorizontalDirection = 0;
	public int punch2VerticalDirection = 0;
	public int punchDamage = 1;

	public bool dialog = false;
	public bool collisionNPC = false;
	public Vector3 knockbackVelocity;
	[SerializeField] private float knockbackDamp = 5f;   // how quickly it fades out

    private SpriteRenderer mySpriteRenderer;

    Animator animator;

    [Header("Wall Slide (Normal)")]
    [SerializeField] private float normalWallStickTime = 0.25f;
    [SerializeField] private float normalWallSlideSpeedMax = 10;

    [Header("Wall Slide (Easy Mode)")]
    [SerializeField] private float easyWallStickTime = 1f;
    [SerializeField] private float easyWallSlideSpeedMax = 3;

    private void Awake()
    {
        ApplyDifficulty(DifficultySettings.EasyMode);
    }

    public void ApplyDifficulty(bool easyMode)
    {
        wallStickTime = easyMode ? easyWallStickTime : normalWallStickTime;
        wallSlideSpeedMax = easyMode ? easyWallSlideSpeedMax : normalWallSlideSpeedMax;
    }

	public void ApplyDifficulty()
	{
		ApplyDifficulty(DifficultySettings.EasyMode);
	}

	void Start()
	{
		//		Debug.Log("Awake:" + SceneManager.GetActiveScene().name);
		// If we don't have a saved health yet, start at full
		if (GameState.I.playerHealth <= 0)
		{
			currentHealth = currentStartHealth;
		}
		else
		{
			// Otherwise, restore from GameState (e.g. after death/respawn)
			currentHealth = GameState.I.playerHealth;
		}

		doubleJump = GameState.I.doubleJump;
		health = currentHealth;
		controller = GetComponent<Controller2D>();
		gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
		maxSecondJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex * 0.8f;
		minSecondJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight) * 0.8f;
        animator = GetComponentInChildren<Animator>();
		mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();

		if (GameState.I != null && GameState.I.respawnAtLastDoor)
		{
			GameState.I.respawnAtLastDoor = false;

			// Only teleport if we have a valid door id
			if (!string.IsNullOrEmpty(GameState.I.lastDoorId))
			{
				Transform target = GameObject.Find(GameState.I.lastDoorId)?.transform;

				if (target != null)
				{
					transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);

					var cameraFollow = GameObject.FindGameObjectWithTag("MainCamera");
					var follow = cameraFollow.GetComponent<CameraFollow>();
					follow.enabled = false;
					follow.enabled = true;
				}
				else
				{
					Debug.LogWarning("RespawnAtLastDoor was true, but door '" +
									GameState.I.lastDoorId + "' was not found in this scene.");
				}
			}
			else
			{
				// No door visited this run --> just respawn at default spawn position.
				Debug.Log("Respawn requested but no lastDoorId set; staying at default spawn.");
			}
		}
	}

	void Update()
	{
		if (directionalInput.x == 1)	// 12-17-2025 - Loop that manages the main character's sprite direction facing
										// (recently flipped the starting sprite in the game from facing left to facing right and switched all false to true and vice versa in this loop)
		{
			if (!oldDirectionalFaceRight){
                mySpriteRenderer.flipX = true;
			}
			faceRight = true;
			oldDirectionalFaceRight = true;
		}
		else if (directionalInput.x == -1)
		{
			if (oldDirectionalFaceRight){
                mySpriteRenderer.flipX = false;
			}
			faceRight = false;
			oldDirectionalFaceRight = false;
		}
		else {
		}

		HandleDash();
		CalculateVelocity();
		HandleWallSliding();

		// Add knockback on top of normal velocity. This is to generate backwards velocity when our back is against a wall, to avoid going through it.
		velocity += knockbackVelocity;

		controller.Move(velocity * Time.deltaTime, directionalInput);

		// Gradually reduce knockback over time
		if (knockbackVelocity != Vector3.zero)
		{
			knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamp * Time.deltaTime);
		}

		if (controller.collisions.above || controller.collisions.below)
		{
			if (controller.collisions.slidingDownMaxSlope)
			{
				velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
			}
			else
			{
				velocity.y = 0;
			}
		}
		if (controller.collisions.below || wallSliding)
		{
			secondJump = true;
			animator.SetBool("Grounded", true);
		}
		else{
			animator.SetBool("Grounded", false);
		}
		if (damageDecreasingCoolDown > 0)
		{
			damageDecreasingCoolDown -= Time.deltaTime;
		}
		else if (damageDecreasingCoolDown < 0)
		{
			damageDecreasingCoolDown = 0;
		}
		if (health <= 0)
		{
			Death();
		}
	}

	public void SetDirectionalInput(Vector2 input)
	{
		directionalInput = input;
	}

	public void OnJumpInputDown()
	{
		if (onLadder)
		{
			onLadder = false;
			velocity.y = maxJumpVelocity;
		}
		if (wallSliding)
		{
			if (wallDirX == directionalInput.x)
			{
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			else if (directionalInput.x == 0)
			{
				velocity.x = -wallDirX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}
			else
			{
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if (controller.collisions.below)
		{
			if (controller.collisions.slidingDownMaxSlope)
			{
				if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
				{
					velocity.x = -wallDirX * wallLeap.x;
					velocity.y = wallLeap.y;
				}
				else if (wallDirX == -directionalInput.x)
				{
					velocity.x = -wallDirX * wallJumpClimb.x;
					velocity.y = wallJumpClimb.y;
				}
				else
				{
					// not jumping against max slope
					velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
					velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
				}
			}
			else
			{
				velocity.y = maxJumpVelocity;
			}
		}
		else if (doubleJump == true && secondJump == true && !wallSliding /*&& !(overlapLadder1 || overlapLadder2)*/)
		{
			velocity.y = maxSecondJumpVelocity;
			secondJump = false;
		}
	}

	public void OnJumpInputUp()
	{
		if (velocity.y > minJumpVelocity)
		{
			velocity.y = minJumpVelocity;
		}
	}


	void HandleWallSliding()
	{
		wallDirX = (controller.collisions.left) ? -1 : 1;
		wallSliding = false;
		if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
		{
			wallSliding = true;

			if (velocity.y < -wallSlideSpeedMax)
			{
				velocity.y = -wallSlideSpeedMax;
			}

			if (timeToWallUnstick > 0)
			{
				velocityXSmoothing = 0;
				velocity.x = 0;

				if (directionalInput.x != wallDirX && directionalInput.x != 0)
				{
					timeToWallUnstick -= Time.deltaTime;
				}
				else
				{
					timeToWallUnstick = wallStickTime;
				}
			}
			else
			{
				timeToWallUnstick = wallStickTime;
			}
		}
	}

	public void PlayerEnnemyCollisions(float ennemyVelocityX)
	{
		controller.collisions.playerEnnemyCollision = "ongoing";

		if (velocity.x > ennemyVelocityX)
		{
			controller.collisions.ennemyPosition = "right";
		}
		else
		{
			controller.collisions.ennemyPosition = "left";
		}
	}
	void CalculateVelocity()
	{
		// if we are pressing left or right, register oldHorizontalInput with its value (+1 or -1)
		if (Mathf.Abs(directionalInput.x) == 1 && dashTime <= 0)
		{
			oldHorizontalInput = directionalInput.x;
		}

		if (controller.collisions.playerEnnemyCollision == "ongoing")
		{
			if (bumpTime > 0)
			{
				//print("bumpTime > 0");
				if (controller.collisions.ennemyPosition == "right")
				{
					velocity.x = -20f;
				}
				else if (controller.collisions.ennemyPosition == "left")
				{
					velocity.x = 20f;
				}
				else if (controller.collisions.ennemyPosition == "top")
				{
					if (!controller.collisions.below) // If we are standing on the ground or slope, we cannot go down and need to go to the opposite direction that we are going.
					{
						velocity.y = -maxJumpVelocity;
					}
					else
					{
						if (controller.goingRight > 0)
						{
							velocity.x = -20f; // to adjust when bumping into ennemy from below (ennemy moving towards player or opposite from player.
						}
						else if (controller.goingRight < 0)
						{
							velocity.x = 20f; // to adjust when bumping into ennemy from below (ennemy moving towards player or opposite from player.
						}
					}
				}
				else if (controller.collisions.ennemyPosition == "bottom")
				{
					secondJump = true;
					if (Input.GetKey(KeyCode.Space))
					{
						velocity.y = maxJumpVelocity;
					}
					else
					{
						velocity.y = minJumpVelocity;
					}
				}
				bumpTime -= Time.realtimeSinceStartup;
			}
			else
			{
				//print("bump time over");
				controller.collisions.playerEnnemyCollision = "no";
			}
		}
		else
		{
			float targetVelocityX = directionalInput.x * moveSpeed * moveSpeedMultiplier;
			velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
		}
		
		// if we are on the ground, pressing x axis while overlapping on ladder or if we are not overlapping anymore (when exiting from top of ladder for example)
		if (onLadder && controller.collisions.below && directionalInput.x != 0){
			onLadder = false;
		}
		if (onLadder)
		{
			velocity.y = ladderSpeed * directionalInput.y - 0.001f;
			velocity.x = 0;
		}
		else
		{
			velocity.y += gravity * Time.deltaTime;
		}
		/* I think we don't need this anymore now that we manage the door interactions in the door script
		if (overlapDoor && directionalInput.y == 1)
		{
			print("entering door"); //works but occurs several times... is it a problem?
		}*/
	}
	public void Attack()
	{
		if (attack == "punch")
		{
			punch = new GameObject[10];
			// Debug.Log(this.transform.transform.GetChild(1).name);
			// Debug.Log(this.transform.transform.GetChild(1).childCount);
			// Debug.Log(this.transform.GetChild(1).childCount);
			for (int i = 1; i <= this.transform.GetChild(1).childCount; i++)
			{
				// Debug.Log(this.transform.transform.GetChild(i).name);
				if (i == 1) {
					// punch[i] = this.transform.transform.GetChild(i).GetChild(0).gameObject;
					punch[i] = this.transform.GetChild(1).GetChild(i-1).gameObject;
					punch[i].gameObject.GetComponent<Punch>().Spawn();
				}
				else
				{
					punch[i] = this.transform.GetChild(1).GetChild(i-1).gameObject;
					punch[i].gameObject.GetComponent<Punch>().Execute();
				}
			}
		}
	}

	public void HandleDash()
	{
		// if we stop pressing input, set the timer for dashing --> check if we change the order of events, by putting this at the end of the loop, for example. Because this triggers the if dash time > 0 loop and increases the multiplier by 1. Is it normal everytime?
		if (oldHorizontalInput != directionalInput.x && directionalInput.x == 0 && dashTime <= 0)
		{
			// print("Setting dash time to set time");
			dashTime = Time.deltaTime + 0.1f;
			// print (dashTime);
		}

		// Checking if directional input is same direction as when we started dashing to avoid counter dashes.
		if ((directionalInput.x == 1 && !dashingRight) || (directionalInput.x == -1 && dashingRight))
		{
			ResetDash();
			if (directionalInput.x == 1)
			{
				dashingRight = true;
			}
			else
			{
				dashingRight = false;
			}
		}

		if (dashTime > 0)
		{
			if (directionalInput.x == oldHorizontalInput && moveSpeedMultiplier < moveSpeedMultiplierLimit && controller.collisions.below)
			{
				// print("Increasing speed!");
				moveSpeedMultiplier += 1;
				if (directionalInput.x == 1)
				{
					dashingRight = true;
				}
				else
				{
					dashingRight = false;
				}
				dashTime = 0;
			}
			else
			{
				dashTime -= Time.deltaTime;
			}
		}

		// if we reach a dash time of 0 or under and stopped pressing left or right, set speed multiplier back to 1.
		if (dashTime < 0 && directionalInput.x == 0)
		{
			ResetDash();
		}
	}
	public void ResetDash()
	{
		moveSpeedMultiplier = 1;
		dashTime = 0;
		oldHorizontalInput = 0;
	}

	// inside Player class:
	private bool isDead = false;

	public void Death()
	{
		if (isDead) return;   // avoid double-trigger
		isDead = true;

		// reset player-related state before reload
		currentHealth = currentStartHealth;
		if (GameState.I.lastDoorId != "0"){
			GameState.I.respawnAtLastDoor = true;
		}

		var scene = SceneManager.GetActiveScene();

		if (ScreenFader.I != null)
		{
			ScreenFader.I.ReloadSceneWithFade(scene.name, 0.3f, 0.3f);
		}
		else
		{
			SceneManager.LoadScene(scene.name);
		}
	}

	public void Pit()
	{
		GameState.I.respawnAtLastDoor = true;
		health -= 1;

		if (health < 1)
		{
			GameState.I.playerHealth = -1;
			// Let Update() detect health<=0 and call Death() (which will fade)
			return;
		}

		// Still have HP left --> respawn with fade and keep reduced health
		GameState.I.playerHealth = health;

		var scene = SceneManager.GetActiveScene();

		if (ScreenFader.I != null)
		{
			ScreenFader.I.ReloadSceneWithFade(scene.name, 0.3f, 0.3f);
		}
		else
		{
			SceneManager.LoadScene(scene.name);
		}
	}
}