using KrutolFramework.Core;
using OpenTK.Mathematics;

namespace PVZRemake.Board
{
    public class SeedBank
    {
        public Vector2 Position { get; set; } = new Vector2(0f, -140f);
        public int SunAmount { get; set; } = 150; 
        private float _appearanceTimer = 0f;
        private const float APPEARANCE_DURATION = 1.0f; // Опускается за 1 секунду
        private bool _isAnimatingAppearance = false;
        public List<SeedCard> Slots { get; private set; } = [];
        private TextureRegion? _bgTexture;
        private readonly FontRenderer _font;

        public SeedBank()
        {
            _bgTexture = AssetManager.GetTexture("IMAGE_REANIM_SEEDBANK");
            _font = AssetManager.GetFont("BRIANNE_TOD", 18); // Оригинальный шрифт счетчика солнц

            _font ??= AssetManager.GetFont("Arial", 16); // Запасной
        }

        /// <summary>
        /// Заполняет банк картами, выбранными в SeedChooser
        /// </summary>
        public void PopulateSlots(List<PlantType> chosenPlants, Action<SeedCard> onCardClicked)
        {
            Slots.Clear();
            float startX = Position.X + 120f; // Смещение относительно счетчика солнц
            float spacing = 75f;

            for (int i = 0; i < chosenPlants.Count; i++)
            {
                Vector2 cardPos = new(startX + (i * spacing), Position.Y + 5f);
                var card = new SeedCard(chosenPlants[i], cardPos)
                {
                    OnSelected = onCardClicked
                };
                Slots.Add(card);
            }
        }

        public void StartAppearanceAnimation()
        {
            _appearanceTimer = 0f;
            _isAnimatingAppearance = true;
            Position = new(Position.X, -140f); // Стартуем за верхней границей экрана
        }

        public void Update(float dt)
        {
            if (_isAnimatingAppearance)
            {
                _appearanceTimer += dt;
                float progress = Math.Clamp(_appearanceTimer / APPEARANCE_DURATION, 0f, 1f);

                // Используем оригинальную кривую EaseInOut для мягкого опускания
                float currentY = TodCurveMath.TodCurveEvaluate(progress, -140f, 0f, TodCurves.CURVE_EASE_IN_OUT);
                Position = new Vector2(Position.X, currentY);

                if (progress >= 1f) _isAnimatingAppearance = false;
            }

            // ОСТАВЛЯЕМ ТОЛЬКО ОДИН ЦИКЛ: пересчитываем координаты и обновляем карты за один проход
            float startX = Position.X + 120f;
            float spacing = 75f;
            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].Position = new Vector2(startX + (i * spacing), Position.Y + 5f);
                Slots[i].IsEnabled = (SunAmount >= Slots[i].SunCost) && !_isAnimatingAppearance;
                Slots[i].Update(dt); // Внутри этого метода корректно обновится и позиция иконки _iconReanim
            }
        }

        public void Render(SpriteBatch batch)
        {
            // 1. Рисуем деревянную подложку банка
            if (_bgTexture.Value.AtlasTextureHandle != 0)
            {
                batch.Draw((TextureRegion)_bgTexture, Position, new Vector2(1.5f, 1.5f), 0f, Color4.White);
            }

            // 2. Рисуем текст количества солнышек (центрируем в области счетчика)
            string sunText = SunAmount.ToString();
            Vector2 textPos = Position + new Vector2(29f, 100f); // Координаты подгоняйте под арт
            _font?.DrawText(batch, sunText, textPos, new Vector2(1.5f,1.5f), Color4.Black);

            // 3. Рисуем карты семян
            foreach (var card in Slots)
            {
                card.Render(batch);
            }
        }
    }

    public class SeedChooser
    {
        private TextureRegion _bgCatalog;
        private readonly List<SeedCard> _catalogCards = [];
        private readonly List<PlantType> _chosenCards = [];

        public bool IsSelectionFinished { get; private set; } = false;
        public List<PlantType> ChosenPlants => _chosenCards;

        private readonly UIButton _btnStartGame;
        private float _appearanceTimer = 0f;
        private const float APPEARANCE_DURATION = 1.0f; // Выдвигается за 1 секунду
        private bool _isAnimatingAppearance = false;

        public Vector2 BasePosition { get; set; } = new Vector2(0f, 950f); // Целевая позиция окна
        public Vector2 CurrentPosition { get; private set; }

        public SeedChooser()
        {
            _bgCatalog = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BACKGROUND");

            // ДОБАВЛЕНЫ ВСЕ НОВЫЕ РАСТЕНИЯ ИЗ ТАБЛИЦЫ
            // Исключаем системные/технические (Imitater, ExplodeONut, GiantWallNut, Sprout, LeftPeater),
            // оставляя основные 40 классических растений из уровней и 8 улучшений (всего 48 штук)
            var allAvailable = new[]
            { 
            // День
            PlantType.Peashooter, PlantType.Sunflower, PlantType.CherryBomb, PlantType.WallNut,
            PlantType.PotatoMine, PlantType.SnowPea, PlantType.Chomper, PlantType.Repeater,
            // Ночь
            PlantType.PuffShroom, PlantType.SunShroom, PlantType.FumeShroom, PlantType.GraveBuster,
            PlantType.HypnoShroom, PlantType.ScaredyShroom, PlantType.IceShroom, PlantType.DoomShroom,
            // Бассейн
            PlantType.LilyPad, PlantType.Squash, PlantType.Threepeater, PlantType.TangleKelp,
            PlantType.Jalapeno, PlantType.Spikeweed, PlantType.Torchwood, PlantType.TallNut,
            // Туман
            PlantType.SeaShroom, PlantType.Plantern, PlantType.Cactus, PlantType.Blover,
            PlantType.SplitPea, PlantType.Starfruit, PlantType.PumpkinShell, PlantType.MagnetShroom,
            // Крыша
            PlantType.CabbagePult, PlantType.FlowerPot, PlantType.KernelPult, PlantType.InstantCoffee,
            PlantType.Garlic, PlantType.Umbrella, PlantType.Marigold, PlantType.MelonPult,
            // Улучшения
            PlantType.GatlingPea, PlantType.TwinSunflower, PlantType.GloomShroom, PlantType.Cattail,
            PlantType.WinterMelon, PlantType.GoldMagnet, PlantType.SpikeRock, PlantType.CobCannon
        };

            // Настраиваем сетку: 8 колонок на 6 строк идеально вмещают 48 основных растений
            float startX = 13f;
            float startY = 120f;
            float spacingX = 90f;
            float spacingY = 125f;
            int cols = 8;

            for (int i = 0; i < allAvailable.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Vector2 pos = new(startX + (c * spacingX), startY + (r * spacingY));

                var card = new SeedCard(allAvailable[i], pos)
                {
                    OnSelected = OnCatalogCardClicked
                };
                _catalogCards.Add(card);
            }
            
            // Кнопка "Let's Rock!" / "Поехали!"
            _btnStartGame = new UIButton
            {
                Position = new Vector2(750f, 820f),
                Size = new Vector2(240f, 67f),
                TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BUTTON"),
                OnClick = () =>
                {
                    if (_chosenCards.Count > 0) IsSelectionFinished = true;
                }
            };
        }

        private void OnCatalogCardClicked(SeedCard clickedCard)
        {
            if (_chosenCards.Contains(clickedCard.PlantType))
            {
                // Если уже выбрана — убираем из списка (девыбор)
                _chosenCards.Remove(clickedCard.PlantType);
            }
            else
            {
                // Ограничение банка (максимум 6 карт на старте игры)
                if (_chosenCards.Count < 6)
                {
                    _chosenCards.Add(clickedCard.PlantType);
                }
            }
        }

        public void StartAppearanceAnimation()
        {
            _appearanceTimer = 0f;
            _isAnimatingAppearance = true;
            IsSelectionFinished = false;
            _chosenCards.Clear(); // Очищаем старый выбор перед новой игрой
            CurrentPosition = new Vector2(BasePosition.X, 950f); // Стартуем под нижним краем экрана
        }

        public void Update(float dt)
        {
            if (_isAnimatingAppearance)
            {
                _appearanceTimer += dt;
                float progress = Math.Clamp(_appearanceTimer / APPEARANCE_DURATION, 0f, 1f);

                // Мягко выезжаем снизу по кривой EaseInOut
                float currentY = TodCurveMath.TodCurveEvaluate(progress, BasePosition.Y, 130f, TodCurves.CURVE_EASE_IN_OUT);
                CurrentPosition = new Vector2(BasePosition.X, currentY);

                if (progress >= 1f) _isAnimatingAppearance = false;
            }

            // Выравнивание сетки относительно текущего положения экрана
            float startCatalogX = CurrentPosition.X + 20f;
            float startCatalogY = CurrentPosition.Y + 50f;
            float spacingX = 83f;
            float spacingY = 116f;
            int cols = 8;

            for (int i = 0; i < _catalogCards.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                _catalogCards[i].Position = new Vector2(startCatalogX + (c * spacingX), startCatalogY + (r * spacingY));

                // КРИТИЧЕСКИЙ ФИКС: Если растение уже выбрано геймером в банк,
                // принудительно отключаем кликабельность (IsEnabled = false), чтобы визуально притемнить карту в каталоге
                bool isAlreadyChosen = _chosenCards.Contains(_catalogCards[i].PlantType);

                _catalogCards[i].IsEnabled = !_isAnimatingAppearance && !isAlreadyChosen;
                _catalogCards[i].Update(dt);
            }

            // Обновляем и двигаем кнопку "Let's Rock!" вслед за окном
            _btnStartGame.Position = CurrentPosition + new Vector2(750f, 650f);
            _btnStartGame.IsEnabled = (_chosenCards.Count > 0 && !_isAnimatingAppearance);
            _btnStartGame.Update(dt);
        }

        public void Render(SpriteBatch batch)
        {
            if (_bgCatalog.AtlasTextureHandle != 0)
            {
                // Рисуем фон каталога в его текущей анимированной позиции
                batch.Draw(_bgCatalog, CurrentPosition, new Vector2(1.5f, 1.5f), 0f, Color4.White);
            }

            // Отрисовка карт каталога
            foreach (var card in _catalogCards)
            {
                // Чтобы дать игроку возможность убирать выбранные карты из банка обратно в каталог,
                // сделаем так: если карта выбрана, поверх неё рисуется легкий полупрозрачный силуэт,
                // но притемненный каркас (`cardColor` внутри `SeedCard.Render`) отработает автоматически благодаря `IsEnabled = false`.
                card.Render(batch);
            }

            var font = AssetManager.GetFont("Arial", 14);
            font?.DrawText(batch, $"Выбрано карт: {_chosenCards.Count} / 6", CurrentPosition + new Vector2(50f, 0f), Vector2.One, Color4.White);

            _btnStartGame.Render(batch);
        }
    }

}
