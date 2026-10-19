# Code and content conventions

## Localization is multi-surface

When changing user-facing text, update every linked source together:

- `ModuleData\module_strings.xml`
- English `Languages\std_*.xml`
- Chinese `Languages\CNs\*.xml`
- French `Languages\FR\std_*.xml`

Storefront and documentation text is maintained separately from in-game localization:

- `README.md`, `README.zh-CN.md`
- `CHANGELOG.md`, `CHANGELOG-zh-CN.md`
- `MountBladeComCnDescription.txt` (Mount & Blade Chinese site description)
- `SteamWorkshopReadMe*.txt`

Reuse existing string IDs and naming style. A new option normally needs all of:

1. a field in `...Config.cs`;
2. an entry in `...OptionClassFactory.cs`;
3. a `module_strings.xml` entry;
4. English, Chinese, and French translations.

Rename string IDs only when the user asks; IDs are referenced from code, so rename code
references and every locale entry in the same change.

## Patch conventions

- Match the native behavior you replace. When copying native logic into a patch, diff it
  against the decompiled source of the branch's target game version.
- Prefer the narrowest patch kind that works. Full-method replacements and broad prefixes
  are used deliberately in places (order placement/queue behavior); keep them, but do not
  add new ones without explaining why a prefix/postfix/transpiler cannot work.
- Transpilers must anchor on semantic patterns and fail fast (throw) when the native IL
  shape changes, instead of silently producing wrong IL.
- Reflection on hot paths (per-frame, per-marker, per-agent) should use cached `MethodInfo`
  or compiled delegates created during patch installation, not repeated `GetProperty`/
  `Invoke` calls.
- Optional-mod compatibility must never turn into a hard dependency. Detect the module,
  gate registration, and access DLC members through reflection.

## Version and branch conventions

- `master`, `beta`, and `v1.3.15` are independent release lines; never merge them wholesale.
  Port individual fixes and re-validate per version.
- Never place branch-specific notes in another branch's changelog.
- Keep feature/fix commits separate from version/changelog commits.
- Changelog wording is intentionally synchronized between `CHANGELOG.md`,
  `CHANGELOG-zh-CN.md`, and `MountBladeComCnDescription.txt`. In the Chinese site
  description, shared notes use merged headings such as `v5.4.16 / v5.3.38`; branch-only
  items are prefixed inline, for example `(v5.4.16)`.

## Repository state is part of the work

Docs, changelogs, Workshop text, module versions, and `.github\resources\config.env` are
tracked project state. Updating them is normal release work, not incidental noise.

## Known gotchas (learned in previous sessions)

- `CHANGELOG.md` may already contain an unreleased heading. Append new notes under the
  existing heading instead of opening a new version unless the user asks for a new release.
- Verify the changelog covers every gameplay commit since the last release tag
  (`git log --oneline <last-release-tag>..HEAD`); missing entries have been caught by the
  user before.
- Cherry-picking between branches frequently conflicts in `MountBladeComCnDescription.txt`
  and `CHANGELOG*.md`; resolve the merged changelog section manually and keep both branches'
  wording identical where the notes are shared.
- `source/library/mission-library` shows as modified after branch switches. Commit the
  submodule change first, then the parent pointer.
- Unity of the two modules matters: an option added to one module's `module_strings.xml`
  still needs the matching language files for that module only.
