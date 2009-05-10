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
#endregion

namespace GeneratedGeometry
{
    /// <summary>
    /// Sample showing how to use geometry that is programatically
    /// generated as part of the content pipeline build process.
    /// </summary>
    public class GeneratedGeometryGame : Microsoft.Xna.Framework.Game
    {
        #region Fields

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Model terrain;
        private Sky sky;

        private Vector3 cameraPosition = Vector3.Zero;
        private Vector3 cameraFront = Vector3.Forward;
        private Matrix projection;

        private Effect lightScatterPostProcess;
        private RenderTarget2D sceneRenderTarget, lightScatterRenderTarget;

        private TreeProfile treeProfile;
        private LinkedList<SimpleTree> trees;

        private int backbufferWidth, backbufferHeight;


        #endregion

        #region Initialization


        public GeneratedGeometryGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
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

            foreach (ModelMesh mesh in terrain.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Projection = projection;

                    effect.PreferPerPixelLighting = true;

                    // Set the specular lighting to match the sky color.
                    effect.SpecularColor = new Vector3(0.6f, 0.4f, 0.2f);
                    effect.SpecularPower = 8;

                    // Set the fog to match the distant mountains
                    // that are drawn into the sky texture.
                    effect.FogEnabled = true;
                    effect.FogColor = new Vector3(0.15f);
                    effect.FogStart = 100;
                    effect.FogEnd = 320;

                    effect.EnableDefaultLighting();
                }
            }

            sky = Content.Load<Sky>("sky");

            backbufferWidth = graphics.PreferredBackBufferWidth;
            backbufferHeight = graphics.PreferredBackBufferHeight;

            lightScatterPostProcess = Content.Load<Effect>("Effects/LightScatterPostProcess");

            //Setup post-process parameters
            lightScatterPostProcess.Parameters["Density"].SetValue(0.7f);
            lightScatterPostProcess.Parameters["Weight"].SetValue(1f / 64f * 2);
            lightScatterPostProcess.Parameters["Decay"].SetValue(0.99f);
            lightScatterPostProcess.Parameters["Exposure"].SetValue(0.3f);

            //Trees
            treeProfile = Content.Load<TreeProfile>("Trees/Graywood");
            trees = new LinkedList<SimpleTree>();
            trees.AddLast(treeProfile.GenerateSimpleTree());

            foreach (SimpleTree tree in trees)
                tree.LeafEffect.CurrentTechnique = tree.LeafEffect.Techniques["SetNoRenderStates"];

            //Render targets
            sceneRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth, backbufferHeight,
                1, SurfaceFormat.Color, GraphicsDevice.PresentationParameters.MultiSampleType,
                GraphicsDevice.PresentationParameters.MultiSampleQuality);
            lightScatterRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth / 4, backbufferHeight / 4,
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

        #region Update and Draw

        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            HandleInput(gameTime);
            
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

            Matrix treeScale = Matrix.CreateScale(0.01f);
            

            // Draw the terrain first, then the sky. This is faster than
            // drawing the sky first, because the depth buffer can skip
            // bothering to draw sky pixels that are covered up by the
            // terrain. This trick works because the effect used to draw
            // the sky forces all the sky vertices to be as far away as
            // possible, and turns depth testing on but depth writes off.

            //GraphicsDevice.RenderState.DepthBufferEnable = true;

            GraphicsDevice.SetRenderTarget(0, sceneRenderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);
            GraphicsDevice.RenderState.AlphaTestEnable = false;
            GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RenderState.AlphaBlendEnable = true;
            GraphicsDevice.RenderState.SourceBlend = Blend.Zero;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = true;

            DrawTerrain(view, projection);
            //trees.First.Value.DrawTrunk(treeScale, view, projection);
            DrawTreeTrunks(treeScale, view, projection, true);

            GraphicsDevice.RenderState.AlphaBlendEnable = false;
            GraphicsDevice.RenderState.SourceBlend = Blend.One;
            sky.Draw(view, projection);

            //GraphicsDevice.RenderState.SourceBlend = Blend.Zero;
            //trees.First.Value.DrawLeaves(treeScale, view, projection);
            DrawTreeLeaves(treeScale, view, projection, true);

            //GraphicsDevice.RenderState.AlphaBlendEnable = false;
            GraphicsDevice.RenderState.AlphaTestEnable = false;
            GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            GraphicsDevice.RenderState.SourceBlend = Blend.One;
            GraphicsDevice.RenderState.DestinationBlend = Blend.Zero;

            GraphicsDevice.SetRenderTarget(0, null);

            DrawLightScattering();

            DrawSprite(sceneRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
                SpriteBlendMode.None, new Color(0.5f, 0.5f, 0.5f));

            DrawTerrain(view, projection);
            //trees.First.Value.DrawTrunk(treeScale, view, projection);
            //trees.First.Value.DrawLeaves(treeScale, view, projection);
            DrawTreeTrunks(treeScale, view, projection, false);
            DrawTreeLeaves(treeScale, view, projection, false);

            GraphicsDevice.RenderState.AlphaTestEnable = false;
            GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RenderState.SourceBlend = Blend.One;
            GraphicsDevice.RenderState.DestinationBlend = Blend.Zero;

            DrawSprite(lightScatterRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
                SpriteBlendMode.Additive, Color.White);

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

        #region Draw Trees
        private void DrawTreeTrunks(Matrix world, Matrix view, Matrix projection, bool black)
        {
            if (black)
            {
                GraphicsDevice.RenderState.AlphaBlendEnable = true;
                GraphicsDevice.RenderState.SourceBlend = Blend.Zero;
            }

            //Draw trunks
            foreach (SimpleTree tree in trees)
                tree.DrawTrunk(world, view, projection);
        }

        private void DrawTreeLeaves(Matrix world, Matrix view, Matrix projection, bool black)
        {
            GraphicsDevice.RenderState.AlphaBlendEnable = true;
            GraphicsDevice.RenderState.SourceBlend = black ? Blend.Zero : Blend.SourceAlpha;
            GraphicsDevice.RenderState.DestinationBlend = black ? Blend.InverseSourceAlpha : Blend.InverseSourceAlpha;

            GraphicsDevice.RenderState.AlphaTestEnable = true;
            GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
            GraphicsDevice.RenderState.ReferenceAlpha = 230;

            GraphicsDevice.RenderState.DepthBufferEnable = true;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = false;

            GraphicsDevice.RenderState.CullMode = CullMode.None;

            //Draw leaves
            foreach (SimpleTree tree in trees)
                tree.DrawLeaves(world, view, projection);
        }
        #endregion

        #region Draw Light Scattering
        private void DrawLightScattering()
        {
            Vector2 lightPos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            lightPos.X = (lightPos.X / backbufferWidth);
            lightPos.Y = (lightPos.Y / backbufferHeight);

            lightScatterPostProcess.Parameters["ScreenLightPos"].SetValue(lightPos);

            DrawQuad(sceneRenderTarget.GetTexture(), lightScatterRenderTarget,
                backbufferWidth / 4, backbufferHeight / 4, lightScatterPostProcess);
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
