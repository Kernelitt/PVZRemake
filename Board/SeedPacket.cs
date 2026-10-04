using KrutolFramework.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PVZRemake.Board
{
    public class SeedCard : UIComponent
    {
        public PlantType PlantType { get; private set; }
        public int SunCost { get; private set; }

        public float CooldownTime { get; private set; }
        public float CooldownTimer { get; private set; } = 0f;
        public bool IsUpgrade { get; private set; } = false;
        public bool IsReady => CooldownTimer <= 0f;
        private TextureRegion _texEmptyPacket;
        private TextureRegion _texGlow;
        private Reanimation _iconReanim;
        public static bool DisableCooldownsCheat { get; set; } = false;
        public Action<SeedCard> OnSelected { get; set; }

        public SeedCard(PlantType type, Vector2 pos)
        {
            PlantType = type;
            Position = pos;
            Size = new Vector2(75f, 115f);

            // ПОЛНАЯ АВТОНАСТРОЙКА ИЗ ДЕФИНИЦИИ
            var def = PlantDatabase.Get(type);
            SunCost = def.SunCost;
            CooldownTime = def.RefreshTime;
            IsUpgrade = def.IsUpgrade;
            if (!IsUpgrade)
            {
                _texEmptyPacket = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDPACKET");
            }
            else
            {
                _texEmptyPacket = (TextureRegion)AssetManager.GetTexture("IMAGE_REANIM_SEEDPACKETUPGRADE");
            }    
            _texGlow = (TextureRegion)AssetManager.GetTexture("IMAGE_SEEDPACKETGLOW");

            InitializeIconReanim();
        }

        private void InitializeIconReanim()
        {
            var plantDef = PlantDatabase.Get(PlantType);
            string animKey = $"REANIM_{plantDef.ReanimKey.ToUpper()}";

            ReanimDefinition def = AssetManager.GetAnimation(animKey);
            _iconReanim = ReanimDatabase.CreateRuntimeAnimation(plantDef.ReanimKey.ToUpper());

            if (_iconReanim != null)
            {
                _iconReanim.LoopType = ReanimLoopType.PlayOnceAndHold;
                string targetMarker = "anim_idle";
                if (_iconReanim.HasMarker("anim_full_idle")) targetMarker = "anim_full_idle";
                else if (!_iconReanim.HasMarker("anim_idle")) targetMarker = "";

                if (!string.IsNullOrEmpty(targetMarker)) _iconReanim.SetFrameBounds(targetMarker);
                else _iconReanim.SetFrameBounds(0, 0);

                if (plantDef.Type == PlantType.PotatoMine) _iconReanim.SetFrameBounds("anim_armed");

                _iconReanim._animTime = 0.0f;
                _iconReanim.Update(0f);

                // Применяем позицию и масштаб
                ApplyMetadataTransform();
            }
        }

        private void ApplyMetadataTransform()
        {
            if (_iconReanim == null) return;

            var plantDef = PlantDatabase.Get(PlantType);
            ReanimMetadata? meta = ReanimDatabase.GetMetadata(plantDef.ReanimKey);

            float scaleOverride = meta?.UIScaleOverride ?? 0.75f;
            _iconReanim.Scale = new Vector2(scaleOverride, scaleOverride);

            float localCenterX = Size.X * 0.1f;
            float localBottomY = Size.Y * 0.2f;

            Vector2 basePosition = Position + new Vector2(localCenterX, localBottomY);

            if (meta != null)
            {
                basePosition += meta.UIBoxOffset;
            }

            _iconReanim.Position = basePosition;
        }

        public override void Update(float deltaTime)
        {
            if (!IsVisible) return;

            if (DisableCooldownsCheat) CooldownTimer = 0f;

            if (CooldownTimer > 0f)
            {
                CooldownTimer -= deltaTime;
                if (CooldownTimer < 0f) CooldownTimer = 0f;
            }
            if (_iconReanim != null)
            {
                ApplyMetadataTransform();
            }

            if (IsEnabled && IsReady && IsMouseOver())
            {
                if (Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    OnSelected?.Invoke(this);
                }
            }
        }


        public void ResetCooldown()
        {
            CooldownTimer = CooldownTime;
        }

        public override void Render(SpriteBatch batch)
        {
            if (!IsVisible || _texEmptyPacket.AtlasTextureHandle == 0) return;

            Vector2 scale = new(Size.X / _texEmptyPacket.Width, Size.Y / _texEmptyPacket.Height);
            Color4 cardColor = Color4.White;
            if (!IsEnabled || CooldownTimer > 0f)
            {
                cardColor = new Color4(0.4f, 0.4f, 0.4f, 1.0f);
            }

            batch.Draw(_texEmptyPacket, Position, scale, 0f, cardColor);
            if (_iconReanim != null)
            {
                _iconReanim.ColorOverride = cardColor;
                _iconReanim.Render(batch);
            }
            var font = AssetManager.GetFont("BRIANNE_TOD", 18);
            font?.DrawText(batch, SunCost.ToString(), Position + new Vector2(45f, 90f), Vector2.One, Color4.Black, TextAlignment.Right);

            if (CooldownTimer > 0f)
            {
                float progress = CooldownTimer / CooldownTime;
                var pixel = AssetManager.GetTexture("IMAGE_WHITEPIXEL");
                if (pixel != null)
                {
                    Vector2 coolSize = new(Size.X, Size.Y * progress);
                    batch.Draw((TextureRegion)pixel, Position, coolSize, 0f, new Color4(0f, 0f, 0f, 0.4f));
                }
            }

            if (IsReady && IsEnabled && IsMouseOver() && _texGlow.AtlasTextureHandle != 0)
            {
                Vector2 glowScale = new(Size.X / _texGlow.Width, Size.Y / _texGlow.Height);
                batch.Draw(_texGlow, Position, glowScale, 0f, new Color4(1f, 1f, 1f, 0.4f));
            }
        }
    }
}
