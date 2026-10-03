using KrutolFramework.Core;
using OpenTK.Mathematics;
using PVZRemake.Board;
using System;
using System.Collections.Generic;

namespace PVZRemake.SubFramework
{
    public class ShopItem(string id, string displayName, int price, string description, string iconAssetKey)
    {
        public string Id = id;
        public string DisplayName = displayName;
        public int Price = price;
        public string Description = description;
        public string IconAssetKey = iconAssetKey; // Ключ текстуры иконки товара
    }

    public class CrazyDaveShop
    {
        private bool _isVisible = false;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (_isVisible) TriggerShopEnterSequence();
            }
        }

        // Слои окружения автомобиля Дейва
        private TextureRegion _bgTexture;       // Store_Car.png
        private TextureRegion _carTexture;       // Store_Car.png
        private TextureRegion _hatchbackOpen;   // Store_HatchbackOpen.png
        private TextureRegion _storeSign;       // Store_Sign.png
        private TextureRegion _priceTagTexture; // Store_PriceTag.png
        private TextureRegion _speechBubble;    // Store_SpeechBubble2.png
        private TextureRegion _coinBank;    // Store_SpeechBubble2.png
        private FontRenderer _font;

        private UIButton _btnExit;
        private List<UIButton> _itemButtons = [];
        private List<ShopItem> _shelfItems = [];

        // Анимации Сумасшедшего Дейва
        private Reanimation? _daveAnim;
        private string _currentDaveSpeech = "";

        // Игры Скины и Окна
        private static readonly DialogWindowSkin windowSkin = new()
        {
            TopLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPLEFT"),
            TopMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPMIDDLE"),
            TopRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPRIGHT"),
            CenterLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERLEFT"),
            CenterMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERMIDDLE"),
            CenterRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERRIGHT"),
            BottomLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BOTTOMLEFT"),
            BottomMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BOTTOMMIDDLE"),
            BottomRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BOTTOMRIGHT"),
        };

        private static readonly Button3PartSkin standardButtonSkin = new()
        {
            Left = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_LEFT"),
            Middle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_MIDDLE"),
            Right = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_RIGHT"),
        };

        private UIDialogWindow ConfirmDialogWindow;
        private UIDialogWindow NoMoneyDialogWindow;

        private bool _showConfirmDialog = false;
        private bool _showNoMoneyDialog = false;
        private ShopItem? _pendingItem = null;

        private UI3PartButton _btnConfirmOk;
        private UI3PartButton _btnConfirmCancel;
        private UI3PartButton _btnNoMoneyClose;

        public CrazyDaveShop()
        {
            // Загрузка графических ассетов PopCap
            _bgTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_BACKGROUND");
            _carTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_CAR");
            _hatchbackOpen = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_HATCHBACKOPEN");
            _storeSign = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_SIGN");
            _priceTagTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_PRICETAG");
            _speechBubble = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_SPEECHBUBBLE2");
            _coinBank = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_COINBANK");

            _font = AssetManager.GetFont("BRIANNE_TOD", 18) ?? AssetManager.GetFont("Arial", 16);

            // Кнопка Выйти (размещается на вывеске MAIN MENU / EXIT)
            _btnExit = new UIButton
            {
                TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_MAINMENUBUTTON"),
                TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_MAINMENUBUTTONHIGHLIGHT"),
                TexturePressed = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_STORE_MAINMENUBUTTONDOWN"),
                Position = new Vector2(700f, 760f), // Оригинальное PvZ-расположение кнопки по центру снизу
                Size = new Vector2(138f * 1.5f, 120f),
                OnClick = () => { IsVisible = false; CloseAllDialogs(); }
            };

            ConfirmDialogWindow = new UIDialogWindow(windowSkin, (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_HEADER"), new Vector2(550f, 250f), new Vector2(500f, 350f));
            NoMoneyDialogWindow = new UIDialogWindow(windowSkin, (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_HEADER"), new Vector2(550f, 250f), new Vector2(500f, 350f));

            _btnConfirmOk = new UI3PartButton(standardButtonSkin, Vector2.Zero, 180) { OnClick = ConfirmPurchase };
            _btnConfirmCancel = new UI3PartButton(standardButtonSkin, Vector2.Zero, 180) { OnClick = CloseAllDialogs };
            _btnNoMoneyClose = new UI3PartButton(standardButtonSkin, Vector2.Zero, 200) { OnClick = CloseAllDialogs };

            // Настройка кнопок полок внутри открытого багажника (Store_HatchbackOpen)
            float startX = 800f;
            float spacingX = 110f;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var itemBtn = new UIButton
                {
                    Position = new Vector2(startX + (i * spacingX), 350f),
                    Size = new Vector2(100f, 100f),
                    OnClick = () => { OnShelfClicked(index); }
                };
                _itemButtons.Add(itemBtn);
            }

            RefreshInventoryData();
            CloseAllDialogs();
        }

        private void TriggerShopEnterSequence()
        {
            CloseAllDialogs();

            _daveAnim = ReanimDatabase.CreateRuntimeAnimation("CRAZYDAVE");
            _daveAnim?.Scale = new Vector2(1.5f, 1.5f);
            _daveAnim?.LoopType = ReanimLoopType.PlayOnceAndHold;
            _daveAnim?.SetFrameBounds(0,17); // Въезд/вход Дейва на экран
            
        }

        private void RefreshInventoryData()
        {
            _shelfItems.Clear();
            UserProfile? profile = SaveSystem.LoadProfile(LawnApp.CurrentUser.UserId);

            // 1-й Товар: Слоты семян (Эволюция)
            if (profile == null || !profile.PurchasedItems.Contains("SeedSlot7"))
                _shelfItems.Add(new ShopItem("SeedSlot7", "7 Слотов", 5000, "Позволяет брать 7 карт растений на уровень.", "IMAGE_REANIM_STORE_PACKETUPGRADE"));
            else if (!profile.PurchasedItems.Contains("SeedSlot8"))
                _shelfItems.Add(new ShopItem("SeedSlot8", "8 Слотов", 15000, "Позволяет брать 8 карт растений на уровень.", "IMAGE_REANIM_STORE_PACKETUPGRADE"));
            else if (!profile.PurchasedItems.Contains("SeedSlot9"))
                _shelfItems.Add(new ShopItem("SeedSlot9", "9 Слотов", 30000, "Позволяет брать 9 карт растений на уровень.", "IMAGE_REANIM_STORE_PACKETUPGRADE"));
            else if (!profile.PurchasedItems.Contains("SeedSlot10"))
                _shelfItems.Add(new ShopItem("SeedSlot10", "10 Слотов", 80000, "Максимальный лимит! 10 карт растений на уровень.", "IMAGE_REANIM_STORE_PACKETUPGRADE"));
            else
                _shelfItems.Add(new ShopItem("SoldOut", "Слоты Макс", 0, "Вы открыли все доступные слоты для семян.", "IMAGE_REANIM_STORE_PACKETUPGRADE"));

            // 2-й и 3-й Товары с графическими иконками
            _shelfItems.Add(new ShopItem("GardenRake", "Грабли", 200, "Убивает первого зомби на уровне.", "IMAGE_REANIM_ICON_RAKE"));
            _shelfItems.Add(new ShopItem("PoolCleaner", "Чистильщик Бассейна", 1000, "Дополнительная линия защиты для уровней на бассейне.", "IMAGE_REANIM_ICON_POOLCLEANER"));
            _shelfItems.Add(new ShopItem("RoofCleaner", "Чистильщик Крыши", 3000, "Дополнительная линия защиты для уровней на крыше.", "IMAGE_REANIM_ICON_ROOFCLEANER"));
        }

        private void OnShelfClicked(int shelfIndex)
        {
            if (shelfIndex >= _shelfItems.Count || _shelfItems[shelfIndex].Id == "SoldOut") return;
            ShopItem item = _shelfItems[shelfIndex];

            UserProfile? profile = SaveSystem.LoadProfile(LawnApp.CurrentUser.UserId);
            if (profile == null) return;

            if (item.Id != "SeedSlot7" && item.Id != "SeedSlot8" && item.Id != "SeedSlot9" && item.Id != "SeedSlot10")
            {
                if (profile.PurchasedItems.Contains(item.Id)) return;
            }

            _pendingItem = item;

            if (profile.Coins >= item.Price)
            {
                _showConfirmDialog = true;
                _showNoMoneyDialog = false;
            }
            else
            {
                _showNoMoneyDialog = true;
                _showConfirmDialog = false;
            }
        }

        private void ConfirmPurchase()
        {
            if (_pendingItem == null) return;

            UserProfile? profile = SaveSystem.LoadProfile(LawnApp.CurrentUser.UserId);
            if (profile != null && profile.Coins >= _pendingItem.Price)
            {
                profile.Coins -= _pendingItem.Price;
                profile.PurchasedItems.Add(_pendingItem.Id);

                SaveSystem.SaveProfile(profile);
                LawnApp.CurrentUser.Coins = profile.Coins;

                RefreshInventoryData();
            }
            CloseAllDialogs();
        }
        private void CloseAllDialogs()
        {
            _showConfirmDialog = false;
            _showNoMoneyDialog = false;
            _pendingItem = null;
        }
        public void Update(float dt)
        {
            if (!IsVisible) return;
            // Логика перехода анимаций Дейва из Enter в Idle покачивание
            if (_daveAnim != null)
            {
                _daveAnim.Update(dt);
                if (((_daveAnim._animTime >= 0.99f && _daveAnim.LoopType == ReanimLoopType.PlayOnceAndHold) || _daveAnim.IsDead))
                {
                    _daveAnim.LoopType = ReanimLoopType.Loop;
                    _daveAnim.SetFrameBounds(64,84); // Мягко переключаем в постоянное дыхание
                }
            }
            // Динамическая обработка НАВЕДЕНИЯ МЫШИ для монологов Дейва
            bool isHoveringAnyItem = false;
            Vector2 mousePos = Input.VirtualMousePosition;
            for (int i = 0; i < _itemButtons.Count; i++)
            {
                var btn = _itemButtons[i];
                // Проверяем попадание курсора в границы ректа кнопки товара
                if (mousePos.X >= btn.Position.X && mousePos.X <= btn.Position.X + btn.Size.X &&
                mousePos.Y >= btn.Position.Y && mousePos.Y <= btn.Position.Y + btn.Size.Y)
                {
                    isHoveringAnyItem = true;
                    if (_pendingItem != _shelfItems[i] && !_showConfirmDialog && !_showNoMoneyDialog)
                    {
                        _currentDaveSpeech = _shelfItems[i].Description;
                        if (_daveAnim != null && _daveAnim.FrameBoundsName == "anim_idle")
                        {
                            _daveAnim.LoopType = ReanimLoopType.Loop;
                            _daveAnim.SetFrameBounds(33,63); 
                        }
                    }
                }
            }
            // Если убрали курсор с товаров — возвращаем базовую реплику
            if (!isHoveringAnyItem && _daveAnim != null && _daveAnim.FrameBoundsName == "anim_blahblah" && !_showConfirmDialog && !_showNoMoneyDialog)
            {
                _daveAnim.LoopType = ReanimLoopType.Loop;
                _daveAnim.SetFrameBounds(64, 84);
            }
            if (_showConfirmDialog)
            {
                ConfirmDialogWindow.Update(dt);
                _btnConfirmOk.Position = ConfirmDialogWindow.Position + new Vector2(40f, 250f);
                _btnConfirmCancel.Position = ConfirmDialogWindow.Position + new Vector2(280f, 250f);
                _btnConfirmOk.Update(dt);
                _btnConfirmCancel.Update(dt);
                return;
            }
            if (_showNoMoneyDialog)
            {
                NoMoneyDialogWindow.Update(dt);
                _btnNoMoneyClose.Position = NoMoneyDialogWindow.Position + new Vector2(150f, 250f);
                _btnNoMoneyClose.Update(dt);
                return;
            }
            _btnExit.Update(dt);
            foreach (var btn in _itemButtons) btn.Update(dt);
        }
        public void Render(SpriteBatch batch)
        {
            if (!IsVisible) return;
            // 1. Отрисовка слоев окружения автомобиля Дейва
            if (_bgTexture.AtlasTextureHandle != 0) batch.Draw(_bgTexture, Vector2.Zero, new Vector2(1600f / _bgTexture.Width, 900f / _bgTexture.Height), 0f, Color4.White);
            if (_bgTexture.AtlasTextureHandle != 0) batch.Draw(_carTexture, new Vector2(480f, 220f), new Vector2(1.5f, 1.5f), 0f, Color4.White);
            if (_hatchbackOpen.AtlasTextureHandle != 0) batch.Draw(_hatchbackOpen, new Vector2(550f, 0f), new Vector2(1.5f, 1.5f), 0f, Color4.White);
            if (_storeSign.AtlasTextureHandle != 0) batch.Draw(_storeSign, new Vector2(520f, 0f), new Vector2(1.5f, 1.5f), 0f, Color4.White);
            _btnExit.Render(batch);
            // 2. Отрисовка Дейва и Облака его диалога
            _daveAnim?.Render(batch);
            if (_speechBubble.AtlasTextureHandle != 0 && !string.IsNullOrEmpty(_currentDaveSpeech))
            {
                Vector2 bubblePos = new Vector2(320f, 80f);
                batch.Draw(_speechBubble, bubblePos, new Vector2(1.5f, 1.5f), 0f, Color4.White);
                // Рисуем текст монолога Дейва внутри bubble-текстуры
                _font?.DrawText(batch, _currentDaveSpeech, bubblePos + new Vector2(35f, 40f), new Vector2(0.9f, 0.9f), Color4.Black);
            }
            // 3. Отрисовка ИКОНОК товаров на полках и ЦЕННИКОВ
            UserProfile? profile = SaveSystem.LoadProfile(LawnApp.CurrentUser.UserId);
            for (int i = 0; i < _shelfItems.Count; i++)
            {
                var item = _shelfItems[i];
                var btn = _itemButtons[i];
                btn.Render(batch);
                // Отрисовка графической иконки товара из ассетов
                TextureRegion iconTex = (TextureRegion)AssetManager.GetTexture(item.IconAssetKey);
                if (iconTex.AtlasTextureHandle != 0)
                {
                    Vector2 iconScale = new Vector2(btn.Size.X / iconTex.Width, btn.Size.Y / iconTex.Height) * 0.85f;
                    batch.Draw(iconTex, btn.Position, iconScale, 0f, Color4.White);
                }
                // Отрисовка подложки ценника Store_PriceTag.png под каждой полкой
                if (_priceTagTexture.AtlasTextureHandle != 0)
                {
                    Vector2 tagPos = btn.Position + new Vector2(5f, 90f);
                    batch.Draw(_priceTagTexture, tagPos, new Vector2(1.5f, 1.5f), 0f, Color4.White);
                    bool isMaxSlots = item.Id == "SoldOut";
                    bool isOwned = !isMaxSlots && item.Id != "SeedSlot7" && item.Id != "SeedSlot8" && item.Id != "SeedSlot9" && item.Id != "SeedSlot10" && profile != null && profile.PurchasedItems.Contains(item.Id);
                    if (isMaxSlots || isOwned)
                        _font?.DrawText(batch, "SOLD", tagPos + new Vector2(30f, 5f), Vector2.One, Color4.Black);
                    else
                        _font?.DrawText(batch, $"${item.Price}", tagPos + new Vector2(25f, 5f), Vector2.One, Color4.Black);
                }
            }
            // Баланс кошелька
            long currentCoins = LawnApp.CurrentUser != null ? LawnApp.CurrentUser.Coins : 0;
            batch.Draw(_coinBank, new Vector2(1290f, 840f), new Vector2(1.5f, 1.5f), 0f, Color4.White);
            _font?.DrawText(batch, $"{currentCoins}", new Vector2(1475f, 850f), new Vector2(1.5f, 1.5f), Color4.Green, TextAlignment.Right);
            // 4. Отрисовка диалоговых окон
            if (_showConfirmDialog && _pendingItem != null)
            {
                ConfirmDialogWindow.Render(batch);
                _btnConfirmOk.Render(batch);
                _btnConfirmCancel.Render(batch);
                _font?.DrawText(batch, $"Купить {_pendingItem.DisplayName}\nза ${_pendingItem.Price}?\n\n{_pendingItem.Description}", ConfirmDialogWindow.Position + new Vector2(40f, 110f), Vector2.One, Color4.White);
                _font?.DrawText(batch, "ДА", _btnConfirmOk.Position + new Vector2(75f, 8f), Vector2.One, Color4.White);
                _font?.DrawText(batch, "НЕТ", _btnConfirmCancel.Position + new Vector2(70f, 8f), Vector2.One, Color4.White);
            }
            if (_showNoMoneyDialog && _pendingItem != null)
            {
                NoMoneyDialogWindow.Render(batch);
                _btnNoMoneyClose.Render(batch);
                _font?.DrawText(batch, $"Недостаточно монет для:\n{_pendingItem.DisplayName}.\n\nНеобходимо: ${_pendingItem.Price}\nУ вас есть: ${currentCoins}", NoMoneyDialogWindow.Position + new Vector2(40f, 110f), Vector2.One, Color4.White);
                _font?.DrawText(batch, "НАЗАД", _btnNoMoneyClose.Position + new Vector2(70f, 8f), Vector2.One, Color4.White);
            }
        }
    }
}