using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Avalonia;
using engenious.Avalonia;
using engenious.Graphics;
using engenious.Helper;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using DrawElementsType = engenious.Graphics.DrawElementsType;

namespace engenious.ContentTool.Avalonia
{
    public class EffectModelViewerGame : AvaloniaGame, INotifyPropertyChanged
    {
        public event EventHandler UpdateBindings;
        public event EventHandler EffectLoaded;
        public Texture2D _texture;
        private SpriteBatch _batch;
        private Effect _effect;
        public Effect Effect        {
            get => _effect;
            private set
            {
                _effect = value;
                OnPropertyChanged();
            }
        }

        public Model Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        public Matrix World { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }

        public Texture2D NightSky { get; private set; }

        public Vector3 Dir => new Vector3(0, 1, 1).Normalized();
        public Matrix WorldViewProjection => World * View * Projection;

        private readonly bool _effectView;
        public float RotationX, RotationY, Scaling = 1;

        public EffectModelViewerGame(AvaloniaRenderingSurface control, bool effectView) : base(control)
        {
            _effectView = effectView;
            World = View = Projection = Matrix.Identity;
        }


        public override void LoadContent()
        {
            base.LoadContent();

            if (_effectView)
            {
                
                PrimitiveModels.CreateUVSphere(out var vertices, out var indices);

                var mesh = new MeshIndexed(
                    GraphicsDevice,
                    indices.Length / 3,
                    new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Length),
                    new IndexBuffer(GraphicsDevice, DrawElementsType.UnsignedInt, indices.Length)
                );
                mesh.VB.SetData(vertices);
                mesh.IB.SetData(indices);
                mesh.BoundingBox = new BoundingBox(-Vector3.One, Vector3.One);
                Model = new Model(GraphicsDevice, 1);
                Model.Meshes[0] = mesh;
            }
            else
            {
                Effect = new BasicEffect(GraphicsDevice);

                EffectLoaded?.Invoke(this, EventArgs.Empty);
            }

            _batch = new SpriteBatch(GraphicsDevice);

            var tmpTextPath = "/home/julian/Bilder/landmass.png";
            if (System.IO.File.Exists(tmpTextPath))
                _texture = Texture2D.FromFile(GraphicsDevice, tmpTextPath);
            tmpTextPath = "/home/julian/Bilder/water.png";

            if (System.IO.File.Exists(tmpTextPath))
                NightSky = Texture2D.FromFile(GraphicsDevice, tmpTextPath);

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
                    Model = Content.Load<Model>(assetPath, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private Action _lateInit;
        private Model _model;
        private int _frame;

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

        public int Frame
        {
            get => _frame;
            set
            {
                _frame = Math.Min(-1, value);

                if (value == -1)
                {
                    
                }
                else
                {
                }
                
                OnPropertyChanged();
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
            GL.Viewport(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            float minScreen = Math.Min(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            //Matrix.CreateRotationY((float) gameTime.TotalGameTime.TotalSeconds) * 
            World = Matrix.CreateRotationX(RotationX) * Matrix.CreateRotationY(RotationY) *
                Matrix.CreateScaling(new Vector3(Scaling*100)) * Matrix.CreateScaling(minScreen / maxD, minScreen / maxD, minScreen / maxD);
            View = Matrix.CreateLookAt(Vector3.Zero,-Vector3.UnitZ, Vector3.UnitY);
            Projection = Matrix.CreateOrthographicOffCenter(-GraphicsDevice.Viewport.Width/2, this.GraphicsDevice.Viewport.Width/2,
                this.GraphicsDevice.Viewport.Height/2,-GraphicsDevice.Viewport.Height/2 , 100000, -100000);

            UpdateBindings?.Invoke(this, EventArgs.Empty);



            if (Model.Animations.Count > 0)
            {
                int index = (int) ((gameTime.TotalGameTime.TotalSeconds / 10.0) % Model.Animations.Count);
                Model.CurrentAnimation = Model.Animations[index];
            }

            if (Effect is BasicEffect basic)
            {
                // Effect.Parameters["World"].SetValue(World);
                // Effect.Parameters["View"].SetValue(View);
                // Effect.Parameters["Proj"].SetValue(Projection);
                basic.TextureEnabled = true;
                Model.Transform = World;
                if (Model.CurrentAnimation != null)
                    Model.UpdateAnimation((float)gameTime.TotalGameTime.TotalSeconds);
                //Model.UpdateAnimation(null, Model.RootNode);
                Model.Draw(basic);
            }
            else
            {
                
                foreach (var p in Effect.CurrentTechnique.Passes)
                {
                    p.Apply();
               
                    Model.Draw();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}