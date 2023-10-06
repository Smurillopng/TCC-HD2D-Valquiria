using System.Collections;
using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
/// This class manages the random enemy encounters for the player.
/// </summary>
/// <remarks>
/// This class manages the random enemy encounters for the player. It is responsible for generating a random amount of steps to start encountering an enemy,
/// calculating the minimum encounter chance, generating a random number to check if an enemy will be encountered, selecting a random enemy from a list of possible enemies,
/// and loading the combat scene.
/// </remarks>
[HideMonoScript]
public class RandomEncounterManager : SerializedMonoBehaviour
{
    #region === Variables ===============================================================

    [TitleGroup("Units", Alignment = TitleAlignments.Centered)]
    [Tooltip("The player unit")]
    public Unit player;

    [Tooltip("A list of possible enemies to encounter")]
    public List<Unit> enemies;

    [TitleGroup("Rates", Alignment = TitleAlignments.Centered)]
    [Range(0, 100)]
    [Tooltip("The rate of encountering an enemy event called in the walking animation")]
    public float areaEncounterRate;

    [SerializeField]
    [MinValue(1)]
    [Tooltip("The minimum and maximum amount of steps to start encountering an enemy")]
    private int minimumSteps, maximumSteps;

    [TitleGroup("Transition", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [ValidateInput("ValidateScene", "The combat scene must exist in the build settings")]
    [Tooltip("The name of the combat scene")]
    private string combatScene;

    [SerializeField]
    [Tooltip("The name of the combat scene")]
    private CombatScenarios combatScenario;

    [SerializeField]
    [Required]
    [Tooltip("The volume to apply fade effect to")]
    private Volume volume;

    [SerializeField]
    [Tooltip("The duration of the fade-in effect")]
    private float fadeTime;

    [TitleGroup("Debug", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [ReadOnly]
    [Tooltip("The minimum encounter chance in this area")]
    private float minimumEncounterChance;

    [SerializeField]
    [ReadOnly]
    [Tooltip("The random amount of steps to start encountering an enemy")]
    private int randomSteps;

    [SerializeField]
    [ReadOnly]
    [Tooltip("The current amount of steps the player has taken")]
    private int currentSteps;

    [SerializeField]
    [Tooltip("Whether to show the encounter chance log in the console")]
    private bool showEncounterLog;

    private Unit _selectedEnemy; // The encountered enemy
    private PlayerMovement _playerMovement; // The PlayerMovement component of the player
    private Vector3 _lastPosition; // The last position of the player
    private LiftGammaGain _liftGammaGain; // The property responsible for the fade effect
    private bool _randomized; // Whether the random amount of steps was already generated
    private Tutorial _tutorial; // The Tutorial component of the player
    private SceneTransitioner _transitioner;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Subscribe to the sceneLoaded event.</summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    /// <summary>Starts the game.</summary>
    /// <remarks>
    /// This method initializes the player movement, sets the current steps to 0, calculates the minimum encounter chance based on the area encounter rate, and retrieves the lift gamma gain from the volume profile.
    /// </remarks>
    private void Start()
    {
        _playerMovement = FindObjectOfType<PlayerMovement>(); // Get the PlayerMovement component
        _transitioner = FindObjectOfType<SceneTransitioner>();
        currentSteps = 0; // Set the current steps to 0
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
        volume.profile.TryGet(out _liftGammaGain);

        _playerMovement.CanMove.Value = true;

        if (SceneManager.GetActiveScene().name.Equals("scn_game"))
            _tutorial = FindObjectOfType<Tutorial>();
    }
    /// <summary>Callback function that is called when a scene is loaded.</summary>
    /// <param name="scene">The scene that was loaded.</param>
    /// <param name="mode">The mode used to load the scene.</param>
    /// <remarks>
    /// If the loaded scene is not the combat scene and the player movement component is not null, 
    /// the player's movement is enabled and reset to zero, and the player's movement function is called.
    /// </remarks>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != combatScene && _playerMovement != null)
        {
            _playerMovement.CanMove.Value = true;
            _playerMovement.MovementValue = Vector3.zero;
            _playerMovement.Direction = Vector2.zero;
            _playerMovement.Movement();
        }
    }
    /// <summary>Unsubscribes the OnSceneLoaded method from the SceneManager's sceneLoaded event.</summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>Checks if the player has taken a step and triggers an enemy encounter if the conditions are met.</summary>
    /// <remarks>
    /// The function checks if the number of steps taken by the player is greater than or equal to a random number of steps between the minimum and maximum steps allowed. If the random chance of encountering an enemy is less than the minimum encounter chance and the current number of steps is greater than or equal to the random number of steps, an enemy encounter is triggered. The function also resets the current number of steps and the randomized flag.
    /// </remarks>
    public void CheckStep()
    {
        if (!_randomized)
        {
            randomSteps = Random.Range(minimumSteps, maximumSteps);
            _randomized = true;
        }
        currentSteps++;
        var randomChance = Random.Range(0f, 1f);
        if (showEncounterLog) print($"Encounter chance: <color=blue>{minimumEncounterChance}</color> | Random chance: <color=green>{randomChance}</color>");
        if (!(randomChance < minimumEncounterChance) || currentSteps < randomSteps) return;
        EncounterEnemy();
        currentSteps = 0;
        _randomized = false;
    }
    /// <summary>Encounters a random enemy.</summary>
    /// <remarks>
    /// Selects a random enemy from the list of enemies.
    /// Then, starts a coroutine to fade in the screen.
    /// </remarks>
    private void EncounterEnemy()
    {
        var randomIndex = Random.Range(0, enemies.Count);
        _selectedEnemy = enemies[randomIndex];

        var save = QuickSaveWriter.Create("GameInfo");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("LastScene", SceneManager.GetActiveScene().name);
        save.Write("EncounteredEnemy", _selectedEnemy.name);
        save.Commit();

        StartCoroutine(_transitioner.TransitionTo(combatScene));
        SceneManager.sceneLoaded += SetScene;
    }
    /// <summary>Encounters the final boss, saves the game, and fades in the screen.</summary>
    /// <remarks>
    /// Sets the selected enemy to the first enemy in the enemies array.
    /// Fades in the screen by starting a coroutine that gradually increases the gamma gain of the lift.
    /// </remarks>
    public void SpecificEncounter(Unit enemy)
    {
        _selectedEnemy = enemy;

        var save = QuickSaveWriter.Create("GameInfo");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("LastScene", SceneManager.GetActiveScene().name);
        save.Write("EncounteredEnemy", _selectedEnemy.name);
        save.Commit();

        StartCoroutine(_transitioner.TransitionTo(combatScene));
        SceneManager.sceneLoaded += SetScene;
    }

    public void TutorialEncounter(Unit enemy)
    {
        _selectedEnemy = enemy;

        var save = QuickSaveWriter.Create("GameInfo");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("LastScene", SceneManager.GetActiveScene().name);
        save.Write("EncounteredEnemy", _selectedEnemy.name);
        save.Commit();

        _tutorial.director.Pause();
        StartCoroutine(_transitioner.TransitionTo("scn_combat_tutorial"));
        SceneManager.sceneLoaded += SetScene;
    }


    /// <summary>Sets the current scene and performs any necessary actions based on the scene type.</summary>
    /// <param name="scene">The scene to set.</param>
    /// <param name="mode">The mode to load the scene in.</param>
    /// <remarks>
    /// If the scene is already loaded and is a combat scene, this method will activate the scenario corresponding to the current combat scenario.
    /// </remarks>
    private void SetScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.isLoaded && PlayerControls.Instance.SceneMap.TryGetValue(scene.name, out var type))
        {
            if (type == SceneType.Combat)
            {
                var setter = FindObjectOfType<SetScenario>();
                foreach (var scenario in setter.scenarios)
                {
                    scenario.Value.SetActive(scenario.Key == combatScenario);
                }
            }
        }
    }

    [Button]
    private void UpdateEncounterChance()
    {
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
    }

    #endregion

    #region === Validation Methods ======================================================

    /// <summary>
    /// Validates whether the given scene name exists in the build settings.
    /// </summary>
    /// <param name="value">The scene name to validate.</param>
    /// <returns>True if the scene name exists in the build settings, false otherwise.</returns>
    private bool ValidateScene(string value)
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == value) return true;
        }
        return false;
    }

    #endregion
}