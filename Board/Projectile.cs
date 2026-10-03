using KrutolFramework.Core;
using OpenTK.Mathematics;
using PVZRemake.SubFramework;

namespace PVZRemake.Board
{
    public enum ProjectileType
    {
        Pea,         
        SnowPea,     
        Puff,
        Cabbage,     
        Melon        
    }
    public class ProjectileManager
    {
        private readonly List<Projectile> _projectiles = [];

        /// <summary>
        /// Спавнит новый снаряд на игровое поле.
        /// </summary>
        /// <param name="type">Тип (Обычный/Ледяной горох...)</param>
        /// <param name="row">Линия газона</param>
        /// <param name="startPos">Экранная позиция спавна (обычно из головы растения)</param>
        public void SpawnProjectile(ProjectileType type, int row, Vector2 startPos)
        {
            var projectile = new Projectile(type, row, startPos);
            _projectiles.Add(projectile);
        }

        public void Update(float dt, List<Zombie> zombies, ParticleEffectManager particleManager)
        {
            // Обновляем и чистим список уничтоженных снарядов
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(dt, zombies, particleManager);

                if (_projectiles[i].IsDestroyed)
                {
                    _projectiles.RemoveAt(i);
                }
            }
        }

        public void Render(SpriteBatch batch)
        {
            // Отрисовываем все снаряды
            foreach (var projectile in _projectiles)
            {
                projectile.Render(batch);
            }
        }

        public void Clear()
        {
            _projectiles.Clear();
        }
    }
    public class Projectile
    {
        public ProjectileType Type { get; private set; }
        public int Row { get; private set; }
        public Vector2 Position { get; private set; }

        public float Speed { get; private set; } = 400f; // Скорость полета (пикселей в секунду)
        public int Damage { get; private set; } = 20;     // Базовый урон горошины в PvZ (20 единиц)
        public bool IsDestroyed { get; private set; } = false;

        private TextureRegion? _texture;

        public Projectile(ProjectileType type, int row, Vector2 startPosition)
        {
            Type = type;
            Row = row;
            Position = startPosition;

            // Настраиваем параметры под тип снаряда
            string textureKey = "IMAGE_REANIM_PROJECTILEPEA"; // Замените на точное имя вашего ассета в атласе

            if (type == ProjectileType.SnowPea)
            {
                textureKey = "IMAGE_REANIM_PROJECTILESNOWPEA";
                Damage = 20;
            }
            else if (type == ProjectileType.Puff)
            {
                textureKey = "IMAGE_PUFFSHROOM_PUFF2";
                Damage = 20;
            }

            // Получаем текстуру напрямую из активной группы ассетов
            var tex = AssetManager.GetTexture(textureKey);
            if (tex != null)
            {
                _texture = tex;
            }
        }

        public void Update(float dt, List<Zombie> zombies, ParticleEffectManager particleManager)
        {
            if (IsDestroyed) return;

            // 1. Движение снаряда вправо
            Position = new Vector2(Position.X + Speed * dt, Position.Y);

            // 2. Если снаряд улетел за экран (ширина окна у вас 1600), удаляем его
            if (Position.X > 1650f)
            {
                Destroy();
                return;
            }

            // 3. Проверка столкновения с зомби на нашей дорожке
            Zombie? hitZombie = CheckCollision(zombies);
            if (hitZombie != null)
            {
                OnHit(hitZombie, particleManager);
            }
        }

        private Zombie? CheckCollision(List<Zombie> zombies)
        {
            // Хитбокс коллизии снаряда в PvZ — тонкая вертикальная линия.
            // Зомби считается пораженным, если координата X горошины пересекает координату X зомби.
            float collisionThreshold = 15f;

            foreach (var zombie in zombies)
            {
                // Снаряд реагирует только на зомби на своей строке (Row) и только на живых
                if (zombie.Row == Row && zombie.State != ZombieState.Dying && !zombie.IsDead)
                {
                    // Горошина летит слева направо. Она попадает, когда её X сравнивается с X зомби 
                    // (с учетом небольшого оффсета, так как точка привязки зомби у ног по центру)
                    float zombieHitX = zombie.Position.X + 30f; // Примерный центр тела зомби по X

                    if (Position.X >= zombieHitX - collisionThreshold && Position.X <= zombieHitX + collisionThreshold)
                    {
                        return zombie;
                    }
                }
            }
            return null;
        }

        private void OnHit(Zombie zombie, ParticleEffectManager particleManager)
        {
            // Наносим урон зомби
            zombie.TakeDamage(Damage);

            // Если это ледяной горох — накладываем эффект замедления (в будущем реализуете в самом зомби)
            if (Type == ProjectileType.SnowPea)
            {
                // zombie.ApplyChillingEffect();
            }

            if (Type == ProjectileType.Pea)
            {
                // Передаем имя XML файла без расширения: "PeaSplat"
                particleManager.SpawnEffect("PARTICLE_PEASPLAT", Position);
            }
            else if (Type == ProjectileType.SnowPea)
            {
                // На будущее, если у вас будет ледяной всплеск
                particleManager.SpawnEffect("PARTICLE_SNOWPEASPLAT", Position);
            }
            else if (Type == ProjectileType.Puff)
            {
                // На будущее, если у вас будет ледяной всплеск
                particleManager.SpawnEffect("PARTICLE_PUFFSPLAT", Position);
            }

            Console.WriteLine($"[Projectile] Попадание! Снаряд {Type} поразил зомби на линии {Row}.");
            Destroy();
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }

        public void Render(SpriteBatch batch)
        {
            if (IsDestroyed) return;

            if (_texture != null)
            {
                // Центрируем спрайт горошины относительно логической позиции
                Vector2 originOffset = new(_texture.Value.Width * 0.5f, _texture.Value.Height * 0.5f);
                batch.Draw((TextureRegion)_texture, Position - originOffset, new(1.5f,1.5f), 0f, Color4.White);
            }
        }
    }
}
