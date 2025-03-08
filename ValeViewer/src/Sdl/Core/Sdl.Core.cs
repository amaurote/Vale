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

    private IntPtr _defaultCursor;
    private IntPtr _handCursor;

    private readonly ImageLoader.ImageLoader _imageLoader;
    private ImageComposite _composite = new();

    private bool _running = true;

    #region Initialize

    public SdlCore(string? imagePath, bool startInFullscreen)
    {
        // TODO known issue: starting the application in full screen doesn't work well.
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
        LoadCursor();
        
        stopwatch.Stop();
        Logger.Log($"[Core] Startup time: {stopwatch.ElapsedMilliseconds} ms");
        
        _imageLoader = new ImageLoader.ImageLoader(_renderer);
        
        if (imagePath != null)
        {
            DirectoryNavigator.SearchImages(imagePath);
            LoadImage(true);
        }
    }

    private void LoadFont()
    {
        _font16 = SDL_ttf.TTF_OpenFont(TtfLoader.GetMonospaceFontPath(), 16);
        if (_font16 == IntPtr.Zero)
        {
            throw new Exception($"[Core] Failed to load font: {SDL_GetError()}");
        }
    }

    private void LoadCursor()
    {
        _handCursor = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
        _defaultCursor = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);

        if (_handCursor == IntPtr.Zero || _defaultCursor == IntPtr.Zero)
        {
            throw new Exception($"[Core] Failed to load cursors: {SDL_GetError()}");
        }
    }

    #endregion

    #region Load Image

    private void OnDropFile(SDL_Event e)
    {
        var droppedFile = Marshal.PtrToStringUTF8(e.drop.file);
        if (!string.IsNullOrEmpty(droppedFile))
        {
            Logger.Log($"[Events] File dropped: {droppedFile}");
            DirectoryNavigator.SearchImages(droppedFile);
            LoadImage(true);
        }
        else
        {
            Logger.Log("[Events] File drop failed.", Logger.LogLevel.Warn);
        }

        SDL_free(e.drop.file);
    }
    
    private void LoadImage(bool synchronously = false)
    {
        if (synchronously)
            _composite = _imageLoader.GetImageSynchronously() ?? new ImageComposite();
        else
            _composite = _imageLoader.GetImage() ?? new ImageComposite();

        _imageLoader.UpdateCollection();
        Task.Run(() => _imageLoader.Preload());
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
    
    private void ExitApplication()
    {
        LoadTimeEstimator.SaveTimeDataToFile();
        _running = false;
    }

    public void Dispose()
    {
        Logger.Log("[Core] Disposing...");

        _imageLoader.DisposeAll();
        
        if (_font16 != IntPtr.Zero)
            SDL_ttf.TTF_CloseFont(_font16);
        
        if (_handCursor != IntPtr.Zero) 
            SDL_FreeCursor(_handCursor);
        
        if (_defaultCursor != IntPtr.Zero) 
            SDL_FreeCursor(_defaultCursor);
        
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