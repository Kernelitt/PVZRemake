using KrutolFramework.Core;
using OpenTK.Mathematics;

namespace PVZRemake.Board
{
    public enum ItemType
    {
        SunNormal,
        SunSmall,
        CoinSilver,
        CoinGold,
        Diamond
    }


    public class BoardItem
    {
        public ItemType Type { get; private set; }
        public Vector2 Position { get; private set; }
        public bool IsCollected { get; private set; }
        public bool IsDead { get; private set; }

        private readonly Reanimation _reanim;
        private Vector2 _velocity;
        private readonly float _targetY;
        private float _existTimer = 0f;
        private const float MAX_EXIST_TIME = 10f;

        private bool _isFlyingToUI = false;
        private Vector2 _flyStartPos;
        private float _flyProgress = 0f;
        private const float FLY_DURATION = 0.5f;
        private float _alpha = 1f;
        public BoardItem(ItemType type, Vector2 startPos, Vector2 initialVelocity, float targetY)
        {
            Type = type;
            Position = startPos;
            _velocity = initialVelocity;
            _targetY = targetY;

            // Подгружаем соответствующую .reanim анимацию из базы данных
            string animKey = type == ItemType.SunNormal || type == ItemType.SunSmall ? "SUN" : "COIN_SILVER";
            _reanim = ReanimDatabase.CreateRuntimeAnimation(animKey) ?? throw new Exception($"[Item] Анимация {animKey} не найдена!");

            _reanim.LoopType = ReanimLoopType.Loop;
            _reanim.Scale = type == ItemType.SunSmall ? new Vector2(0.9f, 0.9f) : new Vector2(1.3f, 1.3f);
        }

        public void Update(float dt, Vector2 uiTargetPos, Action<BoardItem> onArrivedAtUI)
        {
            if (IsDead) return;

            _reanim.Update(dt);

            if (_isFlyingToUI)
            {
                _flyProgress += dt;
                float t = Math.Clamp(_flyProgress / FLY_DURATION, 0f, 1f);

                float smoothT = 1f - MathF.Pow(1f - t, 3f);

                Position = Vector2.Lerp(_flyStartPos, uiTargetPos, smoothT);

                // 2. УМЕНЬШАЕМ ALPHA ПЕРЕД КОНЦОМ: если пролетели больше 70% пути
                if (t > 0.7f)
                {
                    // Плавно сводим альфу от 1 до 0 на оставшихся 30% пути
                    _alpha = MathHelper.Clamp((1f - t) / 0.3f, 0f, 1f);
                }

                // Применяем прозрачность к скелету предмета
                _reanim.ColorOverride = new Color4(1f, 1f, 1f, _alpha);

                if (t >= 1f)
                {
                    onArrivedAtUI?.Invoke(this);
                    IsDead = true;
                }
                _reanim.Position = Position;
                return;
            }

            if (Position.Y < _targetY || _velocity.X != 0f)
            {
                if (_velocity.X != 0f) _velocity.Y += 450f * dt;

                Position += _velocity * dt;
                _velocity.X = MathHelper.Lerp(_velocity.X, 0f, dt * 5f);

                if (Position.Y >= _targetY && _velocity.Y > 0)
                {
                    Position = new Vector2(Position.X, _targetY);
                    _velocity = Vector2.Zero;
                }
            }
            else
            {
                _existTimer += dt;
                if (_existTimer >= MAX_EXIST_TIME) IsDead = true;
            }
        }

        public bool CheckClick(Vector2 mousePos, Vector2 cameraOffset)
        {
            if (_isFlyingToUI || IsDead) return false;

            // Переводим мировые координаты предмета в экранные для проверки клика мышкой
            Vector2 screenPos = Position + cameraOffset;
            float clickRadius = 55f; // Чуть увеличим хитбокс для удобства сбора

            if (Vector2.Distance(mousePos, screenPos) <= clickRadius)
            {
                _isFlyingToUI = true;

                // ФИКС: Запоминаем точную ЭКРАННУЮ позицию, где предмет физически находился перед глазами игрока
                _flyStartPos = screenPos;
                _flyProgress = 0f;
                return true;
            }
            return false;
        }


        public void Render(SpriteBatch batch, Vector2 cameraOffset)
        {
            if (IsDead) return;

            // Во время полета в UI предмет рисуется в экранных координатах, оффсет камеры не нужен
            if (_isFlyingToUI)
            {
                _reanim.Position = Position;
            }
            else
            {
                _reanim.Position = Position + cameraOffset;
            }

            _reanim.Render(batch);
        }
    }

    public class ItemManager
    {
        private readonly List<BoardItem> _items = [];
        private float _skySunTimer = 0f;
        private float _nextSkySunInterval = 5f; // Каждые 5-9 секунд

        public void Clear() => _items.Clear();

        /// <summary>
        /// Спавн вылетающего предмета (из Подсолнуха или поверженного Зомби)
        /// </summary>
        public void SpawnItem(ItemType type, Vector2 worldPos)
        {
            // Эффектный вылет по параболе вверх-вбок
            float randX = (Random.Shared.NextSingle() - 0.5f) * 160f;
            float randY = -220f - Random.Shared.NextSingle() * 80f;
            Vector2 velocity = new(randX, randY);

            // Приземляется чуть ниже места генерации
            float targetY = worldPos.Y + 30f + Random.Shared.NextSingle() * 20f;

            _items.Add(new BoardItem(type, worldPos, velocity, targetY));
        }

        public void Update(float dt, bool isGameplayActive, bool isDay, SeedBank seedBank, Vector2 cameraOffset)
        {
            // Точка, куда улетают солнышки (счетчик солнца). 
            // Для монет/алмазов можно сделать отдельный вектор UI, если у вас будет счетчик денег!
            Vector2 sunCounterUiPos = seedBank.Position + new Vector2(40f, 40f);
            Vector2 moneyUiPos = new(30f, 870f); // Заглушка: левый нижний угол для монет

            // 1. Сбор предметов кликом мыши
            if (isGameplayActive && Input.IsMouseButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
            {
                Vector2 mousePos = Input.VirtualMousePosition;
                foreach (var item in _items)
                {
                    if (item.CheckClick(mousePos, cameraOffset))
                    {
                        Console.WriteLine($"[ItemManager] Игрок подобрал: {item.Type}");
                        break; // Собираем строго один предмет за один клик
                    }
                }
            }

            // 2. Логика появления солнца с неба
            if (isGameplayActive && isDay)
            {
                _skySunTimer += dt;
                if (_skySunTimer >= _nextSkySunInterval)
                {
                    _skySunTimer = 0f;
                    _nextSkySunInterval = 6f + Random.Shared.NextSingle() * 5f;

                    // Рабочая область газона по X
                    float spawnX = 550f + Random.Shared.NextSingle() * 750f;

                    // ФИКС: старт с Y = 10f, чтобы солнце плавно опускалось из верхней видимой границы экрана
                    float startY = 10f;
                    float targetY = 250f + Random.Shared.NextSingle() * 350f;

                    _items.Add(new BoardItem(ItemType.SunNormal, new Vector2(spawnX, startY), new Vector2(0, 90f), targetY));
                }
            }

            // 3. Апдейт полета и начисление наград
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                // Выбираем в какой угол экрана полетит предмет в зависимости от его типа
                bool isSun = _items[i].Type == ItemType.SunNormal || _items[i].Type == ItemType.SunSmall;
                Vector2 targetUi = isSun ? sunCounterUiPos : moneyUiPos;

                _items[i].Update(dt, targetUi, (item) =>
                {
                    // НАЧИСЛЕНИЕ НАГРАД ПРИ ПРИЛЕТЕ В ИНТЕРФЕЙС
                    switch (item.Type)
                    {
                        case ItemType.SunNormal:
                            seedBank.SunAmount += 25;
                            break;
                        case ItemType.SunSmall:
                            seedBank.SunAmount += 15;
                            break;
                        case ItemType.CoinSilver:
                            Console.WriteLine("[Wallet] +10 монет (Серебро)");
                            // LawnApp.CurrentUser.Money += 10;
                            break;
                        case ItemType.CoinGold:
                            Console.WriteLine("[Wallet] +50 монет (Золото)");
                            // LawnApp.CurrentUser.Money += 50;
                            break;
                        case ItemType.Diamond:
                            Console.WriteLine("[Wallet] ++ АЛМАЗ! +1000 монет");
                            // LawnApp.CurrentUser.Money += 1000;
                            break;
                    }
                });

                if (_items[i].IsDead) _items.RemoveAt(i);
            }
        }

        public void SpawnZombieLoot(ItemType type, Vector2 zombieWorldPos)
        {
            // Импульс слегка вверх и НАЗАД (вправо по оси X, т.к. зомби двигался влево)
            float forceX = 40f + Random.Shared.NextSingle() * 60f;
            float forceY = -280f - Random.Shared.NextSingle() * 70f; // Сильный толчок вверх
            Vector2 velocity = new(forceX, forceY);

            // Монета должна упасть ровно на уровень ног зомби (чуть ниже его текущего Y)
            float targetY = zombieWorldPos.Y + 40f + (Random.Shared.NextSingle() - 0.5f) * 10f;

            // Спавним монету из центра тела зомби
            Vector2 spawnPos = zombieWorldPos + new Vector2(0f, -30f);

            _items.Add(new BoardItem(type, spawnPos, velocity, targetY));
        }

        public void Render(SpriteBatch batch, Vector2 cameraOffset)
        {
            foreach (var item in _items)
            {
                item.Render(batch, cameraOffset);
            }
        }
    }
}
