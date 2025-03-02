using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using ValeViewer.ImageLoader;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore : IDisposable
{
    private IntPtr _font16;

    private readonly ImageComposite _composite = new();

    private bool _running = true;

    #region Initialize

    public SdlCore(string? imagePath, bool startInFullscreen)
    {
        _fullscreen = startInFullscreen;

        var stopwatch = Stopwatch.StartNew();

        if (SDL_Init(SDL_INIT_VIDEO) < 0)
        {
            throw new Exception($"[Core] SDL could not initialize! SDL_Error: {SDL_GetError()}");
        }

        Logger.Log("[Core] SDL Initialized");

        if (SDL_ttf.TTF_Init() < 0)
        {
            throw new Exception($"[Core] SDL_ttf could not initialize! SDL_Error: {SDL_GetError()}");
        }

        Logger.Log("[Core] SDL_ttf Initialized");

        InitializeInput();
        CreateWindow();
        CreateRenderer();
        LoadFont();

        Logger.Log($"[Core] Startup time: {stopwatch.ElapsedMilliseconds} ms");

        if (imagePath != null)
            DirectoryNavigator.SearchImages(imagePath);

        LoadImage(DirectoryNavigator.Current());
    }

    private void LoadFont()
    {
        _font16 = SDL_ttf.TTF_OpenFont(TtfLoader.GetMonospaceFontPath(), 16);
        if (_font16 == IntPtr.Zero)
        {
            throw new Exception($"[Core] Failed to load font: {SDL_GetError()}");
        }
    }

    #endregion

    #region Load Image

    private readonly Stopwatch _loadingTimer = new();

    private void LoadImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        _loadingTimer.Restart();

        _ = _composite.LoadImageAsync(imagePath, _renderer);
    }

    #endregion

    #region Main Loop

    public void Run()
    {
        while (_running)
        {
            HandleEvents();
            Render();
        }
    }

    private void HandleEvents()
    {
        while (SDL_PollEvent(out var e) != 0)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_KEYDOWN when _scanActions.TryGetValue(e.key.keysym.scancode, out var scanAction):
                    scanAction.Invoke();
                    break;

                case SDL_EventType.SDL_DROPBEGIN:
                    Logger.Log("[Core] File drop started.");
                    break;

                case SDL_EventType.SDL_DROPFILE:
                    var droppedFile = Marshal.PtrToStringUTF8(e.drop.file);
                    if (!string.IsNullOrEmpty(droppedFile))
                    {
                        Logger.Log($"[Core] File dropped: {droppedFile}");
                        DirectoryNavigator.SearchImages(droppedFile);
                        LoadImage(DirectoryNavigator.Current());
                    }
                    else
                    {
                        Logger.Log("[Core] File drop failed.", Logger.LogLevel.Warn);
                    }

                    SDL_free(e.drop.file);
                    break;

                case SDL_EventType.SDL_DROPCOMPLETE:
                    Logger.Log("[Core] File drop completed.");
                    break;

                case SDL_EventType.SDL_QUIT:
                    ExitApplication();
                    break;
            }
        }
    }

    private void ExitApplication()
    {
        LoadTimeEstimator.SaveTimeDataToFile();
        _running = false;
    }

    public void Dispose()
    {
        Logger.Log("[Core] Disposing...");

        _composite.Dispose();

        if (_font16 != IntPtr.Zero)
            SDL_ttf.TTF_CloseFont(_font16);
        if (_renderer != IntPtr.Zero)
            SDL_DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero)
            SDL_DestroyWindow(_window);

        SDL_Quit();
        GC.SuppressFinalize(this);
    }

    #endregion

    private delegate void SdlFreeDelegate(IntPtr mem);

    private static readonly SdlFreeDelegate SDL_free = LoadSdlFunction<SdlFreeDelegate>("SDL_free");

    private static TDelegate LoadSdlFunction<TDelegate>(string functionName) where TDelegate : Delegate
    {
        var libHandle = NativeLibraryLoader.Resolve("SDL2");
        var functionPtr = NativeLibrary.GetExport(libHandle, functionName);
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPtr);
    }
}