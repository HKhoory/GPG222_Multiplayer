using System.Collections.Generic;
using UnityEngine;

namespace __SAE.Leonardo.Scripts.Player
{
    /// <summary>
    /// Manages spawn points for players in the lobby scene.
    /// </summary>
    public class SpawnPointsManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints = new();
        [SerializeField] private bool createSpawnPointsIfEmpty = true;
        [SerializeField] private int numberOfSpawnPointsToCreate = 4;
        [SerializeField] private float spawnCircleRadius = 5f;

        private static SpawnPointsManager _instance;
        public static SpawnPointsManager Instance => _instance;

        private int _nextSpawnPointIndex = 0;

        private void Awake() {
            if (_instance == null) {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
                return;
            }

            if (spawnPoints.Count == 0 && createSpawnPointsIfEmpty) {
                CreateSpawnPoints();
            }
        }

        /// <summary>
        /// Creates spawn points dynamically if none are provided in the inspector.
        /// </summary>
        private void CreateSpawnPoints() {
            GameObject spawnPointsContainer = new GameObject("SpawnPoints");
            spawnPointsContainer.transform.position = Vector3.zero;
            spawnPointsContainer.transform.parent = transform;

            for (int i = 0; i < numberOfSpawnPointsToCreate; i++) {
                // Position spawn points in a circle.
                float angle = i * (360f / numberOfSpawnPointsToCreate);
                float radian = angle * Mathf.Deg2Rad;

                Vector3 position = new Vector3(
                    Mathf.Sin(radian) * spawnCircleRadius,
                    1f,
                    Mathf.Cos(radian) * spawnCircleRadius
                );

                GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
                spawnPoint.transform.position = position;
                spawnPoint.transform.parent = spawnPointsContainer.transform;

                spawnPoints.Add(spawnPoint.transform);
            }

            Debug.Log($"SpawnPointsManager: Created {numberOfSpawnPointsToCreate} spawn points");
        }

        /// <summary>
        /// Gets the next available spawn point in a round-robin fashion.
        /// </summary>
        /// <returns>The spawn point transform or null if none are available.</returns>
        public Transform GetNextSpawnPoint() {
            if (spawnPoints.Count == 0) {
                Debug.LogWarning("SpawnPointsManager: No spawn points available!");
                return null;
            }

            Transform spawnPoint = spawnPoints[_nextSpawnPointIndex];
            _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % spawnPoints.Count;

            return spawnPoint;
        }

        /// <summary>
        /// Gets a spawn point by client/player ID.
        /// </summary>
        /// <param name="id">The client/player ID.</param>
        /// <returns>The spawn point transform or null if none are available.</returns>
        public Transform GetSpawnPointById(int id) {
            if (spawnPoints.Count == 0) {
                Debug.LogWarning("SpawnPointsManager: No spawn points available!");
                return null;
            }

            // Use modulo to stay within the bounds of the spawn points list.
            int index = (id - 1) % spawnPoints.Count;
            return spawnPoints[index];
        }
    }
}