using System;
using engenious.Avalonia;
using engenious.Graphics;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    public class SimpleGame : AvaloniaGame
    {
        public event Action<GameTime, SpriteBatch> Render;
        public event Action Init;
        private SpriteBatch _batch;
        public SimpleGame([NotNull] AvaloniaRenderingSurface control) : base(control)
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _batch = new SpriteBatch(GraphicsDevice);
            Init?.Invoke();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            Render?.Invoke(gameTime, _batch);
        }
    }
}