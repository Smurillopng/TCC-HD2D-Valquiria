using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTree : MonoBehaviour
{
    private AIController aiController;

    void Start()
    {
        aiController = GetComponent<AIController>();
    }

    void Update()
    {
        if (aiController.CanSeePlayer())
        {
            aiController.ChasePlayer();
        }
        else
        {
            aiController.PatrolRandomly();
        }
    }
}
