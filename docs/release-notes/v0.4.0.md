# KeyFix 0.4.0

KeyFix 0.4.0 makes automatic correction of the previously mistyped word actually work, and
greatly improves how many words are recognized.

## What's New

- **Auto-correction now works.** A bug in the `SendInput` interop (the `INPUT` struct was the
  wrong size on 64-bit Windows) meant every key injection silently failed, so KeyFix only
  switched the language and never rewrote the word. That is fixed: type `hello` while the
  Persian layout is active (you get `اثممخ`), press Space, and it becomes `hello`.
- **Fast typing is handled.** The whole correction is sent as one atomic batch and runs on a
  background thread, so it no longer garbles text or drops characters when you type quickly.
- **Real dictionaries.** KeyFix now ships with about 6000 of the most common words for each of
  Persian, English, German, and Arabic and uses them to decide whether a word was typed in the
  wrong layout, so it recognizes far more everyday words.
- **Safer.** It never rewrites a word that is already valid in the active language, and it now
  handles three-letter words as well.

## How To Use

1. Open KeyFix from the Windows tray and go to **Settings**.
2. Enable only the languages you actually use (for example, English and Persian).
3. Keep the mode on **AutoSwitch** with **Correct mistyped word automatically** checked.
4. Type normally. When KeyFix detects a wrong-layout word, after Space it fixes the word and
   switches the language.

## Download

```text
KeyFixSetup-0.4.0.exe
KeyFixSetup-0.4.0.exe.sha256
```

## Privacy

KeyFix is local-only. It does not save typed text, upload typed text, or use telemetry. The
default correction path does not use the clipboard.
