# Phase 7: recitation highlight timing

One WinForms timer samples the Windows Media Player position while playback is active. The reading view advances when that position reaches the ayah boundary and changes the word highlight only when the active word changes. A gap between timed words removes the word color and keeps the ayah color. A seek waits for the player to be ready; pause stops updates and resume keeps the playback position. The timer uses a 20 ms interval, but Windows message scheduling can delay updates.

## Before and after

The optional playback check hosts the same player control as the app. It generates a silent eight-second WAV and a matching database with a boundary at 4,000 ms and ten word intervals. It compares each notification with the position reported by the player. Negative values mean the update came before its boundary. These readings measure player synchronization, rather than speaker or display latency.

| Speed | Ayah offset before | Ayah offset after | Word transitions before → after |
| --- | ---: | ---: | ---: |
| 0.6× | −306.3 ms | +25.1 ms | 9/9 → 9/9 |
| 1.0× | −636.9 ms | +23.3 ms | 8/9 → 9/9 |
| 1.5× | −778.8 ms | +18.2 ms | 7/9 → 9/9 |

The first word is already active at playback start; nine transitions are expected after it. The latest observed word transition after the change was 46.8 ms past its boundary. The transport check changes speed during playback, pauses and resumes, verifies a gap, seeks forward and backward, jumps to an ayah with no word timings, crosses a surah boundary, and stops. The bundled picture checks separately compare pixels to confirm that clearing a word restores the ayah color in both light and dark modes.

Build as described in [the navigation notes](Phase-1-Navigation.md). With the navigation test executable and its dependencies available on a Windows desktop, run each speed in a separate process with a fresh fixture folder:

```powershell
& $quranTests --audio-playback-verify $quranFreshFolder 1.0
& $quranTests --audio-transport-check $quranOtherFreshFolder
```
