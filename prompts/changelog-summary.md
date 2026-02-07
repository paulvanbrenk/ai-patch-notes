# Changelog Summary Prompt

Use this prompt to generate concise, developer-friendly summaries of changelogs.

## Usage

Replace `[CHANGELOG CONTENT]` with the raw changelog text.

## Prompt

```
You are a senior developer writing release notes for your team. Summarize this changelog in under 150 words.

Rules:
- Lead with what matters most to users, not internal changes
- Breaking changes first, always
- Skip internal refactoring unless it changes behavior users will notice
- No version tags like "(beta.12)"
- Combine related items
- Be direct, no filler phrases
- If mentioning a tool/concept that might be unfamiliar, add a 3-5 word clarification in parentheses
- Never repeat information between sections

Format:
## TL;DR
[Answer: "What's the biggest change and why should I care?" in one sentence. Be specific - mention the main technology or feature by name.]

## Breaking
[Bullet list with brief parenthetical explanations, or "None"]

## New
[2-3 most impactful features max - skip if already in TL;DR]

## Fixes Worth Knowing
[Only fixes affecting typical daily usage - skip if unsure]

## Before You Upgrade
[1-2 specific action steps, not vague warnings]

---

CHANGELOG:

[CHANGELOG CONTENT]

---

Summarize:
```

## Example Output

For Vite 8.0.0-beta:

> ## TL;DR
> Rolldown (Rust-based build bundler) beta sheds legacy behavior so builds are more predictable.
>
> ## Breaking
> - Removed import.meta.hot.accept fallback (hot module reload API) and inlineDynamicImport (legacy dynamic import option).
> - Removed experimental.enableNativePlugin resolver option (native plugin toggle).
>
> ## New
> - Manifest now lists CSS entry-point assets (build output map).
> - Hooks are now separate per environment (plugin lifecycle callbacks).
>
> ## Fixes Worth Knowing
> - CSS asset paths and relative `new URL` mapping now resolve correctly (relative asset URLs).
>
> ## Before You Upgrade
> - Update HMR accept calls to explicit module paths.
> - Remove `inlineDynamicImport` and `experimental.enableNativePlugin` from config.
