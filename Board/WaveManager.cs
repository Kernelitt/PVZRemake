using PVZRemake.Scenes;

namespace PVZRemake.Board
{
    /// <summary>
    /// Описание состава одной конкретной волны
    /// </summary>
    public class WaveDefinition
    {
        public List<ZombieType> ZombiesToSpawn { get; set; } = [];
        public bool IsFlagWave { get; set; } = false; // Большая волна со спавном флагоносца
    }

    /// <summary>
    /// Конфигурация параметров всего уровня (уровни 1-1, 1-2 и т.д.)
    /// </summary>
    public struct LevelDefinition
    {
        public string LevelName { get; set; }
        public BoardType BoardType { get; set; }
        public List<ZombieType> AllowedZombies { get; set; }
        public int WavePointsMultiplier { get; set; }
        public int WavePointsGainSpeed { get; set; }
        public int Waves { get; set; }
        public PlantType RewardPlant { get; set; }

        public LevelDefinition(string name, BoardType boardType, List<ZombieType> allowedZombies, int waves, PlantType? reward = PlantType.Sunflower)
        {
            LevelName = name;
            BoardType = boardType;
            AllowedZombies = allowedZombies;
            Waves = waves;
            RewardPlant = reward ?? PlantType.Sunflower;

            // Значения по умолчанию
            WavePointsMultiplier = 1;
            WavePointsGainSpeed = 3;
        }
    }

    public static class LevelDatabase
    {
        // Статический реестр всех существующих в игре уровней
        private static readonly Dictionary<string, LevelDefinition> _levels = new(StringComparer.OrdinalIgnoreCase);

        static LevelDatabase()
        {
            // Автоматически регистрируем все уровни в базу данных при старте приложения
            RegisterLevel(new LevelDefinition("1-1", BoardType.Day, [ZombieType.Normal], 4));
            RegisterLevel(new LevelDefinition("1-2", BoardType.Day, [ZombieType.Normal], 8));
            RegisterLevel(new LevelDefinition("1-3", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("1-4", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("1-5", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead], 10)); 
            RegisterLevel(new LevelDefinition("1-6", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("1-7", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("1-8", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 10));
            RegisterLevel(new LevelDefinition("1-9", BoardType.Day, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("1-10",BoardType.Day, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));

            RegisterLevel(new LevelDefinition("2-1", BoardType.Night, [ZombieType.Normal], 10));
            RegisterLevel(new LevelDefinition("2-2", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("2-3", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("2-4", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("2-5", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 10)); 
            RegisterLevel(new LevelDefinition("2-6", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("2-7", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("2-8", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("2-9", BoardType.Night, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("2-10",BoardType.Night, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));

            RegisterLevel(new LevelDefinition("3-1", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("3-2", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("3-3", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("3-4", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("3-5", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 20)); 
            RegisterLevel(new LevelDefinition("3-6", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("3-7", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("3-8", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("3-9", BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));
            RegisterLevel(new LevelDefinition("3-10",BoardType.Pool, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));

            RegisterLevel(new LevelDefinition("4-1", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 10));
            RegisterLevel(new LevelDefinition("4-2", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("4-3", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("4-4", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("4-5", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 20)); 
            RegisterLevel(new LevelDefinition("4-6", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("4-7", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("4-8", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("4-9", BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));
            RegisterLevel(new LevelDefinition("4-10",BoardType.Fog, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));

            RegisterLevel(new LevelDefinition("5-1", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("5-2", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));
            RegisterLevel(new LevelDefinition("5-3", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("5-4", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("5-5", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("5-6", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 20));
            RegisterLevel(new LevelDefinition("5-7", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead], 30));
            RegisterLevel(new LevelDefinition("5-8", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 20));
            RegisterLevel(new LevelDefinition("5-9", BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));
            RegisterLevel(new LevelDefinition("5-10",BoardType.Roof, [ZombieType.Normal, ZombieType.Conehead, ZombieType.Buckethead], 30));
        }

        private static void RegisterLevel(LevelDefinition level)
        {
            _levels[level.LevelName] = level;
        }

        /// <summary>
        /// Возвращает конфигурацию уровня по его строковому имени
        /// </summary>
        public static LevelDefinition GetLevelConfig(string levelName)
        {
            if (_levels.TryGetValue(levelName, out var config))
            {
                return config;
            }

            // Запасной дефолтный вариант на случай ошибки, чтобы игра не крашилась
            return new LevelDefinition("1-1", BoardType.Day, [ZombieType.Normal], 4, PlantType.Sunflower);
        }

        /// <summary>
        /// Возвращает список имён абсолютно всех зарегистрированных в базе уровней
        /// </summary>
        public static List<string> GetAllLevelNames()
        {
            return [.. _levels.Keys];
        }
    }

    public class WaveManager(LevelDefinition levelDef)
    {
        private readonly LevelDefinition _levelDef = levelDef;

        public int CurrentWaveIndex { get; private set; } = -1;
        public int TotalWaves => _levelDef.Waves;

        // Таймеры управления
        private float _timeSinceLastWave = 0f;
        private float _currentWaveCooldown = 15f;
        private bool _hasStarted = false;

        // Состояния прогресса
        public bool IsLevelFinished { get; private set; } = false;
        public bool IsShowingFlagWarning { get; private set; } = false;
        private float _flagWarningTimer = 0f;

        // ФИКС: Храним изначальный суммарный запас HP текущей волны для точного расчета 50% порога
        private int _initialWaveTotalHp = 0;

        // Делегаты (ивенты) для обратной связи с BoardScene
        public Action<ZombieType, int>? OnSpawnZombie { get; set; }
        public Action? OnFlagWaveWarning { get; set; }

        public void StartLevel()
        {
            _hasStarted = true;
            CurrentWaveIndex = -1;
            _timeSinceLastWave = 0f;
            _currentWaveCooldown = 15f; // 15 секунд игроку на подготовку
            IsLevelFinished = false;
            _initialWaveTotalHp = 0;
            Console.WriteLine("[WaveManager] Уровень начался. Идет подготовка к первой волне...");
        }

        public void Update(float dt, int activeZombiesCount)
        {
            if (!_hasStarted || IsLevelFinished) return;

            if (IsShowingFlagWarning)
            {
                _flagWarningTimer -= dt;
                if (_flagWarningTimer <= 0f) IsShowingFlagWarning = false;
            }

            _timeSinceLastWave += dt;

            // Условие перехода по тайм-ауту
            bool isTimeout = _timeSinceLastWave >= _currentWaveCooldown;

            // ИСПРАВЛЕНО: Стабильный расчет условия 50% здоровья текущей волны
            bool isHpConditionMet = false;
            if (CurrentWaveIndex >= 0 && _timeSinceLastWave > 4f)
            {
                var activeZombies = BoardScene.CurrentZombies;
                if (activeZombies == null || activeZombies.Count == 0)
                {
                    isHpConditionMet = true; // На поле никого нет — сразу пускаем следующую волну
                }
                else
                {
                    int totalCurrentHp = 0;

                    foreach (var zombie in activeZombies)
                    {
                        if (!zombie.IsDead && zombie.State != ZombieState.Dying)
                        {
                            // Считаем текущее здоровье зомби вместе с его броней
                            totalCurrentHp += (int)(zombie.Health + zombie.ArmorHealth);
                        }
                    }

                    // Если суммарное здоровье упало ниже половины от стартового здоровья этой волны
                    if (_initialWaveTotalHp > 0 && ((float)totalCurrentHp / _initialWaveTotalHp) < 0.5f)
                    {
                        isHpConditionMet = true;
                    }
                }
            }

            if (isTimeout || isHpConditionMet)
            {
                TriggerNextWave(activeZombiesCount);
            }
        }

        private void TriggerNextWave(int activeZombiesCount)
        {
            int nextWaveIndex = CurrentWaveIndex + 1;

            if (nextWaveIndex < TotalWaves)
            {
                CurrentWaveIndex = nextWaveIndex;
                _timeSinceLastWave = 0f;

                // Если это финал или флаг (каждая 10-я волна), даем игроку больше времени
                _currentWaveCooldown = ((CurrentWaveIndex + 1) % 10 == 0) ? 45f : 30f;

                bool isFlagWave = (CurrentWaveIndex + 1) % 10 == 0;
                if (isFlagWave)
                {
                    IsShowingFlagWarning = true;
                    _flagWarningTimer = 4f;
                    OnFlagWaveWarning?.Invoke();
                }

                GenerateAndSpawnWave(isFlagWave);

                // ИСПРАВЛЕНО: Сразу после спавна волны фиксируем её суммарное начальное здоровье для проверки в Update
                CalculateInitialWaveHp();
            }
            else
            {
                if (activeZombiesCount == 0)
                {
                    IsLevelFinished = true;
                }
            }
        }

        /// <summary>
        /// Подсчитывает общее стартовое здоровье всех заспавненных зомби
        /// </summary>
        private void CalculateInitialWaveHp()
        {
            _initialWaveTotalHp = 0;
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return;

            foreach (var zombie in activeZombies)
            {
                if (!zombie.IsDead && zombie.State != ZombieState.Dying)
                {
                    // Учитываем начальное здоровье зомби + здоровье его брони (конуса/ведра)
                    _initialWaveTotalHp += zombie.MaxHealth + zombie.MaxArmorHealth;
                }
            }
        }

        /// <summary>
        /// Исправленный сбалансированный алгоритм закупки волны
        /// </summary>
        private void GenerateAndSpawnWave(bool isFlagWave)
        {
            int currentWaveNumber = CurrentWaveIndex + 1;

            // Расчет бюджета очков
            int budget = (int)(1 + (currentWaveNumber * (_levelDef.WavePointsMultiplier / _levelDef.WavePointsGainSpeed)));
            if (isFlagWave) budget = (int)(budget * 2.5f);
            budget = Math.Max(1, budget);

            if (isFlagWave) SpawnSingleZombie(ZombieType.Flag);
           
            List<ZombieDef> allowedPool = [];
            ZombieDef? cheapestZombie = null;

            foreach (var type in _levelDef.AllowedZombies)
            {
                ZombieDef? def = ZombieDatabase.Get(type);
                if (def == null) continue;

                if (currentWaveNumber >= def.StartingWave)
                {
                    allowedPool.Add(def);

                    if (cheapestZombie == null || def.Points < cheapestZombie.Points) cheapestZombie = def;     
                }
            }

            if (allowedPool.Count == 0) return;
            

            bool spawnedAtLeastOne = false;
            int safetyCounter = 0;
            int initialBudget = budget;

            while (budget > 0 && safetyCounter < 10000)
            {
                safetyCounter++;

                int totalWeight = 0;
                List<ZombieDef> affordableZombies = [];

                foreach (var z in allowedPool)
                {
                    if (z.Points <= budget)
                    {
                        affordableZombies.Add(z);
                        totalWeight += z.Weight;
                    }
                }

                if (affordableZombies.Count == 0 || totalWeight == 0)
                {
                    if (!spawnedAtLeastOne && cheapestZombie != null) SpawnSingleZombie(cheapestZombie.Type);
                    
                    break;
                }

                int roll = Random.Shared.Next(0, totalWeight);
                int currentWeightSum = 0;
                ZombieDef? zombieSelected = null;

                foreach (var z in affordableZombies)
                {
                    currentWeightSum += z.Weight;
                    if (roll < currentWeightSum)
                    {
                        zombieSelected = z;
                        break;
                    }
                }

                if (zombieSelected != null)
                {
                    SpawnSingleZombie(zombieSelected.Type);
                    budget -= zombieSelected.Points;
                    spawnedAtLeastOne = true;
                }
            }
        }

        private void SpawnSingleZombie(ZombieType type)
        {
            int randomRow = Random.Shared.Next(0, Grid.TotalRows);
            OnSpawnZombie?.Invoke(type, randomRow);
        }

        public float GetLevelProgress()
        {
            if (TotalWaves == 0) return 0f;
            if (CurrentWaveIndex < 0) return 0f;
            return (float)(CurrentWaveIndex + 1) / TotalWaves;
        }
    }

}
