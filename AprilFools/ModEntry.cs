﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;

namespace AprilFools
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;
        public static string[] ravenText;
        public static Texture2D blackTexture;
        public static Texture2D beeTexture;
        public static bool asciifying;
        public static bool pixelating;
        public static bool beeing;
        public static bool backwardsFarmer;
        public static bool backwardsCursor;
        public static Point lastMousePos;
        public static List<BeeData> beeDataList = new List<BeeData>();
        public static int ravenTicks;
        public static SpriteFont font;
        public static SpriteBatch screenBatch;
        public static Texture2D screenTexture;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            if (!Config.EnableMod)
                return;

            context = this;

            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Display.RenderedWorld += Display_RenderedWorld;
            helper.Events.Display.Rendered += Display_Rendered;

            ravenText = File.ReadAllLines(Path.Combine(Helper.DirectoryPath, "assets", "raven.txt"));
            blackTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            blackTexture.SetData(new Color[] { Color.Black });

            font = helper.Content.Load<SpriteFont>("assets/Fira_Code");
            beeTexture = helper.Content.Load<Texture2D>("assets/bee");

            var screenWidth = Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            var screenHeight = Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;
            screenTexture = new Texture2D(Game1.graphics.GraphicsDevice, screenWidth, screenHeight);
            screenBatch = new SpriteBatch(Game1.graphics.GraphicsDevice);

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (backwardsCursor)
            {
                var pos = Game1.getMousePosition();
                if(pos != lastMousePos)
                {
                    Point newPos = new Point(lastMousePos.X - (pos.X - lastMousePos.X), lastMousePos.Y - (pos.Y - lastMousePos.Y));
                    Game1.setMousePosition(newPos);
                }
            }
            lastMousePos = Game1.getMousePosition();
        }

        private void GameLoop_OneSecondUpdateTicked(object sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if(backwardsFarmer)
            {
                if(Game1.random.NextDouble() < 0.1)
                {
                    backwardsFarmer = false;
                }
                
            }
            else
            {
                backwardsFarmer = Game1.random.NextDouble() < 0.05;
            }
            if(asciifying || pixelating)
            {
                if(Game1.random.NextDouble() < 0.1)
                {
                    asciifying = false;
                    pixelating = false;
                }

            }
            else
            {
                pixelating = Game1.random.NextDouble() < 0.05;
                if (!pixelating)
                    asciifying = Game1.random.NextDouble() < 0.01;
                else
                    asciifying = false;
            }
            if (beeDataList.Count > 30)
                beeDataList.Clear();
            if (Game1.random.NextDouble() < (beeDataList.Count + 1) / 20f)
            {
                beeDataList.Add(new BeeData()
                {
                    pos = new Vector2(Game1.random.Next(Game1.viewport.Width), Game1.random.Next(Game1.viewport.Height))
                });
            }
        }

        private void Display_Rendered(object sender, StardewModdingAPI.Events.RenderedEventArgs e)
        {
            if (beeDataList.Count > 0)
            {
                for (int i = beeDataList.Count - 1; i >= 0; i--)
                {
                    var beeDirection = beeDataList[i].dir;
                    var beeAngle = beeDataList[i].angle;
                    var beePos = beeDataList[i].pos;

                    int size = 64;
                    if (beeDirection != Vector2.Zero)
                    {
                        int period = 36;
                        if (Game1.random.NextDouble() < 0.01)
                            beeDirection = Vector2.Zero;
                        beeDataList[i].ticks++;
                        var which = beeDataList[i].ticks % period;
                        beeDataList[i].currentSprite = which < period / 4 ? 0 : ((which < period  * 3 / 4 && which >= period / 2) ? 2 : 1);
                    }
                    else
                    {
                        beeDataList[i].ticks = 0;
                        if (Game1.random.NextDouble() < 0.01)
                        {
                            beeAngle = Game1.random.NextDouble() * 2 * Math.PI;
                            beeDirection = new Vector2((float)Math.Cos(beeAngle), (float)Math.Sin(beeAngle));
                            beeDirection.Normalize();
                        }
                    }
                    beePos += beeDirection;
                    if (beePos.X < -size || beePos.Y < -size || beePos.X > Game1.viewport.Width + size / 2 || beePos.Y > Game1.viewport.Height + size / 2)
                    {
                        beeDataList.RemoveAt(i);
                        continue;
                    }
                    e.SpriteBatch.Draw(beeTexture, new Rectangle(Utility.Vector2ToPoint(beePos), new Point(size, size)), new Rectangle(beeDataList[i].currentSprite * 512, 0, 512, 512), Color.White, (float)(beeAngle + Math.PI / 2), new Vector2(256, 256), SpriteEffects.None, 1);
                    beeDataList[i].dir = beeDirection;
                    beeDataList[i].angle = beeAngle;
                    beeDataList[i].pos = beePos;
                }

            }
        }


        private void Display_RenderedWorld(object sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
        {
            if (asciifying)
            {
                int scale = 16;
                var lines = ConvertToAscii(ScaleScreen(scale), Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth / scale);
                if (lines.Count > 0)
                {
                    e.SpriteBatch.Draw(blackTexture, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
                    int height = Game1.viewport.Height / lines.Count;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        e.SpriteBatch.DrawString(font, lines[i], new Vector2(0, height * i), Color.White, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 1);
                    }
                }
            }
            if (pixelating)
            {
                int scale = 16;
                Texture2D pixelScreen = new Texture2D(Game1.graphics.GraphicsDevice, Game1.viewport.Width / scale, Game1.viewport.Height / scale);
                pixelScreen.SetData(ScaleScreen(16));
                e.SpriteBatch.Draw(pixelScreen, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
            }
            if (Game1.timeOfDay >= 2100 || Game1.timeOfDay <= 200)
            {
                for(int i = 0; i < ravenText.Length; i++)
                {
                    var y = Game1.viewport.Height - ravenTicks / 2 + i * 48;
                    if (y < -48)
                        continue;
                    if (y > Game1.viewport.Height)
                        break;
                    e.SpriteBatch.DrawString(Game1.dialogueFont, ravenText[i],new Vector2(0, y), Color.DarkGray * 0.75f);
                    e.SpriteBatch.DrawString(Game1.dialogueFont, ravenText[i],new Vector2(2, y + 2), Color.Black * 0.75f);
                }
                ravenTicks++;
            }
            else
            {
                ravenTicks = 0;
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Mod Enabled?",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Bees?",
                getValue: () => Config.BeesEnabled,
                setValue: value => Config.BeesEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Backwards?",
                getValue: () => Config.BackwardsEnabled,
                setValue: value => Config.BackwardsEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Ascii?",
                getValue: () => Config.AsciiEnabled,
                setValue: value => Config.AsciiEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Pixelate?",
                getValue: () => Config.PixelateEnabled,
                setValue: value => Config.PixelateEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Tree Screams?",
                getValue: () => Config.TreeScreamEnabled,
                setValue: value => Config.TreeScreamEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Inventory Avoid?",
                getValue: () => Config.InventoryEnabled,
                setValue: value => Config.InventoryEnabled = value
            );
        }
    }
}