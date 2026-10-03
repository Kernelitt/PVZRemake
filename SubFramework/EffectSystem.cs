using KrutolFramework.Core;
using OpenTK.Mathematics;

namespace PVZRemake.SubFramework
{
    public class ParticleEffectManager
    {
        private readonly List<TodParticleSystem> _activeSystems = [];

        public void SpawnEffect(string sysName, Vector2 position)
        {
            try
            {
                // Вызываем ваш статический парсер из AssetGroup
                ParticleSystemDefinition def = AssetManager.GetParticle(sysName);

                if (def != null && AssetManager.Active != null)
                {
                    // Создаем живую систему частиц на основе вашего класса TodParticleSystem
                    
                    var liveSystem = new TodParticleSystem(def, AssetManager.Active)
                    {
                        Position = position,
                        Scale = 1.5f
                    };
                    
                    _activeSystems.Add(liveSystem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParticleEffectManager Error] Не удалось создать систему частиц '{sysName}': {ex.Message}");
            }
        }

        public void Update(float dt)
        {
            for (int i = _activeSystems.Count - 1; i >= 0; i--)
            {
                _activeSystems[i].Update(dt);

                // Проверяем ваш флаг IsDead (когда все эмиттеры догорели и частицы исчезли)
                if (_activeSystems[i].IsDead)
                {
                    _activeSystems.RemoveAt(i);
                }
            }
        }

        public void Render(SpriteBatch batch)
        {
            // Отрисовываем все живые вспышки частиц
            foreach (var system in _activeSystems)
            {
                system.Render(batch);
            }
        }

        public void Clear()
        {
            _activeSystems.Clear();
        }
    }
}
