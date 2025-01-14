﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	#region properties (private)
	
	[SerializeField] private float directionDampTime = .25f;
	[SerializeField] private Camera gamecam;
	[SerializeField] private float directionSpeed = 3.0f;
	[SerializeField] private float rotationDegreePerSecond = 120f;
	[SerializeField] private float locomotionThreshold = 0.2f;
	[SerializeField] private float speedDampTime = 0.05f;
	[SerializeField] private float jumpMultiplier = 1f;
	[SerializeField] private float jumpDist = 1f;
	[SerializeField] private CapsuleCollider capCollider;
	
	private Animator anim;
	private AnimatorStateInfo stateInfo;
	private AnimatorStateInfo transInfo;
	
	private float speed = 0.0f;
	private float direction = 0.0f;
	private float h = 0.0f;
	private float v = 0.0f;
	private float charAngle = 0.0f;
	private float capsuleHeight;	
	
	private const float SPRINT_SPEED = 2.0f;	
	private const float SPRINT_FOV = 75.0f;
	private const float NORMAL_FOV = 60.0f;
	
    private int hashLocomotionId = 0;
	private int hashLocomotionPivotLId = 0;
	private int hashLocomotionPivotRId = 0;	
	private int hashLocomotionPivotLTransId = 0;	
	private int hashLocomotionPivotRTransId = 0;	
	private int hashLocomotionJump = 0;
	private int hashIdleJump = 0;
	
	#endregion
	
	#region properties (private)
	
	#endregion
	
	#region unity event functions
	
	// Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
		capCollider = GetComponent<CapsuleCollider>();
		capsuleHeight = capCollider.height;
		
		if (anim.layerCount >= 2) {
			anim.SetLayerWeight(1, 1);
		}
		
		hashLocomotionId = Animator.StringToHash("Base Layer.Locomotion");
		hashLocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
		hashLocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
		hashLocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
		hashLocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
    }

    // Update is called once per frame
    void Update()
    {
		if (anim)
		{
			stateInfo = anim.GetCurrentAnimatorStateInfo(0);
			transInfo = anim.GetCurrentAnimatorStateInfo(0);
			
			
			anim.SetBool("Jump", Input.GetButton("Jump"));
		
			
			// Pull values from controller/keyboard
			v = Input.GetAxis("Vertical");
			h = Input.GetAxis("Horizontal");
			
			charAngle = 0.0f;
			direction = 0.0f;
			
			// Translate controls stick coordinates into world/cam/character space
            StickToWorldspace(this.transform, gamecam.transform, ref direction, ref speed, ref charAngle, isInPivot());	
			
			anim.SetFloat("Speed", speed, directionDampTime, Time.deltaTime);
			anim.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
			
			if (speed > locomotionThreshold) // Dead Zone
			{
				if (!isInPivot())
				{
					anim.SetFloat("Angle", charAngle);
				}
			}
			
			if (speed < locomotionThreshold && Mathf.Abs(h) < 0.05) // Dead Zone
			{
				anim.SetFloat("Direction", 0f);
				anim.SetFloat("Angle", 0f);
			}
			
		}
    }
	
	void FixedUpdate()
	{
		// Rotate character model right or left, but only if character is moving in that direction
		if (isInLocomotion() && ((direction >= 0 && h >= 0) || (direction < 0 && h < 0)))
		{
			Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (h < 0f ? -1f : 1f), 0f), Mathf.Abs(h));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
			this.transform.rotation = (this.transform.rotation * deltaRotation);
		}
		
		if (IsInJump())
		{
			float oldY = transform.position.y;
			transform.Translate(Vector3.up * jumpMultiplier * anim.GetFloat("JumpCurve"));
			if (IsInLocomotionJump())
			{
				transform.Translate(Vector3.forward * Time.deltaTime * jumpDist);
			}
			capCollider.height = capsuleHeight + (anim.GetFloat("CapsuleCurve") * 0.5f);
		}
	}
	
	
	#endregion
	
	#region methods
	
	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting)
    {
        Vector3 rootDirection = root.forward;
				
        Vector3 stickDirection = new Vector3(h, 0, v);
		
		speedOut = stickDirection.sqrMagnitude;		

        // Get camera rotation
        Vector3 CameraDirection = camera.forward;
        CameraDirection.y = 0.0f; // kill Y
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(CameraDirection));

        // Convert joystick input in Worldspace coordinates
        Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);
		
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2.5f, root.position.z), axisSign, Color.red);
		
		float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		
		if (!isPivoting)
		{
			angleOut = angleRootToMove;
		}
		
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;
	}	
	
	public bool isInLocomotion()
    {
        return stateInfo.nameHash == hashLocomotionId;
    }
	
	public bool isInPivot()
	{
		return stateInfo.nameHash == hashLocomotionPivotLId || 
			stateInfo.nameHash == hashLocomotionPivotRId || 
			transInfo.nameHash == hashLocomotionPivotLTransId || 
			transInfo.nameHash ==hashLocomotionPivotRTransId;
	}
	
	public bool IsInJump()
	{
		return (IsInIdleJump() || IsInLocomotionJump());
	}
	
	public bool IsInIdleJump()
	{
		return stateInfo.nameHash == hashIdleJump;
	}
	
	public bool IsInLocomotionJump()
	{
		return stateInfo.nameHash == hashLocomotionJump;
	}
	
	#endregion
}
