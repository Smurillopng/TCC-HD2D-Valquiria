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
/// Created by Sérgio Murillo da Costa Faria on 06/04/2023.
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
    [Tooltip("The minimum amount of steps to start encountering an enemy")]
    private int minimumSteps;

    [TitleGroup("Transition", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [ValidateInput("ValidateScene", "The combat scene must exist in the build settings")]
    [Tooltip("The name of the combat scene")]
    private string combatScene;
    
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
    [Tooltip("The current amount of steps the player has taken")]
    private int currentSteps;
    
    [SerializeField]
    [Tooltip("Whether to show the encounter chance log in the console")]
    private bool showEncounterLog;
    
    private Unit _selectedEnemy; // The encountered enemy
    private PlayerMovement _playerMovement; // The PlayerMovement component of the player
    private Vector3 _lastPosition; // The last position of the player
    private LiftGammaGain _liftGammaGain; // The property responsible for the fade effect

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Register the OnSceneLoaded method to be called when a scene is loaded.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Called before the first frame update.
    /// Gets the PlayerMovement component, initializes some variables and reads data from a QuickSave file.
    /// </summary>
    private void Start()
    {
        _playerMovement = FindObjectOfType<PlayerMovement>(); // Get the PlayerMovement component
        currentSteps = 0; // Set the current steps to 0
        minimumEncounterChance = areaEncounterRate / 100f; // Calculate the minimum encounter chance
        volume.profile.TryGet(out _liftGammaGain);
        var reader = QuickSaveReader.Create("GameSave");
        
        if (reader.Read<string>("CurrentScene") == SceneManager.GetActiveScene().name)
        {
            _playerMovement.CanMove.Value = true;
        }
    }
    
    /// <summary>
    /// Called after a new scene has finished loading.
    /// If the current scene is not the combat scene and the PlayerMovement component exists,
    /// resets the movement of the player to zero and enables movement.
    /// </summary>
    /// <param name="scene">The loaded scene.</param>
    /// <param name="mode">The scene loading mode.</param>
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

    /// <summary>
    /// Called when the behaviour becomes disabled.
    /// Unregister the OnSceneLoaded method from being called when a scene is loaded.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Checks the number of steps taken and decides whether to initiate an encounter with an enemy.
    /// </summary>
    public void CheckStep()
    {
        currentSteps++;
        var randomChance = Random.Range(0f, 1f);
        if (showEncounterLog) print($"Encounter chance: <color=blue>{minimumEncounterChance}</color> | Random chance: <color=green>{randomChance}</color>");
        if (!(randomChance < minimumEncounterChance) || currentSteps < minimumSteps) return;
        EncounterEnemy();
        currentSteps = 0;
    }

    /// <summary>
    /// Initiates an encounter with an enemy.
    /// </summary>
    private void EncounterEnemy()
    {
        var randomIndex = Random.Range(0, enemies.Count);
        _selectedEnemy = enemies[randomIndex];

        var save = QuickSaveWriter.Create("GameSave");
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("LastScene", SceneManager.GetActiveScene().name);
        save.Write("EncounteredEnemy", _selectedEnemy.name);
        save.Commit();

        StartCoroutine(FadeIn(_liftGammaGain));
    }
    
    /// <summary>
    /// Fades in the scene for combat.
    /// </summary>
    /// <param name="lgg">The LiftGammaGain object used for fading.</param>
    /// <returns>An IEnumerator object for the coroutine.</returns>
    private IEnumerator FadeIn(LiftGammaGain lgg)
    {
        float time = 0;
        _playerMovement.CanMove.Value = false;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            lgg.gamma.value = new Vector4(-1, -1, -1, 0 - time / fadeTime);
            yield return null;
        }
        SceneManager.LoadScene(combatScene);
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