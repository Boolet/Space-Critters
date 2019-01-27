using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CritterHead : MonoBehaviour
{

//==============================================================================
// Enums
//==============================================================================

    public enum Side {
        LeftHorizontal, LeftVertical, RightHorizontal, RightVertical
    }

    public enum MotorState {
        Extend, Retract, Off
    }

//==============================================================================
// Required references
//==============================================================================

    [SerializeField] SliderJoint2D leftHorizontalJoint;
    [SerializeField] SliderJoint2D leftVerticalJoint;
    [SerializeField] SliderJoint2D rightHorizontalJoint;
    [SerializeField] SliderJoint2D rightVerticalJoint;

    [Space(10)]
    [SerializeField] FixedJoint2D leftHorizontalLock;
    [SerializeField] FixedJoint2D leftVerticalLock;
    [SerializeField] FixedJoint2D rightHorizontalLock;
    [SerializeField] FixedJoint2D rightVerticalLock;

//==============================================================================
// Adjustment variables
//==============================================================================

    [Space(10)]
    [SerializeField] float horizontalMax = 1.5f;
    [SerializeField] float horizontalMin = 0.5f;
    [SerializeField] float critterStrength = 50f;
    [SerializeField] float targetSpeed = 1f;

    float jointLockTolerance = 0.01f;

//==============================================================================
// Local variables
//==============================================================================

    JointTranslationLimits2D leftVerticalLimits;
    JointTranslationLimits2D rightVerticalLimits;
    //the horizontal limits are always the same

//==============================================================================
// Sensors
//==============================================================================

    /// <summary>
    /// Returns the progress, in percent of total distance, that the object has moved
    /// along the joint path. Note that the value increases DOWN and IN and decreases
    /// OUT and UP.
    /// </summary>
    /// <returns>The progress.</returns>
    /// <param name="side">Side.</param>
    public float JointProgress(Side side) {
        SliderJoint2D joint = SideToSlideJoint(side);
        return Mathf.InverseLerp(joint.limits.min, joint.limits.max, joint.jointTranslation);
    }

    /*
    public bool LimbContact(Side side) {

    }
    */   

//==============================================================================
// Code Control
//==============================================================================

    // Start is called before the first frame update
    void Start()
    {
        leftVerticalLimits = leftVerticalJoint.limits;
        rightVerticalLimits = rightVerticalJoint.limits;

        JointMotor2D defaultMotor = new JointMotor2D() {
            maxMotorTorque = critterStrength
        };
        leftVerticalJoint.motor = defaultMotor;
        leftHorizontalJoint.motor = defaultMotor;
        rightVerticalJoint.motor = defaultMotor;
        rightHorizontalJoint.motor = defaultMotor;
    }

    /// <summary>
    /// Locks or unlocks the joint.
    /// </summary>
    /// <param name="side">Side.</param>
    /// <param name="locked">If set to <c>true</c> locked.</param>
    public void SetLockJoint(Side side, bool locked) {
        SliderJoint2D joint = SideToSlideJoint(side);
        //print(joint.jointTranslation);
        if (locked)
            LockJoint(side);
        else
            UnlockJoint(side);
    }

    /// <summary>
    /// Changes the state of the joint motor.
    /// For the vertical joints, Extend is down and Retract is up.
    /// </summary>
    /// <param name="side">Side.</param>
    /// <param name="state">State.</param>
    public void SetJointMotor(Side side, MotorState state) {
        SliderJoint2D joint = SideToSlideJoint(side);
        if (state == MotorState.Off) {
            joint.useMotor = false;
        } else {
            float speed = targetSpeed * (state == MotorState.Extend ? 1 : -1);
            JointMotor2D motor = joint.motor;
            motor.motorSpeed = speed;
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

//==============================================================================
// Locking and unlocking systems
//==============================================================================

    /// <summary>
    /// Locks the joint on the specified side.
    /// </summary>
    /// <param name="side">Side.</param>
    void LockJoint(Side side) {
        SetLockViaFixed(side, true);
    }

    /// <summary>
    /// Locks a slider joint connection by squeezing its limits down to near zero space.
    /// </summary>
    /// <param name="side">Side.</param>
    void LockViaLimits(Side side) {
        SliderJoint2D joint = SideToSlideJoint(side);
        if (joint.limits.max - joint.limits.min <= jointLockTolerance * 2) {
            print("Locked");
            return;
        }
        JointTranslationLimits2D lockedLimits = new JointTranslationLimits2D {
            min = joint.jointTranslation - jointLockTolerance,
            max = joint.jointTranslation + jointLockTolerance
        };
        joint.limits = lockedLimits;
    }

    /// <summary>
    /// Locks a slider joint by disabling it and enabling a fixed joint instead.
    /// </summary>
    /// <param name="side">Side.</param>
    void SetLockViaFixed(Side side, bool locked) {
        SliderJoint2D joint = SideToSlideJoint(side);
        FixedJoint2D lockJoint = SideToLockJoint(side);

        joint.enabled = !locked;
        lockJoint.enabled = locked;
    }

    /// <summary>
    /// Unlocks the joint on the specifide side.
    /// </summary>
    /// <param name="side">Side.</param>
    void UnlockJoint(Side side) {
        SetLockViaFixed(side, false);
    }

    /// <summary>
    /// Unlocks the given joint by adjusting its limits
    /// </summary>
    /// <param name="side">Side.</param>
    void UnlockViaLimits(Side side) {
        SliderJoint2D joint = SideToSlideJoint(side);
        if (side == Side.LeftHorizontal || side == Side.RightHorizontal) {
            JointTranslationLimits2D unlockedLimits = new JointTranslationLimits2D {
                max = horizontalMax,
                min = horizontalMin
            };
            joint.limits = unlockedLimits;
        } else {
            if (side == Side.RightVertical)
                joint.limits = rightVerticalLimits;
            else
                joint.limits = leftVerticalLimits;
        }
    }

//==============================================================================
// Converters
//==============================================================================

    /// <summary>
    /// Returns the joint that corresponds with the given side.
    /// </summary>
    /// <returns>The slide joint.</returns>
    /// <param name="side">Side.</param>
    SliderJoint2D SideToSlideJoint(Side side) {
        switch (side) {
            case Side.LeftHorizontal:
                return leftHorizontalJoint;
            case Side.LeftVertical:
                return leftVerticalJoint;
            case Side.RightHorizontal:
                return rightHorizontalJoint;
            default:
                return rightVerticalJoint;
        }
    }

    /// <summary>
    /// Returns the lock that corresponds with the given side.
    /// </summary>
    /// <returns>The lock joint.</returns>
    /// <param name="side">Side.</param>
    FixedJoint2D SideToLockJoint(Side side) {
        switch (side) {
            case Side.LeftHorizontal:
                return leftHorizontalLock;
            case Side.LeftVertical:
                return leftVerticalLock;
            case Side.RightHorizontal:
                return rightHorizontalLock;
            default:
                return rightVerticalLock;
        }
    }
}
