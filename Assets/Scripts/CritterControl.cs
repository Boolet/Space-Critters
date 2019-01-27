using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CritterControl : MonoBehaviour
{
//================================================================================
// Required References
//================================================================================

    [SerializeField] CritterHead testHead;

//================================================================================
// Behavior Variables
//================================================================================

    [Space(10)]
    [SerializeField] float maxLiftTime = 3f;
    [SerializeField] float maxForwardTime = 3f;
    [SerializeField] float maxBackTime = 3f;
    [SerializeField] float maxDropTime = 3f;
    [SerializeField] float verticalHaltThreshold = 0.1f;
    [SerializeField] float horizontalHaltThreshold = 0.03f;

//================================================================================
// Movement State Enum
//================================================================================

    enum MoveState {
        LeftLegUp, LeftLegDown, LeftLegIn, LeftLegOut,
        RightLegUp, RightLegDown, RightLegIn, RightLegOut
    }

//================================================================================
// Local Variables
//================================================================================

    int orderCount = 8;
    MoveState[] moveOrderLeft = { MoveState.LeftLegUp, MoveState.LeftLegOut, MoveState.LeftLegDown, MoveState.LeftLegIn,
                            MoveState.RightLegUp, MoveState.RightLegIn, MoveState.RightLegDown, MoveState.RightLegOut};
    MoveState[] moveOrderRight = { MoveState.RightLegUp, MoveState.LeftLegOut, MoveState.RightLegOut, MoveState.RightLegDown,
                            MoveState.LeftLegUp, MoveState.RightLegIn, MoveState.LeftLegIn, MoveState.LeftLegDown};
    bool moveRight = true;
    int orderIndex = 0;
    float timer = 0f;

//================================================================================
// Code Control
//================================================================================

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //ManualControl();
        AutoControl();
    }

    void LegMotion(float input, CritterHead.Side side) {
        if (System.Math.Abs(input) < Mathf.Epsilon) {
            testHead.SetLockJoint(side, true);
            testHead.SetJointMotor(side, CritterHead.MotorState.Off);
        } else {
            testHead.SetLockJoint(side, false);
            CritterHead.MotorState state;
            if (input > 0) {
                state = CritterHead.MotorState.Extend;
            } else {
                state = CritterHead.MotorState.Retract;
            }
            testHead.SetJointMotor(side, state);
        }
    }

//================================================================================
// Walk Behavior
//================================================================================

    void AutoControl() {
        Walk();
    }

    void Walk() {
        timer += Time.fixedDeltaTime;
        MoveState[] moveOrder = moveRight ? moveOrderRight : moveOrderLeft;
        float progressForjoint = testHead.JointProgress(JointFromMoveState(moveOrder[orderIndex]));

        //Check if the state is complete; may add additional sensors later for things like ceiling collision.
        if (timer >= MaxTimeForState(moveOrder[orderIndex], moveRight)) {
            print("Timed out on: " + moveOrder[orderIndex]);
            IncrementState();
        } else if (ThresholdFinished(moveOrder[orderIndex], progressForjoint)) {
            print("Finished movement on: " + moveOrder[orderIndex]);
            IncrementState();
        }

        ExecuteMotion(moveOrder[orderIndex]);
    }

    void ExecuteMotion(MoveState state) {
        switch (state) {
            case MoveState.LeftLegDown:
                LegMotion(1f, CritterHead.Side.LeftVertical);
                break;
            case MoveState.LeftLegIn:
                LegMotion(-1f, CritterHead.Side.LeftHorizontal);
                break;
            case MoveState.LeftLegUp:
                LegMotion(-1f, CritterHead.Side.LeftVertical);
                break;
            case MoveState.LeftLegOut:
                LegMotion(1f, CritterHead.Side.LeftHorizontal);
                break;
            case MoveState.RightLegIn:
                LegMotion(-1f, CritterHead.Side.RightHorizontal);
                break;
            case MoveState.RightLegUp:
                LegMotion(-1f, CritterHead.Side.RightVertical);
                break;
            case MoveState.RightLegDown:
                LegMotion(1f, CritterHead.Side.RightVertical);
                break;
            default:
                LegMotion(1f, CritterHead.Side.RightHorizontal);
                break;
        }
    }

    void IncrementState() {
        Halt();
        orderIndex = (orderIndex + 1) % orderCount;
        timer = 0f;
    }

    void Halt() {
        LegMotion(0f, CritterHead.Side.LeftHorizontal);
        LegMotion(0f, CritterHead.Side.LeftVertical);
        LegMotion(0f, CritterHead.Side.RightHorizontal);
        LegMotion(0f, CritterHead.Side.RightVertical);
    }

//================================================================================
// Converters
//================================================================================

    CritterHead.Side JointFromMoveState(MoveState state) {
        switch (state) {
            case MoveState.LeftLegOut:
            case MoveState.LeftLegIn:
                return CritterHead.Side.LeftHorizontal;
            
            case MoveState.LeftLegUp:
            case MoveState.LeftLegDown:
                return CritterHead.Side.LeftVertical;
            
            case MoveState.RightLegIn:
            case MoveState.RightLegOut:
                return CritterHead.Side.RightHorizontal;
            
            default:
                return CritterHead.Side.RightVertical;
        }
    }

    /// <summary>
    /// Returns whether the distance threshold has been reached under the given criteria
    /// </summary>
    /// <returns><c>true</c>, if finished was thresholded, <c>false</c> otherwise.</returns>
    /// <param name="state">State.</param>
    /// <param name="progress">Progress.</param>
    bool ThresholdFinished(MoveState state, float progress) {
        if(state == MoveState.LeftLegOut || state == MoveState.LeftLegIn)
            print("Progress for " + state + ": " + progress);
        switch (state) {
            case MoveState.LeftLegUp:
            case MoveState.RightLegUp:
                return progress < verticalHaltThreshold;    //progress is smaller as leg rises; when less than threshold -> finished
            case MoveState.LeftLegOut:
            case MoveState.RightLegOut:
                return progress > 1f - horizontalHaltThreshold;   //progress is larger as leg moves away
            case MoveState.LeftLegIn:
            case MoveState.RightLegIn:
                return progress < (float)horizontalHaltThreshold;
            default:
                return progress > 1f - verticalHaltThreshold;
        }
    }

    /// <summary>
    /// Returns the maximum time allowed for the given state.
    /// </summary>
    /// <returns>The time for state.</returns>
    /// <param name="state">State.</param>
    /// <param name="moveRight">If set to <c>true</c> move right.</param>
    float MaxTimeForState(MoveState state, bool moveRight) {
        switch (state) {
            case MoveState.LeftLegUp:
            case MoveState.RightLegUp:
                return maxLiftTime;
            case MoveState.LeftLegOut:
            case MoveState.RightLegIn:
                return moveRight ? maxBackTime : maxForwardTime;
            case MoveState.LeftLegIn:
            case MoveState.RightLegOut:
                return moveRight ? maxForwardTime : maxBackTime;
            default:
                return maxDropTime;
        }
    }

//================================================================================
// Debugging
//================================================================================

    void ManualControl() {
        float leftFootDown = Input.GetAxis("Vertical");
        float rightFootDown = Input.GetAxis("Second Vertical");
        float leftFootOut = Input.GetAxis("Horizontal");
        float rightFootOut = Input.GetAxis("Second Horizontal");

        LegMotion(leftFootOut, CritterHead.Side.LeftHorizontal);
        LegMotion(leftFootDown, CritterHead.Side.LeftVertical);
        LegMotion(rightFootOut, CritterHead.Side.RightHorizontal);
        LegMotion(rightFootDown, CritterHead.Side.RightVertical);
    }
}
