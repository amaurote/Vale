# Vale Viewer #

A fast, GPU-accelerated image viewer built with SDL2.
<br/>

### File types currently supported: ###

BMP, JPG, PNG, TGA, TIFF, WEBP, HEIC, HEIF, AVIF
<br/>

### Keybindings & controls ###

| Key            | Action                              |
|----------------|-------------------------------------|
| `←` / `→`      | Previous / Next image               |
| `Home` / `End` | First / Last image                  |
| `+` / `-`      | Zoom in / Zoom out                  |
| `0`            | Fit to screen / Original image size |
| `F`            | Toggle fullscreen                   |
| `B`            | Toggle background                   |
| `I`            | Toggle info                         |
| `Esc`          | Exit application                    |

- Possible to drag & drop file or directory.

### Prerequisites & dependencies:

- .NET runtime (version 9.0)
- SDL2-CS.NetCore - https://github.com/flibitijibibo/SDL2-CS
- SixLabors.ImageSharp - https://github.com/SixLabors/ImageSharp
- LibHeifSharp - https://github.com/0xC0000054/libheif-sharp
- MetadataExtractor - https://github.com/drewnoakes/metadata-extractor-dotnet

### Vale Viewer uses the following third-party libraries:

- SDL2 (zlib License) - https://github.com/libsdl-org/SDL
- SDL2_ttf (zlib License) - https://github.com/libsdl-org/SDL_ttf
- ImageSharp (Apache 2.0 License) - https://github.com/SixLabors/ImageSharp
- LibHeif (LGPL-3.0) - https://github.com/strukturag/libheif

### Future challenges: ###

* Enable pan when image rectangle is bigger than the window
* Introduce DI for choosing from multiple image decoders (and do NOT break memory management)
