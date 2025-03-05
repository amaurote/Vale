# Vale Viewer #

A fast, GPU-accelerated image viewer built with SDL2.  
It focuses on **performance, minimalism, and smooth navigation**, making it ideal for quickly browsing large image collections.
<br/>

## Supported File Formats

Vale Viewer currently supports the following image formats:

- **BMP**
- **JPG / JPEG**
- **PNG**
- **TGA**
- **TIFF**
- **WEBP**
- **HEIC / HEIF**
- **AVIF**

## Keybindings & Controls

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

- **Drag & Drop:** Open a file or directory by dragging it into Vale Viewer.

## Prerequisites & Dependencies

**Vale Viewer** uses **.NET Runtime** (version **9.0**) and it also integrates several open-source libraries:

| Library                   | Purpose                                 | License     |
|---------------------------|-----------------------------------------|------------|
| **SixLabors.ImageSharp**  | Advanced image processing               | Apache 2.0 |
| **LibHeifSharp**          | HEIF/HEIC support                       | LGPL-3.0   |
| **MetadataExtractor**     | Extracts EXIF & metadata from images    | Apache 2.0 |
| **SDL2**                  | Core graphics & input library           | zlib       |
| **SDL2_ttf**              | TrueType font rendering                 | zlib       |
| **LibHeif**               | HEIF/HEIC format decoding               | LGPL-3.0   |

For more details, visit the respective **GitHub repositories**:

- [SDL2](https://github.com/libsdl-org/SDL)
- [SDL2_ttf](https://github.com/libsdl-org/SDL_ttf)
- [SDL2-CS](https://github.com/flibitijibibo/SDL2-CS)
- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
- [LibHeifSharp](https://github.com/0xC0000054/libheif-sharp)
- [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet)

## Future Challenges

Planned improvements and new features for **Vale Viewer**:

### **UI & Usability**
- **Window Resize Support**  
  Allow dynamic resizing of the application window.
- **Panning Support**  
  Enable **image panning** when the image is larger than the window.
- **Mouse Wheel Zoom**  
  Enable zooming in and out, centered around the cursor position.

### **Image Format Support**
- **Additional Formats**  
  Investigate **EXR, HDR, SVG, ICO** and other formats.
- **Animated Format Support**  
  Add support for **GIF & APNG**.

### **Backend & Architecture**
- **Preload**  
  Implement preloading of adjacent images to improve navigation speed.  
  Determine how many images to preload, the optimal strategy (by order, file size / expected load time), and an efficient disposal.
- **Dependency Injection for Decoders**  
  Introduce **DI (Dependency Injection)** for multiple image decoders  
  _(without breaking memory management)_.

---