# Phase 4: Quran classes

Saving a reciter description now binds the extension and comment as text and updates only the audio description record. The form reports a save failure and keeps the edited text available. The save no longer compacts the database or changes verse timestamps.

`VisualQuranNavigation` resolves the target verse for the text, picture, and font readers. Each reader still draws the page and highlights the verse itself. The shared lookup preserves the existing rules for surah and page boundaries, including the ayah-zero basmalah entries and wraparound at the end.

The console runner covers navigation with the bundled text and picture editions, navigation and word selection with a two-page font fixture, and description round-trips with Arabic text, quotes, and line breaks on a copy of the bundled audio database. It also compares the timestamps before and after saving a description. See [the navigation notes](Phase-1-Navigation.md) for the shared build and test commands.
