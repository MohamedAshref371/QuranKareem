# Phase 8: video line image export

Picture export reads line rectangles and word marks in two queries for the page. It crops each requested line once and releases the crops and temporary images after rendering. Font export finds the target line by word, measures each line once, and reuses its glyph positions. The color font path transfers pixels directly from Skia to the video canvas, avoiding an intermediate PNG and repeated per-pixel GDI calls in dark mode.

The export form prepares the page data before starting background rendering. It keeps the video settings fixed until the image list is ready and checks that the list still has one frame per audio interval before writing the project. A pause keeps the current line with its base colors.

All paths reuse a transparent canvas and write full-size RGBA images. Only the area covered by the rendered line is read back from the canvas; the remaining pixels are transparent. The encoder uses .NET's DeflateStream and the [PNG container and zlib layout](https://www.w3.org/TR/png-3/#10Compression). The faster compression can increase file size without changing decoded pixels.

The optional benchmark uses bundled picture page 30 (158 frames) and a 12-line fixture with the bundled font (147 frames per font path), all at 1920×1080 in dark mode. The fixture includes repeated words and pauses. Compare the generated images in order using decoded BGRA frame hashes. The regular test suite also checks that the writer preserves transparency and clips a line extending beyond the canvas.

| Path | Seconds before → after | Peak working set before → after | Output size before → after | Pixel matches |
| --- | ---: | ---: | ---: | ---: |
| Picture | 15.36 → 11.02 | 554 → 73 MiB | 23.32 → 29.15 MiB | 158/158 |
| GDI font | 9.72 → 7.34 | 69 → 70 MiB | 4.89 → 10.10 MiB | 147/147 |
| Skia font | 108.26 → 8.15 | 93 → 79 MiB | 6.33 → 11.14 MiB | 147/147 |

An independent FFmpeg decode matched every frame in sequence, including alpha. The measurement includes image generation and file writes, and excludes setup of the fixture and the subsequent pixel comparison.

```powershell
& $quranTests --line-export-check $quranAssets $quranPictureOutput picture dark
& $quranTests --line-export-check $quranAssets $quranFontOutput gdi dark
& $quranTests --line-export-check $quranAssets $quranColorFontOutput skia dark
```
