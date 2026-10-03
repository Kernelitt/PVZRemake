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
                Vector2 sunSpawnPos = Position + new Vector2(300f, -40f);
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

                        if (distanceX <= 180f && zombie.State != ZombieState.Dying && !zombie.IsDead) zombie.TakeDamage(EXPLOSION_DAMAGE);
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
    public class PotatoMine(int r, int c) : Plant(PlantType.PotatoMine, r, c) { }
    public class SnowPea(int r, int c) : Plant(PlantType.SnowPea, r, c) { }
    public class Chomper(int r, int c) : Plant(PlantType.Chomper, r, c) { }
    public class Repeater(int r, int c) : Plant(PlantType.Repeater, r, c) { }

    public class PuffShroom(int r, int c) : Plant(PlantType.PuffShroom, r, c) { }
    public class SunShroom(int r, int c) : Plant(PlantType.SunShroom, r, c) { }
    public class FumeShroom(int r, int c) : Plant(PlantType.FumeShroom, r, c) { }
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



