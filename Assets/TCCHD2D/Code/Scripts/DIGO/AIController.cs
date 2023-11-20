using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed = 5f;
    public float patrolSpeed = 2f;

    private Vector3 patrolDestination;
    private bool isPatrolling = false;

    // Verifica se o jogador está à vista
    public bool CanSeePlayer()
    {
        Vector3 direction = player.position - transform.position;
        if (Vector3.Angle(transform.forward, direction) < 60)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Persegue o jogador
    public void ChasePlayer()
    {
        transform.LookAt(player);
        transform.Translate(Vector3.forward * chaseSpeed * Time.deltaTime);
    }

    // Patrulha aleatoriamente
    public void PatrolRandomly()
    {
        if (!isPatrolling)
        {
            patrolDestination = Random.insideUnitSphere * 10f; // Destino aleatório
            patrolDestination.y = 0f; // Mantém a IA no mesmo nível que o chão
            isPatrolling = true;
        }

        transform.LookAt(patrolDestination);
        transform.Translate(Vector3.forward * patrolSpeed * Time.deltaTime);

        // Verifica se chegou ao destino
        if (Vector3.Distance(transform.position, patrolDestination) < 1f)
        {
            isPatrolling = false;
        }
    }
}
