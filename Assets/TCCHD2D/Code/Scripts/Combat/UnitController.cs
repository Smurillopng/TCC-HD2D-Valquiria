// Created by Sérgio Murillo da Costa Faria

using System.Collections;
using CI.QuickSave;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

[HideMonoScript]
public class UnitController : MonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Unit Controller")]
    [BoxGroup("Unit Controller/Unit Info")]
    [SerializeField, InlineEditor, Tooltip("The unit data that this controller controls.")]
    private Unit unit;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The playable director that controls the animations of this unit.")]
    private PlayableDirector director;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicAttack;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicCutAttack;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicBluntAttack;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicRangedAttack;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's use item action.")]
    private TimelineAsset useItem;

    [FoldoutGroup("Unit Controller/Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's run action.")]
    private TimelineAsset run;

    [FoldoutGroup("Unit Controller/Vfx's")]
    [SerializeField]
    private VisualEffect hitVfx;

    [FoldoutGroup("Unit Controller/Vfx's")]
    [SerializeField]
    private VisualEffect swordAttackVfx;

    [FoldoutGroup("Unit Controller/Vfx's")]
    [SerializeField]
    private VisualEffect hammerAttackVfx;

    [FoldoutGroup("Unit Controller/Unit Floating Numbers")]
    [SerializeField, Tooltip("The animator that controls the damage text animation.")]
    private Animator damageTextAnimator;

    [FoldoutGroup("Unit Controller/Unit Floating Numbers")]
    [SerializeField, Tooltip("The text that displays the damage taken by the unit.")]
    private TMP_Text damageText;

    [FoldoutGroup("Unit Controller/Debug")]
    [ReadOnly]
    public int damageTakenThisTurn;

    [FoldoutGroup("Unit Controller/Debug")]
    [ReadOnly]
    public int attackDamageCalculated;

    [FoldoutGroup("Unit Controller/Debug")]
    [ReadOnly]
    public int defenceCalculated;

    [FoldoutGroup("Unit Controller/Debug")]
    [ReadOnly]
    public int speedCalculated;

    [FoldoutGroup("Unit Controller/Debug")]
    [ReadOnly]
    public int charges;

    private int _ongoingChargeAttacks;
    private bool _criticalHit, _isCoroutineRunning;
    private PlayerCombatHUD _playerCombatHUD;
    private SceneTransitioner _sceneTransitioner;

    #endregion ==========================================================================

    #region === Properties ==============================================================

    public Unit Unit
    {
        get => unit;
        set => unit = value;
    }
    public PlayableDirector Director => director;
    public TimelineAsset BasicAttack => basicAttack;
    public TimelineAsset BasicCutAttack => basicCutAttack;
    public TimelineAsset BasicBluntAttack => basicBluntAttack;
    public TimelineAsset BasicRangedAttack => basicRangedAttack;
    public TimelineAsset UseItem => useItem;
    public TimelineAsset Run => run;
    public VisualEffect HitVfx => hitVfx;
    public int Charges
    {
        get => charges;
        set => charges = value;
    }

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the unit's state when it wakes up.</summary>
    /// <remarks>
    /// If the unit is not a player, it sets its state to alive with full health and the basic attack animation.
    /// If the unit is a player, it checks its current health and death status and updates them accordingly.
    /// </remarks>
    public void Awake()
    {
        if (_playerCombatHUD == null) _playerCombatHUD = FindObjectOfType<PlayerCombatHUD>();
        if (_sceneTransitioner == null) _sceneTransitioner = FindObjectOfType<SceneTransitioner>();
        TurnManager.onDeath += SetDead;
        if (!unit.IsPlayer)
        {
            unit.IsDead = false;
            unit.CurrentHp = unit.MaxHp;
            basicAttack = unit.AttackAnimation;
            // TODO instanciar inimigo ao invés disso
        }
        else
        {
            if (unit.CurrentHp == unit.MaxHp && unit.IsDead)
                unit.IsDead = false;
            if (!unit.IsDead && unit.CurrentHp == 0)
                unit.CurrentHp = unit.MaxHp;
            if (unit.IsDead && unit.CurrentHp == 0)
            {
                unit.CurrentHp = unit.MaxHp;
                unit.IsDead = false;
            }
        }
    }
    /// <summary>Updates the current health and TP of a unit if they exceed their maximum values.</summary>
    /// <remarks>
    /// If the unit is a player and their current TP exceeds their maximum TP, their current TP is set to their maximum TP.
    /// If the unit is a player and their current health exceeds their maximum health, their current health is set to their maximum health.
    /// If the unit is not a player and their current health exceeds their maximum health, their current health is set to their maximum health.
    /// </remarks>
    private void Update()
    {
        if (unit.IsPlayer && unit.CurrentTp > unit.MaxTp)
            unit.CurrentTp = unit.MaxTp;
        if (unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
        if (!unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
        if (charges <= 0 && _isCoroutineRunning)
        {
            //stop the coroutine only if it is running
            StopCoroutine(ChargeAttackCoroutine(null)); // Stop the coroutine
            _isCoroutineRunning = false;
        }
    }

    private void OnDisable()
    {
        TurnManager.onDeath -= SetDead;
    }
    #endregion ==========================================================================

    #region === Methods =================================================================

    /// <summary>Performs an attack action on a target unit.</summary>
    /// <param name="target">The unit controller representing the target of the attack.</param>
    /// <remarks>Updates the combat HUD to reflect the attack action.</remarks>
    public void AttackAction(UnitController target)
    {
        AttackLogic(target);
        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
    }
    /// <summary>Executes the attack logic on a target unit.</summary>
    /// <param name="target">The target unit to attack.</param>
    /// <remarks>
    /// This method calculates the attack damage based on the unit's attack stat and any equipped weapon.
    /// If the unit has charges, the attack damage is increased by the number of charges and the charges are reset to 0.
    /// If the unit is a player, it sets up the animation and signal bindings for the basic attack.
    /// If the player has a weapon equipped, it plays the appropriate attack animation based on the weapon's attack type.
    /// If the player's TP is less than the maximum TP, it increases the TP by 10 and updates the player's TP
    /// </remarks>
    private void AttackLogic(UnitController target)
    {
        attackDamageCalculated = unit.Attack;
        foreach (var equipment in InventoryManager.Instance.EquipmentSlots)
        {
            attackDamageCalculated += equipment.equipItem != null ? equipment.equipItem.StatusValue.Attack : 0;
        }
        CalcDamage(target);

        if (charges > 0)
            StartCoroutine(ChargeAttackCoroutine(target));
        else
            PlayerCombatHUD.TakenAction.Invoke();

        if (_ongoingChargeAttacks > 0)
            StartCoroutine(WaitForChargeAttacksToFinish());
    }

    private IEnumerator ChargeAttackCoroutine(UnitController target)
    {
        _isCoroutineRunning = true;
        _ongoingChargeAttacks++; // Increment the counter
        while (charges > 0)
        {
            charges--;
            var animationDuration = (float)Director.duration;
            yield return new WaitForSeconds(animationDuration); // Wait for the animation to finish
            CalcDamage(target);
        }
        _ongoingChargeAttacks--; // Decrement the counter
    }

    private IEnumerator WaitForChargeAttacksToFinish()
    {
        yield return new WaitWhile(() => _ongoingChargeAttacks > 0); // Wait until all ongoing charge attacks finish
        PlayerCombatHUD.TakenAction.Invoke();
        _playerCombatHUD.playerCharges.fillAmount -= 0.25f;
    }

    public IEnumerator CritNumbers(UnitController target)
    {
        yield return new WaitUntil(() => target.damageTextAnimator.GetCurrentAnimatorClipInfo(0) != null);
        target.damageText.color = Color.red;
        target.damageText.outlineColor = Color.yellow;
        target.damageText.fontSize += 50;
        yield return new WaitForSeconds(target.damageTextAnimator.GetCurrentAnimatorStateInfo(0).length * 3);
        target.damageText.color = Color.black;
        target.damageText.outlineColor = Color.white;
        target.damageText.fontSize -= 50;
    }
    public IEnumerator HealingNumbers(UnitController target)
    {
        yield return new WaitUntil(() => target.damageTextAnimator.GetCurrentAnimatorClipInfo(0) != null);
        target.damageText.color = Color.green;
        target.damageText.outlineColor = Color.magenta;
        target.damageText.fontSize += 20;
        yield return new WaitForSeconds(target.damageTextAnimator.GetCurrentAnimatorStateInfo(0).length * 2);
        target.damageText.color = Color.black;
        target.damageText.outlineColor = Color.white;
        target.damageText.fontSize -= 20;
    }

    public void CalcDamage(UnitController target)
    {
        var randomFactor = Random.Range(0f, 1f);
        var criticalHitChance = (unit.Luck / 100f) + randomFactor;
        _criticalHit = criticalHitChance >= 0.9f;

        PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        CalcAnimation();

        if (unit.IsPlayer && unit.CurrentTp < unit.MaxTp && charges <= 0)
        {
            unit.CurrentTp += 10;
            unit.CurrentTp = Mathf.Clamp(unit.CurrentTp, 0, unit.MaxTp);
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }

        var math = Random.Range(((unit.Level * 2) + 2) / 5 - 1, ((unit.Level * 2) + 2) / 5 + 1);
        var randomIncDec = Mathf.RoundToInt(Random.Range(-3, 4));
        var randomBool = Random.Range(0, 2);
        randomIncDec = !_criticalHit ? (randomBool == 0 ? randomIncDec : -randomIncDec) : randomIncDec;
        var calculatedDamage = Mathf.RoundToInt(attackDamageCalculated + math + randomIncDec);
        if (_criticalHit)
        {
            math = ((unit.Level * 2) + 2) / 5;
            var multiplier = 1f;
            if (math < 1.5) multiplier = 1.5f;
            if (math > 2) multiplier = 2f;
            randomIncDec = Mathf.RoundToInt(Random.Range(0, 4));
            calculatedDamage = Mathf.RoundToInt(attackDamageCalculated * multiplier + randomIncDec);
            StartCoroutine(CritNumbers(target));
            PlayerCombatHUD.CombatTextEvent.Invoke($"Acerto <color=red>Crítico!</color>", 3f);
        }
        if (calculatedDamage < 0) calculatedDamage = 0;
        target.TakeDamage(calculatedDamage);
    }

    private void CalcAnimation()
    {
        if (unit.IsPlayer)
        {
            var enemyObject = GameObject.FindWithTag("Enemy");
            foreach (var track in basicAttack.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "AttackAnimation":
                        director.SetGenericBinding(track, gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "MovementAnimation":
                        director.SetGenericBinding(track, gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "Signals":
                        director.SetGenericBinding(track, enemyObject.GetComponentInChildren<SignalReceiver>());
                        break;
                    case "AttackVfx":
                        if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                            switch (InventoryManager.Instance.EquipmentSlots[3].equipItem.AttackType)
                            {
                                case AttackType.Blunt:
                                    director.SetGenericBinding(track, hammerAttackVfx);
                                    break;
                                case AttackType.Cut:
                                    director.SetGenericBinding(track, swordAttackVfx);
                                    break;
                                default:
                                    director.SetGenericBinding(track, swordAttackVfx);
                                    break;
                            }
                        else
                        {
                            director.SetGenericBinding(track, swordAttackVfx);
                        }
                        break;
                    case "HitVfx":
                        director.SetGenericBinding(track, enemyObject.GetComponent<UnitController>().HitVfx);
                        break;
                }
            }
        }
        if (_ongoingChargeAttacks <= 0)
        {
            if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                switch (InventoryManager.Instance.EquipmentSlots[3].equipItem.AttackType)
                {
                    case AttackType.Blunt:
                        Director.Play(basicBluntAttack);
                        break;
                    case AttackType.Cut:
                        Director.Play(basicCutAttack);
                        break;
                    case AttackType.Ranged:
                        Director.Play(basicRangedAttack);
                        break;
                    default:
                        Director.Play(basicAttack);
                        break;
                }
            else
                Director.Play(basicAttack);
        }
        else
        {
            if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                switch (InventoryManager.Instance.EquipmentSlots[3].equipItem.AttackType)
                {
                    case AttackType.Blunt:
                        Director.time = 0.54f;
                        Director.Play(basicBluntAttack);
                        break;
                    case AttackType.Cut:
                        Director.time = 0.56f;
                        Director.Play(basicCutAttack);
                        break;
                    case AttackType.Ranged:
                        Director.time = 0.43f;
                        Director.Play(basicRangedAttack);
                        break;
                    default:
                        Director.Play(basicAttack);
                        Director.Evaluate();
                        break;
                }
            else
            {
                Director.time = 0.56f;
                Director.Play(basicAttack);
            }
        }
    }

    public void OngoingAttackSignal()
    {
        if (_ongoingChargeAttacks > 0)
            Director.Pause();
    }
    /// <summary>Reduces the unit's health by the given amount of damage, taking into account the unit's defense and equipment.</summary>
    /// <param name="damage">The amount of damage to be taken.</param>
    /// <returns>The amount of damage taken after defense and equipment have been taken into account.</returns>
    public int TakeDamage(int damage)
    {
        defenceCalculated = unit.Defence;
        foreach (var equipment in InventoryManager.Instance.EquipmentSlots)
        {
            defenceCalculated += equipment.equipItem != null ? equipment.equipItem.StatusValue.Defence : 0;
        }
        // Calculate damage taken based on defense
        damageTakenThisTurn = damage - defenceCalculated / 2;

        if (damage <= 0)
            damageTakenThisTurn = 0;

        // Subtract damage from health
        unit.CurrentHp -= damageTakenThisTurn;

        if (unit.IsPlayer)
        {
            unit.CurrentTp += 5;
            if (unit.CurrentTp > unit.MaxTp)
                unit.CurrentTp = unit.MaxTp;
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }
        // Check if the unit has died
        if (unit.CurrentHp <= 0)
        {
            unit.IsDead = true;
            unit.CurrentHp = 0;
        }
        return damageTakenThisTurn;
    }
    public void KillUnit()
    {
        StartCoroutine(Dissolve());
    }
    private void SetDead()
    {
        unit.IsDead = false;
    }
    private IEnumerator Dissolve()
    {
        var renderer = GetComponent<SpriteRenderer>();
        var material = renderer.material;
        material.SetInt("_Is_Dead", unit.IsDead ? 1 : 0);
        var dissolveAmount = 1f;
        while (dissolveAmount >= 0)
        {
            dissolveAmount -= Time.deltaTime * 0.5f;
            material.SetFloat("_Cutoff_Height", dissolveAmount);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public int TakeRawDamage(int damage)
    {
        damageTakenThisTurn = damage;

        if (damage == 0)
            damageTakenThisTurn = 0;

        // Subtract damage from health
        unit.CurrentHp -= damageTakenThisTurn;

        // Check if the unit has died
        if (unit.CurrentHp <= 0)
        {
            unit.IsDead = true;
            unit.CurrentHp = 0;
            // TODO: Play death animation
        }
        return damageTakenThisTurn;
    }
    /// <summary>Displays the damage taken by a unit as text.</summary>
    /// <remarks>
    /// The damage taken is displayed using an animator that triggers a specific animation depending on whether the unit is a player or an enemy.
    /// </remarks>
    public void DisplayDamageText()
    {
        damageText.text = damageTakenThisTurn.ToString();
        damageTextAnimator.SetTrigger(unit.IsPlayer ? "PlayerTookDamage" : "EnemyTookDamage");
    }
    public void DisplayHealText()
    {
        damageText.text = $"+{PlayerCombatHUD._usedItemValue}";
        damageTextAnimator.SetTrigger("PlayerHealed");
        StartCoroutine(HealingNumbers(this));
    }
    /// <summary>Runs the logic for a player's escape attempt and updates the game state accordingly.</summary>
    /// <remarks>
    /// If the player successfully escapes, the game will load the last scene visited and update the game save file with the name of the current scene.
    /// If the player fails to escape, the game will display a message indicating the failure.
    /// </remarks>
    public void RunAction(UnitController target)
    {
        if (target.unit.UnitName == "Boneco de Treino") // tutorial
        {
            if (unit.Experience.Equals(0)) unit.Experience = 1;
            var reader = QuickSaveReader.Create("GameInfo");
            _sceneTransitioner.StartCoroutine(_sceneTransitioner.TransitionTo(reader.Read<string>("LastScene")));
            PlayerCombatHUD.CombatTextEvent.Invoke($"Você terminou seu treino", 5f);
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            var gotAway = RunLogic();

            if (gotAway)
            {
                var reader = QuickSaveReader.Create("GameInfo");
                _sceneTransitioner.StartCoroutine(_sceneTransitioner.TransitionTo(reader.Read<string>("LastScene")));
                PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=green>fugiu com sucesso</color>", 5f);
                PlayerCombatHUD.TakenAction.Invoke();
                PlayerCombatHUD.ForceDisableButtons.Invoke(true);
            }
            else
            {
                PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=red>falhou em fugir</color>", 4f);
                PlayerCombatHUD.TakenAction.Invoke();
                PlayerCombatHUD.ForceDisableButtons.Invoke(true);
            }
        }
    }
    /// <summary>Runs a logic that involves a random chance and a unit's luck.</summary>
    /// <returns>True if the random chance plus the unit's luck is greater than 50, false otherwise.</returns>
    public bool RunLogic()
    {
        var randomChance = Random.Range(0, 100);
        randomChance += Unit.Luck + Unit.Dexterity;
        randomChance = randomChance > 50 ? 1 : 0;
        if (randomChance == 1)
        {
            if (run != null) Director.Play(run);
            return true;
        }
        return false;
    }
    /// <summary>Selects an action for the unit to perform on the target.</summary>
    /// <param name="target">The target unit.</param>
    /// <remarks>If the unit is a player, no action is taken. Otherwise, the unit attacks the target and displays the damage dealt in the player combat HUD.</remarks>
    public void SelectAction(UnitController target)
    {
        if (!unit.IsPlayer && unit.UnitName == "Boneco de Treino") return;
        // AI logic for selecting an action goes here
        AttackLogic(target);
    }
    #endregion ==========================================================================
}
