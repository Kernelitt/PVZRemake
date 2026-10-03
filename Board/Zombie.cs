using KrutolFramework.Core;
using OpenTK.Mathematics;
using PVZRemake.Scenes;

namespace PVZRemake.Board
{
    public class Zombie : IRenderableEntity
    {
        public ZombieType Type { get; protected set; }
        public ZombieState State { get; protected set; }
        public int Row { get; set; }

        public float X => Position.X;
        public Vector2 Position { get; set; }

        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int ArmorHealth { get; protected set; }
        public int MaxArmorHealth { get; protected set; }

        public float Speed { get; protected set; }
        public bool IsDead => State == ZombieState.Dead;

        protected Reanimation ZombieAnim;
        protected Plant? TargetPlant = null;

        private float _eatTimer = 0f;
        private const float EAT_COOLDOWN = 0.5f;
        private const int EAT_DAMAGE = 50;

        // Состояния отпавших частей, чтобы не спавнить частицы повторно каждым кадром
        private bool _hasDroppedArm = false;
        private bool _hasDroppedHead = false;

        private Vector2i IdleFrames1 = new(0, 28);
        private Vector2i IdleFrames2 = new(29, 43);
        private Vector2i WalkFrames1 = new(44, 90);
        private Vector2i WalkFrames2 = new(91, 137);
        private Vector2i EatingFrames = new(138, 178);
        private Vector2i DyingFrames1 = new(178, 216);
        private Vector2i DyingFrames2 = new(217, 249);

        public Zombie(ZombieType type, int row, float startX)
        {
            Type = type;
            Row = row;
            State = ZombieState.Walking;
            startX += Random.Shared.Next(0, 100);

            GridCoords rowCoords = Grid.GetCoords(row, 0);
            float zombieVisualOffsetY = -60f;
            Position = new Vector2(startX, rowCoords.CellTopLeft.Y + zombieVisualOffsetY);

            MaxHealth = 270;
            Health = MaxHealth;
            ArmorHealth = 0;
            MaxArmorHealth = 0;
            Speed = 22f + Random.Shared.Next(0, 4);
        }

        public virtual void Initialize()
        {
            string animKey = "ZOMBIE";
            if (AssetManager.Active != null)
            {
                ZombieAnim ??= ReanimDatabase.CreateRuntimeAnimation(animKey);

                ZombieAnim.LoopType = ReanimLoopType.Loop;
                ZombieAnim.Scale = new Vector2(1.5f, 1.5f);

                ConfigureArmorTracks();
                UpdateAnimState();
            }
        }

        protected virtual void ConfigureArmorTracks()
        {
            if (ZombieAnim == null) return;
            ZombieAnim.SetTrackVisible("anim_bucket", false);
            ZombieAnim.SetTrackVisible("anim_cone", false);
            ZombieAnim.SetTrackVisible("anim_screendoor", false);
            ZombieAnim.SetTrackVisible("Zombie_outerarm_screendoor", false);
            ZombieAnim.SetTrackVisible("Zombie_innerarm_screendoor", false);
            ZombieAnim.SetTrackVisible("Zombie_innerarm_screendoor_hand", false);
            ZombieAnim.SetTrackVisible("Zombie_flaghand", false);
            ZombieAnim.SetTrackVisible("Zombie_mustache", false);
            ZombieAnim.SetTrackVisible("Zombie_duckytube", false);
            ZombieAnim.SetTrackVisible("anim_tongue", false);
        }

        public virtual void Update(float dt, List<Plant> activePlants)
        {
            if (IsDead) return;

            ZombieAnim?.Update(dt);

            switch (State)
            {
                case ZombieState.Idle:
                    // Просто стоит и дышит
                    break;

                case ZombieState.Walking:
                    TargetPlant = FindPlantAhead(activePlants);

                    if (TargetPlant != null)
                    {
                        SetState(ZombieState.Eating);
                    }
                    else
                    {
                        float time = ZombieAnim._animTime;
                        bool isStepping = (time > 0.1f && time < 0.4f) || (time > 0.6f && time < 0.9f);

                        if (isStepping)
                        {
                            // Двигаемся чуть быстрее во время шага, чтобы компенсировать простои
                            Position = new Vector2(Position.X - (Speed * 1.6f) * dt, Position.Y);
                        }
                    }
                    break;

                case ZombieState.Eating:
                    if (TargetPlant == null || TargetPlant.IsDead)
                    {
                        TargetPlant = null;
                        SetState(ZombieState.Walking);
                    }
                    else
                    {
                        _eatTimer += dt;
                        if (_eatTimer >= EAT_COOLDOWN)
                        {
                            TargetPlant.TakeDamage(EAT_DAMAGE);
                            _eatTimer = 0f;
                        }
                    }
                    break;

                case ZombieState.Dying:
                    if (ZombieAnim != null && ZombieAnim.IsDead)
                    {
                        State = ZombieState.Dead;
                    }
                    break;
            }

            ZombieAnim?.Position = Position;

            if (Health <= 100)
            {
                Health--;
            }
        }

        public virtual void Render(SpriteBatch batch, Vector2? camera_offset)
        {
            if (IsDead) return;
            if (ZombieAnim != null)
            {
                ZombieAnim.Position = Position + (camera_offset ?? Vector2.Zero);
                ZombieAnim.Render(batch);
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (State == ZombieState.Dying || IsDead) return;

            // Включаем белое мигание при получении любого урона
            ZombieAnim?.TriggerDamageFlash();

            if (ArmorHealth > 0)
            {
                ArmorHealth -= damage;
                UpdateArmorDamageStage(); // Проверяем и обновляем текстуру трещин конуса/ведра

                if (ArmorHealth <= 0)
                {
                    damage = Math.Abs(ArmorHealth);
                    ArmorHealth = 0;
                    OnArmorBroken();
                }
                else
                {
                    damage = 0; // Броня полностью поглотила урон
                }
            }

            Health -= damage;

            // Потеря руки при здоровье <= 135 (50% ХП)
            if (Health <= 170 && !_hasDroppedArm)
            {
                _hasDroppedArm = true;
                ZombieAnim?.OverrideTrackImage("IMAGE_REANIM_ZOMBIE_OUTERARM_UPPER", "IMAGE_REANIM_ZOMBIE_OUTERARM_UPPER2"); // Скрываем руку
                ZombieAnim?.SetTrackVisible("Zombie_outerarm_lower", false); // Скрываем руку
                ZombieAnim?.SetTrackVisible("Zombie_outerarm_hand", false); // Скрываем руку

                // Спавним частицу отлетающей руки
                Vector2 particlePos = Position + new Vector2(50f, 10f);
                BoardScene.CurrentParticleManager?.SpawnEffect("PARTICLE_ZOMBIEARM", particlePos);
            }

            // Критический урон: отлетает голова
            if (Health <= 70 && !_hasDroppedHead)
            {
                _hasDroppedHead = true;
                ZombieAnim?.SetTrackVisible("anim_head1", false);
                ZombieAnim?.SetTrackVisible("anim_head2", false);
                ZombieAnim?.SetTrackVisible("anim_hair", false);

                // Спавним частицу отлетающей головы
                Vector2 particlePos = Position + new Vector2(10f, 0f);
                BoardScene.CurrentParticleManager?.SpawnEffect("PARTICLE_ZOMBIEHEAD", particlePos);
            }

            if (Health <= 0)
            {
                Health = 0;
                StartDeathAnimation();
            }
        }

        protected virtual void UpdateArmorDamageStage()
        {
            // Переопределяется в подклассах для Конуса и Ведра
        }

        protected virtual void OnArmorBroken()
        {
            ConfigureArmorTracks();
        }

        protected virtual void StartDeathAnimation()
        {
            SetState(ZombieState.Dying);
        }

        public void SetState(ZombieState newState)
        {
            if (State == newState || State == ZombieState.Dead) return;
            State = newState;
            UpdateAnimState();
        }

        protected virtual void UpdateAnimState()
        {
            if (ZombieAnim == null) return;

            switch (State)
            {
                case ZombieState.Idle:
                    ZombieAnim.LoopType = ReanimLoopType.Loop;
                    Vector2i chosenIdle = Random.Shared.Next(0, 2) == 0 ? IdleFrames1 : IdleFrames2;
                    ZombieAnim.SetFrameBounds(chosenIdle.X, chosenIdle.Y);
                    break;
                case ZombieState.Walking:
                    ZombieAnim.LoopType = ReanimLoopType.Loop;
                    Vector2i chosenWalk = Random.Shared.Next(0, 2) == 0 ? WalkFrames1 : WalkFrames2;
                    ZombieAnim.SetFrameBounds(chosenWalk.X, chosenWalk.Y);
                    break;
                case ZombieState.Eating:
                    ZombieAnim.LoopType = ReanimLoopType.Loop;
                    ZombieAnim.SetFrameBounds(EatingFrames.X, EatingFrames.Y);
                    break;
                case ZombieState.Dying:
                    ZombieAnim.LoopType = ReanimLoopType.PlayOnce;
                    Vector2i chosenDead = Random.Shared.Next(0, 2) == 0 ? DyingFrames1 : DyingFrames2;
                    ZombieAnim.SetFrameBounds(chosenDead.X, chosenDead.Y);
                    break;
            }
        }

        private Plant? FindPlantAhead(List<Plant> activePlants)
        {

            float eatRange = 40f;
            foreach (var plant in activePlants)
            {
                if (plant.Row == Row && !plant.IsDead)
                {
                    float distanceX = Position.X - plant.Position.X;
                    if (distanceX >= 0 && distanceX <= eatRange)
                    {
                        return plant;
                    }
                }
            }
            return null;
        }
    }

    public static class ZombieFactory
    {
        /// <summary>
        /// Создает и инициализирует зомби определенного типа на указанной дорожке.
        /// </summary>
        /// <param name="type">Тип зомби</param>
        /// <param name="row">Строка (0..5)</param>
        /// <param name="startX">Начальная координата появления (обычно за правым краем экрана, например 1650f)</param>
        public static Zombie? CreateZombie(ZombieType type, int row, float startX = 1650f)
        {
            Zombie? zombie;
            switch (type)
            {
                case ZombieType.Normal:
                    zombie = new Zombie(ZombieType.Normal, row, startX);
                    break;
                case ZombieType.Conehead:
                    zombie = new ConeheadZombie(row, startX);
                    break;
                case ZombieType.Buckethead:
                    zombie = new BucketheadZombie(row, startX);
                    break;
                default:
                    Console.WriteLine($"[ZombieFactory] Тип зомби {type} не поддерживается.");
                    return null;
            }

            zombie.Initialize();
            return zombie;
        }
    }

    public class ConeheadZombie : Zombie
    {
        public ConeheadZombie(int row, float startX) : base(ZombieType.Conehead, row, startX)
        {
            // Обычный зомби (270) + Конус (370) = 640 здоровья всего
            ArmorHealth = 370;
        }

        protected override void ConfigureArmorTracks()
        {
            base.ConfigureArmorTracks();
            if (ZombieAnim == null) return;

            // Если броня еще цела — показываем конус, иначе скрываем
            bool showCone = ArmorHealth > 0;
            ZombieAnim.SetTrackVisible("anim_cone", showCone);
        }
    }

    public class BucketheadZombie : Zombie
    {
        public BucketheadZombie(int row, float startX) : base(ZombieType.Buckethead, row, startX)
        {
            // Обычный зомби (270) + Ведро (1100) = 1370 здоровья всего
            ArmorHealth = 1100;
        }

        protected override void ConfigureArmorTracks()
        {
            base.ConfigureArmorTracks();
            if (ZombieAnim == null) return;

            bool showBucket = ArmorHealth > 0;
            ZombieAnim.SetTrackVisible("anim_bucket", showBucket);
        }
    }
}
