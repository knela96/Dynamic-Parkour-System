using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubStateMonitor : StateMachineBehaviour
{
    [SerializeField] string myBool = "ClimbAnimations";
    public delegate void OnExitClimbingDelegate();
    public event OnExitClimbingDelegate OnExitClimbing;

    override public void OnStateEnter(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
    {
        anim.SetBool(myBool, true);
    }

    override public void OnStateExit(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(stateInfo.IsTag("Drop"))
            anim.SetBool(myBool, false);
    }


    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    //override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}
}
