// Created by Sérgio Murillo da Costa Faria
// Date: 06/04/2023

using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RandomEncounterManager : SerializedMonoBehaviour
{
    [TitleGroup("Units", Alignment = TitleAlignments.Centered)]
    public Unit player;
    public List<Unit> enemies; // A list of possible enemies to encounter
    
    [TitleGroup("Rates", Alignment = TitleAlignments.Centered)]
    public Dictionary<string, float> areas; // A dictionary of areas and their encounter rates
    [Range(0, 100)] public float areaEncounterRate; // The rate of encountering an enemy per second

    [TitleGroup("Debug", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly] private float minimumEncounterChance;
    [SerializeField] private bool showEncounterLog;

    private Unit _selectedEnemy;
    private PlayerMovement _playerMovement; // The PlayerMovement component of the player
    private Vector3 _lastPosition; // The last position of the player

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        _playerMovement = FindObjectOfType<PlayerMovement>(); // Get the PlayerMovement component
        areaEncounterRate = areas[SceneManager.GetActiveScene().name]; // Get the encounter rate for the current area
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
    }

    public void CheckStep()
    {
        var randomChance = Random.Range(0f, 1f); // Get a random chance
        if (showEncounterLog) print($"Encounter chance: <color=blue>{minimumEncounterChance}</color> | Random chance: <color=green>{randomChance}</color>");
        if (randomChance < minimumEncounterChance)
        {
            EncounterEnemy(); // Encounter an enemy
        }
    }

    private void EncounterEnemy()
    {
        var randomIndex = Random.Range(0, enemies.Count); // Get a random index
        _selectedEnemy = enemies[randomIndex]; // Get the enemy at that index

        Debug.Log("You encountered " + _selectedEnemy.name + "!"); // Log the encounter

        GlobalHelper.Instance.SaveScene();
        var save = QuickSaveWriter.Create("GameSave");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Commit();
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