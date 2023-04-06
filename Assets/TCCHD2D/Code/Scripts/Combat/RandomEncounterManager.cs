// Created by Sérgio Murillo da Costa Faria
// Date: 06/04/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RandomEncounterManager : MonoBehaviour
{
    public Unit player;
    public List<Unit> enemies; // A list of possible enemies to encounter
    [Range(0, 63)] public float areaEncounterRate; // The rate of encountering an enemy per second

    private PlayerMovement _playerMovement; // The PlayerMovement component of the player
    private Vector3 _lastPosition; // The last position of the player
    
    private Unit _selectedEnemy;
    [SerializeField, ReadOnly] private float minimumEncounterChance;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        _playerMovement = FindObjectOfType<PlayerMovement>(); // Get the PlayerMovement component
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
    }

    private void Update()
    {
        if (_playerMovement.CanMove.Value) // If the player is moving
        {
            if (_lastPosition != _playerMovement.MovementValue)
            {
                // TODO: Check once per step
                var randomChance = Random.Range(0f, 1f); // Get a random chance
                print($"Encounter chance: <color=blue>{minimumEncounterChance}</color> | Random chance: <color=green>{randomChance}</color>");
                if (randomChance < minimumEncounterChance)
                {
                        EncounterEnemy(); // Encounter an enemy
                }
            }
            _lastPosition = _playerMovement.MovementValue; // Update the last position
        }
    }

    private void EncounterEnemy()
    {
        var randomIndex = Random.Range(0, enemies.Count); // Get a random index
        _selectedEnemy = enemies[randomIndex]; // Get the enemy at that index

        Debug.Log("You encountered " + _selectedEnemy.name + "!"); // Log the encounter
        
        SceneManager.LoadScene("scn_combat");
        
        // TODO: Director/Actions
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "scn_combat")
        {
            var enemyObject = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
            enemyObject.Unit = _selectedEnemy;
        }

        if (scene.name != "scn_combat" && _playerMovement != null)
        {
            // TODO: Fix this
            _playerMovement.MovementValue = Vector3.zero;
            _playerMovement.Direction = Vector2.zero;
            _playerMovement.Movement();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}