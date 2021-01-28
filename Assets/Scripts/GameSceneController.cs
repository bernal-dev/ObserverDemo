﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneController : MonoBehaviour
{
    public event EnemyDestroyedHandler ScoreUpdateOnKill;
    public event Action<int> LifeLost;

    #region Field Declarations

    [Header("Enemy & Power Prefabs")]
    [Space]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private PlayerController playerShip;
    [SerializeField] private PowerupController[] powerUpPrefabs;

    [Header("Level Definitions")]
    [Space]
    public List<LevelDefinition> levels;
    [HideInInspector] public LevelDefinition currentLevel;

    [Header("Player ship settings")][Space]
    [Range(3, 8)]
    public float playerSpeed = 5;
    [Range(1, 10)]
    public float shieldDuration = 3;

    private int totalPoints;
    private int lives = 3;

    private int currentLevelIndex = 0;
    private WaitForSeconds shipSpawnDelay = new WaitForSeconds(2);

    #endregion

    #region Subject Implementation
    private List<IEndGameObserver> endGameObserverList;

    public void AddObserver(IEndGameObserver endGameObserver)
    {
        endGameObserverList.Add(endGameObserver);
    }

    public void RemoveObserver(IEndGameObserver endGameObserver)
    {
        endGameObserverList.Remove(endGameObserver);
    }

    private void NotifyObservers()
    {
        foreach(var observer in endGameObserverList)
        {
            observer.Notify();
        }
    }

    #endregion

    #region Startup

    private void Awake()
    {
        endGameObserverList = new List<IEndGameObserver>();
    }

    void Start()
    {
        StartLevel(currentLevelIndex);
    }

    #endregion

    #region Level Management

    private void StartLevel(int levelIndex)
    {
        currentLevel = levels[levelIndex];

        StartCoroutine(SpawnShip(false));
        StartCoroutine(SpawnEnemies());

        if (currentLevel.hasPowerUps)
            StartCoroutine(SpawnPowerUp());
    }

    private void EndLevel()
    {
        currentLevelIndex++;
        StopAllCoroutines();

        //If last level the game over
        if (currentLevelIndex < levels.Count)
        {
            //TODO: Clean up
            StartLevel(currentLevelIndex);
        }
    }

    #endregion

    #region Spawning

    private IEnumerator SpawnShip(bool delayed)
    {
        if(delayed)
            yield return shipSpawnDelay;

        PlayerController ship = Instantiate(playerShip, new Vector2(0, -4.67f), Quaternion.identity);
        ship.speed = playerSpeed;
        ship.shieldDuration = shieldDuration;
        ship.HitByEnemy += Ship_HitByEnemy;

        yield return null;
    }

    private void Ship_HitByEnemy()
    {
        lives--;
        LifeLost?.Invoke(lives);
        if (lives > 0)
        {
            StartCoroutine(SpawnShip(true));
        } else
        {
            StopAllCoroutines();
            NotifyObservers();
        }
    }

    private IEnumerator SpawnEnemies()
    {
        WaitForSeconds wait = new WaitForSeconds(currentLevel.enemySpawnDelay);
        yield return wait;

        for (int i = 0; i < currentLevel.numberOfEnemies; i++)
        {
            Vector2 spawnPosition = ScreenBounds.RandomTopPosition();

            EnemyController enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");
            enemy.shotSpeed = currentLevel.enemyShotSpeed;
            enemy.speed = currentLevel.enemySpeed;
            enemy.shotdelayTime = currentLevel.enemyShotDelay;
            enemy.angerdelayTime = currentLevel.enemyAngerDelay;
            enemy.EnemyDetroyed += EnemyDestroyed;
            AddObserver(enemy);

            yield return wait;
        }
    }

    private void EnemyDestroyed(int pointValue)
    {
        totalPoints += pointValue;
        ScoreUpdateOnKill?.Invoke(totalPoints);
    }

    private IEnumerator SpawnPowerUp()
    {
        while (true)
        {
            int index = UnityEngine.Random.Range(0, powerUpPrefabs.Length);
            Vector2 spawnPosition = ScreenBounds.RandomTopPosition();
            var powerUp = Instantiate(powerUpPrefabs[index], spawnPosition, Quaternion.identity);
            AddObserver(powerUp);

            yield return new WaitForSeconds(UnityEngine.Random.Range(currentLevel.powerUpMinimumWait,currentLevel.powerUpMaximumWait));
        }
    }

    #endregion
}
