#region File Description
//-----------------------------------------------------------------------------
// GeneratedGeometry.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using LTreesLibrary;
using LTreesLibrary.Trees;
using LTreesLibrary.Trees.Wind;
#endregion

namespace GeneratedGeometry
{
    #region Tree
    public struct Tree
    {
        public SimpleTree simpleTree;
        public Matrix transformationMatrix;

        public Tree(SimpleTree simpleTree, Matrix transformationMatrix)
        {
            this.simpleTree = simpleTree;
            this.transformationMatrix = transformationMatrix;
        }
    }
    #endregion

    /// <summary>
    /// Sample showing how to use geometry that is programatically
    /// generated as part of the content pipeline build process.
    /// </summary>
    public class GeneratedGeometryGame : Microsoft.Xna.Framework.Game
    {
        #region Fields

        const float terrainScale = 3;
        const float terrainBumpiness = 64;
        //const float texCoordScale = 0.1f;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Model terrain;
        private Texture2D heightmap;
        private float[,] heightValues;
        private float terrainWidth, terrainHeight;
        private Sky sky;
        private Texture2D sunTexture;
        private Vector3 directionToSun;
        private Vector3 sunPosition2D;
        const int sunSize = 256;

        private Vector3 cameraPosition = Vector3.Zero;
        private Vector3 cameraFront = Vector3.Forward;
        private Matrix projection;

        private Effect lightScatterPostProcess;
        private RenderTarget2D sceneRenderTarget, lightScatterRenderTarget;

        private WindStrengthSin wind;
        private TreeWindAnimator windAnimator;
        private TreeProfile treeProfile;
        private LinkedList<Tree> trees;

        private int backbufferWidth, backbufferHeight;


        #endregion

        #region Initialization

        public GeneratedGeometryGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferMultiSampling = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.SynchronizeWithVerticalRetrace = false;
            this.IsFixedTimeStep = false;
            this.IsMouseVisible = false;
            base.Initialize();
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Calculate the projection matrix.
            Viewport viewport = GraphicsDevice.Viewport;

            float aspectRatio = (float)viewport.Width / (float)viewport.Height;

            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    aspectRatio,
                                                                    1, 10000);

            terrain = Content.Load<Model>("terrain");
            BasicDirectionalLight terrainDirectionalLight = null;
            directionToSun = Vector3.Normalize(new Vector3(2f, 0.1f, -0.7f));

            foreach (ModelMesh mesh in terrain.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Projection = projection;

                    effect.PreferPerPixelLighting = true;

                    // Set the specular lighting to match the sky color.
                    effect.SpecularColor = new Vector3(0f);

                    // Set the fog to match the distant mountains
                    // that are drawn into the sky texture.
                    effect.FogEnabled = false;
                    effect.FogColor = new Vector3(0.15f);
                    effect.FogStart = 100;
                    effect.FogEnd = 320;

                    effect.EnableDefaultLighting();

                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;
                    effect.DirectionalLight0.Direction = -directionToSun;
                    terrainDirectionalLight = effect.DirectionalLight0;
                }
            }

            heightmap = Content.Load<Texture2D>("Textures/terrain");
            CalculateHeightValues();

            sky = Content.Load<Sky>("skybox");
            sunTexture = Content.Load<Texture2D>("Textures/Sun");

            backbufferWidth = graphics.PreferredBackBufferWidth;
            backbufferHeight = graphics.PreferredBackBufferHeight;

            lightScatterPostProcess = Content.Load<Effect>("Effects/LightScatterPostProcess");

            //Setup post-process parameters
            lightScatterPostProcess.Parameters["Density"].SetValue(0.85f);
            lightScatterPostProcess.Parameters["Weight"].SetValue(1f / 120f * 2);
            lightScatterPostProcess.Parameters["Decay"].SetValue(0.99f);
            lightScatterPostProcess.Parameters["Exposure"].SetValue(0.5f);

            //Trees
            Random r = new Random(Environment.TickCount);
            treeProfile = Content.Load<TreeProfile>("Trees/Willow");

            wind = new WindStrengthSin(0.1f);
            windAnimator = new TreeWindAnimator(wind);

            trees = new LinkedList<Tree>();

            for (int i = 0; i < 300; i++)
            {
                SimpleTree simpleTree = treeProfile.GenerateSimpleTree();
                simpleTree.LeafEffect.CurrentTechnique = simpleTree.LeafEffect.Techniques["SetNoRenderStates"];
     
                simpleTree.TrunkEffect.Parameters["DirLight0DiffuseColor"].SetValue(new Vector4(terrainDirectionalLight.DiffuseColor, 1f));
                simpleTree.TrunkEffect.Parameters["DirLight0Direction"].SetValue(terrainDirectionalLight.Direction);
                simpleTree.TrunkEffect.Parameters["DirLight1Enabled"].SetValue(false);

                simpleTree.LeafEffect.Parameters["DirLight0DiffuseColor"].SetValue(new Vector4(terrainDirectionalLight.DiffuseColor, 1f));
                simpleTree.LeafEffect.Parameters["DirLight0Direction"].SetValue(terrainDirectionalLight.Direction);
                simpleTree.LeafEffect.Parameters["DirLight1Enabled"].SetValue(false);

                int x = r.Next((int)(terrainWidth / terrainScale));
                int z = r.Next((int)(terrainHeight / terrainScale));
                float y = heightValues[x, z];

                x *= (int)terrainScale;
                z *= (int)terrainScale;

                Vector3 position = new Vector3(x, y, z) - new Vector3(terrainWidth * 0.5f, 0f, terrainHeight * 0.5f);
                Matrix scale = Matrix.CreateScale(Vector3.Lerp(new Vector3(0.005f), new Vector3(0.015f), (float)r.NextDouble()));

                trees.AddLast(new Tree(simpleTree, scale * Matrix.CreateTranslation(position)));
            }

            //Render targets
            sceneRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth, backbufferHeight,
                1, SurfaceFormat.Color, GraphicsDevice.PresentationParameters.MultiSampleType,
                GraphicsDevice.PresentationParameters.MultiSampleQuality);
            lightScatterRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth, backbufferHeight,
                1, SurfaceFormat.Color, GraphicsDevice.PresentationParameters.MultiSampleType,
                GraphicsDevice.PresentationParameters.MultiSampleQuality);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            if (sceneRenderTarget != null)
            {
                sceneRenderTarget.Dispose();
                sceneRenderTarget = null;
            }
            if (lightScatterRenderTarget != null)
            {
                lightScatterRenderTarget.Dispose();
                lightScatterRenderTarget = null;
            }
        }
        #endregion

        #region Calculate Height Values
        private void CalculateHeightValues()
        {
            terrainWidth = terrainScale * heightmap.Width;
            terrainHeight = terrainScale * heightmap.Height;

            heightValues = new float[heightmap.Width, heightmap.Height];
            Color[] colorData = new Color[heightmap.Width * heightmap.Height];
            heightmap.GetData<Color>(colorData);

            // Create the terrain vertices.
            for (int y = 0; y < heightmap.Height; y++)
            {
                for (int x = 0; x < heightmap.Width; x++)
                {
                    heightValues[x, y] = ((float)(colorData[x + y * heightmap.Width].R) / 255f - 1f) * terrainBumpiness;
                }
            }
        }
        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            HandleInput(gameTime);

            wind.Update(gameTime);
            
            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.SetRenderTarget(0, sceneRenderTarget);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            //GraphicsDevice.Clear(ClearOptions.Target, new Color(0.1f, 0.1f, 0.1f, 1f), 1f, 0);

            /*
             // Calculate a view matrix, moving the camera around a circle.
             float time = (float)gameTime.TotalGameTime.TotalSeconds * 0.333f;

             float cameraX = (float)Math.Cos(time);
             float cameraY = (float)Math.Sin(time);

             Vector3 cameraPosition = new Vector3(cameraX, 0, cameraY) * 64;
             Vector3 cameraFront = new Vector3(-cameraY, 0, cameraX);
             */

            Matrix view = Matrix.CreateLookAt(cameraPosition,
                                              cameraPosition + cameraFront,
                                              Vector3.Up);

            //Matrix treeScale = Matrix.CreateScale(0.01f);
            

            // Draw the terrain first, then the sky. This is faster than
            // drawing the sky first, because the depth buffer can skip
            // bothering to draw sky pixels that are covered up by the
            // terrain. This trick works because the effect used to draw
            // the sky forces all the sky vertices to be as far away as
            // possible, and turns depth testing on but depth writes off.

            //GraphicsDevice.RenderState.DepthBufferEnable = true;

            //Animate trees
            foreach (Tree tree in trees)
                windAnimator.Animate(tree.simpleTree.Skeleton, tree.simpleTree.AnimationState, gameTime);

            Vector3 sunPosition = directionToSun * 1000000f;
            sunPosition2D = GraphicsDevice.Viewport.Project(sunPosition, projection, view, Matrix.Identity);
            //if (Vector3.Dot(cameraFront, directionToSun) <= 0f)
            //    sunPosition2D = new Vector3(-sunPosition2D.X, sunPosition2D.Y, sunPosition2D.Z);

            //sunPosition2D -= (new Vector3(sunSize, sunSize, 0f) * 0.5f);

            if (Vector3.Dot(cameraFront, directionToSun) >= 0f)
            {
                GraphicsDevice.SetRenderTarget(0, sceneRenderTarget);
                GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);

                GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                GraphicsDevice.RenderState.DepthBufferWriteEnable = true;

                GraphicsDevice.RenderState.AlphaBlendEnable = false;
                GraphicsDevice.RenderState.SourceBlend = Blend.One;

                sky.Draw(view, projection);

                DrawSun();

                GraphicsDevice.RenderState.SourceBlend = Blend.Zero;
                GraphicsDevice.RenderState.AlphaBlendEnable = true;

                DrawTerrain(view, projection);
                DrawTreeTrunks(view, projection, true);
                DrawTreeLeaves(view, projection, true);

                GraphicsDevice.SetRenderTarget(0, null);

                DrawLightScattering();

                DrawSprite(sceneRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
                    SpriteBlendMode.None, new Color(1.0f, 1.0f, 1.0f, 1.0f));

                DrawTerrain(view, projection);
                DrawTreeTrunks(view, projection, false);
                DrawTreeLeaves(view, projection, false);

                DrawSprite(lightScatterRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
                    SpriteBlendMode.Additive, new Color(1.0f, 1.0f, 1.0f, (float)Math.Asin(Vector3.Dot(cameraFront, directionToSun)) * 1.0f)); //1.0f));
            }
            else
            {
                GraphicsDevice.SetRenderTarget(0, null);

                GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);

                sky.Draw(view, projection);

                DrawSun();

                DrawTerrain(view, projection);
                DrawTreeTrunks(view, projection, false);
                DrawTreeLeaves(view, projection, false);
            }            

            //DrawSprite(lightScatterRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
            //    SpriteBlendMode.Additive, new Color(1.0f, 1.0f, 1.0f, 1.0f));

            //DrawSprite(sceneRenderTarget.GetTexture(), 0, 0, 512, 512,
            //    SpriteBlendMode.None, new Color(1f, 1f, 1f));

            base.Draw(gameTime);
        }


        /// <summary>
        /// Helper for drawing the terrain model.
        /// </summary>
        void DrawTerrain(Matrix view, Matrix projection)
        {
            foreach (ModelMesh mesh in terrain.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = view;
                }

                mesh.Draw();
            }
        }

        #region Draw Sun
        private void DrawSun()
        {
            if (Vector3.Dot(cameraFront, directionToSun) <= 0f)
                return;

            DrawSprite(sunTexture, (int)sunPosition2D.X - sunSize / 2, (int)sunPosition2D.Y - sunSize / 2, sunSize, sunSize, SpriteBlendMode.AlphaBlend, new Color(0.8f, 0.8f, 0.6f, 0.2f));
        }
        #endregion

        #region Draw Trees
        private void DrawTreeTrunks(Matrix view, Matrix projection, bool black)
        {
            if (black)
            {
                GraphicsDevice.RenderState.AlphaBlendEnable = true;
                GraphicsDevice.RenderState.SourceBlend = Blend.Zero;
            }

            //Draw trunks
            foreach (Tree tree in trees)
                tree.simpleTree.DrawTrunk(tree.transformationMatrix, view, projection);
        }

        private void DrawTreeLeaves(Matrix view, Matrix projection, bool black)
        {
            GraphicsDevice.RenderState.AlphaBlendEnable = true;
            GraphicsDevice.RenderState.SourceBlend = black ? Blend.Zero : Blend.SourceAlpha;
            GraphicsDevice.RenderState.DestinationBlend = black ? Blend.InverseSourceAlpha : Blend.InverseSourceAlpha;

            GraphicsDevice.RenderState.AlphaTestEnable = true;
            GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
            GraphicsDevice.RenderState.ReferenceAlpha = 230;

            GraphicsDevice.RenderState.DepthBufferWriteEnable = false;

            GraphicsDevice.RenderState.CullMode = CullMode.None;

            //Draw leaves
            foreach (Tree tree in trees)
                tree.simpleTree.DrawLeaves(tree.transformationMatrix, view, projection);

            GraphicsDevice.RenderState.AlphaBlendEnable = false;
            GraphicsDevice.RenderState.AlphaTestEnable = false;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RenderState.SourceBlend = Blend.One;
            GraphicsDevice.RenderState.DestinationBlend = Blend.Zero;
        }
        #endregion

        #region Draw Light Scattering
        private void DrawLightScattering()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
            lightScatterPostProcess.Parameters["ViewportSize"].SetValue(viewportSize);

            //Vector2 lightPos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Vector2 lightPos = new Vector2(sunPosition2D.X, sunPosition2D.Y);
            lightPos.X = (lightPos.X / backbufferWidth);
            lightPos.Y = (lightPos.Y / backbufferHeight);

            lightScatterPostProcess.Parameters["ScreenLightPos"].SetValue(lightPos);

            DrawQuad(sceneRenderTarget.GetTexture(), lightScatterRenderTarget,
                lightScatterRenderTarget.Width, lightScatterRenderTarget.Height, lightScatterPostProcess);
        }
        #endregion

        #region Helpers

        #region Draw Sprite
        private void DrawSprite(Texture2D source, int x, int y, int width, int height, SpriteBlendMode spriteBlendMode, Color color)
        {
            // Draw a fullscreen sprite to apply the postprocessing effect.
            spriteBatch.Begin(spriteBlendMode,
                              SpriteSortMode.Immediate,
                              SaveStateMode.SaveState);

            spriteBatch.Draw(source, new Rectangle(x, y, width, height), color);

            spriteBatch.End();
        }
        #endregion

        #region Draw Quad
        private void DrawQuad(Texture2D source, RenderTarget2D target, int x, int y, int width, int height, SpriteBlendMode spriteBlendMode, Effect effect, Color color)
        {
            graphics.GraphicsDevice.SetRenderTarget(0, target);

            if (effect != null)
                graphics.GraphicsDevice.Textures[0] = source;

            // Draw a fullscreen sprite to apply the postprocessing effect.
            spriteBatch.Begin(spriteBlendMode,
                              SpriteSortMode.Immediate,
                              SaveStateMode.SaveState);

            if (effect != null)
            {
                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();
            }

            spriteBatch.Draw(source, new Rectangle(x, y, width, height), color);

            spriteBatch.End();

            if (effect != null)
            {
                effect.CurrentTechnique.Passes[0].End();
                effect.End();
            }

            graphics.GraphicsDevice.SetRenderTarget(0, null);
        }

        private void DrawQuad(Texture2D source, RenderTarget2D target, int width, int height, Effect effect)
        {
            DrawQuad(source, target, 0, 0, width, height, SpriteBlendMode.None, effect, Color.White);
        }
        #endregion

        #endregion

        #endregion

        #region Handle Input


        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            GamePadState currentGamePadState = GamePad.GetState(PlayerIndex.One);
            MouseState currentMouseState = Mouse.GetState();
            
            Viewport viewport = graphics.GraphicsDevice.Viewport;

            Mouse.SetPosition(viewport.Width / 2, viewport.Height / 2);

            float deltaX = (viewport.Width / 2 - currentMouseState.X) *
                gameTime.ElapsedGameTime.Milliseconds * (float)Math.PI / 20000;
            float deltaY = (viewport.Height / 2 - currentMouseState.Y) *
                gameTime.ElapsedGameTime.Milliseconds * (float)Math.PI / 20000;

            cameraFront = Vector3.Transform(cameraFront,
                Quaternion.CreateFromAxisAngle(Vector3.Up, deltaX));
            cameraFront = Vector3.Transform(cameraFront,
                Quaternion.CreateFromAxisAngle(Vector3.Cross(cameraFront, Vector3.Up), deltaY));
            cameraFront.Normalize();

            Mouse.SetPosition(viewport.Width / 2, viewport.Height / 2);

            // Check for exit.
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            if (currentKeyboardState.IsKeyDown(Keys.W))
                cameraPosition += cameraFront * gameTime.ElapsedGameTime.Milliseconds / 10;

            if (currentKeyboardState.IsKeyDown(Keys.S))
                cameraPosition -= cameraFront * gameTime.ElapsedGameTime.Milliseconds / 10;

            if (currentKeyboardState.IsKeyDown(Keys.A))
            {
                Vector3 vec = new Vector3(cameraFront.X, 0, cameraFront.Z);
                cameraPosition += Vector3.Transform(vec, Quaternion.CreateFromYawPitchRoll(
                    (float)Math.PI / 2, 0, 0)) * gameTime.ElapsedGameTime.Milliseconds / 10;
            }
            if (currentKeyboardState.IsKeyDown(Keys.D))
            {
                Vector3 vec = new Vector3(cameraFront.X, 0, cameraFront.Z);
                cameraPosition += Vector3.Transform(vec, Quaternion.CreateFromYawPitchRoll(
                    (float)-Math.PI / 2, 0, 0)) * gameTime.ElapsedGameTime.Milliseconds / 10;
            }
        }


        #endregion
    }


    #region Entry Point

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static class Program
    {
        static void Main()
        {
            using (GeneratedGeometryGame game = new GeneratedGeometryGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
