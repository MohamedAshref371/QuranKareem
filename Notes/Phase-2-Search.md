# Phase 2: Quran search

Search loads and normalizes the bundled verses on a background thread, then reuses that data for later searches. The search box accepts Enter and ignores surrounding spaces. Each result shows the surah name, ayah number, and a preview. Selecting it sends the full destination to the reading navigator in one step. Canceling a search or closing the results clears the active request so its results cannot overwrite a newer search.

The Debug build and the existing console checks passed against the bundled Hafs text database. The checks cover exact and spelling-tolerant searches, surrounding spaces, empty input, cancellation, and reading navigation. Five warm searches for `الله` had a median of 1 ms in each mode in the console runner; this does not include opening the application or displaying results.

In the running picture mushaf, Enter opened the results from the search field. A search for `قل هو الله أحد` displayed `الإخلاص (1): قل هو الله أحد`. After selection, the reading controls showed surah 112, ayah 1, page 604. Cancellation was checked in the runner; the search completed too quickly to verify canceling an in-progress search through the window.

See [the navigation notes](Phase-1-Navigation.md) for the build and test commands.
