using KrutolFramework.Core;
using OpenTK.Mathematics;
using PVZRemake.Board;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PVZRemake.Scenes
{
    internal class SelectorScreen : IScene
    {
        private Reanimation? _background;

        public UserProfile? CurrentUser { get; private set; }
        private IReadOnlyList<UserMetaEntry> _cachedUsers = Array.Empty<UserMetaEntry>();

        private bool _adventureButtonVisible = false;
        private bool _usersButtonVisible = false;
        private bool _mainButtonsVisible = false;

        private FontRenderer _menuFont;

        #region Скины и базовые текстуры
        private static readonly DialogWindowSkin windowSkin = new()
        {
            TopLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPLEFT"),
            TopMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPMIDDLE"),
            TopRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_TOPRIGHT"),
            CenterLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERLEFT"),
            CenterMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERMIDDLE"),
            CenterRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_CENTERRIGHT"),
            BottomLeft = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BIGBOTTOMLEFT"),
            BottomMiddle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BIGBOTTOMMIDDLE"),
            BottomRight = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_BIGBOTTOMRIGHT"),
        };

        private static readonly Button3PartSkin standardButtonSkin = new()
        {
            Left = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_LEFT"),
            Middle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_MIDDLE"),
            Right = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_RIGHT"),
        };
        #endregion

        #region UI Главный Экран
        private readonly UIButton AdventureButton = new()
        {
            TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SELECTORSCREEN_STARTADVENTURE_BUTTON1"),
            TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SELECTORSCREEN_STARTADVENTURE_HIGHLIGHT"),
            Position = new Vector2(807, 98),
            Size = new Vector2(496, 219),
        };

        private readonly UIButton UsersButton = new()
        {
            TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SELECTORSCREEN_WOODSIGN2"),
            TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SELECTORSCREEN_WOODSIGN2_PRESS"),
            Position = new Vector2(239, 190),
            Size = new Vector2(436, 106),
        };

        private readonly UIButton MiniGamesButton = new() { Position = new Vector2(810, 310), Size = new Vector2(410, 95) };
        private readonly UIButton PuzzleButton = new() { Position = new Vector2(812, 400), Size = new Vector2(380, 90) };
        private readonly UIButton SurvivalButton = new() { Position = new Vector2(815, 485), Size = new Vector2(350, 90) };
        private readonly UIButton OptionsButton = new() { Position = new Vector2(1000, 620), Size = new Vector2(110, 40) };
        private readonly UIButton HelpButton = new() { Position = new Vector2(1115, 620), Size = new Vector2(80, 40) };
        private readonly UIButton QuitButton = new() { Position = new Vector2(1200, 615), Size = new Vector2(80, 50) };
        #endregion

        #region UI Окно Смены Профиля
        private static readonly UIDialogWindow UsersSettings = new(
            skin: windowSkin,
            headerTexture: (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_HEADER"),
            position: new Vector2(240, 60),
            size: new Vector2(800, 600)
        )
        { IsVisible = false };

        private UI3PartButton NewUserButton;
        private UI3PartButton DeleteUserButton;
        private UI3PartButton CloseUsersDialogButton;
        private readonly List<UI3PartButton> _profileSelectButtons = [];
        #endregion

        #region UI Окно Создания Нового Профиля
        private bool _isCreateUserWindowVisible = false;
        private static readonly UIDialogWindow CreateUserWindow = new(
            skin: windowSkin,
            headerTexture: (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_DIALOG_HEADER"),
            position: new Vector2(390, 220),
            size: new Vector2(500, 320)
        )
        { IsVisible = false };

        private UITextInput? _userNameInput;
        private UI3PartButton? _confirmCreateButton;
        private UI3PartButton? _cancelCreateButton;
        #endregion

        #region UI МЕНЮ ВЫБОРА УРОВНЕЙ (Новый функционал)
        private bool _isLevelSelectVisible = false;
        private TextureRegion _levelMenuBg;
        private UIButton? _closeLevelMenuButton;

        // Кнопки для самих уровней
        private readonly List<UIButton> _levelButtons = [];
        // Названия уровней для передачи в BoardScene
        private readonly List<string> _levelNames = LevelDatabase.GetAllLevelNames();
        // Нарезанные иконки из общего атласа thumbnails
        private readonly List<TextureRegion> _levelThumbnails = [];
        #endregion

        public void Initialize()
        {
            _menuFont = AssetManager.GetFont("BRIANNE_TOD", 18) ?? AssetManager.GetFont("Arial", 14);

            _background = ReanimDatabase.CreateRuntimeAnimation("REANIM_SELECTORSCREEN");
            _background?.Position = new Vector2(200, 0);
            _background?.Scale = new Vector2(1.5f, 1.5f);
            _background?.LoopType = ReanimLoopType.PlayOnceAndHold;
            _background?.SetFrameBounds(0, 41);

            _background?.SetTrackVisible("SelectorScreen_Adventure_button", false);
            _background?.SetTrackVisible("SelectorScreen_Adventure_shadow", false);

            _adventureButtonVisible = false;
            _usersButtonVisible = false;
            _mainButtonsVisible = false;

            // Загрузка сохраненного пользователя
            RefreshCachedUsersList();
            if (_cachedUsers.Count > 0)
            {
                CurrentUser = SaveSystem.LoadProfile(_cachedUsers[0].UserId);
            }
            else
            {
                CurrentUser = SaveSystem.CreateNewProfile("Browncoat");
                RefreshCachedUsersList();
            }

            // Вместо перехода на один уровень, большая кнопка теперь открывает меню выбора
            AdventureButton.OnClick = () => { OpenLevelSelectMenu(); };
            MiniGamesButton.OnClick = () => { OpenLevelSelectMenu(); };
            SurvivalButton.OnClick = () => { OpenLevelSelectMenu(); };

            UsersButton.OnClick = () => { OpenProfileDialog(); };
            QuitButton.OnClick = () => { Environment.Exit(0); };

            InitializeProfileDialogComponents();
            InitializeCreateUserComponents();
            InitializeLevelSelectComponents();
        }

        private void InitializeProfileDialogComponents()
        {
            // ИСПРАВЛЕНО: Координаты кнопок жестко наследуют динамическую позицию родительского диалогового окна!
            NewUserButton = new UI3PartButton(standardButtonSkin, UsersSettings.Position + new Vector2(40, 520), 220)
            {
                OnClick = () => { OpenCreateUserModal(); }
            };

            DeleteUserButton = new UI3PartButton(standardButtonSkin, UsersSettings.Position + new Vector2(280, 520), 220)
            {
                OnClick = () =>
                {
                    if (CurrentUser != null && _cachedUsers.Count > 1)
                    {
                        SaveSystem.DeleteUser(CurrentUser.UserId);
                        RefreshCachedUsersList();
                        CurrentUser = SaveSystem.LoadProfile(_cachedUsers[0].UserId);
                        RefreshProfileList();
                    }
                }
            };

            CloseUsersDialogButton = new UI3PartButton(standardButtonSkin, UsersSettings.Position + new Vector2(540, 520), 220)
            {
                OnClick = () => { UsersSettings.IsVisible = false; }
            };
        }


        private void InitializeCreateUserComponents()
        {
            _userNameInput = new UITextInput()
            {
                Font = _menuFont,
                Position = CreateUserWindow.Position + new Vector2(50, 130),
                Size = new Vector2(400, 45),
                MaxLength = 12,
                BackgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_BUTTON_MIDDLE")
            };

            _confirmCreateButton = new UI3PartButton(standardButtonSkin, CreateUserWindow.Position + new Vector2(40, 230), 180)
            {
                OnClick = () =>
                {
                    string trimmedName = _userNameInput.Text.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        UserProfile newProfile = SaveSystem.CreateNewProfile(trimmedName);

                        var allUsers = new List<UserMetaEntry>(SaveSystem.LoadUsers());
                        var currentEntry = allUsers.Find(u => u.UserId == newProfile.UserId);
                        if (currentEntry != null)
                        {
                            currentEntry.Name = trimmedName;
                            SaveSystem.SaveUsers(allUsers);
                        }

                        CurrentUser = newProfile;
                        _isCreateUserWindowVisible = false;
                        RefreshCachedUsersList();

                        // ДОБАВИТЬ СТРОКУ НИЖЕ: Синхронизируем мета-данные фреймворка с только что созданным юзером (он автоматически встал на 0 место)
                        if (_cachedUsers.Count > 0) LawnApp.CurrentUsers = _cachedUsers[0];

                        RefreshProfileList();
                    }
                }
            };

            _cancelCreateButton = new UI3PartButton(standardButtonSkin, CreateUserWindow.Position + new Vector2(280, 230), 180)
            {
            OnClick = () => { _isCreateUserWindowVisible = false; }
            };
        }
        private void InitializeLevelSelectComponents()
        {
            _levelMenuBg = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_CHALLENGE_BACKGROUND");
            // Кнопка закрытия меню выбора уровней (справа внизу)
            _closeLevelMenuButton = new UIButton()
            {
                TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BUTTON2"),
                TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDCHOOSER_BUTTON2_GLOW"),
                Position = new Vector2(10, 870),
                Size = new Vector2(111, 26),
                OnClick = () => { _isLevelSelectVisible = false; }
            };
            // Загружаем общий большой атлас с иконками (содержит 11 штук в длину)
            TextureRegion fullThumbnailsAtlas = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SURVIVAL_THUMBNAILS");
            _levelThumbnails.Clear();
            _levelButtons.Clear();
            // Логика автоматической нарезки атласа на 11 равных частей по горизонтали
            if (fullThumbnailsAtlas.AtlasTextureHandle != 0)
            {
                float singleIconWidth = fullThumbnailsAtlas.Width / 11f;
                float totalUWidth = fullThumbnailsAtlas.U2 - fullThumbnailsAtlas.U1;
                for (int i = 0; i < 11; i++)
                {
                    // Вычисляем точные UV-координаты для каждого отдельного прямоугольника иконки
                    float u1 = fullThumbnailsAtlas.U1 + (totalUWidth / 11f) * i;
                    float u2 = u1 + (totalUWidth / 11f);
                    var iconRegion = new TextureRegion
                    {
                        AtlasTextureHandle = fullThumbnailsAtlas.AtlasTextureHandle,
                        Width = (int)singleIconWidth,
                        Height = fullThumbnailsAtlas.Height,
                        U1 = u1,
                        V1 = fullThumbnailsAtlas.V1,
                        U2 = u2,
                        V2 = fullThumbnailsAtlas.V2
                    };
                    _levelThumbnails.Add(iconRegion);
                }
            }
            // Параметры построения сетки кнопок выбора уровней (4 колонки, 3 строки)
            float startX = 50f;
            float startY = 180f;
            float spacingX = 140f;
            float spacingY = 130f;
            for (int i = 0; i < _levelNames.Count; i++)
            {
                string currentLevel = _levelNames[i];
                int row = i / 10;
                int col = i % 10;
                Vector2 btnPos = new Vector2(startX + col * spacingX, startY + row * spacingY);
                var levelBtn = new UIButton()
                {
                    TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_CHALLENGE_WINDOW"),
                    TextureHover = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_CHALLENGE_WINDOW_HIGHLIGHT"),
                    Position = btnPos,
                    Size = new Vector2(118, 120),
                    OnClick = () =>
                    {
                        // Переходим на выбранный из меню уровень
                        _isLevelSelectVisible = false;
                        SceneManager.SwitchScene(new BoardScene(currentLevel));
                    }
                };
                _levelButtons.Add(levelBtn);
            }
        }
        private void OpenLevelSelectMenu()
        {
            _isLevelSelectVisible = true;
            UsersSettings.IsVisible = false;
            _isCreateUserWindowVisible = false;
        }
        private void OpenProfileDialog()
        {
            UsersSettings.IsVisible = true;
            _isCreateUserWindowVisible = false;
            _isLevelSelectVisible = false;
            RefreshProfileList();
        }
        private void OpenCreateUserModal()
        {
            _isCreateUserWindowVisible = true;
            _userNameInput.Text = "";
            _userNameInput.IsFocused = true;
        }
        private void RefreshCachedUsersList()
        {
            _cachedUsers = SaveSystem.LoadUsers();
        }
        private void RefreshProfileList()
        {
            RefreshCachedUsersList();
            _profileSelectButtons.Clear();

            if (_cachedUsers.Count >= 5)
            {
                NewUserButton.IsEnabled = false;
                NewUserButton.IsVisible = false;
            }
            else
            {
                NewUserButton.IsEnabled = true;
                NewUserButton.IsVisible = true;
            }

            float startY = UsersSettings.Position.Y + 120;
            float startX = UsersSettings.Position.X + 50;

            for (int i = 0; i < _cachedUsers.Count; i++)
            {
                var userMeta = _cachedUsers[i];
                Vector2 btnPos = new(startX, startY + (i * 65));

                var profileBtn = new UI3PartButton(standardButtonSkin, btnPos, 700)
                {
                    OnClick = () =>
                    {
                        // 1. ГЛУБОКАЯ ЗАГРУЗКА: Читаем чистый профиль нового игрока с диска (все PurchasedItems, слоты и ХП)
                        UserProfile? freshlyLoadedProfile = SaveSystem.LoadProfile(userMeta.UserId);

                        if (freshlyLoadedProfile == null)
                        {
                            Console.WriteLine($"[Profile Error] Не удалось загрузить файл user{userMeta.UserId}.dat");
                            return;
                        }

                        // 2. Назначаем его текущим активным игроком везде — и в сцене, и в глобальном фреймворке
                        this.CurrentUser = freshlyLoadedProfile;
                        LawnApp.CurrentUser = freshlyLoadedProfile; // <--- ТЕПЕРЬ СЮДА ПЕРЕДАЕТСЯ ВЕСЬ ПРОФИЛЬ, А НЕ КОРОТКАЯ МЕТА!

                        // 3. Ротируем список users.dat, чтобы этот игрок стал первым на запуск (индекс 0)
                        var allUsersList = SaveSystem.LoadUsers().ToList();
                        var selectedEntry = allUsersList.FirstOrDefault(u => u.UserId == userMeta.UserId);
                        if (selectedEntry != null)
                        {
                            allUsersList.Remove(selectedEntry);
                            allUsersList.Insert(0, selectedEntry);
                            SaveSystem.SaveUsers(allUsersList);
                        }


                        UsersSettings.IsVisible = false;
                        Console.WriteLine($"[Profile System] Успешно загружен профиль: {freshlyLoadedProfile.UserId} ({userMeta.Name}). Кошелек: {freshlyLoadedProfile.Coins}$. Слотов: {freshlyLoadedProfile.AdventureLevel}. Кэш памяти очищен.");
                    }
                };
                _profileSelectButtons.Add(profileBtn);
            }
        }


        public void Update(float dt)
        {
            if (_background == null) return;
            _background.Update(dt);
            float animProgress = _background._animTime;
            if (!_adventureButtonVisible && animProgress >= 0.3f) _adventureButtonVisible = true;
            if (!_usersButtonVisible && animProgress >= 0.95f)
            {
                _usersButtonVisible = true;
                _mainButtonsVisible = true;
            }
            // ПРИОР ИЕРАРХИЯ ОБНОВЛЕНИЯ 1: Окно выбора уровней блокирует всё под собой
            if (_isLevelSelectVisible)
            {
                _closeLevelMenuButton.Update(dt);
                foreach (var btn in _levelButtons)
                {
                    btn.Update(dt);
                }
                return;
            }
            // ПРИОР ИЕРАРХИЯ ОБНОВЛЕНИЯ 2: Модалка создания юзера
            if (_isCreateUserWindowVisible)
            {
                CreateUserWindow.Update(dt);

                // ДИНАМИЧЕСКИЙ СДВИГ: Привязываем элементы ввода к текущей позиции модалки в реальном времени
                _userNameInput.Position = CreateUserWindow.Position + new Vector2(50, 130);
                _confirmCreateButton.Position = CreateUserWindow.Position + new Vector2(40, 230);
                _cancelCreateButton.Position = CreateUserWindow.Position + new Vector2(280, 230);

                _userNameInput.Update(dt);
                _confirmCreateButton.Update(dt);
                _cancelCreateButton.Update(dt);
                return;
            }

            // ПРИОР ИЕРАРХИЯ ОБНОВЛЕНИЯ 3: Окно настроек списка профилей
            if (UsersSettings.IsVisible)
            {
                UsersSettings.Update(dt);

                // ДИНАМИЧЕСКИЙ СДВИГ: Привязываем нижние кнопки управления к позиции окна
                NewUserButton.Position = UsersSettings.Position + new Vector2(40, 520);
                DeleteUserButton.Position = UsersSettings.Position + new Vector2(280, 520);
                CloseUsersDialogButton.Position = UsersSettings.Position + new Vector2(540, 520);

                // Постоянно сдвигаем кнопки строк профилей, если окно движется
                float startY = UsersSettings.Position.Y + 120;
                float startX = UsersSettings.Position.X + 50;
                for (int i = 0; i < _profileSelectButtons.Count; i++)
                {
                    _profileSelectButtons[i].Position = new Vector2(startX, startY + (i * 65));
                }

                NewUserButton.Update(dt);
                DeleteUserButton.Update(dt);
                CloseUsersDialogButton.Update(dt);
                foreach (var btn in _profileSelectButtons) btn.Update(dt);
                return;
            }
            // Нижний стандартный слой
            if (_adventureButtonVisible) AdventureButton?.Update(dt);
            if (_usersButtonVisible) UsersButton?.Update(dt);
            if (_mainButtonsVisible)
            {
                MiniGamesButton.Update(dt);
                PuzzleButton.Update(dt);
                SurvivalButton.Update(dt);
                OptionsButton.Update(dt);
                HelpButton.Update(dt);
                QuitButton.Update(dt);
            }
        }
        public void Render(SpriteBatch spriteBatch)
        {
            // Рисуем базовое главное меню
            _background?.Render(spriteBatch);
            if (_adventureButtonVisible) AdventureButton.Render(spriteBatch);
            if (_usersButtonVisible)
            {
                UsersButton.Render(spriteBatch);
                string welcomeText = "Change user";
                if (CurrentUser != null)
                {
                    var meta = _cachedUsers.FirstOrDefault(u => u.UserId == CurrentUser.UserId);
                    welcomeText = meta != null ? meta.Name ?? $"Player {meta.UserId}" : $"Player {CurrentUser.UserId}";
                }
                Vector2 centerSign = UsersButton.Position + UsersButton.Size * 0.5f;
                _menuFont?.DrawText(spriteBatch, welcomeText, new Vector2(centerSign.X, centerSign.Y - 12f), Vector2.One, Color4.Gold, TextAlignment.Center);
            }
            if (_mainButtonsVisible)
            {
                MiniGamesButton.Render(spriteBatch);
                PuzzleButton.Render(spriteBatch);
                SurvivalButton.Render(spriteBatch);
                OptionsButton.Render(spriteBatch);
                HelpButton.Render(spriteBatch);
                QuitButton.Render(spriteBatch);
            }
            // Рисуем менеджер профилей
            if (UsersSettings.IsVisible)
            {
                UsersSettings.Render(spriteBatch);
                NewUserButton.Render(spriteBatch);
                DeleteUserButton.Render(spriteBatch);
                CloseUsersDialogButton.Render(spriteBatch);
                _menuFont?.DrawText(spriteBatch, "New", NewUserButton.Position + new Vector2(110, 8), Vector2.One, Color4.White, TextAlignment.Center);
                _menuFont?.DrawText(spriteBatch, "Delete", DeleteUserButton.Position + new Vector2(110, 8), Vector2.One, Color4.White, TextAlignment.Center);
                _menuFont?.DrawText(spriteBatch, "OK", CloseUsersDialogButton.Position + new Vector2(110, 8), Vector2.One, Color4.White, TextAlignment.Center);
                for (int i = 0; i < _profileSelectButtons.Count; i++)
                {
                    _profileSelectButtons[i].Render(spriteBatch);
                    var meta = _cachedUsers[i];
                    string userRowText = $"{meta.Name}";
                    string levelText = $"Adenture Level {meta.AdventureLevel}";
                    Vector2 rowPos = _profileSelectButtons[i].Position;
                    Color4 rowColor = (CurrentUser != null && meta.UserId == CurrentUser.UserId) ? Color4.Gold : Color4.White;
                    _menuFont?.DrawText(spriteBatch, userRowText, rowPos + new Vector2(30, 8), Vector2.One, rowColor, TextAlignment.Left);
                    _menuFont?.DrawText(spriteBatch, levelText, rowPos + new Vector2(670, 8), Vector2.One, Color4.LightGreen, TextAlignment.Right);
                }
            }
            // Рисуем окно создания профиля
            if (_isCreateUserWindowVisible)
            {
                CreateUserWindow.Render(spriteBatch);
                _userNameInput.Render(spriteBatch);
                _confirmCreateButton.Render(spriteBatch);
                _cancelCreateButton.Render(spriteBatch);
                _menuFont?.DrawText(spriteBatch, "Creating new profile", CreateUserWindow.Position + new Vector2(250, 45), Vector2.One, Color4.Gold, TextAlignment.Center);
                _menuFont?.DrawText(spriteBatch, "Please enter your name:", CreateUserWindow.Position + new Vector2(250, 95), Vector2.One, Color4.LightGray, TextAlignment.Center);
                _menuFont?.DrawText(spriteBatch, "OK", _confirmCreateButton.Position + new Vector2(90, 8), Vector2.One, Color4.White, TextAlignment.Center);
                _menuFont?.DrawText(spriteBatch, "Cancel", _cancelCreateButton.Position + new Vector2(90, 8), Vector2.One, Color4.White, TextAlignment.Center);
            }
            // НАЛОЖЕНИЕ ВЕРХНЕГО СЛОЯ: Экран выбора уровней (рисуется поверх всего остального)
            if (_isLevelSelectVisible)
            {
                if (_levelMenuBg.AtlasTextureHandle != 0)
                {
                    spriteBatch.Draw(_levelMenuBg, Vector2.Zero, new Vector2(1600f / _levelMenuBg.Width, 900f / _levelMenuBg.Height), 0f, Color4.White);
                }

                _menuFont?.DrawText(spriteBatch, "ADVENTURE", new Vector2(800, 70), new Vector2(2f, 2f), Color4.LightGray, TextAlignment.Center);

                // Выясняем максимальный уровень приключения текущего игрока
                int maxAllowedLevel = CurrentUser != null ? CurrentUser.AdventureLevel : 1;

                for (int i = 0; i < _levelButtons.Count; i++)
                {
                    var btn = _levelButtons[i];

                    // ПРОВЕРКА ДОСТУПНОСТИ УРОВНЯ:
                    // Если индекс кнопки (0..49) больше или равен maxAllowedLevel, уровень считается заблокированным
                    bool isLevelLocked = i >= maxAllowedLevel;

                    // Отключаем кликабельность кнопки, если уровень закрыт
                    btn.IsEnabled = !isLevelLocked;
                    btn.Render(spriteBatch);

                    var levelConfig = LevelDatabase.GetLevelConfig(_levelNames[i]);

                    int levelNumber = 1;
                    string[] parts = levelConfig.LevelName.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int parsedNumber))
                    {
                        levelNumber = parsedNumber;
                    }

                    int thumbIndex = levelConfig.BoardType switch
                    {
                        BoardType.Day => 0,
                        BoardType.Night => 1,
                        BoardType.Pool => 2,
                        BoardType.Fog => 3,
                        BoardType.Roof => 4,
                        _ => 0
                    };

                    if (levelNumber == 10)
                    {
                        thumbIndex += 5;
                    }

                    thumbIndex = Math.Clamp(thumbIndex, 0, _levelThumbnails.Count - 1);

                    if (_levelThumbnails.Count > 0 && _levelThumbnails[thumbIndex].AtlasTextureHandle != 0)
                    {
                        var thumb = _levelThumbnails[thumbIndex];
                        Vector2 thumbSize = new(84, 62);
                        Vector2 thumbPos = btn.Position + new Vector2((btn.Size.X - thumbSize.X) * 0.448f, 7f);
                        Vector2 thumbScale = new(thumbSize.X / thumb.Width, thumbSize.Y / thumb.Height);

                        // Если уровень закрыт, рисуем миниатюру локации притемненной наполовину
                        Color4 thumbColor = isLevelLocked ? new Color4(0.3f, 0.3f, 0.3f, 1f) : Color4.White;
                        spriteBatch.Draw(thumb, thumbPos, thumbScale, 0f, thumbColor);
                    }

                    // Текст номера уровня
                    Vector2 textPos = new Vector2(btn.Position.X + btn.Size.X * 0.5f, btn.Position.Y + btn.Size.Y - 32f);
                    Color4 textColor = isLevelLocked ? Color4.Gray : Color4.White;
                    _menuFont?.DrawText(spriteBatch, _levelNames[i], textPos, Vector2.One, textColor, TextAlignment.Center);

                    // Если уровень закрыт, можно нарисовать маленький замочек поверх плашки (необязательно, по желанию)
                    if (isLevelLocked)
                    {
                        _menuFont?.DrawText(spriteBatch, "LOCKED", btn.Position + new Vector2(btn.Size.X * 0.5f, 40f), new Vector2(0.8f, 0.8f), Color4.Red, TextAlignment.Center);
                    }
                }

                _closeLevelMenuButton.Render(spriteBatch);
                _menuFont?.DrawText(spriteBatch, "Back", _closeLevelMenuButton.Position + new Vector2(_closeLevelMenuButton.Size.X * 0.5f, 10f), Vector2.One, Color4.DeepSkyBlue, TextAlignment.Center);
            }
        }
        public void Destroy()
        {
            if (CurrentUser != null) SaveSystem.SaveProfile(CurrentUser);
        }
    }
}