# Vale Viewer #

A fast, GPU-accelerated image viewer built with SDL2.
<br/>

### File types currently supported: ###
BMP, JPG, PNG, TGA, TIFF, WEBP, HEIC, HEIF, AVIF
<p>(.heic, .heif, .avif files need to be tested with an extensive batch of images!)
<br/>

### Keybindings ###

| Key       | Action                              |
|-----------|-------------------------------------|
| `←` / `→` | Previous / Next image               |
| `+` / `-` | Zoom in / Zoom out                  |
| `0`       | Fit to screen / Original image size |
| `F`       | Toggle fullscreen                   |
| `B`       | Toggle background                   |
| `I`       | Toggle info (not implemented yet)   |
| `Esc`     | Exit application                    |

### Prerequisites & dependencies:
- .NET runtime (version 9.0)
- SDL2-CS.NetCore ([github](https://github.com/flibitijibibo/SDL2-CS))
- SixLabors.ImageSharp ([github](https://github.com/SixLabors/ImageSharp))
- LibHeifSharp ([github](https://github.com/0xC0000054/libheif-sharp))

### Future challenges: ###
* Enable pan when image rectangle is bigger than the window
* Process and display image metadata / EXIF
* Introduce DI for choosing from multiple image decoders (and do NOT break memory management)