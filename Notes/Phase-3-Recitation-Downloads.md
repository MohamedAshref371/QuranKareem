# Phase 3: recitation downloads

The download window owns one queue and transfers one recording at a time. The user can move a waiting file to the front, remove waiting files, or cancel the current transfer. The queue shows progress and counts of completed, failed, and canceled files. Closing the download window hides it while work continues. Confirming exit from the main window cancels downloads and waits for cleanup before closing.

Each transfer writes a temporary file beside the destination. Before moving or replacing the recording, it checks that the response is complete, compares the byte count when the server provides one, and checks for an audio frame in MP3 files. Failures and cancellations clean up the temporary file and leave an existing recording intact. A response that stops sending data times out after 30 seconds of inactivity.

The Debug build and console checks passed. The checks used simulated HTTP responses to verify new downloads, successful replacements, failed and canceled replacements, cleanup, and the WinForms queue continuing after a failed file. No live recitation server was contacted. See [the navigation notes](Phase-1-Navigation.md) for the shared build and test commands.
