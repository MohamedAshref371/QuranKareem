# Phase 9: tafseer and translations

Each source in the dropdown carries its own path. The saved preference can distinguish files with the same display name, and older preferences saved by name still load. Each query opens a read-only connection and captures its source and verse before starting. Preview and copy have separate request slots. Navigating to a different verse, changing source, or closing the reader invalidates both slots. A result or an error from an invalidated request cannot update the interface or the clipboard.

Quick read and the picture view's right click share the same nonmodal preview. The title identifies the source, surah and ayah. A displayed preview closes when the verse or source changes, so it can never be mistaken for the new selection. The copy button sends the source text as Unicode and leaves the clipboard alone when there is no text.

SRT takes a snapshot of the reciter's start and end times and joins the selected text by ayah number. A missing translation does not shift the subsequent captions. The full surah keeps the original timeline; current-page export is relative to the first timed ayah on that page. An export with no matching timed text does not write an empty file.

The navigation suite reads a sample from all 17 bundled sources, runs concurrent Arabic and English lookups, and deliberately completes old preview and copy requests after newer ones. It covers full-surah and page SRT, missing rows, and exact cue boundaries from a copy of the bundled timing database. FFprobe independently read the exported sample and confirmed cues at 0.000–1.234 and 1.234–2.450 seconds. The optional `--tafseer-ui-check` takes the same Quran database argument and uses the real WinForms controls with isolated settings and a stand-in clipboard.
