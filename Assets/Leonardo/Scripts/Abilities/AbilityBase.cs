using UnityEngine;

namespace Leonardo.Scripts.Abilities
{
    public abstract class AbilityBase : MonoBehaviour
    {
        [Header("- Ability Settings")]
        [SerializeField] protected float cooldownTime = 2f;
        [SerializeField] protected string abilityName = "Ability";
        [SerializeField] protected string abilityDescription = "";
        
        [Header("- Visual Feedback")]
        [SerializeField] protected GameObject effectPrefab;
        
        protected bool isOnCooldown;
        protected float cooldownRemaining;
        
        public bool IsOnCooldown() => isOnCooldown;
        public float GetCooldownRemaining() => cooldownRemaining;
        public float GetCooldownTimeTotal() => cooldownTime;
        public string GetAbilityName() => abilityName;
        public string GetAbilityDescription() => abilityDescription;
        public float GetCooldownPercentage() => isOnCooldown ? cooldownRemaining / cooldownTime : 0f;
        
        /// <summary>
        /// Attempt to activate this ability. Returns true if activation was successful.
        /// </summary>
        /// <param name="playerTransform">Transform of the player using the ability.</param>
        /// <returns>True if ability was activated, false if on cooldown or other conditions prevented activation.</returns>
        public virtual bool TryActivateAbility(Transform playerTransform)
        {
            // We can add more conditions for an abilitiy not being able to be turned on here guys.
            if (isOnCooldown)
            {
                return false;
            }
            
            bool success = ExecuteAbilityEffect(playerTransform);
            
            if (success)
            {
                StartCooldown();
            }
            return success;
        }
        
        /// <summary>
        /// Executes whatever effect an ability has.
        /// </summary>
        /// <param name="playerTransform">Transform of the player using the ability.</param>
        /// <returns>True if ability executed successfully.</returns>
        protected virtual bool ExecuteAbilityEffect(Transform playerTransform)
        {
            return false;
        }

        /// <summary>
        /// Start the cooldown of the ability.
        /// </summary>
        protected virtual void StartCooldown()
        {
            isOnCooldown = true;
            cooldownRemaining = cooldownTime;
        }
        
        /// <summary>
        /// Update the cooldown timer. Should be called every frame.
        /// </summary>
        public virtual void UpdateCooldown(float deltaTime)
        {
            if (!isOnCooldown) return;
            
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownRemaining = 0f;
                OnCooldownComplete();
            }
        }

        /// <summary>
        /// This is going to be called when the cooldown is completed, and we can add stuff for the actual ability in its corresponding script.
        /// </summary>
        protected virtual void OnCooldownComplete()
        {
        }
    }
}