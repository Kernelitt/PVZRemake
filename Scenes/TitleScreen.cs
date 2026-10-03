using KrutolFramework.Core;
using OpenTK.Mathematics;

namespace PVZRemake.Scenes
{
    internal class TitleScreen : IScene
    {
        private TextureRegion _backgroundTexture;
        private UIButton goToMenu;
        public void Initialize()
        {
            _backgroundTexture = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_TITLESCREEN");
            goToMenu = new UIButton()
            {
                Position = new Vector2(600,700),
                Size = new Vector2(321,53),
                TextureIdle = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_LOADBAR_DIRT"),
                OnClick = () => { SceneManager.SwitchScene(new SelectorScreen()); }
            };
        }
        public void Update(float dt)
        {
            goToMenu.Update(dt);
        }
        public void Render(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, Vector2.Zero, new Vector2(1.5f, 1.5f), 0f, Color4.White);
            goToMenu.Render(spriteBatch);
        }
        public void Destroy()
        {

        }
    }
}
