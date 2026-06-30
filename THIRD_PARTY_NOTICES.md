# Third-Party Notices

KeyFix includes the following third-party data.

## Word frequency lists

The embedded word lists in `src/KeyboardLanguageGuard.Core/Resources/` (the most common
words for Persian, English, German, and Arabic) are derived from the **FrequencyWords**
project, which is based on OpenSubtitles word frequency data. The lists are built by
merging the 2016 and 2018 OpenSubtitles corpora and filtering to the top ~30,000 words
per language (English, German, Arabic) and ~26,000 for Persian.

- Source: https://github.com/hermitdave/FrequencyWords
- License: Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)
  https://creativecommons.org/licenses/by-sa/4.0/

The lists were filtered (kept only words of each language's script, normalized, and limited
to the most frequent entries) before being embedded.
