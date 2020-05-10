using System;
using engenious.Graphics;
using engenious.WinForms;

namespace engenious.ContentTool.Viewer.Viewers
{
    internal class EffectModelViewerGame : WinFormsGame
    {
        public event EventHandler UpdateBindings;
        public event EventHandler EffectLoaded;
        public Texture2D _texture;
        private SpriteBatch _batch;
        public Effect Effect { get; private set; }
        public Model Model { get; set; }
            
        public Matrix World { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
            
        public Texture2D NightSky { get; private set; }

        public Vector3 Dir => new Vector3(0,1,1).Normalized();
        public Matrix WorldViewProjection => Projection * View * World;

        private readonly bool _effectView;
        public EffectModelViewerGame(GameControl control, bool effectView) : base(control)
        {
            _effectView = effectView;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (_effectView)
            {
                Model = new Model(GraphicsDevice);
                var mesh = new MeshIndexed(GraphicsDevice);
                PrimitiveModels.CreateUVSphere(out var vertices, out var indices);
                mesh.VB = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Length);
                mesh.VB.SetData(vertices);
                mesh.IB = new IndexBuffer(GraphicsDevice, DrawElementsType.UnsignedInt, indices.Length);
                mesh.IB.SetData(indices);
                mesh.PrimitiveCount = mesh.IB.IndexCount / 3;
                mesh.BoundingBox = new BoundingBox(-Vector3.One, Vector3.One);
                Model.Meshes = new IMesh[] {mesh};
            }
            else
            {
                Effect = new BasicEffect(GraphicsDevice);
                
                EffectLoaded?.Invoke(this, EventArgs.Empty);
            }
                            
            _batch = new SpriteBatch(GraphicsDevice);
            
            _texture = Texture2D.FromFile(GraphicsDevice, "/home/julian/Bilder/landmass.png");
            NightSky = Texture2D.FromFile(GraphicsDevice,
                "/home/julian/Projects/octoawesome/OctoAwesome/OctoAwesome.Client/Content/Textures/skymap.png");
                
            _lateInit?.Invoke();
        }
        public void SetModel(string outputDir, string assetPath)
        {
            if (Content == null)
            {
                _lateInit = () => SetModel(outputDir, assetPath);
            }
            else
            {
                try
                {
                    Content.RootDirectory = outputDir;
                    Model = Content.Load<Model>(assetPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private Action _lateInit;
        public void SetEffect(string outputDir, string assetPath)
        {
            if (Content == null)
            {
                _lateInit = () => SetEffect(outputDir, assetPath);
            }
            else
            {
                try
                {
                    Content.RootDirectory = outputDir;
                    Effect = Content.Load<Effect>(assetPath);
                        
                    EffectLoaded?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (Effect == null || Model == null)
            {
                return;
            }
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            BoundingBox box = default;
            foreach (var m in Model.Meshes)
                box = BoundingBox.CreateMerged(box, m.BoundingBox);
            var d = box.Max - box.Min;
            d = new Vector3(Math.Abs(d.X), Math.Abs(d.Y), Math.Abs(d.Z));
            var maxD = Math.Max(d.X, Math.Max(d.Y, d.Z)) * 2;
            float minScreen = Math.Min(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            World = Matrix.CreateScaling(minScreen / maxD, minScreen / maxD, minScreen / maxD) * Matrix.CreateRotationX(-MathF.PI / 4) * Matrix.CreateRotationZ((float)gameTime.TotalGameTime.TotalSeconds);
            Projection = Matrix.CreateOrthographic(this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height, 1000, -1000);
            View = Matrix.Identity;

            foreach (var p in Effect.CurrentTechnique.Passes)
            {
                p.Apply();
                    
                UpdateBindings?.Invoke(this, EventArgs.Empty);
                Model.Draw();
            }
        }
    }
}