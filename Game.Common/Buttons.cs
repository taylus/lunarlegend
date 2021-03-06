﻿using Microsoft.Xna.Framework.Input;

//TODO: for demo purposes, display a controller image and highlight the buttons pressed
public static class Buttons
{
    //TODO: allow these to be customized by the player (allow secondary keys?)
    public const Keys MOVE_UP = Keys.W;
    public const Keys MOVE_DOWN = Keys.S;
    public const Keys MOVE_LEFT = Keys.A;
    public const Keys MOVE_RIGHT = Keys.D;
    public const Keys USE = Keys.Space;
    public const Keys DEBUG = Keys.Tab;
    public const Keys QUIT = Keys.Escape;
    
    //use LMB as "OK/Confirm" and RMB as "No/Cancel"?
    public const Keys CONFIRM = USE;
    public const Keys CANCEL = Keys.LeftShift;
}
