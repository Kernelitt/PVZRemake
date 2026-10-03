using KrutolFramework.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PVZRemake.Board;
using PVZRemake.Scenes;


namespace PVZRemake;

public class LawnApp(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : FrameworkGameWindow(gameWindowSettings, nativeWindowSettings)
{
    public AssetGroup mainLawnGroup;

    public SpriteBatch _spriteBatch;
    public static UserMetaEntry CurrentUsers { get; set; }
    public static UserProfile? CurrentUser { get; set; }
    public static LawnApp App { get; private set; }

    protected override void OnLoad()
    {
        App = this;

        base.OnLoad();

        GL.ClearColor(0f, 0f, 0f, 0.0f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var users = SaveSystem.LoadUsers();
        if (users.Count == 0) SaveSystem.SaveProfile( SaveSystem.CreateNewProfile("Player"));

        CurrentUsers = users[0];
        CurrentUser = SaveSystem.LoadProfile(users[0].UserId);
        _spriteBatch = new SpriteBatch();

        bool isRelease = true; // Выставьте в false, если хотите читать из обычных папок

        if (isRelease)
        {
            // Имя вашего файла-архива. Расширение может быть любым (.pkg, .zip, .dat)
            string archiveName = "main_content.pkg";

            // Задаем корень поиска внутри архива (если файлы лежат в подпапке)
            AssetManager.RootPath = "";

            // Включаем архивный движок
            AssetManager.InitializeZipArchive(archiveName);
        }
        else
        {
            // Старый режим чтения папок с диска
            AssetManager.RootPath = "";
        }

        mainLawnGroup = AssetManager.CreateGroup("LawnContext", atlasSize: 4096*4, layersPerPage: 4);
        AssetManager.Active = mainLawnGroup;

        Input.Initialize(this);

        mainLawnGroup.DiscoverAndLoadTextures("images");
        mainLawnGroup.DiscoverAndLoadTextures("particles");
        mainLawnGroup.DiscoverAndLoadTextures("reanim");
        mainLawnGroup.DiscoverAndLoadAnimations("animations");    // Все анимации (REANIM_*) + авторегистрация в БД
        mainLawnGroup.DiscoverAndLoadParticles("particles");

        mainLawnGroup.LoadFont("BRIANNE_TOD","fonts/BrianneTod.ttf",18);
        mainLawnGroup.LoadFont("Arial","Arial",14);
        mainLawnGroup.LoadFont("Arial","Arial",48);

        SceneManager.SwitchScene(new TitleScreen());
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        Input.Update();

        float dt = (float)args.Time;

        Profiler.BeginSample("Update");
        SceneManager.Update(dt);
        Profiler.EndSample("Update");

        Input.ClearFrameTextInput();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 1600, 900, 0, -1.0f, 1.0f);

        _spriteBatch.Begin(projection);
        Profiler.BeginSample("Draw");
        SceneManager.Render(_spriteBatch);
        Profiler.EndSample("Draw");

        
        _spriteBatch.End();

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
    }

    protected override void OnUnload()
    {
        _spriteBatch.Dispose();
        mainLawnGroup.Dispose();
        base.OnUnload();
    }
}
