#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
#endregion

namespace kaorudono
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Kaoru : Microsoft.Xna.Framework.Game
    {
        #region Fields
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D sunTexture, blackTexture;
        private Effect lightScatterPostProcess;
        private RenderTarget2D sceneRenderTarget, lightScatterRenderTarget;

        private int backbufferWidth, backbufferHeight;

        private MouseState mState;
        private KeyboardState kState;
        #endregion

        #region Initialization
        public Kaoru()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
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
            this.IsMouseVisible = true;
            base.Initialize();
        }
        #endregion

        #region Load/Unload Content
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            backbufferWidth = graphics.PreferredBackBufferWidth;
            backbufferHeight = graphics.PreferredBackBufferHeight;

            sunTexture = Content.Load<Texture2D>("Textures/Sun");
            blackTexture = Content.Load<Texture2D>("Textures/Black");
            lightScatterPostProcess = Content.Load<Effect>("Effects/LightScatterPostProcess");

            //Setup post-process parameters
            lightScatterPostProcess.Parameters["Density"].SetValue(0.7f);
            lightScatterPostProcess.Parameters["Weight"].SetValue(1f / 64f * 2);
            lightScatterPostProcess.Parameters["Decay"].SetValue(0.99f);
            lightScatterPostProcess.Parameters["Exposure"].SetValue(0.3f);

            //Render targets
            sceneRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth, backbufferHeight,
                1, SurfaceFormat.Color, MultiSampleType.None, 0);
            lightScatterRenderTarget = new RenderTarget2D(GraphicsDevice, backbufferWidth / 4, backbufferHeight / 4,
                1, SurfaceFormat.Color, MultiSampleType.None, 0);

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

        #region Update
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            mState = Mouse.GetState();
            kState = Keyboard.GetState();

            // Allows the game to exit
            if (kState.IsKeyDown(Keys.Escape))
                this.Exit();

            base.Update(gameTime);
        }
        #endregion

        #region Draw
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);

            GraphicsDevice.SetRenderTarget(0, sceneRenderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, new Color(0.1f, 0.1f, 0.1f, 1f), 1f, 0);
            
            DrawSun();
            DrawOccluders();


            GraphicsDevice.SetRenderTarget(0, null);

            DrawLightScattering();

            DrawQuad(sceneRenderTarget.GetTexture(), null, backbufferWidth, backbufferHeight, null);

            DrawSprite(lightScatterRenderTarget.GetTexture(), 0, 0, backbufferWidth, backbufferHeight,
                 SpriteBlendMode.Additive, Color.White);

            base.Draw(gameTime);
        }
        #endregion

        #region Draw Sun
        private void DrawSun()
        {
            DrawSprite(sunTexture, mState.X - 64, mState.Y - 64, 128, 128,
                SpriteBlendMode.Additive, Color.White);
        }
        #endregion

        #region Draw Occluders
        private void DrawOccluders()
        {
            Rectangle[] rects = new Rectangle[4];
            rects[0] = new Rectangle(80, 200, 64, 200);
            rects[1] = new Rectangle(180, 200, 64, 200);
            rects[2] = new Rectangle(270, 200, 64, 200);
            rects[3] = new Rectangle(400, 200, 64, 200);

            //GraphicsDevice.SetRenderTarget(0, sceneRenderTarget);
            //graphics.GraphicsDevice.Clear(ClearOptions.Target, Color.Blue, 1f, 0, rects);
            //GraphicsDevice.SetRenderTarget(0, null);
            for (int i = 0; i < rects.Count(); i++)
                DrawSprite(blackTexture, rects[i].X, rects[i].Y, rects[i].Width, rects[i].Height,
                    SpriteBlendMode.None, Color.Black);
        }
        #endregion

        #region Draw Light Scattering
        private void DrawLightScattering()
        {
            //GraphicsDevice.SetRenderTarget(0, lightScatterRenderTarget);

            //GraphicsDevice.Textures[0] = sceneRenderTarget.GetTexture();
            Vector2 lightPos = new Vector2(mState.X, mState.Y);
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
                              SaveStateMode.None);

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
                              SaveStateMode.None);

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
    }
}
