# Phase 5: picture page turns

The picture reader reuses its pixel buffers when the next page has the same size and format. It releases both old bitmaps after the WinForms view switches to the new image. Each turn also shares one SQLite connection across the navigation and coloring queries.

Release build, `Quran-CD-Roaya-5`, 240 turns per process after warming pages 1–16. The sequence was pages 20–179, then 178–99. Before and after runs alternated on the same machine. Time measures `PictureQuran.Set`, including page preparation and coloring, without waiting for an on-screen repaint. Private memory was sampled every five turns. Values in the summary are the median of the three runs.

| Measure | Before | After |
| --- | ---: | ---: |
| Median turn | 23.50 ms | 15.06 ms |
| 95th percentile turn | 25.24 ms | 16.05 ms |
| Peak sampled private memory | 57.4 MiB | 43.1 MiB |
| Generation 2 garbage collections | 159 | 0 |

All runs produced identical before and after pixel hashes for the clean page, highlighted verse, and first two highlighted words of 2:255 in day and night modes. The checks also changed pages and returned to the same verse. A WinForms test verified that the view could paint the replacement image and that both superseded bitmaps were disposed after the swap.

Build the test runner in Release, then pass `--picture-pages` and the path to the picture edition to repeat the measurement. The normal runner also checks highlighting and bitmap ownership.
