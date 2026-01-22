using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController
{
	public float maxSlopeAngle = 80;
	public bool verticalCollisionWithLadder = false;
	public bool horizontalCollisionWithLadder = false;
	public Vector2 position, oldPosition;
	public int goingUp, goingRight;
	public int activeDirectionZones;


	// Some tutorial:
	// To create a new object to interact with:
	// 	- If it is going to be an object belonging to a layer mask:
	//		- Add the layer mask in the raycast controller.
	//		- Create a collider 2D component and activate the "is Trigger" box.
	//		- Code whatever you want in a loop that will most likely interact with a component that shoots raycasts (player, ennemy, npc...).
	//		- You can associate the gameobject with a tag for more specific interact if it is in the same layer as other objects.
	//  - If it is going to be an object with its own code.
	//		- Create a collider 2D component and activate the "is Trigger" box.
	//		- Just code whatever you want into this gameobject script, interact with objects with tags, like "player".

	// Bugs to address:

	// To do:
	//	- on movement:
	//      - Fixed - Dashes even when pressing a second time a while after the first time, does not seem to reset correctly - Fixed, needed to also reinitialize the oldHorizontalInput.
	//		- Fixed - need to address dash bug when changing direction - Fixed.
	//	- on collisions:
	//		- Fixed ? sometimes, there is no push backs - Resolved, but it would be good to QA for collisions, ladders and slopes.
	//		- Work around - no side collisions on slim platforms when jumping - Resolved by reducing the space between rays so that the gap is smaller than a platform's width in RaycastController: dstBetweenRays
	//		- No need to fix - Work on collision with ennemies when touching angles (sometimes getting hit horizontally, either ennemy or player) - Won't fix: This is good behavior, we want the ennemy to be pushed backward when the player lands on him or the player to be pushed backwards if they land on the side of the ennemy.
	//	- Fixed - Need to flip the platform so its z rotate value is positive if it's a bottom left to top right platform and vice versa - Climbing a diagonale slope (bottom left top right) sometimes, bouncing the opposite direction.
	//  - Fixed - When dying, we come back to life in the wrong scene - Fixed!
	//  - Fixed - Delayed jump when hitting jump button while in conversation with Josephine and double jump activated (otherwise, can't reproduce bug). Fixed!
	//  - Fixed - Player appears behind doors when arriving to one. Fixed itself?
	//  - Fixed - Player takes damage in Stage 1 but not in main scene when jumping on top of ennemy almost 50% of the time
		//  --> When objects are below 0 in the stage, they don't behave as they should because the raycast flick up and down (to fix?)
		//  --> It's actually when objects are right at the limit between positive and negative positions. May as well stay above it and not bother about this bug.
		//  --> I have established it was most likely not related to goingUp variable but I think it is related to the origin of the rays...
	//  - Add ability to look up and down.
	    //  --> Actually, working on a zoom in and out system --> Scripting smooth zoom in zone gameobject --> Do it in CameraFollow script for smooth zooms (one step per frame rate). Done!
		//  --> ZoomOut works, but not zoom back in... Fixed!
	//  - Zones: Test multiple zones interfering with each other --. Done, works with another object called LowerSpeed (Did not actually affect the speed, just simulating by printing output).
	//  - Fixed by reducing dashTime from 0.5 to 0.1. Adjust delay until speeding up as it gets quickly out of control. For example, when pressing another direction, it should reset the counter
	//  - Fixed - Correct how the player appears behind the destination door.
	//  - Almost fixed problems with sprites collisions. Doing it with Patsy by adding a child component for its sprite. Need to increase the size of the Player GameObject and shrink the sprite within to fit in it while being the correct size.
	//    - Need to do it to Carrot as well and test.
	// Note: The colliders hider (which makes the basic elements that are not graphic be invisible while in game) are defined in a script called colliderHider that's attached to anything that we want to be invisible during gameplay.
	// Cat meows from her butt at the beginning if we don't press any arrow!
	//	- Fixed - Prototype almost ready. Somehow, collecting the powerup does not work anymore. Need to fix this one last thing!
	// Done - Avoid thin walls because if we jump on top of them and the colliders miss it, we end up with a bug sliding through the wall all the way down.
	// Done - Finalize interaction with the NPC.
	// Fixed - It was not an Ennemy collision problem but a camera zooming indefinitely when exiting zoom zone before it reaches its target - Ennemies can squash you against a wall and that creates a camera bug. Test with the first brocoli.
	// Fixed with ResolveEnnemyOverlap() - Sometimes, collisions with ennemies glitch when you corner an ennemy against the wall and somehow it goes through you. You get stuck within the ennemy.
	// Slow loading of scene from first screen selection and slow menu screen loading.
	// Fixed - Need to keep the double jump after dying but not when restarting the game.
	// Fixed again (even dying after having used the secret door last) - When returning to the secret zone, it looks like you are in the wall
	// Fixed - When reaching the ending screen, the music does not stop. If we return to the start screen, the music still plays.
	// Fixed - Screen does not fade when you die anymore.
	// Fixed - The pause menu screen is not centered well.
	// Fixed - When we find the final ending and then restart the game, it keeps on the final music, instead or returning to the cute one.
	// Done - Need to improve the ending texts.
	// Fixed - If you pause the game while talking to an NPC, when you unpause, the game is not frozen anymore, but the text is stuck showing up along with the moving NPC.
    // Checked - OK - Check if I did not introduce a bug when I edited the NPC script, removing oldTimeScale
    // Done - Last door needs to be resetted when restarting the game.
	// Solved with OneWaySet bool in zone music loop - Secret stage music stays if you go back to the regular stage.
	// Fixed - quit button does not work on splash screen
	// Fixed - directional helper wrong when we need to reach final door on ladder level.
	// consider changing time to wall unstick if too hard to jump from wall to wall (one feedback about it so far).

	// Currently, the pause menu gameobject that controls the pause action is the Canvas parent game object.
	// Similarly, the First screen menu's gameobject that controls that screen is the GameObject called MainMenuController.
	// The other text field (currently the two final screens with thank you message) are managed from the finalscene.cs script.

	Player player;
	Ennemy ennemy;

	public CollisionInfo collisions;
	public ZoneInfo zoneInfo;
	//	public EnnemyCollisionInfo ennemyCollisions;
	[HideInInspector]
	public Vector2 playerInput;

	public override void Start()
	{
		position = this.transform.position; //working on figuring out if character is going up or down...
		collisions.ennemyPlayerCollision = "no";
		collisions.playerEnnemyCollision = "no";
		base.Start();
		collisions.faceDir = 1;
		player = GetComponent<Player>();
		ennemy = GetComponent<Ennemy>();
		Physics2D.queriesStartInColliders = false;
		goingUp = 0;
		goingRight = 0;
		activeDirectionZones = 0;
	}

	public void Update()
	{
		oldPosition = position; // To figure out if character is going up or down...
		position = this.transform.position; // To figure out if character is going up or down...
		if (position.y >= 0){
			if (position.y - oldPosition.y > 0)
			{
				goingUp = 1;
			}
			else
			{
				goingUp = -1;
			}
		}

		if (position.x - oldPosition.x > 0)
		{
			goingRight = 1;
		}
		else if (position.x - oldPosition.x < 0)
		{
			goingRight = -1;
		}
		else
		{
			goingRight = 0;
		}
	}

	public void Move(Vector2 moveAmount, bool standingOnPlatform)
	{
		Move(moveAmount, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
	{
		UpdateRaycastOrigins();
		collisions.Reset();
		collisions.moveAmountOld = moveAmount;
		playerInput = input;

		if (moveAmount.y < 0)
		{
			DescendSlope(ref moveAmount);
		}

		if (moveAmount.x != 0)
		{
			collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
		}

		if (moveAmount.y != 0)
		{
			VerticalCollisions(ref moveAmount);
		}
		HorizontalCollisions(ref moveAmount);

		//	OverlapCollisions(ref moveAmount);

		if (Time.timeScale != 0) { // if we don't put this condition, we have weird glitches when interacting with an NPC (teleporting to the left after speaking to same npc, not moving, and moving horizontally 2 times)
			transform.Translate(moveAmount); 
		}

		if (standingOnPlatform)
		{
			collisions.below = true;
		}
		// Giving one frame to avoid that upon frontal impact, while being pushed back, the back ray could detect a collision with the once front collider.
		if (collisions.ennemyPlayerCollision == "beingPushedBack")
		{
			collisions.ennemyPlayerCollision = "no";
		}
		ResolveEnemyOverlap();
	}

	void HorizontalCollisions(ref Vector2 moveAmount)
	{
		float directionX = collisions.faceDir; // Do not replace with Mathf.Sign(moveAmount.x) because it generates a bug when climbing on left facing wall
		float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

		float HorizontalEnnemyPlayerFrontCollisionsRayLength = this.transform.localScale.x * 0.5f;
		float HorizontalEnnemyPlayerBackCollisionsRayLength = this.transform.localScale.x * 0.5f;

		if (Mathf.Abs(moveAmount.x) < skinWidth)
		{
			rayLength = 2 * skinWidth;
		}

		// Collision with walls, important to put before collision with Ennemies, because otherwise, it may reset the push back after hitting an ennemy while on a slope, for example.
		for (int i = 0; i < horizontalRayCount; i++)
		{
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionEnvMask);

						Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.green);

			if (hit)
			{
				// --- Secret wall reveal (player presses toward wall) ---
				if (this.CompareTag("Player"))
				{
					var secretWall = hit.collider.GetComponent<SecretWall>();
					if (secretWall != null)
					{
						// Only reveal if the player is actively pressing into the wall.
						// directionX is -1 (left) or +1 (right)
						if ((directionX == 1 && playerInput.x > 0.1f) || (directionX == -1 && playerInput.x < -0.1f))
						{
							secretWall.Reveal();
							// Allow movement through in the SAME frame:
							// skip processing this hit (wall is now disabled)
							continue;
						}
					}
				}
				// --- end secret wall reveal ---
				if (/*!hit.collider.CompareTag("Through") &&*/ !hit.collider.CompareTag("Ladder")) // Removed the Through condition because we seem to get through diagonal platforms when running fast if they have the Through tag...
				{
					if (hit.distance == 0)
					{
						continue;
					}

					float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

					if (i == 0 && slopeAngle <= maxSlopeAngle)
					{
						if (collisions.descendingSlope)
						{
							collisions.descendingSlope = false;
							moveAmount = collisions.moveAmountOld;
						}
						float distanceToSlopeStart = 0;
						if (slopeAngle != collisions.slopeAngleOld)
						{
							distanceToSlopeStart = hit.distance - skinWidth;
							moveAmount.x -= distanceToSlopeStart * directionX;
						}
						ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
						moveAmount.x += distanceToSlopeStart * directionX;
					}

					if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
					{
						moveAmount.x = (hit.distance - skinWidth) * directionX;
						rayLength = hit.distance;

						if (collisions.climbingSlope)
						{
							moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
						}
						collisions.left = directionX == -1;
						collisions.right = directionX == 1;
					}
				}
			}
		}

		// horizontalCollisionWithLadder = false; I think I forgot to remove this when moving ladder to its own function
		// Collision with an ennemy
		for (int i = 0; i < horizontalRayCount; i++)
		{
			Vector2 rayOrigin = (raycastOrigins.bottomLeft + raycastOrigins.bottomRight) / 2;
			rayOrigin.x += moveAmount.x;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hitFront = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, HorizontalEnnemyPlayerFrontCollisionsRayLength, collisionEnnemyMask);
			RaycastHit2D hitBack = Physics2D.Raycast(rayOrigin, Vector2.right * -directionX, HorizontalEnnemyPlayerBackCollisionsRayLength, collisionEnnemyMask);
			//	Debug.DrawRay(rayOrigin, Vector2.right * directionX * HorizontalEnnemyPlayerFrontCollisionsRayLength, Color.yellow);
			//	Debug.DrawRay(rayOrigin, Vector2.right * -directionX * HorizontalEnnemyPlayerBackCollisionsRayLength, Color.blue);

			if (hitFront)
			{
				// collision between Player and Ennemy
				if (this.CompareTag ("Player") && hitFront.collider.CompareTag ("Ennemy") && collisions.playerEnnemyCollision == "no")
				{
					hitFront.collider.gameObject.GetComponent<Ennemy>().ennemyPlayerCollisionsTrigger = moveAmount.x;
					player.PlayerEnnemyCollisions(hitFront.collider.gameObject.GetComponent<Ennemy>().velocity.x);
					player.bumpTime = Time.realtimeSinceStartup + 0.5f;
					Player.health = PlayerDamage(Player.health, hitFront.collider.gameObject.GetComponent<Ennemy>().damage, player.armor, player.damageCoolDown, ref player.damageDecreasingCoolDown);
					collisions.ennemyPlayerCollision = "ongoing";
				}

				// collision from Ennemy with Player
				else if (this.CompareTag ("Ennemy") && hitFront.collider.CompareTag ("Player") && collisions.ennemyPlayerCollision == "no")
				{
					Player.health = PlayerDamage(Player.health, this.gameObject.GetComponent<Ennemy>().damage, hitFront.collider.gameObject.GetComponent<Player>().armor, hitFront.collider.gameObject.GetComponent<Player>().damageCoolDown, ref hitFront.collider.gameObject.GetComponent<Player>().damageDecreasingCoolDown);

					collisions.ennemyPlayerCollision = "ongoing";

					if (moveAmount.x > 0)
					{
						collisions.ennemyPosition = "left";
					}
					else
					{
						collisions.ennemyPosition = "right";
					}
				}

				// collision from Ennemy with Ennemy other than itself
				else if (this.CompareTag ("Ennemy") && hitFront.collider.CompareTag ("Ennemy") && hitFront.collider.name != this.name)
				{
				}

				// collision from Ennemy with Player's attack
				if (hitFront && this.CompareTag("Ennemy") && hitFront.collider.CompareTag("Attack"))
				{
				    TryApplyAttackHit(hitFront.collider, horizontal: true);
				}
			}
			else if (hitBack)
			{
				// collision between Player and Ennemy
				if (this.CompareTag ("Player") && hitBack.collider.CompareTag ("Ennemy") && collisions.playerEnnemyCollision == "no")
				{
					hitBack.collider.gameObject.GetComponent<Ennemy>().ennemyPlayerCollisionsTrigger = moveAmount.x;
					player.PlayerEnnemyCollisions(hitBack.collider.gameObject.GetComponent<Ennemy>().velocity.x);
					player.bumpTime = Time.realtimeSinceStartup + 0.5f;
					Player.health = PlayerDamage(Player.health, hitBack.collider.gameObject.GetComponent<Ennemy>().damage, player.armor, player.damageCoolDown, ref player.damageDecreasingCoolDown);
					collisions.ennemyPlayerCollision = "ongoing";
				}
				// work on ennemy bump back on collisions with controller.collisions.playerEnnemyCollision = "ongoing";
				// collision from Ennemy with Ennemy other than itself
				else if (this.CompareTag ("Ennemy") && hitBack.collider.CompareTag ("Ennemy") && hitBack.collider.name != this.name)
				{
				}

				// collision from Ennemy with Player's attack
				if (hitBack && this.CompareTag("Ennemy") && hitBack.collider.CompareTag("Attack"))
				{
					TryApplyAttackHit(hitBack.collider, horizontal: true);
				}
			}
		}
	}
	void VerticalCollisions(ref Vector2 moveAmount)
	{
		float directionY = Mathf.Sign(moveAmount.y);
		float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

		//		verticalCollisionWithLadder = false; I think I forgot to remove this when moving ladder to its own function

		float VerticalEnnemyPlayerFrontCollisionsRayLength = this.transform.localScale.y * 0.5f; //(before both lines were 0.51f to keep in mind in case there is a vertical glitch)
		float VerticalEnnemyPlayerBackCollisionsRayLength = this.transform.localScale.y * 0.5f;

		// Collision with an ennemy
		for (int i = 0; i < verticalRayCount; i++)
		{
			Vector2 rayOrigin = (raycastOrigins.bottomLeft + raycastOrigins.topLeft) / 2;
			rayOrigin.y += moveAmount.y;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hitFront = Physics2D.Raycast(rayOrigin, Vector2.up * goingUp, VerticalEnnemyPlayerFrontCollisionsRayLength, collisionEnnemyMask);
			RaycastHit2D hitBack = Physics2D.Raycast(rayOrigin, Vector2.up * -goingUp, VerticalEnnemyPlayerBackCollisionsRayLength, collisionEnnemyMask);
			//Debug.DrawRay(rayOrigin, Vector2.up * goingUp * VerticalEnnemyPlayerFrontCollisionsRayLength, Color.yellow);
			Debug.DrawRay(rayOrigin, Vector2.up * -goingUp * VerticalEnnemyPlayerBackCollisionsRayLength, Color.blue);

			if (hitFront)
			{
				// collision from Player with Ennemy
				if (this.CompareTag ("Player") && hitFront.collider.CompareTag ("Ennemy") && collisions.playerEnnemyCollision == "no")
				{
					collisions.playerEnnemyCollision = "ongoing";
					player.bumpTime = Time.realtimeSinceStartup + 0.5f;

					if (this.gameObject.name == "Walker")
					{
						//print("Vertical Collision detected from ennemy with ennemy on top");
					}
					if (this.gameObject.name == "Player")
					{
						//print("Vertical Collision detected from player with ennemy on top");
					}
					//collisions.ennemyPosition = "top";
					//print("player position: " + gameObject.GetComponent<Player>().transform.localPosition.y + " and ennemy position: " + hitFront.collider.gameObject.GetComponent<Ennemy>().transform.localPosition.y);
					collisions.ennemyPosition = CalculateEnnemyPosition(gameObject.GetComponent<Player>().transform.localPosition.y, hitFront.collider.gameObject.GetComponent<Ennemy>().transform.localPosition.y);
					if (collisions.ennemyPosition == "top")
					{
						Player.health = PlayerDamage(Player.health, hitFront.collider.gameObject.GetComponent<Ennemy>().damage, gameObject.GetComponent<Player>().armor, gameObject.GetComponent<Player>().damageCoolDown, ref gameObject.GetComponent<Player>().damageDecreasingCoolDown);
					}
					if (collisions.ennemyPosition == "bottom")
					{
						hitFront.collider.GetComponent<Ennemy>().PlayHitSound();
						hitFront.collider.gameObject.GetComponent<Ennemy>().health = EnnemyDamage(hitFront.collider.gameObject.GetComponent<Ennemy>().health, this.gameObject.GetComponent<Player>().damage, hitFront.collider.gameObject.GetComponent<Ennemy>().armor, hitFront.collider.gameObject.GetComponent<Ennemy>().damageCoolDown, ref hitFront.collider.gameObject.GetComponent<Ennemy>().damageDecreasingCoolDown);
					}
				}

				// collision from Ennemy with Ennemy other than itself
				else if (this.CompareTag ("Ennemy") && hitFront.collider.CompareTag ("Ennemy") && hitFront.collider.name != this.name)
				{
				}

				// collision from Ennemy with Player
				if (this.CompareTag("Ennemy") && hitFront.collider.CompareTag("Player") && collisions.ennemyPlayerCollision == "no")
				{
					collisions.ennemyPlayerCollision = "ongoing";
					collisions.ennemyPosition = CalculateEnnemyPosition(hitFront.collider.GetComponent<Player>().transform.localPosition.y, gameObject.GetComponent<Ennemy>().transform.localPosition.y);
						Player.health = PlayerDamage(Player.health, this.gameObject.GetComponent<Ennemy>().damage, hitFront.collider.gameObject.GetComponent<Player>().armor, hitFront.collider.gameObject.GetComponent<Player>().damageCoolDown, ref hitFront.collider.gameObject.GetComponent<Player>().damageDecreasingCoolDown);
				}
				// collision from Ennemy with Player's attack
				if (this.CompareTag ("Ennemy") && hitFront.collider.CompareTag ("Attack")/* && collisions.ennemyAttackCollision == "no"*/)
				{
    				TryApplyAttackHit(hitFront.collider, horizontal: false);
				}
			}
			else if (hitBack)
			{
				// collision from Player with Ennemy
				if (this.CompareTag ("Player") && hitBack.collider.CompareTag ("Ennemy") && collisions.playerEnnemyCollision == "no")
				{
					collisions.playerEnnemyCollision = "ongoing";
					player.bumpTime = Time.realtimeSinceStartup + 0.5f;

					if (goingUp > 0)
					{
						collisions.ennemyPosition = "bottom";
					}
					else
					{
						collisions.ennemyPosition = "top";
					}
				}

				// collision from Ennemy with Player
				else if (this.CompareTag ("Ennemy") && hitBack.collider.CompareTag ("Player") && collisions.ennemyPlayerCollision == "no")
				{
					collisions.ennemyPlayerCollision = "ongoing";
					if (goingUp > 0)
					{
						collisions.ennemyPosition = "top";
						Player.health = PlayerDamage(Player.health, this.gameObject.GetComponent<Ennemy>().damage, hitBack.collider.gameObject.GetComponent<Player>().armor, hitBack.collider.gameObject.GetComponent<Player>().damageCoolDown, ref hitBack.collider.gameObject.GetComponent<Player>().damageDecreasingCoolDown);
					}
					else
					{
						collisions.ennemyPosition = "bottom";
					}
				}

				// collision from Ennemy with Ennemy other than itself
				else if (this.CompareTag ("Ennemy") && hitBack.collider.CompareTag ("Ennemy") && hitBack.collider.name != this.name)
				{
				}

				// collision from Ennemy with Player's attack
				if (hitBack && this.CompareTag("Ennemy") && hitBack.collider.CompareTag("Attack"))
				{
				    TryApplyAttackHit(hitBack.collider, horizontal: false);
				}

				if (collisions.ennemyPlayerCollision == "beingPushedBack") // (same as above in horizontal collisions).
				{
					collisions.ennemyPlayerCollision = "no";
				}
			}
		}

		// Collision with walls
		for (int i = 0; i < verticalRayCount; i++)
		{
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionEnvMask);

			// Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.green);

			if (hit)
			{
				if (hit.collider.tag == "Through")
				{
					GameObject body = hit.collider.gameObject;
					if (directionY == 1 || (hit.distance == 0 && body.transform.parent.gameObject.tag != "MovingPlatform")) // Added the parent.GameObject.tag check to avoid falling through a normal platform when the moving platform descends through player and the platform it is standing on: https://answers.unity.com/questions/708455/need-to-get-parent-object-after-collision.html
					{
						continue;
					}
					if (collisions.fallingThroughPlatform)
					{
						continue;
					}
					if (playerInput.y == -1)
					{
						collisions.fallingThroughPlatform = true;
						Invoke("ResetFallingThroughPlatform", .1f); // The .1f determines the time it takes to go through. Using .5f was too long and we would go through 2 platforms without holding the button anymore.
						continue;
					}
				}

				moveAmount.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

				if (collisions.climbingSlope)
				{
					moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
		}

		if (collisions.climbingSlope)
		{
			float directionX = Mathf.Sign(moveAmount.x);
			rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionEnvMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != collisions.slopeAngle)
				{
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
					collisions.slopeNormal = hit.normal;
				}
			}
		}
	}

	// End of manual management of overlap collisions */
	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
	{
		float moveDistance = Mathf.Abs(moveAmount.x);
		float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY)
		{
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
			collisions.slopeNormal = slopeNormal;
		}
	}

	void DescendSlope(ref Vector2 moveAmount)
	{

		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionEnvMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionEnvMask);
		if (maxSlopeHitLeft ^ maxSlopeHitRight)
		{
			SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
			SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
		}

		if (!collisions.slidingDownMaxSlope)
		{
			float directionX = Mathf.Sign(moveAmount.x);
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionEnvMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
				{
					if (Mathf.Sign(hit.normal.x) == directionX)
					{
						if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
						{
							float moveDistance = Mathf.Abs(moveAmount.x);
							float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
							moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
							moveAmount.y -= descendmoveAmountY;

							collisions.slopeAngle = slopeAngle;
							collisions.descendingSlope = true;
							collisions.below = true;
							collisions.slopeNormal = hit.normal;
						}
					}
				}
			}
		}
	}

	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
	{

		if (hit)
		{
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle > maxSlopeAngle)
			{
				moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

				collisions.slopeAngle = slopeAngle;
				collisions.slidingDownMaxSlope = true;
				collisions.slopeNormal = hit.normal;
			}
		}
	}

	void ResetFallingThroughPlatform()
	{
		collisions.fallingThroughPlatform = false;
	}

// I think we are not using this loop anymore, Items are handled in the item script attached to the item game object.
	// void ActivateItem(string item)
	// {
	// 	if (item == "DoubleJump")
	// 	{
	// 		Player.doubleJump = true;			
	// 	}
	// }

	int PlayerDamage(int health, int damage, int armor, float damageCoolDown, ref float damageDecreasingCoolDown)
	{
		if (damageDecreasingCoolDown == 0)
		{
			health -= damage - armor;
			damageDecreasingCoolDown = damageCoolDown;
			return (health);
		}
		else
		{
			return (health);
		}
	}
	int EnnemyDamage(int health, int damage, int armor, float damageCoolDown, ref float damageDecreasingCoolDown)
	{
		if (damageDecreasingCoolDown == 0)
		{
			health -= damage - armor;
			damageDecreasingCoolDown = damageCoolDown;
			return (health);
		}
		else
		{
			return (health);
		}
	}

	string CalculateEnnemyPosition(float playerPosition, float ennemyPosition)
	{
		if (playerPosition > ennemyPosition)
		{
			return "bottom";
		}
		else if (playerPosition == ennemyPosition)
		{
			return "equal";
		}
		else
		{
			return "top";
		}
	}

void ResolveEnemyOverlap()
{
    // Only do this for the player to avoid weird enemy–enemy jitter
    if (!CompareTag("Player")) return;
    if (collider == null) return;

    // Don't resolve overlap while airborne.
    // This avoids fighting the stomp/bounce logic when landing on an enemy.
    if (!collisions.below)
        return;

    Bounds myBounds = collider.bounds;
    Vector2 center = myBounds.center;
    Vector2 size = myBounds.size;

    // 1) If we are also overlapping environment (walls), do not try to resolve here.
    //    Let the normal raycast collision handle that so we don't shove into walls.
    Collider2D envOverlap = Physics2D.OverlapBox(
        center,
        size,
        0f,
        collisionEnvMask
    );

    if (envOverlap != null)
    {
        return;
    }

    // 2) Find all enemies overlapping the player
    Collider2D[] overlaps = Physics2D.OverlapBoxAll(
        center,
        size,
        0f,
        collisionEnnemyMask
    );

    if (overlaps == null || overlaps.Length == 0)
        return;

    foreach (var overlap in overlaps)
    {
        if (overlap == null) continue;

        Bounds enemyBounds = overlap.bounds;

        // Horizontal penetration depth
        float playerHalf = myBounds.extents.x;
        float enemyHalf = enemyBounds.extents.x;

        float distX = center.x - enemyBounds.center.x;
        float penetrationX = playerHalf + enemyHalf - Mathf.Abs(distX);

        // If somehow not really overlapping on X, skip
        if (penetrationX <= 0f)
            continue;

        // Direction to push the player: away from enemy
        float dir = (distX >= 0f) ? 1f : -1f;

        // Add a tiny epsilon so we’re clearly outside
        float pushX = (penetrationX + 0.001f) * dir;

        Vector2 newCenter = center + new Vector2(pushX, 0f);

        // 3) Make sure the new position does NOT overlap walls
        Collider2D envCheck = Physics2D.OverlapBox(
            newCenter,
            size,
            0f,
            collisionEnvMask
        );

        if (envCheck != null)
        {
            // Can't safely push in X without going into a wall -> skip this enemy
            continue;
        }

        // 4) Apply the separation
        transform.Translate(new Vector2(pushX, 0f));

        // Update our bounds/center for subsequent enemies
        myBounds = collider.bounds;
        center = myBounds.center;
    }
}

	public struct CollisionInfo
	{
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
		public bool descendingSlope;
		public bool slidingDownMaxSlope;

		public float slopeAngle, slopeAngleOld;
		public Vector2 slopeNormal;
		public Vector2 moveAmountOld;
		public int faceDir;
		public bool fallingThroughPlatform;
		public string ennemyPlayerCollision;
		public string ennemyAttackCollision;
		public string playerEnnemyCollision;
		public string ennemyPosition;
		public string punch2Position;
		public void Reset()
		{
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;
			slopeNormal = Vector2.zero;
			punch2Position = "none";

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

	public struct ZoneInfo
	{
	    public bool insideZoomZone;
		public float zoomBackIn;
		public bool exitZone;
		public float zoomOut;
		public void Reset()
		{
			zoomBackIn = zoomOut = 0;
			exitZone = false;
		    insideZoomZone = false;
		}
	}
	
	bool TryApplyAttackHit(Collider2D attackCollider, bool horizontal)
	{
	    if (attackCollider == null) return false; // Prevents crash

		var enemy = GetComponentInParent<Ennemy>();
		if (enemy == null) return false;

		var hitbox = attackCollider.GetComponentInParent<AttackHitbox>();
		if (hitbox == null) hitbox = attackCollider.GetComponent<AttackHitbox>();
		if (hitbox == null)
		{
			Debug.LogError("Attack collider hit enemy but no AttackHitbox found on the Attack object.");
			return false;
		}

		enemy.health = EnnemyDamage(
			enemy.health,
			hitbox.damage,
			enemy.armor,
			enemy.damageCoolDown,
			ref enemy.damageDecreasingCoolDown
		);

		// Set knockback direction info used by Ennemy.CalculateVelocity()
		if (horizontal)
		{
			if (hitbox.dirX == 1) collisions.punch2Position = "right";
			else if (hitbox.dirX == -1) collisions.punch2Position = "left";
		}
		else
		{
			if (hitbox.dirY == 1) collisions.punch2Position = "bottom";
			else if (hitbox.dirY == -1) collisions.punch2Position = "top";
		}
		return true;
	}
}