using KrutolFramework.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PVZRemake.Board;
using PVZRemake.SubFramework;

namespace PVZRemake.Scenes
{
    internal class BoardScene(string levelName = "1-1") : IScene
    {
        private TextureRegion _backgroundTexture;
        private Vector2 _backgroundOffset = new(0f, 0f);
        private GameLevelState _currentState;
        private ProjectileManager _projectileManager;
        private ParticleEffectManager _particleEffectManager;
        private readonly List<Plant> _plants = [];
        private readonly List<Zombie> _zombies = [];

        private SeedBank _seedBank;
        private SeedChooser _seedChooser;
        private SeedCard _activeSelectedCardInHand = null;
        private Reanimation _previewReanimInHand = null;

        private WaveManager _waveManager;
        private const float CAM_X_DIALOGUE = 0f;
        private const float CAM_X_ZOMBIES = -900f; // Подгоняйте под ширину вашего 1.5x масштабированного арта (край справа)
        private const float CAM_X_GAMEPLAY = -300f; // Рабочая позиция игры

        private float _panTimer = 0f;                // Текущее время движения камеры
        private const float PAN_DURATION = 2.5f;     // Длительность панорамы в секундах
        private float _panStartX = 0f;
        private float _countdownTimer = 0f;
        private readonly List<Zombie> _previewZombies = [];
        private Reanimation? zombies_won, set_ready_plant;
        private readonly string LevelName = levelName;
        private ItemManager _itemManager;
        private bool isDay = true;

        public static bool IsDay { get; private set; }
        public static ItemManager? CurrentItemManager { get; private set; }
        public static ParticleEffectManager? CurrentParticleManager { get; private set; }
        public static List<Zombie> CurrentZombies { get; private set; } = [];
        private static readonly CrazyDaveShop _shopOverlay = new();
        private static readonly UIButton _shopButton = new()
        {
            TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BUTTON2"),
            TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BUTTON2_GLOW"),
            Size = new Vector2(111,26),
            Position = new Vector2(750,600),
            OnClick = () =>
            {
                _shopOverlay.IsVisible = true;
            }
        };
        public void Initialize()
        {
            zombies_won = ReanimDatabase.CreateRuntimeAnimation("REANIM_ZOMBIESWON");
            zombies_won.LoopType = ReanimLoopType.PlayOnceAndHold;

            zombies_won.Position = new(200f, 70f);
            zombies_won.Scale = new(1.5f, 1.5f);

            set_ready_plant = ReanimDatabase.CreateRuntimeAnimation("REANIM_STARTREADYSETPLANT");
            set_ready_plant.LoopType = ReanimLoopType.PlayOnce;

            set_ready_plant.Position = new(700f, 500f);
            set_ready_plant.Scale = new(1.5f, 1.5f);


            _currentState = GameLevelState.Dialogue;
            _backgroundOffset.X = CAM_X_DIALOGUE;



            _seedBank = new SeedBank();
            _seedChooser = new SeedChooser();

            _itemManager = new ItemManager();
            _projectileManager = new ProjectileManager();
            _particleEffectManager = new ParticleEffectManager();

            
            CurrentItemManager = _itemManager;
            CurrentParticleManager = _particleEffectManager;
            CurrentZombies = _zombies;

            LevelDefinition levelConfig = LevelDatabase.GetLevelConfig(LevelName);
            _waveManager = new WaveManager(levelConfig)
            {
                OnSpawnZombie = (type, row) =>
                {
                    Zombie? newZombie = ZombieFactory.CreateZombie(type, row, startX: 1650f);
                    if (newZombie != null) _zombies.Add(newZombie);

                },
                OnFlagWaveWarning = () =>
                {
                    Console.WriteLine("[UI] Огромная волна зомби приближается!");
                }
            };

            Grid.SetBoardType(levelConfig.BoardType);
            switch (levelConfig.BoardType)
            {
                case BoardType.Day:
                    _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BACKGROUND1");
                    break;
                case BoardType.Night:
                    _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BACKGROUND2");
                    isDay = false;
                    break;
                case BoardType.Pool:
                    _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BACKGROUND3");
                    break;
                case BoardType.Fog:
                    _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BACKGROUND4");
                    isDay = false;
                    break;
                case BoardType.Roof:
                    _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BACKGROUND5");
                    break;
            }
            IsDay = isDay;
        }
        public void Update(float dt)
        {
            // ИСПРАВЛЕНО: Безопасное накопление времени игры без постоянного насилия жесткого диска в цикле Update
            if (LawnApp.CurrentUser != null)
            {
                // Накапливаем время напрямую в активный профиль оперативной памяти фреймворка
                LawnApp.CurrentUser.TotalPlayTime = LawnApp.CurrentUser.TotalPlayTime.Add(TimeSpan.FromSeconds(dt));
            }

            switch (_currentState)
            {
                case GameLevelState.Dialogue:
                    if (Input.IsMouseButtonPressed(MouseButton.Left))
                    {
                        _currentState = GameLevelState.PanToZombies;
                        _panTimer = 0f;
                        _panStartX = CAM_X_DIALOGUE;

                        LevelDefinition levelConfig = LevelDatabase.GetLevelConfig(LevelName);
                        SpawnPreviewZombies(levelConfig);

                        Console.WriteLine("[BoardScene] Камера плавно едет к зомби по кривой EaseInOut...");
                    }
                    break;

                case GameLevelState.PanToZombies:
                    _panTimer += dt;
                    float progressRight = Math.Clamp(_panTimer / PAN_DURATION, 0f, 1f);
                    _backgroundOffset.X = TodCurveMath.TodCurveEvaluate(progressRight, _panStartX, CAM_X_ZOMBIES, TodCurves.CURVE_EASE_IN_OUT);

                    foreach (var z in _previewZombies) z.Update(dt, _plants);

                    if (progressRight >= 1f)
                    {
                        _currentState = GameLevelState.ChoosingSeeds;
                        _seedChooser.StartAppearanceAnimation();
                        _seedBank.StartAppearanceAnimation();
                    }
                    break;

                case GameLevelState.ChoosingSeeds:
                    if (_shopOverlay.IsVisible)
                    {
                        _shopOverlay.Update(dt);
                        return;
                    }
                    _seedChooser.Update(dt);
                    _seedBank.Update(dt);
                    _shopButton.Update(dt);
                    foreach (var z in _previewZombies) z.Update(dt, _plants);

                    if (_seedChooser.IsSelectionFinished)
                    {
                        _seedBank.PopulateSlots(_seedChooser.ChosenPlants, OnSeedCardSelectedFromBank);
                        _currentState = GameLevelState.PanToGameplay;
                        _panTimer = 0f;
                        _panStartX = CAM_X_ZOMBIES;
                        Console.WriteLine("[BoardScene] Семена выбраны. Камера плавно возвращается...");
                    }
                    break;

                case GameLevelState.PanToGameplay:
                    _seedBank.Update(dt);
                    _panTimer += dt;
                    float progressLeft = Math.Clamp(_panTimer / PAN_DURATION, 0f, 1f);
                    _backgroundOffset.X = TodCurveMath.TodCurveEvaluate(progressLeft, _panStartX, CAM_X_GAMEPLAY, TodCurves.CURVE_EASE_IN_OUT);

                    if (progressLeft >= 1f)
                    {
                        _previewZombies.Clear();
                        _currentState = GameLevelState.Countdown;
                        _countdownTimer = 1.4f;
                    }
                    break;

                case GameLevelState.Countdown:
                    UpdateCountdown(dt);
                    break;

                case GameLevelState.ActiveGameplay:
                    UpdateActiveGameplay(dt);
                    break;

                case GameLevelState.LevelLost:
                    zombies_won.Update(dt);
                    if (Input.IsMouseButtonPressed(MouseButton.Left))
                    {
                        this.Destroy(); // Гарантируем зачистку
                        SceneManager.SwitchScene(new SelectorScreen());
                    }
                    break;
                case GameLevelState.LevelWon:
                    if (Input.IsMouseButtonPressed(MouseButton.Left))
                    {
                        this.Destroy(); // Гарантируем зачистку
                        SceneManager.SwitchScene(new SelectorScreen());
                    }
                    break;
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                this.Destroy();
                SceneManager.SwitchScene(new SelectorScreen());
            }
        }


        private void UpdateCountdown(float dt)
        {
            _countdownTimer -= dt;

            if (_countdownTimer > 0) 
            {
                set_ready_plant.Update(dt);
            }
            else
            {
                // Отсчет завершен, включаем полноценный геймплей и запускаем WaveManager!
                _currentState = GameLevelState.ActiveGameplay;
                _waveManager.StartLevel();
                Console.WriteLine("[BoardScene] Бой начался! Удачи.");
            }
        }

        private void SpawnPreviewZombies(LevelDefinition config)
        {
            _previewZombies.Clear();

            // Смотрим, какие зомби разрешены на уровне, и спавним их для превью справа
            for (int i = 0; i < 12; i++)
            {
                int randomRow = Random.Shared.Next(0, Grid.TotalRows);
                // Распределяем их стационарно на правом краю фона (X = 1400..1600)
                float spawnX = 2150f + Random.Shared.NextSingle() * 120f;

                // Берем случайный тип из разрешенных на уровне (например, Normal или Conehead)
                ZombieType randomType = config.AllowedZombies[Random.Shared.Next(0, config.AllowedZombies.Count)];

                Zombie? previewZombie = ZombieFactory.CreateZombie(randomType, randomRow, spawnX);
                if (previewZombie != null)
                {
                    // КРИТИЧЕСКИЙ ФИКС: Принудительно ставим стейт в Idle!
                    // В этом состоянии зомби НЕ идет влево, а просто крутит кадры дыхания IdleFrames1/2
                    previewZombie.SetState(ZombieState.Idle); // Или ZombieState.Idle, если у вас выведена логика покоя

                    // Если вы хотите, чтобы они использовали именно Idle-кадры (0..43) из ваших лимитов:
                    // Доработайте метод в Zombie.cs, чтобы при флаге превью он включал Idle-таймлайн.

                    _previewZombies.Add(previewZombie);
                    _previewZombies.Reverse();
                }
            }
        }

        private void OnSeedCardSelectedFromBank(SeedCard clickedCard)
        {
            if (clickedCard.IsReady && _seedBank.SunAmount >= clickedCard.SunCost)
            {
                _activeSelectedCardInHand = clickedCard;

                string animKey = $"REANIM_{PlantDatabase.Get(clickedCard.PlantType).ReanimKey}";
                ReanimDefinition def = AssetManager.GetAnimation(animKey);

                if (def != null && AssetManager.Active != null)
                {
                    _previewReanimInHand = new Reanimation(def, AssetManager.Active)
                    {
                        LoopType = ReanimLoopType.Loop, 
                        Scale = new Vector2(1.5f,1.5f) 
                    };
                }

                Console.WriteLine($"[Hand] В руку взят скелетный силуэт: {clickedCard.PlantType}");
            }
        }
        private void UpdateActiveGameplay(float dt)
        {
            _seedBank.Update(dt);

            _waveManager.Update(dt, _zombies.Count);
            _itemManager.Update(dt, _currentState == GameLevelState.ActiveGameplay, isDay, _seedBank, _backgroundOffset);

            if (_waveManager.IsLevelFinished && _zombies.Count == 0)
            {
                _currentState = GameLevelState.LevelWon;
                Console.WriteLine("[Win] Уровень успешно пройден!");

                // Принудительно вызываем метод фиксации прогресса и монет
                ApplyLevelResultsAndSave(true);
            }

            // Если в руке есть растение — двигаем и плавно анимируем его силуэт под курсором
            if (_activeSelectedCardInHand != null && _previewReanimInHand != null)
            {

                string targetMarker = "anim_idle";
                if (_previewReanimInHand.HasMarker("anim_full_idle"))
                {
                    targetMarker = "anim_full_idle";
                }
                else if (!_previewReanimInHand.HasMarker("anim_idle"))
                {
                    // На случай, если в кастомном или сломанном .reanim файле нет вообще никаких маркеров
                    targetMarker = "";
                }

                if (!string.IsNullOrEmpty(targetMarker))
                {
                    _previewReanimInHand.SetFrameBounds(targetMarker);
                }
                else
                {
                    _previewReanimInHand.SetFrameBounds("anim_idle");
                }
                _previewReanimInHand._animTime = 0.999f;
                _previewReanimInHand.Update(0f);
                // Точка привязки оригинальных растений PopCap находится у корней (снизу по центру),
                // поэтому смещаем позицию чуть ниже курсора мыши, чтобы игрок держал растение "за стебель"

                _previewReanimInHand.Position = Input.VirtualMousePosition + new Vector2(-80f, -80f);
                var gridCoords = Grid.GetCoordsFromScreen(Input.VirtualMousePosition);
                if (gridCoords.HasValue)
                {
                    _previewReanimInHand.Position = new Vector2(gridCoords.Value.CellTopLeft.X, gridCoords.Value.CellTopLeft.Y + 15f);
                }
            }

            // Посадка растений по клику на газон
            if (Input.IsMouseButtonPressed(MouseButton.Left) && _activeSelectedCardInHand != null)
            {
                var coords = Grid.GetCoordsFromScreen(Input.VirtualMousePosition);
                if (coords.HasValue)
                {
                    GridCoords cell = coords.Value;
                    if (!IsCellOccupied(cell.Row, cell.Col))
                    {
                        Plant? newPlant = PlantFactory.CreatePlant(_activeSelectedCardInHand.PlantType, cell.Row, cell.Col);
                        if (newPlant != null)
                        {
                            _plants.Add(newPlant);
                            _seedBank.SunAmount -= _activeSelectedCardInHand.SunCost;
                            _activeSelectedCardInHand.ResetCooldown();

                            // Успешно посадили — очищаем руку и силуэт
                            _activeSelectedCardInHand = null;
                            _previewReanimInHand = null;
                        }
                    }
                }
            }

            if (Input.IsMouseButtonPressed(MouseButton.Right))
            { _activeSelectedCardInHand = null; _previewReanimInHand = null;  }

            for (int i = _plants.Count - 1; i >= 0; i--)
            {
                _plants[i].Update(dt, _projectileManager);
                if (_plants[i].IsDead) _plants.RemoveAt(i);
            }

            for (int i = _zombies.Count - 1; i >= 0; i--)
            {
                _zombies[i].Update(dt, _plants);
                if (_zombies[i].Position.X < 100f && _zombies[i].State != ZombieState.Dying) _currentState = GameLevelState.LevelLost;

                if (_zombies[i].IsDead)
                {
                    // КАТЕГОРИЯ: ГЕНЕРАЦИЯ МОНЕТ И АЛМАЗОВ ПРИ СМЕРТИ ЗОМБИ
                    // Берем позицию зомби в мире (смещаем чуть выше к центру его тела, чтобы летело красиво)
                    Vector2 deathPos = _zombies[i].Position + new Vector2(0f, -40f);

                    int chance = Random.Shared.Next(0, 100);

                    if (chance < 2) // 2% Алмаз
                    {
                        _itemManager.SpawnZombieLoot(ItemType.Diamond, deathPos);
                    }
                    else if (chance < 12) // 10% Золото
                    {
                        _itemManager.SpawnZombieLoot(ItemType.CoinGold, deathPos);
                    }
                    else if (chance < 35) // 23% Серебро
                    {
                        _itemManager.SpawnZombieLoot(ItemType.CoinSilver, deathPos);
                    }


                    _zombies.RemoveAt(i);
                }
            }

            _projectileManager.Update(dt, _zombies, _particleEffectManager);
            _particleEffectManager.Update(dt);
        }
        public void Render(SpriteBatch spriteBatch)
        {
            if (_shopOverlay.IsVisible)
            {
                _shopOverlay.Render(spriteBatch);
                return;
            }

            spriteBatch.Draw(_backgroundTexture, _backgroundOffset, new Vector2(1.5f, 1.5f), 0f, Color4.White);


            if (_currentState == GameLevelState.ChoosingSeeds ||
                _currentState == GameLevelState.PanToZombies ||
                _currentState == GameLevelState.PanToGameplay)
            {
                // Просто рисуем зомби один за другим, предварительно отсортировав их по Y (номеру ряда) один раз!
                _previewZombies.Sort((a, b) => a.Row.CompareTo(b.Row));
                foreach (var zombie in _previewZombies)
                {
                    zombie.Render(spriteBatch, _backgroundOffset);
                }
            }

            if (_currentState == GameLevelState.ChoosingSeeds)
            {
                // Рисуем интерфейс каталога выбора семян
                _seedChooser.Render(spriteBatch);
                _seedBank.Render(spriteBatch);
                _shopButton.Render(spriteBatch);
            }
            else
            {
                // Временный список для сортировки объектов на конкретной дорожке
                List<IRenderableEntity> rowEntities = [];

                // 2. Построчный изометрический рендеринг (от строки 0 до конца поля)
                for (int r = 0; r < Grid.TotalRows; r++)
                {
                    rowEntities.Clear();

                    // Собираем все растения на текущей строке
                    foreach (var plant in _plants)
                    {
                        if (plant.Row == r) rowEntities.Add(plant);
                    }

                    // Собираем всех зомби на текущей строке
                    foreach (var zombie in _zombies)
                    {
                        if (zombie.Row == r) rowEntities.Add(zombie);
                    }

                    rowEntities.Sort((a, b) => b.X.CompareTo(a.X));

                    // Отрисовываем отсортированные объекты текущего ряда
                    foreach (var entity in rowEntities)
                    {
                        entity.Render(spriteBatch, Vector2.Zero);
                    }
                }



                _projectileManager.Render(spriteBatch);
                _particleEffectManager.Render(spriteBatch);

                // Поверх игрового поля рисуем верхнюю панель SeedBank
                _seedBank.Render(spriteBatch);

                if (_activeSelectedCardInHand != null && _previewReanimInHand != null)
                {
                    // Задаем прозрачность 50-60% (эффект призрака перед посадкой)
                    _previewReanimInHand.ColorOverride = new Color4(1f, 1f, 1f, 0.55f);

                    // Рендерим всю структуру костей растения прямо под мышкой
                    _previewReanimInHand.Render(spriteBatch);
                }
                if (_currentState == GameLevelState.Dialogue)
                {
                    var font = AssetManager.GetFont("Arial", 48);
                    string playerName = "Player";

                    if (LawnApp.CurrentUser != null)
                    {
                        // Загружаем имя из сохраненного профиля, чтобы избежать десинхронизаций списков мета-данных
                        var metaList = SaveSystem.LoadUsers();
                        var currentMeta = metaList.FirstOrDefault(u => u.UserId == LawnApp.CurrentUser.UserId);
                        if (currentMeta != null) playerName = currentMeta.Name ?? "Player";
                    }

                    font?.DrawText(spriteBatch, $"{playerName} House!\n\n(Click to continue)", new Vector2(200f, 650f), Vector2.One, Color4.White);
                }
                _itemManager.Render(spriteBatch, _backgroundOffset);
                if (_currentState == GameLevelState.Countdown && _countdownTimer > 0)
                {
                    set_ready_plant.Render(spriteBatch);
                }
                // Экраны завершения уровня
                if (_currentState == GameLevelState.LevelLost)
                {
                    zombies_won.Render(spriteBatch);
                }
                if (_currentState == GameLevelState.LevelWon)
                {
                    var font = AssetManager.GetFont("Arial", 48);
                    font?.DrawText(spriteBatch, "LEVEL COMPLETE!", new Vector2(600f, 400f), Vector2.One, Color4.Green);
                }
            }
        }
        private bool IsCellOccupied(int row, int col)
        {
            return _plants.Exists(p => p.Row == row && p.Col == col);
        }

        private void ApplyLevelResultsAndSave(bool isVictory)
        {
            if (LawnApp.CurrentUser == null) return;

            int currentUserId = LawnApp.CurrentUser.UserId;
            UserProfile? activeProfile = SaveSystem.LoadProfile(currentUserId);

            if (activeProfile != null)
            {
                // Фиксируем монеты
                if (_itemManager != null && _itemManager.CollectedLevelCoins > 0)
                {
                    activeProfile.Coins += _itemManager.CollectedLevelCoins;
                    _itemManager.ResetLevelCoins();
                }

                // Фиксируем прогресс уровня
                if (isVictory)
                {
                    List<string> allLevels = LevelDatabase.GetAllLevelNames();
                    int currentLevelIndex = allLevels.IndexOf(LevelName);

                    if (currentLevelIndex != -1 && currentLevelIndex == activeProfile.AdventureLevel - 1)
                    {
                        activeProfile.AdventureLevel++;
                        activeProfile.GamesWon++;
                    }
                }

                // СИНХРОНИЗАЦИЯ ВРЕМЕНИ ИГРЫ: Переносим накопленное за матч время в сохранение перед записью
                activeProfile.TotalPlayTime = LawnApp.CurrentUser.TotalPlayTime;

                // Записываем файл user#.dat на диск
                SaveSystem.SaveProfile(activeProfile);

                // Синхронизируем оперативную память фреймворка
                LawnApp.CurrentUser.AdventureLevel = activeProfile.AdventureLevel;
                LawnApp.CurrentUser.Coins = activeProfile.Coins;
            }
        }


        public void Destroy()
        {
            // Гарантированный сейв собранных монет при любом выходе с уровня (даже досрочном в меню)
            ApplyLevelResultsAndSave(false);

            _plants.Clear();
            _zombies.Clear();
            _projectileManager.Clear();
            _particleEffectManager.Clear();
            CurrentItemManager = null;
            CurrentZombies = null;
        }
    }
}
