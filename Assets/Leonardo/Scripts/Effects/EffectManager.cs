using System.Collections.Generic;
using UnityEngine;

namespace Leonardo.Scripts.Effects
{
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }
        
        [Tooltip("Dictionary of effect prefabs by name")]
        [SerializeField] private List<GameObject> effectPrefabs = new List<GameObject>();
        
        private Dictionary<string, GameObject> _effectDictionary = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // This will automatically populate the dictionary.
            foreach (GameObject prefab in effectPrefabs)
            {
                if (prefab != null)
                {
                    _effectDictionary[prefab.name] = prefab;
                }
            }
        }
        
        /// <summary>
        /// Play an effect at the specified position and rotation.
        /// </summary>
        public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation, float duration = 2f)
        {
            if (_effectDictionary.TryGetValue(effectName, out GameObject prefab))
            {
                GameObject effect = Instantiate(prefab, position, rotation);
                
                if (duration > 0)
                {
                    Destroy(effect, duration);
                }
                
                return effect;
            }
            
            Debug.LogWarning($"EffectManager.cs: Effect '{effectName}' not found");
            return null;
        }
    }
}