﻿namespace SadConsole.Consoles
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using SadConsole.Input;
    using System;
    using System.Runtime.Serialization;
    using System.Linq;

    /// <summary>
    /// Represents a traditional console that implements mouse and keyboard handling as well as a cursor.
    /// </summary>
    public partial class Console : SurfaceEditor, IConsole
    {

        #region Events
        /// <summary>
        /// Raised when the a mosue button is clicked on this console.
        /// </summary>
        public event EventHandler<MouseEventArgs> MouseButtonClicked;

        /// <summary>
        /// Raised when the mouse moves around the this console.
        /// </summary>
        public event EventHandler<MouseEventArgs> MouseMove;

        /// <summary>
        /// Raised when the mouse exits this console.
        /// </summary>
        public event EventHandler<MouseEventArgs> MouseExit;

        /// <summary>
        /// Raised when the mouse enters this console.
        /// </summary>
        public event EventHandler<MouseEventArgs> MouseEnter;
        #endregion

        /// <summary>
        /// The renderer used to draw the <see cref="TextSurface"/>.
        /// </summary>
        protected ITextSurfaceRenderer _renderer;

        /// <summary>
        /// Where the console should be located on the screen.
        /// </summary>
        protected Point _position;

        /// <summary>
        /// Indicates the console is visible.
        /// </summary>
        protected bool _isVisible = true;

        /// <summary>
        /// The parent console.
        /// </summary>
        protected IConsoleList _parentConsole;

        /// <summary>
        /// Indicates that the mouse is currently over this console.
        /// </summary>
        protected bool _isMouseOver = false;

        /// <summary>
        /// The private virtual curser reference.
        /// </summary>
        protected Cursor _virtualCursor;

        /// <summary>
        /// Toggles the VirtualCursor as visible\hidden when the console if focused\unfocused.
        /// </summary>
        public bool AutoCursorOnFocus { get; set; }

        /// <summary>
        /// Represents a _virtualCursor that can be used to input information into the console.
        /// </summary>
        public Cursor VirtualCursor
        {
            get { return _virtualCursor; }
            //set
            //{
            //    if (value != null)
            //        _virtualCursor = value;
            //    else
            //        throw new Exception("VirtualCursor cannot be null");
            //}
        }

        /// <summary>
        /// Indicates that the mouse is currently over this console.
        /// </summary>
        public bool IsMouseOver { get { return _isMouseOver; } }

        /// <summary>
        /// Gets or sets the Parent console.
        /// </summary>
        public IConsoleList Parent
        {
            get { return _parentConsole; }
            set
            {
                if (_parentConsole != value)
                {
                    if (_parentConsole == null)
                    {
                        _parentConsole = value;
                        _parentConsole.Add(this);
                        OnParentConsoleChanged(null, _parentConsole);
                    }
                    else
                    {
                        var oldParent = _parentConsole;
                        _parentConsole = value;

                        oldParent.Remove(this);

                        if (_parentConsole != null)
                            _parentConsole.Add(this);

                        OnParentConsoleChanged(oldParent, _parentConsole);
                    }
                }

            }
        }

        /// <summary>
        /// When true, this console will move to the front of its parent console when focused.
        /// </summary>
        public bool MoveToFrontOnMouseFocus { get; set; }

        /// <summary>
        /// Allows the mouse (with a click) to focus this console.
        /// </summary>
        public bool MouseCanFocus { get; set; }

        /// <summary>
        /// Allows this console to accept keyboard input.
        /// </summary>
        public bool CanUseKeyboard { get; set; } = true;

        /// <summary>
        /// Allows this console to accept mouse input.
        /// </summary>
        public bool CanUseMouse { get; set; } = true;

        /// <summary>
        /// Allows this console to be focusable.
        /// </summary>
        public bool CanFocus { get; set; }

        /// <summary>
        /// Indicates whether or not this console is visible.
        /// </summary>
        public bool IsVisible { get { return _isVisible; } set { _isVisible = value; OnVisibleChanged(); } }

        /// <summary>
        /// When false, does not perform the code within the <see cref="Update"/> method. Defaults to true.
        /// </summary>
        public bool DoUpdate { get; set; } = true;

        /// <summary>
        /// The renderer used to draw <see cref="TextSurface"/>.
        /// </summary>
        public ITextSurfaceRenderer Renderer
        {
            get { return _renderer; }
            set
            {
                if (_renderer != null)
                {
                    _renderer.BeforeRenderCallback = null;
                    _renderer.AfterRenderCallback = null;
                }

                _renderer = value;
                _renderer.BeforeRenderCallback = this.OnBeforeRender;
                _renderer.AfterRenderCallback = this.OnAfterRender;
            }
        }
        protected ITextSurfaceRendered textSurface;

        /// <summary>
        /// The text surface to be rendered or changed.
        /// </summary>
        public new ITextSurfaceRendered TextSurface
        {
            get { return textSurface; }
            set { textSurface = value; base.TextSurface = value; }
        }

        /// <summary>
        /// Gets or sets the position to render the cells.
        /// </summary>
        public Point Position
        {
            get { return _position; }
            set { Point previousPosition = _position; _position = value; OnPositionChanged(previousPosition); }
        }

        /// <summary>
        /// Gets or sets this console as the <see cref="Engine.ActiveConsole"/> value.
        /// </summary>
        /// <remarks>If the <see cref="Engine.ActiveConsole"/> has the <see cref="Console.ExclusiveFocus"/> property set to true, you cannot use this property to set this console to focused.</remarks>
        public bool IsFocused
        {
            get { return Engine.ActiveConsole == this; }
            set
            {
                if (Engine.ActiveConsole != null)
                {
                    if (value && Engine.ActiveConsole != this && !Engine.ActiveConsole.ExclusiveFocus)
                    {
                        Engine.ActiveConsole = this;
                        OnFocused();
                    }

                    else if (value && Engine.ActiveConsole == this)
                        OnFocused();

                    else if (!value)
                    {
                        if (Engine.ActiveConsole == this)
                            Engine.ActiveConsole = null;

                        OnFocusLost();
                    }
                }
                else
                {
                    if (value)
                    {
                        Engine.ActiveConsole = this;
                        OnFocused();
                    }
                    else
                        OnFocusLost();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether or not this console has exclusive access to the mouse events.
        /// </summary>
        public bool ExclusiveFocus { get; set; }

        /// <summary>
        /// An alternative method handler for handling the mouse logic.
        /// </summary>
        public Func<IConsole, MouseInfo, bool> MouseHandler { get; set; }

        /// <summary>
        /// An alternative method handler for handling the keyboard logic.
        /// </summary>
        public Func<IConsole, KeyboardInfo, bool> KeyboardHandler { get; set; }

        /// <summary>
        /// Treats the <see cref="Position"/> of the console as if it is pixels and not cells.
        /// </summary>
        public bool UsePixelPositioning { get; set; } = false;

        #region Constructors
        //public Console() : this(1, 1, Engine.DefaultFont) { }

        public Console(int width, int height): this(width, height, Engine.DefaultFont) { }

        public Console(int width, int height, Font font) : this(new TextSurface(width, height, font)) { }

        public Console(ITextSurfaceRendered textData): base(textData)
        {
            _virtualCursor = new Cursor(this);
            Renderer = new TextSurfaceRenderer();
            textSurface = textData;
        }
        #endregion
        
        protected virtual void OnMouseEnter(MouseInfo info)
        {
            if (MouseEnter != null)
                MouseEnter(this, new MouseEventArgs(info));
        }

        protected virtual void OnMouseExit(MouseInfo info)
        {
            // Force mouse off just incase
            _isMouseOver = false;

            if (MouseExit != null)
                MouseExit(this, new MouseEventArgs(info));
        }

        protected virtual void OnMouseIn(MouseInfo info)
        {
            if (MouseMove != null)
                MouseMove(this, new MouseEventArgs(info));
        }

        protected virtual void OnMouseLeftClicked(MouseInfo info)
        {
            if (MouseButtonClicked != null)
                MouseButtonClicked(this, new MouseEventArgs(info));
        }

        protected virtual void OnRightMouseClicked(MouseInfo info)
        {
            if (MouseButtonClicked != null)
                MouseButtonClicked(this, new MouseEventArgs(info));
        }

        
        /// <summary>
        /// Processes the mouse.
        /// </summary>
        /// <param name="info"></param>
        /// <returns>True when the mouse is over this console.</returns>
        public virtual bool ProcessMouse(MouseInfo info)
        {
            var handlerResult = MouseHandler == null ? false : MouseHandler(this, info);

            if (!handlerResult)
            {
                if (this.IsVisible && this.CanUseMouse)
                {
                    info.Fill(this);

                    if (info.Console == this)
                    {
                        if (this.CanFocus && this.MouseCanFocus && info.LeftClicked)
                        {
                            IsFocused = true;

                            if (IsFocused && this.MoveToFrontOnMouseFocus && this.Parent != null && this.Parent.IndexOf(this) != this.Parent.Count - 1)
                                this.Parent.MoveToTop(this);
                        }

                        if (_isMouseOver != true)
                        {
                            _isMouseOver = true;
                            OnMouseEnter(info);
                        }

                        OnMouseIn(info);

                        if (info.LeftClicked)
                            OnMouseLeftClicked(info);

                        if (info.RightClicked)
                            OnRightMouseClicked(info);

                        return true;
                    }
                    else
                    {
                        if (_isMouseOver)
                        {
                            _isMouseOver = false;
                            OnMouseExit(info);
                        }
                    }
                }
            }

            return handlerResult;
        }

        /// <summary>
        /// Called by the engine to process the keyboard. If the <see cref="KeyboardHandler"/> has been set, that will be called instead of this method.
        /// </summary>
        /// <param name="info">Keyboard information.</param>
        /// <returns>True when the keyboard had data and this console did something with it.</returns>
        public virtual bool ProcessKeyboard(KeyboardInfo info)
        {
            var handlerResult = KeyboardHandler == null ? false : KeyboardHandler(this, info);

            if (!handlerResult && this.CanUseKeyboard)
            {
                bool didSomething = false;
                foreach (var key in info.KeysPressed)
                {
                    if (key.Character == '\0')
                    {
                        switch (key.XnaKey)
                        {
                            case Keys.Space:
                                this._virtualCursor.Print(key.Character.ToString());
                                didSomething = true;
                                break;
                            case Keys.Enter:
                                this._virtualCursor.CarriageReturn().LineFeed();
                                didSomething = true;
                                break;
#if !SILVERLIGHT
                            case Keys.LeftShift:
                            case Keys.RightShift:
                            case Keys.LeftAlt:
                            case Keys.RightAlt:
                            case Keys.LeftControl:
                            case Keys.RightControl:
                            case Keys.LeftWindows:
                            case Keys.RightWindows:
                            case Keys.F1:case Keys.F2:case Keys.F3:case Keys.F4:case Keys.F5:case Keys.F6:case Keys.F7:case Keys.F8:case Keys.F9:case Keys.F10:
                            case Keys.F11:case Keys.F12:case Keys.F13:case Keys.F14:case Keys.F15:case Keys.F16:case Keys.F17:case Keys.F18:case Keys.F19:case Keys.F20:
                            case Keys.F21:case Keys.F22:case Keys.F23:case Keys.F24:
                            case Keys.Pause:
                            case Keys.Escape:
#else
							case Keys.Shift:
							case Keys.Alt:
							case Keys.Ctrl:
#endif
                                //this._virtualCursor.Print(key.Character.ToString());
                                break;
                            case Keys.Up:
                                this._virtualCursor.Up(1);
                                didSomething = true;
                                break;
                            case Keys.Left:
                                this._virtualCursor.Left(1);
                                didSomething = true;
                                break;
                            case Keys.Right:
                                this._virtualCursor.Right(1);
                                didSomething = true;
                                break;
                            case Keys.Down:
                                this._virtualCursor.Down(1);
                                didSomething = true;
                                break;
                            case Keys.None:
                                break;
                            case Keys.Back:
                                this._virtualCursor.Left(1).Print(" ").Left(1);
                                didSomething = true;
                                break;
                            default:
                                this._virtualCursor.Print(key.Character.ToString());
                                didSomething = true;
                                break;
                        }
                    }
                    else
                    {
                        this._virtualCursor.Print(key.Character.ToString());
                        didSomething = true;
                    }
                }

                return didSomething;
            }

            return handlerResult;
        }

        

        /// <summary>
        /// Called when the visibility of the console changes.
        /// </summary>
        protected virtual void OnVisibleChanged() { }

        /// <summary>
        /// Called when this console's focus has been lost.
        /// </summary>
        protected virtual void OnFocusLost()
        {
            if (AutoCursorOnFocus == true)
                _virtualCursor.IsVisible = false;
        }

        /// <summary>
        /// Called when this console is focused.
        /// </summary>
        protected virtual void OnFocused()
        {
            if (AutoCursorOnFocus == true)
                _virtualCursor.IsVisible = true;
        }

        /// <summary>
        /// Called when the <see cref="Position" /> property changes.
        /// </summary>
        /// <param name="oldLocation">The location before the change.</param>
        protected virtual void OnPositionChanged(Point oldLocation) { }

        /// <summary>
        /// Called when the renderer renders the text view.
        /// </summary>
        /// <param name="batch">The batch used in renderering.</param>
        protected virtual void OnAfterRender(SpriteBatch batch)
        {
            if (VirtualCursor.IsVisible)
            {
                int virtualCursorLocationIndex = Consoles.TextSurface.GetIndexFromPoint(
                    new Point(VirtualCursor.Position.X - TextSurface.RenderArea.X,
                              VirtualCursor.Position.Y - TextSurface.RenderArea.Y), TextSurface.RenderArea.Width);

                if (virtualCursorLocationIndex >= 0 && virtualCursorLocationIndex < textSurface.RenderRects.Length)
                {
                    VirtualCursor.Render(batch, textSurface.Font, textSurface.RenderRects[virtualCursorLocationIndex]);
                }
            }
        }

        /// <summary>
        /// Called when the renderer renders the text view.
        /// </summary>
        /// <param name="batch">The batch used in renderering.</param>
        protected virtual void OnBeforeRender(SpriteBatch batch) { }

        /// <summary>
        /// Updates the cell effects and cursor.
        /// </summary>
        public virtual void Update()
        {
            if (DoUpdate)
            {
                Effects.UpdateEffects(Engine.GameTimeElapsedUpdate);

                if (VirtualCursor.IsVisible)
                    VirtualCursor.CursorRenderCell.UpdateAndApplyEffect(Engine.GameTimeElapsedUpdate);
            }
        }

        public virtual void Render()
        {
            if (_isVisible)
            {
                Renderer.Render(textSurface, _position, UsePixelPositioning);
            }
        }

        /// <summary>
        /// Called when the parent console changes for this console.
        /// </summary>
        /// <param name="oldParent">The previous parent.</param>
        /// <param name="newParent">The new parent.</param>
        protected virtual void OnParentConsoleChanged(IConsoleList oldParent, IConsoleList newParent) { }

        /// <summary>
        /// Used by the console engine to properly clear the mouse over flag and call OnMouseExit. Used when mouse exits window.
        /// </summary>
        private void ExitMouse()
        {
            if (_isMouseOver)
            {
                _isMouseOver = false;

                MouseInfo info = Engine.Mouse.Clone();
                info.ConsoleLocation = new Point(-1, -1);

                OnMouseExit(info);
            }
        }


        #region Serialization
        /// <summary>
        /// Saves the <see cref="Console"/> to a file.
        /// </summary>
        /// <param name="file">The destination file.</param>
        /// <param name="saveTextSurface">When false the <see cref="IConsole.TextSurface"/> property will not be serialized.</param>
        /// <param name="knownTypes">Types to provide if the <see cref="SurfaceEditor.TextSurface"/> and <see cref="Renderer" /> types are custom and unknown to the serializer.</param>
        public void Save(string file, bool saveTextSurface, params Type[] knownTypes)
        {
            new Serialized(this, saveTextSurface).Save(file, knownTypes.Union(Serializer.ConsoleTypes).ToArray());
            //Serializer.Save(this, file, new Type[] { typeof(CellAppearance) });
        }

        /// <summary>
        /// Loads a <see cref="Console"/> from a file.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <param name="knownTypes">Types to provide if the <see cref="SurfaceEditor.TextSurface"/> and <see cref="Renderer" /> types are custom and unknown to the serializer.</param>
        /// <returns>The <see cref="Console"/>.</returns>
        public static Console Load(string file, params Type[] knownTypes)
        {
            //return Serializer.Load<Console>(file, new Type[] { typeof(CellAppearance) });
            return Serialized.Load(file, knownTypes.Union(Serializer.ConsoleTypes).ToArray());
        }

        /// <summary>
        /// Serialized instance of a <see cref="Console"/>.
        /// </summary>
        [DataContract]
        public class Serialized
        {
            [DataMember]
            public bool AutoCursorOnFocus;
            [DataMember]
            public bool CanFocus;
            [DataMember]
            public bool CanUseKeyboard;
            [DataMember]
            public bool CanUseMouse;
            [DataMember]
            public ITextSurfaceRendered TextSurface;
            [DataMember]
            public bool DoUpdate;
            [DataMember]
            public bool ExclusiveFocus;
            [DataMember]
            public bool IsFocused;
            [DataMember]
            public bool IsVisible;
            [DataMember]
            public bool MouseCanFocus;
            [DataMember]
            public bool MoveToFrontOnMouseFocus;
            [DataMember]
            public Point Position;
            [DataMember]
            public ITextSurfaceRenderer Renderer;
            [DataMember]
            public bool UsePixelPositioning;
            [DataMember]
            public Cursor VirtualCursor;
            [DataMember]
            public int Width;
            [DataMember]
            public int Height;

            /// <summary>
            /// Creates a serialized object from an existing <see cref="Console"/>.
            /// </summary>
            /// <param name="surface">The surface to serialize.</param>
            public Serialized(Console console, bool serializeTextSurface)
            {
                AutoCursorOnFocus = console.AutoCursorOnFocus;
                CanFocus = console.CanFocus;
                CanUseKeyboard = console.CanUseKeyboard;
                CanUseMouse = console.CanUseMouse;
                if (serializeTextSurface)
                    TextSurface = console.TextSurface;

                Width = console.Width;
                Height = console.Height;
                DoUpdate = console.DoUpdate;
                ExclusiveFocus = console.ExclusiveFocus;
                IsFocused = console.IsFocused;
                IsVisible = console.IsVisible;
                MouseCanFocus = console.MouseCanFocus;
                MoveToFrontOnMouseFocus = console.MoveToFrontOnMouseFocus;
                Position = console.Position;
                Renderer = console.Renderer;
                UsePixelPositioning = console.UsePixelPositioning;
                VirtualCursor = console.VirtualCursor;
            }

            protected Serialized() { }

            /// <summary>
            /// Saves the serialized <see cref="Console"/> to a file.
            /// </summary>
            /// <param name="file">The destination file.</param>
            public void Save(string file, params Type[] knownTypes)
            {
                SadConsole.Serializer.Save(this, file, knownTypes);
            }

            /// <summary>
            /// Loads a <see cref="Consoles.TextSurface"/> from a file.
            /// </summary>
            /// <param name="file">The source file.</param>
            /// <returns>A surface.</returns>
            public static Console Load(string file, params Type[] knownTypes)
            {
                var data = Serializer.Load<Serialized>(file, knownTypes);
                Console console = new Console(data.TextSurface);

                console.AutoCursorOnFocus = data.AutoCursorOnFocus;
                console.CanFocus = data.CanFocus;
                console.CanUseKeyboard = data.CanUseKeyboard;
                console.CanUseMouse = data.CanUseMouse;

                if (data.TextSurface != null)
                    console.TextSurface = data.TextSurface;
                else
                    console.TextSurface = new TextSurface(data.Width, data.Height, Engine.DefaultFont);

                console.DoUpdate = data.DoUpdate;
                console.ExclusiveFocus = data.ExclusiveFocus;
                console.IsFocused = data.IsFocused;
                console.IsVisible = data.IsVisible;
                console.MouseCanFocus = data.MouseCanFocus;
                console.MoveToFrontOnMouseFocus = data.MoveToFrontOnMouseFocus;
                console.Position = data.Position;
                console.Renderer = data.Renderer;
                console.UsePixelPositioning = data.UsePixelPositioning;
                console._virtualCursor = data.VirtualCursor;
                console._virtualCursor.AttachConsole(console);
                console._virtualCursor.ResetCursorEffect();

                return console;
            }
        }
        #endregion

    }
}
