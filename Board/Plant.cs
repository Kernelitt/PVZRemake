using KrutolFramework.Core;
using OpenTK.Mathematics;
using PVZRemake.Scenes;
using System.Runtime.CompilerServices;

namespace PVZRemake.Board
{
    public abstract class Plant : IRenderableEntity
    {
        public PlantType Type { get; protected set; }
        public PlantState State { get; protected set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public float X => Position.X;

        public Vector2 Position { get; protected set; }
        public Vector2 CenterPosition { get; protected set; }

        // Автоматически настраиваемые свойства геймплея из базы данных
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int SunCost { get; protected set; }
        public float LaunchRate { get; protected set; }
        public PlantSubClass SubClass { get; protected set; }

        public bool IsDead => Health <= 0;
        private TextureRegion _shadowTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_PLANTSHADOW");

        protected Reanimation PlantAnim;

        public Plant(PlantType type, int row, int col)
        {
            Type = type;
            Row = row;
            Col = col;
            State = PlantState.Idle;

            GridCoords coords = Grid.GetCoords(row, col);
            Position = coords.CellTopLeft;
            CenterPosition = coords.CellCenter;

            // Авто-настройка характеристик
            var def = PlantDatabase.Get(type);
            MaxHealth = def.MaxHealth;
            Health = MaxHealth;
            SunCost = def.SunCost;
            LaunchRate = def.LaunchRate;
            SubClass = def.SubClass;
        }

        public virtual void Initialize()
        {
            var def = PlantDatabase.Get(Type);

            // Создаем рантайм-анимацию на основе ключа из базы данных
            PlantAnim = ReanimDatabase.CreateRuntimeAnimation(def.ReanimKey);

            if (PlantAnim != null)
            {
                PlantAnim.LoopType = ReanimLoopType.Loop;
                PlantAnim.Scale = new Vector2(1.5f, 1.5f);

                // Применяем приоритетный маркер покоя, который мы спроектировали ранее
                PlantAnim.SetFrameBounds("anim_idle");

                UpdateAnimPosition();
            }
            else
            {
                Console.WriteLine($"[Plant Error] Не удалось создать анимацию для {Type} по ключу {def.ReanimKey}.");
            }
        }

        /// <summary>
        /// Синхронизирует экранную позицию анимации с физическим местоположением на поле
        /// </summary>
        protected virtual void UpdateAnimPosition()
        {
            if (PlantAnim == null) return;

            // Устанавливаем позицию. Корректируем базовый сдвиг по Y на 10-20 пикселей вверх/вниз, 
            // чтобы растение визуально "стояло" на траве, а не парило в воздухе.
            PlantAnim.Position = new Vector2(Position.X, Position.Y);
        }

        /// <summary>
        /// Позволяет переключить логическое состояние растения и обновить его поведение.
        /// </summary>
        public virtual void SetState(PlantState newState)
        {
            if (State == newState || IsDead) return;
            State = newState;
            OnStateChanged(newState);
        }

        /// <summary>
        /// Срабатывает при смене состояний. Здесь можно настраивать диапазоны кадров (Frame Bounds).
        /// </summary>
        protected virtual void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;

            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.SetFrameBounds("anim_idle");
                    break;
            }
        }

        public virtual void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            // Обновляем таймлайн скелетной анимации
            PlantAnim?.Update(dt);
        }

        public virtual void Render(SpriteBatch batch, Vector2? camera_offset)
        {
            if (IsDead) return;

            if (PlantAnim != null)
            {
                // Отрисовываем всю структуру костей и треков растения через матричный рендерер квадов
                batch.Draw(_shadowTexture, new Vector2(Position.X, Position.Y + 80f), new Vector2(1.5f, 1.5f), 0f, Color4.White);
                PlantAnim.Render(batch);
            }
        }

        public virtual void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            SetState(PlantState.Dying);
        }
    }

    public static class PlantFactory
    {
        public static Plant? CreatePlant(PlantType type, int row, int col)
        {
            Plant? plant = type switch
            {
                PlantType.Peashooter => new Peashooter(row, col),
                PlantType.Sunflower => new Sunflower(row, col),
                PlantType.CherryBomb => new CherryBomb(row, col),
                PlantType.WallNut => new WallNut(row, col),
                PlantType.PotatoMine => new PotatoMine(row, col),
                PlantType.SnowPea => new SnowPea(row, col),
                PlantType.Chomper => new Chomper(row, col),
                PlantType.Repeater => new Repeater(row, col),

                PlantType.PuffShroom => new PuffShroom(row, col),
                PlantType.SunShroom => new SunShroom(row, col),
                PlantType.FumeShroom => new FumeShroom(row, col),
                PlantType.GraveBuster => new GraveBuster(row, col),
                PlantType.HypnoShroom => new HypnoShroom(row, col),
                PlantType.ScaredyShroom => new ScaredyShroom(row, col),
                PlantType.IceShroom => new IceShroom(row, col),
                PlantType.DoomShroom => new DoomShroom(row, col),

                PlantType.LilyPad => new LilyPad(row, col),
                PlantType.Squash => new Squash(row, col),
                PlantType.Threepeater => new Threepeater(row, col),
                PlantType.TangleKelp => new TangleKelp(row, col),
                PlantType.Jalapeno => new Jalapeno(row, col),
                PlantType.Spikeweed => new Spikeweed(row, col),
                PlantType.Torchwood => new Torchwood(row, col),
                PlantType.TallNut => new TallNut(row, col),

                PlantType.SeaShroom => new SeaShroom(row, col),
                PlantType.Plantern => new Plantern(row, col),
                PlantType.Cactus => new Cactus(row, col),
                PlantType.Blover => new Blover(row, col),
                PlantType.SplitPea => new SplitPea(row, col),
                PlantType.Starfruit => new Starfruit(row, col),
                PlantType.PumpkinShell => new PumpkinShell(row, col),
                PlantType.MagnetShroom => new MagnetShroom(row, col),

                PlantType.CabbagePult => new CabbagePult(row, col),
                PlantType.FlowerPot => new FlowerPot(row, col),
                PlantType.KernelPult => new KernelPult(row, col),
                PlantType.InstantCoffee => new InstantCoffee(row, col),
                PlantType.Garlic => new Garlic(row, col),
                PlantType.Umbrella => new Umbrella(row, col),
                PlantType.Marigold => new Marigold(row, col),
                PlantType.MelonPult => new MelonPult(row, col),

                PlantType.GatlingPea => new GatlingPea(row, col),
                PlantType.TwinSunflower => new TwinSunflower(row, col),
                PlantType.GloomShroom => new GloomShroom(row, col),
                PlantType.Cattail => new Cattail(row, col),
                PlantType.WinterMelon => new WinterMelon(row, col),
                PlantType.GoldMagnet => new GoldMagnet(row, col),
                PlantType.SpikeRock => new SpikeRock(row, col),
                PlantType.CobCannon => new CobCannon(row, col),

                PlantType.Imitater => new Imitater(row, col),
                PlantType.ExplodeONut => new ExplodeONut(row, col),
                PlantType.GiantWallNut => new GiantWallNut(row, col),
                PlantType.Sprout => new Sprout(row, col),
                PlantType.LeftPeater => new LeftPeater(row, col),

                _ => null
            };

            if (plant == null)
            {
                Console.WriteLine($"[PlantFactory] Ошибка: тип {type} не поддерживается фабрикой.");
                return null;
            }

            plant.Initialize();
            return plant;
        }
    }

    public class Peashooter : Plant
    {
        private float _shootTimer = 0f;
        private const float SHOOT_INTERVAL = 1.5f;
        private Reanimation _headAnim;
        private bool _isAttacking = false;
        private bool _hasSpawnedProjectile = false;

        public Peashooter(int row, int col) : base(PlantType.Peashooter, row, col) { SunCost = 100; }

        public override void Initialize()
        {
            base.Initialize();

            string animKey = $"REANIM_{Type.ToString().ToUpper()}";
            ReanimDefinition def = AssetManager.GetAnimation(animKey);

            if (def != null && AssetManager.Active != null)
            {
                PlantAnim.SetFrameBounds("anim_idle");
                PlantAnim.LoopType = ReanimLoopType.Loop;

                _headAnim = new Reanimation(def, AssetManager.Active)
                {
                    LoopType = ReanimLoopType.Loop,
                    Scale = new Vector2(1.5f, 1.5f)
                };
                _headAnim.SetFrameBounds("anim_head_idle"); 
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (PlantAnim != null && _headAnim != null)
            {
                // Динамически цепляемся к кости стебля и применяем калибровочный оффсет
                _headAnim.Position = PlantAnim.GetTrackPosition("anim_stem") + new Vector2(-56f, -70f);
                _headAnim.Update(dt * 2f);
            }

            _shootTimer += dt;

            if (HasZombieInLane())
            {
                if (!_isAttacking && _shootTimer >= (SHOOT_INTERVAL - 0.5f))
                {
                    _isAttacking = true;
                    _hasSpawnedProjectile = false;
                    if (_headAnim != null)
                    {
                        _headAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        _headAnim.SetFrameBounds("anim_shooting"); // Переключаем голову на маркер атаки!
                    }
                }

                if (_isAttacking && !_hasSpawnedProjectile && _shootTimer >= SHOOT_INTERVAL)
                {
                    _hasSpawnedProjectile = true;
                    Vector2 spawnPos = Position + new Vector2(85f, 25f);
                    projectileManager.SpawnProjectile(ProjectileType.Pea, Row, spawnPos);
                    _shootTimer -= SHOOT_INTERVAL;
                }
            }
            else if (_shootTimer > (SHOOT_INTERVAL - 0.5f) && !_isAttacking)
            {
                _shootTimer = SHOOT_INTERVAL - 0.5f;
            }

            if (_isAttacking && _headAnim != null && (_headAnim._animTime >= 0.98f || _headAnim.IsDead))
            {
                _isAttacking = false;
                _headAnim.LoopType = ReanimLoopType.Loop;
                _headAnim.SetFrameBounds("anim_head_idle"); // Возвращаем голову в idle маркер
            }
        }

        private bool HasZombieInLane()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row &&
                    zombie.State != ZombieState.Dying &&
                    !zombie.IsDead &&
                    zombie.Position.X >= Position.X &&
                    zombie.Position.X < 1600f)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Render(SpriteBatch batch, Vector2? camera_offset)
        {
            if (IsDead) return;

            var shadowTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_PLANTSHADOW");
            batch.Draw(shadowTexture, Position + new Vector2(0f, 80f), new Vector2(1.5f, 1.5f), 0f, Color4.White);

            PlantAnim?.Position = Position + (camera_offset ?? Vector2.Zero);
            PlantAnim?.Render(batch);

            Vector2? baseHeadPos = _headAnim?.Position;
            _headAnim?.Position += (camera_offset ?? Vector2.Zero);
            _headAnim?.Render(batch);
            _headAnim?.Position = (Vector2)baseHeadPos;
            
        }
    }

    public class Sunflower(int row, int col) : Plant(PlantType.Sunflower, row, col)
    {
        private float _sunProduceTimer = SUN_COOLDOWN - (7f + Random.Shared.NextSingle() * 2f);
        private const float SUN_COOLDOWN = 20f;
        private bool _isGlowing = false;
        private float _glowTimer = 0f;

        public override void Initialize()
        {
            base.Initialize();
            PlantAnim.SetFrameBounds("anim_idle");
            PlantAnim.LoopType = ReanimLoopType.Loop;
        }
        public override void Update(float dt, ProjectileManager projectileManager)
        {
            base.Update(dt, projectileManager); 

            if (IsDead) return;

            _sunProduceTimer += dt;

            if (!_isGlowing && _sunProduceTimer >= (SUN_COOLDOWN - 1.5f))
            {
                _isGlowing = true;
                State = PlantState.Action;
                
            }

            if (_isGlowing)
            {
                _glowTimer += dt;
                float glowIntensity = MathF.Sin(_glowTimer * 1f) * 0.05f;
                PlantAnim.ColorOverride = new Color4(1f, 1f, glowIntensity, 1f);
            }

            if (_sunProduceTimer >= SUN_COOLDOWN)
            {
                _sunProduceTimer = 0f;
                _isGlowing = false;
                _glowTimer = 0f;
                State = PlantState.Idle;
                PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                Vector2 sunSpawnPos = Position + new Vector2(30f, -40f);
                BoardScene.CurrentItemManager?.SpawnItem(ItemType.SunNormal, sunSpawnPos);
            }
        }
    }

    public class CherryBomb : Plant
    {
        private bool _hasExploded = false;
        private float _fuseTimer = 0f;
        private const float EXPLOSION_DELAY = 1.0f; // Общее время до взрыва (синхронно с анимацией)

        public CherryBomb(int row, int col) : base(PlantType.CherryBomb, row, col)
        {
            SunCost = 150;
            Health = 300;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (PlantAnim != null)
            {
                PlantAnim.SetFrameBounds("anim_explode");
                // Заставляем проиграть анимацию один раз до кадра удержания
                PlantAnim.LoopType = ReanimLoopType.PlayOnce;
                PlantAnim.Scale = new Vector2(1.5f, 1.5f);
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead || _hasExploded) return;

            // Обновляем базовую анимацию покачивания/надувания
            base.Update(dt, projectileManager);

            _fuseTimer += dt;

            // ВИЗУАЛЬНЫЙ ЭФФЕКТ: Спец-эффект раздувания и покраснения перед взрывом (кривая PopCap)
            if (PlantAnim != null)
            {
                float progress = Math.Clamp(_fuseTimer / EXPLOSION_DELAY, 0f, 1f);

                // Чем ближе взрыв, тем сильнее вишня увеличивается (от 1.5x до 1.9x)
                float scalePulse = 1.5f + MathF.Pow(progress, 3f) * 0.4f;
                PlantAnim.Scale = new Vector2(scalePulse, scalePulse);

            }

            // ТРИГГЕР ДЕТОНАЦИИ: Анимация подошла к концу, либо сработал таймер запала
            if (_fuseTimer >= EXPLOSION_DELAY || (PlantAnim != null && PlantAnim.IsDead))
            {
                ExecuteExplosion();
            }
        }

        public override void TakeDamage(int damage)
        {
            // Вишня неуязвима к обычной атаке
        }

        private void ExecuteExplosion()
        {
            _hasExploded = true;
            Health = 0; 
            Vector2 explosionCenter = Position + new Vector2(80f, 80f);

            BoardScene.CurrentParticleManager?.SpawnEffect("PARTICLE_POWIE", explosionCenter);
            const int EXPLOSION_DAMAGE = 1800;

            int minRow = Math.Max(0, Row - 1);
            int maxRow = Math.Min(Grid.TotalRows - 1, Row + 1);

            List<Zombie> activeZombies = BoardScene.CurrentZombies;

            if (activeZombies != null)
            {
                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    Zombie zombie = activeZombies[i];

                    // Проверяем, находится ли зомби на нашей, верхней или нижней дорожке
                    if (zombie.Row >= minRow && zombie.Row <= maxRow)
                    {
                        // Проверяем расстояние по оси X. 
                        // Одна клетка поля ~80 пикселей. Зона 3х3 покрывает примерно по 150-180 пикселей влево и вправо.
                        float distanceX = MathF.Abs(zombie.Position.X - Position.X);

                        if (distanceX <= 180f && zombie.State != ZombieState.Dying && !zombie.IsDead) zombie.TakeDamage(EXPLOSION_DAMAGE, PlantDamageType.Explosion);
                    }
                }
            }
        }
    }
    public class WallNut : Plant
    {
        public WallNut(int row, int col) : base(PlantType.WallNut, row, col) { SunCost = 50; MaxHealth = 4000; Health = MaxHealth; }

        public override void Initialize()
        {
            base.Initialize();
            PlantAnim.SetFrameBounds("anim_face"); // Стейт здорового ореха по маркеру
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            base.Update(dt, projectileManager);
        }
    }
    public class PotatoMine : Plant
    {
        private float _activationTimer = 0f;
        private const float ACTIVATION_DELAY = 15.0f; // Время взвода мины
        private bool _isArmed = false;
        private bool _isExploding = false;

        public PotatoMine(int row, int col) : base(PlantType.PotatoMine, row, col) { SunCost = 25; }

        public override void Initialize()
        {
            base.Initialize();
            PlantAnim.SetFrameBounds("anim_idle"); // Начальное состояние — под землей
            PlantAnim.LoopType = ReanimLoopType.Loop;
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            // Если растение в состоянии активного геймплея
            if (State == PlantState.Idle)
            {
                if (!_isArmed)
                {
                    _activationTimer += dt;
                    if (_activationTimer >= ACTIVATION_DELAY)
                    {
                        _isArmed = true;
                        // Оригинальный переход: сначала проигрывается анимация подъема "anim_rise"
                        PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        PlantAnim.SetFrameBounds("anim_rise");
                    }
                }
                else if (!_isExploding)
                {
                    // КРИТИЧЕСКИЙ ФИКС АНИМАЦИИ: Ждем окончания анимации подъема, прежде чем зациклить anim_armed
                    if (PlantAnim.LoopType == ReanimLoopType.PlayOnceAndHold && PlantAnim._animTime >= 0.95f)
                    {
                        PlantAnim.LoopType = ReanimLoopType.Loop;
                        PlantAnim.SetFrameBounds("anim_armed");
                        Console.WriteLine("[PotatoMine] Мина полностью поднялась и взведена!");
                    }

                    // Проверяем взрыв только если мина в режиме ожидания (anim_armed)
                    if (PlantAnim.LoopType == ReanimLoopType.Loop && CheckZombieCollision())
                    {
                        TriggerExplosionEffect();
                        ExecuteExplosionDamage();
                    }
                }
            }
        }

        private bool CheckZombieCollision()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row && !zombie.IsDead && zombie.State != ZombieState.Dying)
                {
                    float distanceX = MathF.Abs(zombie.Position.X - Position.X);
                    if (distanceX <= 40f) // Зомби наступил на мину
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void TriggerExplosionEffect()
        {
            _isExploding = true;
            // Переключаем скелет на анимацию взрыва и даем ей проиграться!
            PlantAnim.LoopType = ReanimLoopType.PlayOnce;
            PlantAnim.SetFrameBounds("anim_explode");

            // Сразу спавним визуальный эффект "SPUDOW!"
            Vector2 explosionCenter = Position + new Vector2(40f, 40f);
            BoardScene.CurrentParticleManager?.SpawnEffect("PARTICLE_POTATOMINE", explosionCenter);
        }

        private void ExecuteExplosionDamage()
        {
            Health = 0;
            SetState(PlantState.Dying);

            // Наносим урон 1800 всем зомби в радиусе клетки
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies != null)
            {
                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    var zombie = activeZombies[i];
                    if (zombie.Row == Row && MathF.Abs(zombie.Position.X - Position.X) <= 60f)
                    {
                        zombie.TakeDamage(1800, PlantDamageType.Explosion);
                    }
                }
            }
        }

        protected override void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;

            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                    PlantAnim.SetFrameBounds(_isArmed ? "anim_armed" : "anim_idle");
                    break;
            }
        }
    }

    public class SnowPea : Plant
    {
        private float _shootTimer = 0f;
        private const float SHOOT_INTERVAL = 1.5f;
        private Reanimation _headAnim;
        private bool _isAttacking = false;
        private bool _hasSpawnedProjectile = false;

        public SnowPea(int row, int col) : base(PlantType.SnowPea, row, col) { SunCost = 175; }

        public override void Initialize()
        {
            base.Initialize();

            string animKey = $"REANIM_{Type.ToString().ToUpper()}";
            ReanimDefinition def = AssetManager.GetAnimation(animKey);

            if (def != null && AssetManager.Active != null)
            {
                PlantAnim.SetFrameBounds("anim_idle");
                PlantAnim.LoopType = ReanimLoopType.Loop;

                _headAnim = new Reanimation(def, AssetManager.Active)
                {
                    LoopType = ReanimLoopType.Loop,
                    Scale = new Vector2(1.5f, 1.5f)
                };
                _headAnim.SetFrameBounds("anim_head_idle");
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (PlantAnim != null && _headAnim != null)
            {
                _headAnim.Position = PlantAnim.GetTrackPosition("anim_stem") + new Vector2(-56f, -70f);
                _headAnim.Update(dt * 2f);
            }

            _shootTimer += dt;

            if (HasZombieInLane())
            {
                if (!_isAttacking && _shootTimer >= (SHOOT_INTERVAL - 0.5f))
                {
                    _isAttacking = true;
                    _hasSpawnedProjectile = false;
                    if (_headAnim != null)
                    {
                        _headAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        _headAnim.SetFrameBounds("anim_shooting");
                    }
                }

                if (_isAttacking && !_hasSpawnedProjectile && _shootTimer >= SHOOT_INTERVAL)
                {
                    _hasSpawnedProjectile = true;
                    Vector2 spawnPos = Position + new Vector2(85f, 25f);

                    // Спавним замораживающий горох (проверьте точное имя типа в вашем ProjectileType)
                    projectileManager.SpawnProjectile(ProjectileType.SnowPea, Row, spawnPos);

                    _shootTimer -= SHOOT_INTERVAL;
                }
            }
            else if (_shootTimer > (SHOOT_INTERVAL - 0.5f) && !_isAttacking)
            {
                _shootTimer = SHOOT_INTERVAL - 0.5f;
            }

            if (_isAttacking && _headAnim != null && (_headAnim._animTime >= 0.98f || _headAnim.IsDead))
            {
                _isAttacking = false;
                _headAnim.LoopType = ReanimLoopType.Loop;
                _headAnim.SetFrameBounds("anim_head_idle");
            }
        }

        private bool HasZombieInLane()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row &&
                    zombie.State != ZombieState.Dying &&
                    !zombie.IsDead &&
                    zombie.Position.X >= Position.X &&
                    zombie.Position.X < 1600f)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Render(SpriteBatch batch, Vector2? camera_offset)
        {
            if (IsDead) return;

            var shadowTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_PLANTSHADOW");
            batch.Draw(shadowTexture, Position + new Vector2(0f, 80f), new Vector2(1.5f, 1.5f), 0f, Color4.White);

            PlantAnim?.Position = Position + (camera_offset ?? Vector2.Zero);
            PlantAnim?.Render(batch);

            Vector2? baseHeadPos = _headAnim?.Position;
            _headAnim?.Position += (camera_offset ?? Vector2.Zero);
            _headAnim?.Render(batch);
            _headAnim?.Position = (Vector2)baseHeadPos;
        }
    }

    public class Chomper : Plant
    {
        private float _chewTimer = 0f;
        private const float CHEW_DURATION = 30.0f; // Время пережевывания
        private bool _isChewing = false;
        private bool _isBiting = false;
        private Zombie? _targetZombie = null;

        public Chomper(int row, int col) : base(PlantType.Chomper, row, col) { SunCost = 150; }

        public override void Initialize()
        {
            base.Initialize();
            PlantAnim.SetFrameBounds("anim_idle");
            PlantAnim.LoopType = ReanimLoopType.Loop;
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (State == PlantState.Idle)
            {
                if (_isChewing)
                {
                    _chewTimer -= dt;
                    if (_chewTimer <= 0f)
                    {
                        if (PlantAnim.HasMarker("anim_swallow") && PlantAnim.FrameBoundsName != "anim_swallow")
                        {
                            PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                            PlantAnim.SetFrameBounds("anim_swallow");
                        }
                        if (PlantAnim.FrameBoundsName == "anim_swallow" && (PlantAnim._animTime >= 0.95f || PlantAnim.IsDead))
                        {
                            _isChewing = false;
                            PlantAnim.LoopType = ReanimLoopType.Loop;
                            PlantAnim.SetFrameBounds("anim_idle");
                        }
                    }
                }
                else if (_isBiting)
                {
                    if (PlantAnim._animTime >= 0.65f && _targetZombie != null && !_targetZombie.IsDead && _targetZombie.State != ZombieState.Dying) _targetZombie.TakeDamage(int.MaxValue, PlantDamageType.Instant);

                    if (PlantAnim._animTime >= 0.95f || PlantAnim.IsDead)
                    {
                        _isBiting = false;

                        if (_targetZombie != null && !_targetZombie.IsDead && _targetZombie.State != ZombieState.Dying)
                        {
                             
                            _isChewing = true;
                            _chewTimer = CHEW_DURATION;
                            PlantAnim.LoopType = ReanimLoopType.Loop;
                            PlantAnim.SetFrameBounds("anim_chew");
                        }
                        else
                        {
                            // Если зомби успел умереть от гороха во время броска Зубастика — промах, возврат в Idle
                            PlantAnim.LoopType = ReanimLoopType.Loop;
                            PlantAnim.SetFrameBounds("anim_idle");
                        }
                        _targetZombie = null;
                    }
                }
                else
                {
                    // Обычное состояние ожидания цели
                    Zombie target = FindTargetZombie();
                    if (target != null)
                    {
                        StartBiteSequence(target);
                    }
                }
            }
        }

        private Zombie FindTargetZombie()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return null;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row && !zombie.IsDead && zombie.State != ZombieState.Dying)
                {
                    float diffX = zombie.Position.X - Position.X;
                    if (diffX >= 0f && diffX <= 120f) // Диапазон укуса (~1.5 клетки)
                    {
                        return zombie;
                    }
                }
            }
            return null;
        }

        private void StartBiteSequence(Zombie target)
        {
            _isBiting = true;
            _targetZombie = target;

            PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
            PlantAnim.SetFrameBounds("anim_bite");
        }
        protected override void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;
            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                    PlantAnim.SetFrameBounds(_isChewing ? "anim_chew" : "anim_idle");
                    break;
            }
        }
    }

    public class Repeater : Plant
    {
        private float _shootTimer = 0f;
        private const float SHOOT_INTERVAL = 1.5f;
        private Reanimation _headAnim;
        private bool _isAttacking = false;

        // Переменные для механики двойного выстрела
        private int _peasToLaunch = 0;
        private float _burstTimer = 0f;
        private const float BURST_DELAY = 0.15f; // Задержка между первым и вторым горохом

        public Repeater(int row, int col) : base(PlantType.Repeater, row, col) { SunCost = 200; }

        public override void Initialize()
        {
            base.Initialize();

            string animKey = $"REANIM_{Type.ToString().ToUpper()}";
            ReanimDefinition def = AssetManager.GetAnimation(animKey);

            if (def != null && AssetManager.Active != null)
            {
                PlantAnim.SetFrameBounds("anim_idle");
                PlantAnim.LoopType = ReanimLoopType.Loop;

                _headAnim = new Reanimation(def, AssetManager.Active)
                {
                    LoopType = ReanimLoopType.Loop,
                    Scale = new Vector2(1.5f, 1.5f)
                };
                _headAnim.SetFrameBounds("anim_head_idle");
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (PlantAnim != null && _headAnim != null)
            {
                _headAnim.Position = PlantAnim.GetTrackPosition("anim_stem") + new Vector2(-56f, -70f);
                _headAnim.Update(dt * 2f);
            }

            _shootTimer += dt;

            // Логика интервальной проверки зомби и взвода анимации
            if (HasZombieInLane())
            {
                if (!_isAttacking && _shootTimer >= (SHOOT_INTERVAL - 0.5f))
                {
                    _isAttacking = true;
                    _peasToLaunch = 2; // Заряжаем 2 горошины для выстрела
                    _burstTimer = BURST_DELAY; // Готовим таймер для моментального первого выстрела

                    if (_headAnim != null)
                    {
                        _headAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        _headAnim.SetFrameBounds("anim_shooting");
                    }
                }
            }
            else if (_shootTimer > (SHOOT_INTERVAL - 0.5f) && !_isAttacking)
            {
                _shootTimer = SHOOT_INTERVAL - 0.5f;
            }

            // Механика выпуска очереди снарядов
            if (_isAttacking && _peasToLaunch > 0)
            {
                _burstTimer += dt;
                if (_burstTimer >= BURST_DELAY && _shootTimer >= SHOOT_INTERVAL)
                {
                    Vector2 spawnPos = Position + new Vector2(85f, 25f);
                    projectileManager.SpawnProjectile(ProjectileType.Pea, Row, spawnPos);

                    _peasToLaunch--;
                    _burstTimer = 0f;

                    // Когда обе горошины вылетели, сбрасываем основной кулдаун атаки
                    if (_peasToLaunch == 0)
                    {
                        _shootTimer -= SHOOT_INTERVAL;
                    }
                }
            }

            // Возврат головы в idle состояние по завершении анимации
            if (_isAttacking && _peasToLaunch == 0 && _headAnim != null && (_headAnim._animTime >= 0.98f || _headAnim.IsDead))
            {
                _isAttacking = false;
                _headAnim.LoopType = ReanimLoopType.Loop;
                _headAnim.SetFrameBounds("anim_head_idle");
            }
        }

        private bool HasZombieInLane()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row &&
                    zombie.State != ZombieState.Dying &&
                    !zombie.IsDead &&
                    zombie.Position.X >= Position.X &&
                    zombie.Position.X < 1600f)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Render(SpriteBatch batch, Vector2? camera_offset)
        {
            if (IsDead) return;

            var shadowTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_PLANTSHADOW");
            batch.Draw(shadowTexture, Position + new Vector2(0f, 80f), new Vector2(1.5f, 1.5f), 0f, Color4.White);

            PlantAnim?.Position = Position + (camera_offset ?? Vector2.Zero);
            PlantAnim?.Render(batch);

            Vector2? baseHeadPos = _headAnim?.Position;
            _headAnim?.Position += (camera_offset ?? Vector2.Zero);
            _headAnim?.Render(batch);
            _headAnim?.Position = (Vector2)baseHeadPos;
        }
    }


    public class PuffShroom : Plant
    {
        private float _shootTimer = 0f;
        private const float SHOOT_INTERVAL = 1.5f;
        private bool _isAttacking = false;

        public PuffShroom(int row, int col) : base(PlantType.PuffShroom, row, col) { SunCost = 0; }

        public override void Initialize()
        {
            base.Initialize();
            UpdateSleepState();
        }

        private void UpdateSleepState()
        {
            if (BoardScene.IsDay)
            {
                SetState(PlantState.Ready); // Стейт сна
            }
            else
            {
                SetState(PlantState.Idle);
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            // Если наступил день или изначально был день — гриб засыпает
            if (BoardScene.IsDay && State != PlantState.Ready)
            {
                SetState(PlantState.Ready);
            }
            else if (!BoardScene.IsDay && State == PlantState.Ready)
            {
                SetState(PlantState.Idle);
            }

            // Боевая логика работает только в бодрствующем состоянии
            if (State == PlantState.Idle)
            {
                _shootTimer += dt;

                if (HasZombieInShortRange())
                {
                    if (!_isAttacking && _shootTimer >= (SHOOT_INTERVAL - 0.3f))
                    {
                        _isAttacking = true;
                        PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        PlantAnim.SetFrameBounds("anim_shooting");
                        PlantAnim._animRate = 24f;
                    }

                    if (_isAttacking && _shootTimer >= SHOOT_INTERVAL)
                    {
                        Vector2 spawnPos = Position + new Vector2(70f, 70f);
                        projectileManager.SpawnProjectile(ProjectileType.Puff, Row, spawnPos);
                        _shootTimer -= SHOOT_INTERVAL;
                    }
                }
                else if (_shootTimer > (SHOOT_INTERVAL - 0.3f) && !_isAttacking)
                {
                    _shootTimer = SHOOT_INTERVAL - 0.3f;
                }

                // Возврат в анимацию покачивания
                if (_isAttacking && (PlantAnim._animTime >= 0.95f || PlantAnim.IsDead))
                {
                    _isAttacking = false;
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_idle");
                    PlantAnim._animRate = 12f;
                }
            }
        }

        private bool HasZombieInShortRange()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row && !zombie.IsDead && zombie.State != ZombieState.Dying)
                {
                    float diffX = zombie.Position.X - Position.X;
                    // PuffShroom атакует только в пределах ~3 клеток перед собой (~240-300 пикселей)
                    if (diffX >= 0f && diffX <= 360f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;

            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_idle");
                    break;

                case PlantState.Ready: // Режим сна
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_idle");
                    PlantAnim.ColorOverride = new Color4(0.5f, 0.5f, 0.65f, 1f); // Затемнение PopCap
                    break;
            }
        }
    }

    public class SunShroom : Plant
    {
        private float _sunProduceTimer = 0f;
        private const float SUN_COOLDOWN = 20f;
        private float _growthTimer = 0f;
        private const float GROWTH_DELAY = 120f; // 2 минуты до вырастания

        private bool _isBig = false;
        private bool _isGrowing = false;
        private bool _isGlowing = false;
        private float _glowTimer = 0f;

        public SunShroom(int row, int col) : base(PlantType.SunShroom, row, col) { SunCost = 25; }

        public override void Initialize()
        {
            base.Initialize();
            _sunProduceTimer = SUN_COOLDOWN - (5f + Random.Shared.NextSingle() * 3f);

            if (BoardScene.IsDay)
            {
                SetState(PlantState.Ready);
            }
            else
            {
                PlantAnim.SetFrameBounds("anim_idle"); // Маленький idle
                PlantAnim.LoopType = ReanimLoopType.Loop;
            }
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (BoardScene.IsDay && State != PlantState.Ready)
            {
                SetState(PlantState.Ready);
            }
            else if (!BoardScene.IsDay && State == PlantState.Ready)
            {
                SetState(PlantState.Idle);
            }

            if (State == PlantState.Idle)
            {
                // Логика взросления гриба
                if (!_isBig && !_isGrowing)
                {
                    _growthTimer += dt;
                    if (_growthTimer >= GROWTH_DELAY)
                    {
                        _isGrowing = true;
                        PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        PlantAnim.SetFrameBounds("anim_grow");
                    }
                }

                // Ожидание окончания фазы роста
                if (_isGrowing && (PlantAnim._animTime >= 0.95f || PlantAnim.IsDead))
                {
                    _isGrowing = false;
                    _isBig = true;
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_bigidle"); 
                }

                // Производство солнца
                _sunProduceTimer += dt;

                if (!_isGlowing && _sunProduceTimer >= (SUN_COOLDOWN - 1.5f))
                {
                    _isGlowing = true;
                }

                if (_isGlowing)
                {
                    _glowTimer += dt;
                    float glowIntensity = MathF.Sin(_glowTimer * 6f) * 0.15f;
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 0.7f + glowIntensity, 1f);
                }

                if (_sunProduceTimer >= SUN_COOLDOWN)
                {
                    _sunProduceTimer = 0f;
                    _isGlowing = false;
                    _glowTimer = 0f;
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);

                    Vector2 sunSpawnPos = Position + new Vector2(40f, 0f);

                    // Проверяем тип создаваемого солнца в зависимости от возраста
                    ItemType sunType = _isBig ? ItemType.SunNormal : ItemType.SunSmall; // Убедитесь, что SunSmall зарегистрирован в ItemType
                    BoardScene.CurrentItemManager?.SpawnItem(sunType, sunSpawnPos);
                }
            }
        }

        protected override void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;

            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds(_isBig ? "anim_bigidle" : "anim_idle");
                    break;

                case PlantState.Ready: // Сон
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds(_isBig ? "anim_bigsleep" : "anim_idle"); // PopCap содержит anim_bigsleep для взрослого
                    PlantAnim.ColorOverride = new Color4(0.45f, 0.45f, 0.6f, 1f);
                    break;
            }
        }
    }
    public class FumeShroom : Plant
    {
        private float _shootTimer = 0f;
        private const float SHOOT_INTERVAL = 1.5f;
        private bool _isAttacking = false;
        private bool _hasDealtDamage = false;

        public FumeShroom(int row, int col) : base(PlantType.FumeShroom, row, col) { SunCost = 75; }

        public override void Initialize()
        {
            base.Initialize();
            if (BoardScene.IsDay) SetState(PlantState.Ready);
        }

        public override void Update(float dt, ProjectileManager projectileManager)
        {
            if (IsDead) return;

            PlantAnim?.Update(dt);

            if (BoardScene.IsDay && State != PlantState.Ready)
            {
                SetState(PlantState.Ready);
            }
            else if (!BoardScene.IsDay && State == PlantState.Ready)
            {
                SetState(PlantState.Idle);
            }

            if (State == PlantState.Idle)
            {
                _shootTimer += dt;

                if (HasZombieInFumeRange())
                {
                    if (!_isAttacking && _shootTimer >= (SHOOT_INTERVAL - 0.4f))
                    {
                        _isAttacking = true;
                        _hasDealtDamage = false;
                        PlantAnim.LoopType = ReanimLoopType.PlayOnceAndHold;
                        PlantAnim.SetFrameBounds("anim_shooting");
                        PlantAnim._animRate = 24f;
                    }

                    // В момент пика анимации атаки наносим урон области
                    if (_isAttacking && !_hasDealtDamage && _shootTimer >= (SHOOT_INTERVAL - 0.1f))
                    {
                        _hasDealtDamage = true;
                        ExecuteFumeAreaDamage();
                    }

                    if (_isAttacking && _shootTimer >= SHOOT_INTERVAL)
                    {
                        _shootTimer -= SHOOT_INTERVAL;
                    }
                }
                else if (_shootTimer > (SHOOT_INTERVAL - 0.4f) && !_isAttacking)
                {
                    _shootTimer = SHOOT_INTERVAL - 0.4f;
                }

                if (_isAttacking && (PlantAnim._animTime >= 0.99f || PlantAnim.IsDead))
                {
                    _isAttacking = false;
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_idle");
                    PlantAnim._animRate = 12f;
                }
            }
        }

        private bool HasZombieInFumeRange()
        {
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies == null) return false;

            foreach (var zombie in activeZombies)
            {
                if (zombie.Row == Row && !zombie.IsDead && zombie.State != ZombieState.Dying)
                {
                    float diffX = zombie.Position.X - Position.X;
                    // Дальность поражения дымом составляет ~4 клетки впереди газона (340 пикселей)
                    if (diffX >= 0f && diffX <= 480f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void ExecuteFumeAreaDamage()
        {
            // Спавним систему частиц дыма перед грибом (проверьте имя "PARTICLE_FUMECLOUD")
            for (int i = 0; i < 20; i++)
            {
                Vector2 particlePos = Position + new Vector2(110f + 25 * i, 35f);
                BoardScene.CurrentParticleManager?.SpawnEffect("PARTICLE_FUMECLOUD", particlePos);
            }
            var activeZombies = BoardScene.CurrentZombies;
            if (activeZombies != null)
            {
                // Дымогриб наносит урон ВСЕМ зомби, находящимся в облаке дыма одновременно
                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    var zombie = activeZombies[i];
                    if (zombie.Row == Row && !zombie.IsDead && zombie.State != ZombieState.Dying)
                    {
                        float diffX = zombie.Position.X - Position.X;
                        if (diffX >= 0f && diffX <= 480f)
                        {
                            // Наносим базовый гороховый урон (20 ед), проходящий сквозь двери/щиты
                            zombie.TakeDamage(20, PlantDamageType.Default);
                        }
                    }
                }
            }
        }

        protected override void OnStateChanged(PlantState newState)
        {
            if (PlantAnim == null) return;

            switch (newState)
            {
                case PlantState.Idle:
                    PlantAnim.ColorOverride = new Color4(1f, 1f, 1f, 1f);
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_idle");
                    break;

                case PlantState.Ready: // Сон
                    PlantAnim.LoopType = ReanimLoopType.Loop;
                    PlantAnim.SetFrameBounds("anim_sleep"); // У FumeShroom оригинальный маркер anim_sleep присутствует
                    PlantAnim.ColorOverride = new Color4(0.5f, 0.5f, 0.65f, 1f);
                    break;
            }
        }
    }

    public class GraveBuster(int r, int c) : Plant(PlantType.GraveBuster, r, c) { }
    public class HypnoShroom(int r, int c) : Plant(PlantType.HypnoShroom, r, c) { }
    public class ScaredyShroom(int r, int c) : Plant(PlantType.ScaredyShroom, r, c) { }
    public class IceShroom(int r, int c) : Plant(PlantType.IceShroom, r, c) { }
    public class DoomShroom(int r, int c) : Plant(PlantType.DoomShroom, r, c) { }

    // Водные и бассейн
    public class LilyPad(int r, int c) : Plant(PlantType.LilyPad, r, c) { }
    public class Squash(int r, int c) : Plant(PlantType.Squash, r, c) { }
    public class Threepeater(int r, int c) : Plant(PlantType.Threepeater, r, c) { }
    public class TangleKelp(int r, int c) : Plant(PlantType.TangleKelp, r, c) { }
    public class Jalapeno(int r, int c) : Plant(PlantType.Jalapeno, r, c) { }
    public class Spikeweed(int r, int c) : Plant(PlantType.Spikeweed, r, c) { }
    public class Torchwood(int r, int c) : Plant(PlantType.Torchwood, r, c) { }
    public class TallNut(int r, int c) : Plant(PlantType.TallNut, r, c) { }
    public class SeaShroom(int r, int c) : Plant(PlantType.SeaShroom, r, c) { }

    // Туман и скрытые
    public class Plantern(int r, int c) : Plant(PlantType.Plantern, r, c) { }
    public class Cactus(int r, int c) : Plant(PlantType.Cactus, r, c) { }
    public class Blover(int r, int c) : Plant(PlantType.Blover, r, c) { }
    public class SplitPea(int r, int c) : Plant(PlantType.SplitPea, r, c) { }
    public class Starfruit(int r, int c) : Plant(PlantType.Starfruit, r, c) { }
    public class PumpkinShell(int r, int c) : Plant(PlantType.PumpkinShell, r, c) { }
    public class MagnetShroom(int r, int c) : Plant(PlantType.MagnetShroom, r, c) { }

    // Крыша
    public class CabbagePult(int r, int c) : Plant(PlantType.CabbagePult, r, c) { }
    public class FlowerPot(int r, int c) : Plant(PlantType.FlowerPot, r, c) { }
    public class KernelPult(int r, int c) : Plant(PlantType.KernelPult, r, c) { }
    public class InstantCoffee(int r, int c) : Plant(PlantType.InstantCoffee, r, c) { }
    public class Garlic(int r, int c) : Plant(PlantType.Garlic, r, c) { }
    public class Umbrella(int r, int c) : Plant(PlantType.Umbrella, r, c) { }
    public class Marigold(int r, int c) : Plant(PlantType.Marigold, r, c) { }
    public class MelonPult(int r, int c) : Plant(PlantType.MelonPult, r, c) { }

    // Улучшения
    public class GatlingPea(int r, int c) : Plant(PlantType.GatlingPea, r, c) { }
    public class TwinSunflower(int r, int c) : Plant(PlantType.TwinSunflower, r, c) { }
    public class GloomShroom(int r, int c) : Plant(PlantType.GloomShroom, r, c) { }
    public class Cattail(int r, int c) : Plant(PlantType.Cattail, r, c) { }
    public class WinterMelon(int r, int c) : Plant(PlantType.WinterMelon, r, c) { }
    public class GoldMagnet(int r, int c) : Plant(PlantType.GoldMagnet, r, c) { }
    public class SpikeRock(int r, int c) : Plant(PlantType.SpikeRock, r, c) { }
    public class CobCannon(int r, int c) : Plant(PlantType.CobCannon, r, c) { }

    // Системные / Бонусные
    public class Imitater(int r, int c) : Plant(PlantType.Imitater, r, c) { }
    public class ExplodeONut(int r, int c) : Plant(PlantType.ExplodeONut, r, c) { }
    public class GiantWallNut(int r, int c) : Plant(PlantType.GiantWallNut, r, c) { }
    public class Sprout(int r, int c) : Plant(PlantType.Sprout, r, c) { }
    public class LeftPeater(int r, int c) : Plant(PlantType.LeftPeater, r, c) { }
}



