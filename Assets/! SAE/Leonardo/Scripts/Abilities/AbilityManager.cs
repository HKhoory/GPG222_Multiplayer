using System.Collections.Generic;
using Leonardo.Scripts.Abilities;
using Leonardo.Scripts.Controller;
using UnityEngine;

namespace __SAE.Leonardo.Scripts.Abilities
{
    /// <summary>
    /// Manages all abilities for a player. Handles input, activation, and cooldown updates.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        [Tooltip("List of ability components attached to this player")] [SerializeField]
        private List<AbilityBase> abilities = new();

        [Header("- Input Settings")]
        [Tooltip("Key to activate the first ability")] [SerializeField]
        private KeyCode ability1Key = KeyCode.Q;

        [Tooltip("Key to activate the second ability")] [SerializeField]
        private KeyCode ability2Key = KeyCode.E;

        [Tooltip("Key to activate the third ability")] [SerializeField]
        private KeyCode ability3Key = KeyCode.Space;

        [SerializeField] private bool enableAbilitiesOnAwake = false;
        private bool _abilitiesEnabled = false;

        private PlayerController _playerController;

        private void Awake() {
            _playerController = GetComponent<PlayerController>();

            if (abilities.Count == 0) {
                abilities.AddRange(GetComponents<AbilityBase>());
            }

            _abilitiesEnabled = enableAbilitiesOnAwake;

            Debug.Log($"AbilityManager.cs: Found {abilities.Count} abilities.");
        }

        private void Update() {
            if (_playerController != null && !_playerController.IsLocalPlayer) {
                return;
            }

            // Only update cooldowns and check input if abilities are enabled.
            if (!_abilitiesEnabled) {
                return;
            }

            foreach (var ability in abilities) {
                ability.UpdateCooldown(Time.deltaTime);
            }

            CheckAbilityInput();
        }

        public void SetAbilitiesEnabled(bool enabled) {
            _abilitiesEnabled = enabled;
            Debug.Log($"AbilityManager.cs: Abilities {(enabled ? "enabled" : "disabled")}");
        }

        public bool AreAbilitiesEnabled() {
            return _abilitiesEnabled;
        }

        /// <summary>
        /// Checks inputs for ability.
        /// </summary>
        private void CheckAbilityInput() {
            // FIRST ABILITY. --------------
            if (Input.GetKeyDown(ability1Key) && abilities.Count > 0) {
                ActivateAbility(0);
            }

            // SECOND ABILITY. --------------
            if (Input.GetKeyDown(ability2Key) && abilities.Count > 1) {
                ActivateAbility(1);
            }

            // THIRD ABILITY. --------------
            if (Input.GetKeyDown(ability3Key) && abilities.Count > 2) {
                ActivateAbility(2);
            }
            // FOURTH ABILITY. --------------
            if (Input.GetKeyDown(ability4Key) && abilities.Count > 3)
            {
                ActivateAbility(3);
            }
        }

        /// <summary>
        /// Activate an ability by its index.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability in the abilities list.</param>
        /// <returns>True if ability was successfully activated.</returns>
        public bool ActivateAbility(int abilityIndex) {
            if (abilityIndex < 0 || abilityIndex >= abilities.Count) {
                Debug.LogWarning($"AbilityManager.cs: Tried to activate ability at invalid index {abilityIndex}.");
                return false;
            }

            bool success = abilities[abilityIndex].TryActivateAbility(transform);

            if (success) {
                Debug.Log($"AbilityManager.cs: Activated {abilities[abilityIndex].GetAbilityName()}");
            }
            else {
                Debug.Log($"AbilityManager.cs: Could not activate {abilities[abilityIndex].GetAbilityName()}.");
            }

            return success;
        }

        /// <summary>
        /// Get an ability by index.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to retrieve.</param>
        /// <returns>The ability at the given index, or null if index is invalid.</returns>
        public AbilityBase GetAbility(int abilityIndex) {
            if (abilityIndex < 0 || abilityIndex >= abilities.Count) {
                return null;
            }

            return abilities[abilityIndex];
        }

        /// <summary>
        /// Get all abilities.
        /// </summary>
        /// <returns>List of all abilities.</returns>
        public List<AbilityBase> GetAllAbilities() {
            return abilities;
        }
    }
}