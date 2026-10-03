using KrutolFramework.Core;
using OpenTK.Mathematics;

namespace PVZRemake.Board
{

    public enum BoardType
    {
        Day,        
        Night,      
        Pool,       
        Fog,        
        Roof        
    }
    public enum GameLevelState
    {
        Dialogue,            
        PanToZombies,        
        ChoosingSeeds,       
        PanToGameplay,       
        Countdown,           
        ActiveGameplay,      
        LevelWon,
        LevelLost
    }
    public enum LaneType
    {
        Ground,
        Normal,
        Pool,
        Roof
    }
    public enum PlantType
    {
        Peashooter, Sunflower, CherryBomb, WallNut, PotatoMine, SnowPea, Chomper, Repeater,
        PuffShroom, SunShroom, FumeShroom, GraveBuster, HypnoShroom, ScaredyShroom, IceShroom, DoomShroom,
        LilyPad, Squash, Threepeater, TangleKelp, Jalapeno, Spikeweed, Torchwood, TallNut,
        SeaShroom, Plantern, Cactus, Blover, SplitPea, Starfruit, PumpkinShell, MagnetShroom,
        CabbagePult, FlowerPot, KernelPult, InstantCoffee, Garlic, Umbrella, Marigold, MelonPult,
        GatlingPea, TwinSunflower, GloomShroom, Cattail, WinterMelon, GoldMagnet, SpikeRock, CobCannon,
        Imitater, ExplodeONut, GiantWallNut, Sprout, LeftPeater
    }
    public enum PlantState
    {
        Idle,    
        Action,  
        Spawning,
        Ready,   
        Dying    
    }
    public enum ZombieType
    {
        Normal,
        Conehead,
        Buckethead,
        ScreenDoor
    }

    public enum ZombieState
    {
        Idle,
        Walking,
        Eating, 
        Dying,  
        Dead    
    }

    public interface IRenderableEntity
    {
        int Row { get; }
        float X { get; }
        void Render(SpriteBatch batch, Vector2? cameraOffset);
    }

    public struct BoardLayout
    {
        public int Rows;
        public float StartX;
        public float StartY;
        public float CellWidth;
        public float CellHeight;
        public LaneType[] LaneTypes;
    }

    /// <summary>
    /// Легковесный результат клика по сетке (без хранения игрового состояния)
    /// </summary>
    public struct GridCoords(int row, int col, Vector2 topLeft, Vector2 center)
    {
        public int Row = row;
        public int Col = col;
        public Vector2 CellTopLeft = topLeft;
        public Vector2 CellCenter = center;
    }

    public class ReanimMetadata(string id, string filePath)
    {
        public string Id { get; set; } = id.ToUpper();
        public string FilePath { get; set; } = filePath;
        public float DefaultFPS { get; set; } = 12f; // Дефолтная скорость, если нужно переопределить XML

        // НЕОБЯЗАТЕЛЬНЫЕ ПАРАМЕТРЫ (для геймплея и UI карт)
        public int? SeedPacketFrame { get; set; }    // Фиксированный кадр для иконки пакета семян (null, если не требуется)
        public Vector2i? IdleFrames { get; set; }    // Диапазон кадров покачивания (X - старт, Y - конец)

        // Локальные оффсеты для калибровки отрисовки внутри UI пакета семян
        public Vector2 UIBoxOffset { get; set; } = Vector2.Zero;
        public float UIScaleOverride { get; set; } = 0.75f;
    }

    public class ZombieDef(ZombieType type, int points, int startingWave, int weight, string name)
    {
        public ZombieType Type { get; set; } = type;
        public int Points { get; set; } = points;
        public int StartingWave { get; set; } = startingWave;
        public int Weight { get; set; } = weight;
        public string InternalName { get; set; } = name;
    }

    public enum PlantSubClass
    {
        Normal,
        Shooter
    }

    public class PlantDefinition
    {
        public PlantType Type { get; set; }
        public string ReanimKey { get; set; }
        public int SunCost { get; set; }
        public float RefreshTime { get; set; }
        public int MaxHealth { get; set; }
        public PlantSubClass SubClass { get; set; }
        public float LaunchRate { get; set; }
        public bool IsUpgrade { get; set; } = false;
        public bool IsNocturnal { get; set; } = false;
    }

    public static class PlantDatabase
    {
        private static readonly Dictionary<PlantType, PlantDefinition> _defs = [];

        static PlantDatabase()
        {
            // Дневные растения (1-8)
            Add(new() { Type = PlantType.Peashooter, ReanimKey = "PEASHOOTER", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.Sunflower, ReanimKey = "SUNFLOWER", SunCost = 50, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 20.0f });
            Add(new() { Type = PlantType.CherryBomb, ReanimKey = "CHERRYBOMB", SunCost = 150, RefreshTime = 35.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.WallNut, ReanimKey = "WALLNUT", SunCost = 50, RefreshTime = 30.0f, MaxHealth = 4000, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.PotatoMine, ReanimKey = "POTATOMINE", SunCost = 25, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.SnowPea, ReanimKey = "SNOWPEA", SunCost = 175, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.Chomper, ReanimKey = "CHOMPER", SunCost = 150, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Repeater, ReanimKey = "REPEATER", SunCost = 200, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });

            // Ночные грибы (9-16)
            Add(new() { Type = PlantType.PuffShroom, ReanimKey = "PUFFSHROOM", SunCost = 0, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsNocturnal = true });
            Add(new() { Type = PlantType.SunShroom, ReanimKey = "SUNSHROOM", SunCost = 25, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 20.0f, IsNocturnal = true });
            Add(new() { Type = PlantType.FumeShroom, ReanimKey = "FUMESHROOM", SunCost = 75, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsNocturnal = true });
            Add(new() { Type = PlantType.GraveBuster, ReanimKey = "GRAVEBUSTER", SunCost = 75, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.HypnoShroom, ReanimKey = "HYPNOSHROOM", SunCost = 75, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsNocturnal = true });
            Add(new() { Type = PlantType.ScaredyShroom, ReanimKey = "SCAREDYSHROOM", SunCost = 25, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsNocturnal = true });
            Add(new() { Type = PlantType.IceShroom, ReanimKey = "ICESHROOM", SunCost = 75, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsNocturnal = true });
            Add(new() { Type = PlantType.DoomShroom, ReanimKey = "DOOMSHROOM", SunCost = 125, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsNocturnal = true });

            // Бассейн (17-24)
            Add(new() { Type = PlantType.LilyPad, ReanimKey = "LILYPAD", SunCost = 25, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Squash, ReanimKey = "SQUASH", SunCost = 50, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Threepeater, ReanimKey = "THREEPEATER", SunCost = 325, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.TangleKelp, ReanimKey = "TANGLEKELP", SunCost = 25, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Jalapeno, ReanimKey = "JALAPENO", SunCost = 125, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Spikeweed, ReanimKey = "CALTROP", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Torchwood, ReanimKey = "TORCHWOOD", SunCost = 175, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.TallNut, ReanimKey = "TALLNUT", SunCost = 125, RefreshTime = 30.0f, MaxHealth = 8000, SubClass = PlantSubClass.Normal, LaunchRate = 0f });

            // Туман (25-32)
            Add(new() { Type = PlantType.SeaShroom, ReanimKey = "SEASHROOM", SunCost = 0, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsNocturnal = true });
            Add(new() { Type = PlantType.Plantern, ReanimKey = "PLANTERN", SunCost = 25, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 25.0f });
            Add(new() { Type = PlantType.Cactus, ReanimKey = "CACTUS", SunCost = 125, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.Blover, ReanimKey = "BLOVER", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.SplitPea, ReanimKey = "SPLITPEA", SunCost = 125, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.Starfruit, ReanimKey = "STARFRUIT", SunCost = 125, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });
            Add(new() { Type = PlantType.PumpkinShell, ReanimKey = "PUMPKIN", SunCost = 125, RefreshTime = 30.0f, MaxHealth = 4000, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.MagnetShroom, ReanimKey = "MAGNETSHROOM", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsNocturnal = true });

            // Крыша (33-40)
            Add(new() { Type = PlantType.CabbagePult, ReanimKey = "CABBAGEPULT", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 3.0f });
            Add(new() { Type = PlantType.FlowerPot, ReanimKey = "POT", SunCost = 25, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.KernelPult, ReanimKey = "CORNPULT", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 3.0f });
            Add(new() { Type = PlantType.InstantCoffee, ReanimKey = "COFFEEBEAN", SunCost = 75, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Garlic, ReanimKey = "GARLIC", SunCost = 50, RefreshTime = 7.5f, MaxHealth = 400, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Umbrella, ReanimKey = "UMBRELLALEAF", SunCost = 100, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Marigold, ReanimKey = "MARIGOLD", SunCost = 50, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 25.0f });
            Add(new() { Type = PlantType.MelonPult, ReanimKey = "MELONPULT", SunCost = 300, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 3.0f });

            // Улучшения (41-48)
            Add(new() { Type = PlantType.GatlingPea, ReanimKey = "GATLINGPEA", SunCost = 250, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsUpgrade = true });
            Add(new() { Type = PlantType.TwinSunflower, ReanimKey = "TWINSUNFLOWER", SunCost = 150, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 25.0f, IsUpgrade = true });
            Add(new() { Type = PlantType.GloomShroom, ReanimKey = "GLOOMSHROOM", SunCost = 150, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 2.0f, IsUpgrade = true });
            Add(new() { Type = PlantType.Cattail, ReanimKey = "CATTAIL", SunCost = 225, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f, IsUpgrade = true });
            Add(new() { Type = PlantType.WinterMelon, ReanimKey = "WINTERMELON", SunCost = 200, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 3.0f, IsUpgrade = true });
            Add(new() { Type = PlantType.GoldMagnet, ReanimKey = "GOLDMAGNET", SunCost = 50, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsUpgrade = true });
            Add(new() { Type = PlantType.SpikeRock, ReanimKey = "SPIKEROCK", SunCost = 125, RefreshTime = 50.0f, MaxHealth = 450, SubClass = PlantSubClass.Normal, LaunchRate = 0f, IsUpgrade = true });
            Add(new() { Type = PlantType.CobCannon, ReanimKey = "COBCANNON", SunCost = 500, RefreshTime = 50.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 6.0f, IsUpgrade = true });
            // Спец-сущности и мини-игры (49-53)
            Add(new() { Type = PlantType.Imitater, ReanimKey = "IMITATER", SunCost = 0, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.ExplodeONut, ReanimKey = "WALLNUT", SunCost = 0, RefreshTime = 30.0f, MaxHealth = 4000, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.GiantWallNut, ReanimKey = "WALLNUT", SunCost = 0, RefreshTime = 30.0f, MaxHealth = 4000, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.Sprout, ReanimKey = "ZENGARDENSPROUT", SunCost = 0, RefreshTime = 30.0f, MaxHealth = 300, SubClass = PlantSubClass.Normal, LaunchRate = 0f });
            Add(new() { Type = PlantType.LeftPeater, ReanimKey = "REPEATER", SunCost = 200, RefreshTime = 7.5f, MaxHealth = 300, SubClass = PlantSubClass.Shooter, LaunchRate = 1.5f });

            
        }

        private static void Add(PlantDefinition def) => _defs[def.Type] = def;
        public static PlantDefinition Get(PlantType type) => _defs[type];
    }


    public static class ZombieDatabase
    {
        private static readonly Dictionary<ZombieType, ZombieDef> _defs = [];

        static ZombieDatabase()
        {
            // Переносим точные оригинальные параметры из структуры PopCap
            Add(new ZombieDef(ZombieType.Normal, 1, 1, 4000, "ZOMBIE"));
            Add(new ZombieDef(ZombieType.Conehead, 2, 1, 4000, "CONEHEAD_ZOMBIE")); // В вашей таблице это ZOMBIE_TRAFFIC_CONE, StartingWave = 1
            Add(new ZombieDef(ZombieType.Buckethead, 4, 1, 3000, "BUCKETHEAD_ZOMBIE")); // ZOMBIE_PAIL, StartingWave = 1
            Add(new ZombieDef(ZombieType.ScreenDoor, 4, 5, 3500, "SCREEN_DOOR_ZOMBIE")); // ZOMBIE_DOOR, StartingWave = 5

            // Заглушка для флагового зомби (он идет вне бюджета, жестко по триггеру)
            Add(new ZombieDef(ZombieType.Normal, 1, 1, 0, "FLAG_ZOMBIE"));
        }

        private static void Add(ZombieDef def) => _defs[def.Type] = def;

        public static ZombieDef? Get(ZombieType type)
        {
            if (_defs.TryGetValue(type, out var def)) return def;
            return null;
        }

        public static IEnumerable<ZombieDef> GetAll() => _defs.Values;
    }

    public static class ReanimDatabase
    {
        private static readonly Dictionary<string, ReanimMetadata> _registry = new(StringComparer.OrdinalIgnoreCase);

        // Статический конструктор или метод инициализации базы данных
        static ReanimDatabase()
        {
            // ПОДПИСКА НА СОБЫТИЕ ФРЕЙМВОРКА:
            // Как только фреймворк найдет и распарсит .reanim файл, игровой движок сразу запишет его себе в БД
            AssetGroup.OnAnimationDiscovered += HandleAnimationDiscovered;
        }

        private static void HandleAnimationDiscovered(string assetId, string relativeFilePath)
        {
            string cleanId = assetId.Replace("REANIM_", "");

            ReanimMetadata meta = new(cleanId, relativeFilePath);
            Register(meta);

            Console.WriteLine($"[ReanimDatabase] Динамически синхронизировано из framework: {cleanId}");
        }

        public static void Register(ReanimMetadata metadata)
        {
            _registry[metadata.Id] = metadata;
        }

        public static ReanimMetadata? GetMetadata(string id)
        {
            id = id.ToUpper();

            // Если метаданных ещё нет в словаре, создаём их лету!
            if (!_registry.TryGetValue(id, out var meta))
            {
                // Формируем дефолтный путь к файлу анимации, если его никто не регистрировал
                string defaultPath = $"animations\\{id.ToLower()}.reanim";
                meta = new ReanimMetadata(id, defaultPath);

                // === ЛОКАЛЬНАЯ КАЛИБРОВКА РАСТЕНИЙ ===
                switch (id)
                {
                    case "PEASHOOTER":
                        meta.UIBoxOffset = new Vector2(-10f, -20f);
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "SUNFLOWER":
                        meta.UIBoxOffset = new Vector2(-5f, -20f); 
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "WALLNUT":
                        meta.UIBoxOffset = new Vector2(-5f, -10f); 
                        meta.UIScaleOverride = 0.85f;              
                        break;
                    case "POTATOMINE":
                        meta.UIBoxOffset = new Vector2(5f, 5f); 
                        meta.UIScaleOverride = 0.65f;              
                        break;
                    case "SNOWPEA":
                        meta.UIBoxOffset = new Vector2(-8f, -20f); 
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "CHOMPER":
                        meta.UIBoxOffset = new Vector2(2, -3f);          
                        break;
                    case "REPEATER":
                        meta.UIBoxOffset = new Vector2(-8f, -20f); 
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "PUFFSHROOM":
                        meta.UIBoxOffset = new Vector2(-10f, -10f);
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "SUNSHROOM":
                        meta.UIBoxOffset = new Vector2(-10f, -10f);
                        meta.UIScaleOverride = 0.95f;              
                        break;
                    case "FUMESHROOM":
                        meta.UIBoxOffset = new Vector2(-3f, -5f);
                        break;
                    case "GRAVEBUSTER":
                        meta.UIBoxOffset = new Vector2(5f, 5f);
                        meta.UIScaleOverride = 0.65f;              
                        break;
                    case "SCAREDYSHROOM":
                        meta.UIBoxOffset = new Vector2(0f, -5f);
                        meta.UIScaleOverride = 0.85f;              
                        break;
                    case "TANGLEKELP":
                        meta.UIBoxOffset = new Vector2(2f, 5f);  
                        meta.UIScaleOverride = 0.7f;
                        break;
                    case "TALLNUT":
                        meta.UIBoxOffset = new Vector2(7f, 13f);
                        meta.UIScaleOverride = 0.57f;
                        break;
                    case "PLANTERN":
                        meta.UIBoxOffset = new Vector2(0f, -5f);  
                        break;
                    case "CACTUS":
                        meta.UIBoxOffset = new Vector2(5f, 0f);  
                        break;
                    case "BLOVER":
                        meta.UIBoxOffset = new Vector2(3f, 5f);
                        meta.UIScaleOverride = 0.7f;
                        break;
                    case "SPLITPEA":
                        meta.UIBoxOffset = new Vector2(8f, 0f);
                        break;
                    case "PUMPKIN":
                        meta.UIBoxOffset = new Vector2(5f, 5f);
                        meta.UIScaleOverride = 0.6f;
                        break;
                    case "CABBAGEPULT":
                        meta.UIBoxOffset = new Vector2(14f, 5f);
                        meta.UIScaleOverride = 0.6f;
                        break;
                    case "CORNPULT":
                        meta.UIBoxOffset = new Vector2(13f, 5f);
                        meta.UIScaleOverride = 0.6f;
                        break;
                    case "MELONPULT":
                        meta.UIBoxOffset = new Vector2(18f, 8f);
                        meta.UIScaleOverride = 0.6f;
                        break;
                    case "WINTERMELON":
                        meta.UIBoxOffset = new Vector2(18f, 8f);
                        meta.UIScaleOverride = 0.6f;
                        break;
                    case "COBCANNON":
                        meta.UIBoxOffset = new Vector2(0f, 15f);
                        meta.UIScaleOverride = 0.37f;
                        break;
                }


                _registry[id] = meta;
            }

            return meta;
        }

        public static Reanimation? CreateRuntimeAnimation(string id)
        {
            id = id.ToUpper();
            ReanimMetadata? meta = GetMetadata(id);

            if (AssetManager.Active == null)
                throw new InvalidOperationException("[ReanimDatabase] Нет активной AssetGroup.");

            string assetKey = id.StartsWith("REANIM_") ? id : $"REANIM_{id}";

            ReanimDefinition def;
            try
            {
                def = AssetManager.Active.GetAnimation(assetKey);
            }
            catch (KeyNotFoundException)
            {
                if (meta != null)
                {
                    string fullPath = AssetManager.GetFullPath(meta.FilePath);
                    // Если файл скомпилирован, подменяем расширение
                    if (!File.Exists(fullPath) && File.Exists(fullPath + ".compiled"))
                    {
                        fullPath += ".compiled";
                    }
                    def = AssetManager.Active.LoadAnimation(assetKey, fullPath);
                }
                else
                {
                    Console.WriteLine($"[ReanimDatabase] Критическая ошибка: Файл анимации {assetKey} не найден.");
                    return null;
                }
            }

            return new Reanimation(def, AssetManager.Active);
        }
    }
}

