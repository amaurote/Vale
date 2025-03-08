# **Vale Viewer**  
*A fast, GPU-accelerated image viewer built with SDL2.*

Vale Viewer is designed for **performance, minimalism, and smooth navigation**, making it ideal for quickly browsing large image collections.

---

## **Who is it for?**

Vale Viewer is designed for users who rely on images as a core resource in their creative workflow. Whether you’re a digital artist, graphic designer, photographer, or developer working with image assets, Vale Viewer provides a fast, distraction-free way to preview images.

Unlike general-purpose image viewers, Vale Viewer prioritizes speed, minimalism, and features that directly benefit users who treat images as creative building blocks rather than just media to consume.

---

## **Supported File Formats**  

Vale Viewer currently supports the following image formats:

- **BMP**
- **JPG / JPEG**
- **PNG**
- **TGA**
- **TIFF**
- **WEBP**
- **HEIC / HEIF**
- **AVIF**

---

## **Keyboard Shortcuts & Controls**  

| Key            | Action                                       |
|----------------|----------------------------------------------|
| `←` / `→`      | Previous / Next image                        |
| `Home` / `End` | First / Last image                           |
| `+` / `-`      | Zoom in / Zoom out                           |
| `Mouse Wheel`  | Zoom at cursor position                      |
| `0`            | Toggle **Fit to Screen** / **Original Size** |
| `F`            | Toggle fullscreen                            |
| `B`            | Toggle background mode                       |
| `I`            | Toggle image information overlay             |
| `Esc`          | Exit application                             |

- **Drag & Drop:** Open a file or directory by dragging it into Vale Viewer.

---

## **Prerequisites & Dependencies**

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

---

## Future Challenges

Planned improvements and new features for **Vale Viewer**:

### **Image Format Support**
- **Additional Formats**  
  Investigate **EXR, HDR, SVG, ICO** and other formats.
- **Animated Format Support**  
  Add support for **GIF & APNG**.

### **Backend & Architecture**
- **Dependency Injection for Decoders**  
  Introduce **DI (Dependency Injection)** for multiple image decoders  
  _(without breaking memory management)_.

---