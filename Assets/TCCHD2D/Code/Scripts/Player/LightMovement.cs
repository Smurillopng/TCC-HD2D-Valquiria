using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightMovement : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    [SerializeField] private Transform backPosition;
    [SerializeField] private Transform frontPosition;
    [SerializeField] private Transform leftPosition;
    [SerializeField] private Transform rightPosition;
    
    private void Start()
    {
        _playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }
    
    private void Update()
    {
        if (_playerMovement.Direction.x == 1 && transform.position != rightPosition.position)
        {
            transform.position = rightPosition.position;
        }
        else if (_playerMovement.Direction.x == -1 && transform.position != leftPosition.position)
        {
            transform.position = leftPosition.position;
        }
        else if (_playerMovement.Direction.y == 1 && transform.position != backPosition.position)
        {
            transform.position = backPosition.position;
        }
        else if (_playerMovement.Direction.y == -1 && transform.position != frontPosition.position)
        {
            transform.position = frontPosition.position;
        }
    }
}
