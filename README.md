# Vale Viewer #

A fast, GPU-accelerated image viewer built with SDL2.
<br/>

### File types currently supported: ###
BMP, JPG, PNG, WEBP
<br/>

### Keybindings ###

| Key       | Action                              |
|-----------|-------------------------------------|
| `←` / `→` | Previous / Next image               |
| `+` / `-` | Zoom in / Zoom out                  |
| `0`       | Fit to screen / Original image size |
| `I`       | Toggle info (not implemented yet)   |
| `F`       | Toggle fullscreen                   |
| `Esc`     | Exit application                    |

### Future challenges: ###
* Enable pan when image rectangle is bigger than the window
* Process and display image metadata / EXIF
* Introduce DI for choosing from multiple image decoders (and do NOT break memory management)
* Implement HEIC decoder
