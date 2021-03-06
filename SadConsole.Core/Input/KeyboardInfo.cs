﻿#if !SHARPDX
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
#else
using SharpDX.DirectInput;
using SharpDX.Toolkit;
using Keys = SharpDX.DirectInput.Key;
#endif
using System.Collections.Generic;
using System.Linq;

namespace SadConsole.Input
{
    /// <summary>
    /// Represents the state of the keyboard.
    /// </summary>
    public class KeyboardInfo
    {
        /// <summary>
        /// A collection of keys registered as pressed which behaves like a command prompt when holding down keys. Uses the <see cref="RepeatDelay"/> and <see cref="InitialRepeatDelay"/> settings.
        /// </summary>
        public List<AsciiKey> KeysPressed { get; internal set; }

        /// <summary>
        /// A collection of keys currently held down.
        /// </summary>
        public List<AsciiKey> KeysDown { get; internal set; }

        /// <summary>
        /// A collection of keys that were just released this frame.
        /// </summary>
        public List<AsciiKey> KeysReleased { get; internal set; }

        /// <summary>
        /// How often a key is included in the <see cref="KeysPressed"/> collection after the <see cref="InitialRepeatDelay"/> time has passed.
        /// </summary>
        public float RepeatDelay = 0.04f;

        /// <summary>
        /// The initial delay after a key is first pressed before it is included a second time (while held down) in the <see cref="KeysPressed"/> collection.
        /// </summary>
        public float InitialRepeatDelay = 0.8f;

        public KeyboardInfo()
        {
            KeysPressed = new List<AsciiKey>();
            KeysReleased = new List<AsciiKey>();
            KeysDown = new List<AsciiKey>();
        }

        /// <summary>
        /// Clears the <see cref="KeysPressed"/>, <see cref="KeysDown"/>, <see cref="KeysReleased"/> collections.
        /// </summary>
        public void Clear()
        {
            KeysPressed.Clear();
            KeysDown.Clear();
            KeysReleased.Clear();
        }

        /// <summary>
        /// Reads the keyboard state using the <see cref="GameTime"/> from the update frame.
        /// </summary>
        /// <param name="gameTime"></param>
        public void ProcessKeys(GameTime gameTime)
        {
            this.KeysPressed.Clear();
            this.KeysReleased.Clear();

#if !SILVERLIGHT && !SHARPDX
            KeyboardState state = Keyboard.GetState();
            bool shiftPressed = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
            var keys = state.GetPressedKeys();
#elif SHARPDX
            Keyboard k = new Keyboard(new DirectInput());
            KeyboardState state = k.GetCurrentState();
            k.Dispose();

            bool shiftPressed = state.IsPressed(Keys.LeftShift) || state.IsPressed(Keys.RightShift);
            var keys = state.PressedKeys;
#else
            KeyboardState state = Keyboard.GetState();
            bool shiftPressed = state.IsKeyDown(Keys.Shift);
            var keys = state.GetPressedKeys();
#endif



            // Cycle all the keys down known if any are up currently, remove
            for (int i = 0; i < this.KeysDown.Count; )
            {
#if SHARPDX
                if (!state.PressedKeys.Contains(this.KeysDown[i].XnaKey))
#else
                if (state.IsKeyUp(this.KeysDown[i].XnaKey))
#endif
                {
                    KeysReleased.Add(this.KeysDown[i]);
                    this.KeysDown.Remove(this.KeysDown[i]);
                }
                else
                    i++;
            }

            // For all new keys down, if we don't know them, add them to pressed, add them to down.
#if SHARPDX
            for (int i = 0; i < keys.Count; i++)
#else
            for (int i = 0; i < keys.Length; i++)
#endif
            {
                bool firstPressed = false;

                Input.AsciiKey key = new AsciiKey();
                Input.AsciiKey keyOppositeShift = new AsciiKey();
                Input.AsciiKey activeKey;

                key.Fill(keys[i], shiftPressed);
                keyOppositeShift.Fill(keys[i], !shiftPressed);

                if (this.KeysDown.Contains(key))
                {
                    activeKey = this.KeysDown.First(k => k == key);
                    activeKey.TimeHeld += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    this.KeysDown.Remove(key);
                }
                else if (this.KeysDown.Contains(keyOppositeShift))
                {
                    activeKey = this.KeysDown.First(k => k == keyOppositeShift);
                    activeKey.Character = key.Character;
                    activeKey.TimeHeld += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    this.KeysDown.Remove(keyOppositeShift);
                }
                else
                {
                    activeKey = key;
                    firstPressed = true;
                }

                if (firstPressed)
                {
                    this.KeysPressed.Add(activeKey);
                }
                else if (activeKey.PreviouslyPressed == false && activeKey.TimeHeld >= InitialRepeatDelay)
                {
                    activeKey.PreviouslyPressed = true;
                    activeKey.TimeHeld = 0f;
                    this.KeysPressed.Add(activeKey);
                }
                else if (activeKey.PreviouslyPressed == true && activeKey.TimeHeld >= RepeatDelay)
                {
                    activeKey.TimeHeld = 0f;
                    this.KeysPressed.Add(activeKey);
                }

                this.KeysDown.Add(activeKey);
            }
        }

        /// <summary>
        /// Returns true if the key is not in the <see cref="KeysDown"/> collection.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True when the key is not being pressed.</returns>
        public bool IsKeyUp(Keys key)
        {
            return !KeysDown.Contains(AsciiKey.Get(key));
        }

        /// <summary>
        /// Returns true if the key is in the <see cref="KeysDown"/> collection.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True when the key is being pressed.</returns>
        public bool IsKeyDown(Keys key)
        {
            return KeysDown.Contains(AsciiKey.Get(key));
        }

        /// <summary>
        /// Returns true when they is in the <see cref="KeysReleased"/> collection.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True when the key was released this update frame.</returns>
        public bool IsKeyReleased(Keys key)
        {
            return KeysReleased.Contains(AsciiKey.Get(key));
        }
    }
}
