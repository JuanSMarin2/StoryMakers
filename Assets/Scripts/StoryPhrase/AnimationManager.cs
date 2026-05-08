using UnityEngine;
using System.Collections.Generic;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private List<Animator> animCharacter1 = new List<Animator>();

    [SerializeField] private List<Animator> animCharacter2 = new List<Animator>();

    [SerializeField] private string[] animationTriggers;

    [SerializeField] private StoryBoardManager storyBoardManager;
    private int _currentAnimationIndex = 0;
    private int _currentSceneIndex = 0; //Determina el animator a usar entre 0 y 5
   
    void Start()
    {
        _currentAnimationIndex = 0;
   
    }

    void Update(){
     _currentSceneIndex = storyBoardManager.CurrentSceneNumber -1; 
    }

 
    public void TriggerAnimation(string triggerName, bool character1)
    {
        Debug.Log($"Triggering animation: {triggerName} for {(character1 ? "Character 1" : "Character 2")} at scene index {_currentSceneIndex}");
        if (character1)
        {
            animCharacter1[_currentSceneIndex].SetTrigger(triggerName);
        }
        else
        {
            animCharacter2[_currentSceneIndex].SetTrigger(triggerName);
        }
    }

    // Register an Animator for a copy at a specific index for character1 or character2.
    public void SetAnimatorAt(int index, Animator animator, bool character1)
    {
        List<Animator> target = character1 ? animCharacter1 : animCharacter2;
        if (target == null)
        {
            return;
        }

        // Ensure list has enough capacity
        while (target.Count <= index)
        {
            target.Add(null);
        }

        target[index] = animator;
    }

    public void NextAnimation( bool character1)
    {
        if(_currentAnimationIndex < animationTriggers.Length)
        {
            TriggerAnimation(animationTriggers[_currentAnimationIndex], character1);
            _currentAnimationIndex++;
        }
        else
        {
            _currentAnimationIndex = 0; 
            if (character1)
                animCharacter1[_currentSceneIndex].SetTrigger("Default");
            else
            animCharacter2[_currentSceneIndex].SetTrigger("Default");

        }

}
}
