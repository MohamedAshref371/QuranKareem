# Phase 1: reading navigation extraction

The first implementation step keeps the current WinForms application and bundled Quran data. `ReadingNavigator` now owns the commands for moving to a surah, ayah, page, quarter, juz, hizb, or word. It returns an immutable `ReadingPosition` snapshot. `Form1` applies that snapshot to its controls in one place and suppresses reentrant control events while doing so. Audio-driven navigation updates the reading view without seeking the player again.

The navigation test project exercises the real bundled text Quran database, including a surah boundary, the final page and quarter, and juz/hizb conversion. It is a small console test runner so it requires no new NuGet test framework.

## Run the checks

From the repository root in PowerShell:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe' QuranKareem.sln /t:Build /p:Configuration=Debug /v:minimal
$db = (Get-ChildItem 'Quran Kareem/texts' -Filter '*.db' | Select-Object -First 1).FullName
& 'Tests/NavigationTests/bin/Debug/QuranKareem.NavigationTests.exe' $db
```

The build uses Visual Studio MSBuild because `dotnet msbuild` cannot resolve this project's x86 Windows Media Player COM reference in the current environment. The application builds with existing warnings. The navigation tests pass against the bundled data.

## Interactive UI check

Computer Use connected on a later attempt. Starting from surah 3, ayah 75 (page 59, quarter 24, juz 3, hizb 6), I used the visible selectors in the running WinForms application:

- Entering page 604 and committing the input displayed surah 112, ayah 0, quarter 240, juz 30, and hizb 60, with the corresponding page image.
- Entering juz 1 and committing the input displayed surah 1, ayah 1, page 1, quarter 1, and hizb 1.
- Entering surah 2 displayed its name and page 2. Entering surah 3, then ayah 75, restored the original state and page image. The derived quarter, juz, and hizb values returned to 24, 3, and 6.

These checks passed without a visible crash or conflicting final field values. Numeric text edits took effect after pressing Enter; the automation's `set_value` call itself timed out while waiting for a channel, so each result was checked from a fresh UI observation. This was a short smoke test, not a performance measurement.

Recitation advancing across a surah, switching between image and font Quran, timestamp editing after a surah change, and behavior at other display sizes remain unverified. No startup or responsiveness measurements have been made yet.
