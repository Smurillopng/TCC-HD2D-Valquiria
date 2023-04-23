// Created by Sérgio Murillo da Costa Faria
// Date: 06/04/2023

using System.Collections;
using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    
    [TitleGroup("Transition", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private string combatScene;
    [SerializeField, Required]
    private Volume volume;
    [SerializeField]
    private float fadeTime;
    
    [TitleGroup("Debug", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly] private float minimumEncounterChance;
    [SerializeField] private bool showEncounterLog;

    private Unit _selectedEnemy;
    private PlayerMovement _playerMovement; // The PlayerMovement component of the player
    private Vector3 _lastPosition; // The last position of the player
    private LiftGammaGain liftGammaGain;
    private bool isFading;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        _playerMovement = FindObjectOfType<PlayerMovement>(); // Get the PlayerMovement component
        areaEncounterRate = areas[SceneManager.GetActiveScene().name]; // Get the encounter rate for the current area
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
        volume.profile.TryGet(out liftGammaGain);
        var reader = QuickSaveReader.Create("GameSave");
        
        if (reader.Read<string>("CurrentScene") == SceneManager.GetActiveScene().name)
        {
            _playerMovement.CanMove.Value = true;
        }
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
        
        var save = QuickSaveWriter.Create("GameSave");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("LastScene", SceneManager.GetActiveScene().name);
        save.Write("EncounteredEnemy", _selectedEnemy.name);
        save.Commit();

        StartCoroutine(FadeIn(liftGammaGain));
        
        // TODO: Director/Actions
    }
    
    private IEnumerator FadeIn(LiftGammaGain lgg)
    {
        isFading = true;
        float time = 0;
        var defaultLgg = lgg.gamma.value;
        _playerMovement.CanMove.Value = false;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            lgg.gamma.value = new Vector4(-1, -1, -1, 0 - time / fadeTime);
            yield return null;
        }
        SceneManager.LoadScene(combatScene);
        isFading = false;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != combatScene && _playerMovement != null)
        {
            // TODO: Fix this
            _playerMovement.CanMove.Value = true;
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