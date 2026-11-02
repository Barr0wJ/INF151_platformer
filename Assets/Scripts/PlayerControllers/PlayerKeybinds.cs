using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerAction
{
    Jump,
    MoveRight,
    MoveLeft,
    Dash,
    Slide
}

public static class PlayerKeybinds
{
    private const string FileName = "keybinds.json";

    private static Key jump = Key.Space;
    private static Key moveRight = Key.RightArrow;
    private static Key moveRightAlternate = Key.D;
    private static Key moveLeft = Key.LeftArrow;
    private static Key moveLeftAlternate = Key.A;
    private static Key dash = Key.LeftShift;
    private static Key slide = Key.LeftCtrl;
    private static Key slideAlternate = Key.RightCtrl;
    private static bool loaded;

    [Serializable]
    private class KeybindFile
    {
        public string jump = "Space";
        public string moveRight = "RightArrow";
        public string moveRightAlternate = "D";
        public string moveLeft = "LeftArrow";
        public string moveLeftAlternate = "A";
        public string dash = "LeftShift";
        public string slide = "LeftCtrl";
        public string slideAlternate = "RightCtrl";
    }

    public static Key Jump
    {
        get { EnsureLoaded(); return jump; }
    }

    public static Key MoveRight
    {
        get { EnsureLoaded(); return moveRight; }
    }

    public static Key MoveRightAlternate
    {
        get { EnsureLoaded(); return moveRightAlternate; }
    }

    public static Key MoveLeft
    {
        get { EnsureLoaded(); return moveLeft; }
    }

    public static Key MoveLeftAlternate
    {
        get { EnsureLoaded(); return moveLeftAlternate; }
    }

    public static Key Dash
    {
        get { EnsureLoaded(); return dash; }
    }

    public static Key Slide
    {
        get { EnsureLoaded(); return slide; }
    }

    public static Key SlideAlternate
    {
        get { EnsureLoaded(); return slideAlternate; }
    }

    public static void Load()
    {
        EnsureLoaded();
    }

    public static void SetBinding(PlayerAction action, Key key)
    {
        EnsureLoaded();
        if (key == Key.None || !Enum.IsDefined(typeof(Key), key))
        {
            Debug.LogError("Cannot assign an invalid keybind.");
            return;
        }

        switch (action)
        {
            case PlayerAction.Jump:
                jump = key;
                break;
            case PlayerAction.MoveRight:
                moveRight = key;
                moveRightAlternate = Key.None;
                break;
            case PlayerAction.MoveLeft:
                moveLeft = key;
                moveLeftAlternate = Key.None;
                break;
            case PlayerAction.Dash:
                dash = key;
                break;
            case PlayerAction.Slide:
                slide = key;
                slideAlternate = Key.None;
                break;
            default:
                Debug.LogError("Cannot assign a keybind for an unknown action.");
                return;
        }

        Save();
    }

    public static bool IsPressed(Key primary, Key alternate = Key.None)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
            (IsKeyPressed(keyboard, primary) || IsKeyPressed(keyboard, alternate));
    }

    public static bool WasPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    public static bool WasPressed(Key primary, Key alternate)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && IsControlPair(primary, alternate))
        {
            return keyboard.ctrlKey.wasPressedThisFrame;
        }

        return WasPressed(primary) || WasPressed(alternate);
    }

    public static bool WasReleased(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].wasReleasedThisFrame;
    }

    public static bool WasReleased(Key primary, Key alternate)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && IsControlPair(primary, alternate))
        {
            return keyboard.ctrlKey.wasReleasedThisFrame;
        }

        return WasReleased(primary) || WasReleased(alternate);
    }

    public static string FormatBinding(Key primary, Key alternate = Key.None)
    {
        if ((primary == Key.LeftCtrl && alternate == Key.RightCtrl) ||
            (primary == Key.RightCtrl && alternate == Key.LeftCtrl))
        {
            return "Ctrl";
        }

        string primaryName = FormatKey(primary);
        if (alternate == Key.None || alternate == primary)
        {
            return primaryName;
        }

        return primaryName + " / " + FormatKey(alternate);
    }

    private static bool IsKeyPressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].isPressed;
    }

    private static bool IsControlPair(Key primary, Key alternate)
    {
        return (primary == Key.LeftCtrl && alternate == Key.RightCtrl) ||
            (primary == Key.RightCtrl && alternate == Key.LeftCtrl);
    }

    private static string FormatKey(Key key)
    {
        return key.ToString()
            .Replace("Arrow", " Arrow")
            .Replace("Shift", " Shift")
            .Replace("Ctrl", " Ctrl");
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            return;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException exception)
        {
            Debug.LogError("Could not read saved keybinds: " + exception.Message);
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError("Could not read saved keybinds: " + exception.Message);
            return;
        }

        KeybindFile data;
        try
        {
            data = JsonUtility.FromJson<KeybindFile>(json);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError("Could not parse saved keybinds: " + exception.Message);
            return;
        }

        if (data == null)
        {
            Debug.LogError("Could not parse saved keybinds: the file is empty or invalid.");
            return;
        }

        jump = ReadKey(data.jump, Key.Space, "jump");
        moveRight = ReadKey(data.moveRight, Key.RightArrow, "moveRight");
        moveRightAlternate = ReadKey(data.moveRightAlternate, Key.D, "moveRightAlternate", true);
        moveLeft = ReadKey(data.moveLeft, Key.LeftArrow, "moveLeft");
        moveLeftAlternate = ReadKey(data.moveLeftAlternate, Key.A, "moveLeftAlternate", true);
        dash = ReadKey(data.dash, Key.LeftShift, "dash");
        slide = ReadKey(data.slide, Key.LeftCtrl, "slide");
        slideAlternate = ReadKey(data.slideAlternate, Key.RightCtrl, "slideAlternate", true);
    }

    private static Key ReadKey(string value, Key fallback, string field, bool allowNone = false)
    {
        if (allowNone && string.IsNullOrEmpty(value))
        {
            return Key.None;
        }

        if (Enum.TryParse(value, true, out Key key) &&
            Enum.IsDefined(typeof(Key), key) &&
            (allowNone || key != Key.None))
        {
            return key;
        }

        Debug.LogWarning("Saved keybind '" + field + "' is invalid; using its default.");
        return fallback;
    }

    private static void Save()
    {
        var data = new KeybindFile
        {
            jump = jump.ToString(),
            moveRight = moveRight.ToString(),
            moveRightAlternate = moveRightAlternate == Key.None ? string.Empty : moveRightAlternate.ToString(),
            moveLeft = moveLeft.ToString(),
            moveLeftAlternate = moveLeftAlternate == Key.None ? string.Empty : moveLeftAlternate.ToString(),
            dash = dash.ToString(),
            slide = slide.ToString(),
            slideAlternate = slideAlternate == Key.None ? string.Empty : slideAlternate.ToString()
        };

        try
        {
            File.WriteAllText(GetFilePath(), JsonUtility.ToJson(data, true));
        }
        catch (IOException exception)
        {
            Debug.LogError("Could not save keybinds: " + exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError("Could not save keybinds: " + exception.Message);
        }
    }

    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }
}
