# Combat UI font

`UnitySkillsCN-Regular.ttf` is an unchanged copy of the OFL-licensed font already
bundled by `Assets/Plugin3rd/com.besty.unity-skills/Editor/UI/Fonts`.
It is placed outside `Editor` so the combat UI can render Chinese in players.

- Derived from Maple Mono CN v7.9, Copyright 2022 The Maple Mono Project Authors.
- Upstream: https://github.com/subframe7536/maple-font
- CN glyph base: Resource Han Rounded (https://github.com/CyanoHao/Resource-Han-Rounded).
- License: SIL Open Font License 1.1; the complete notice is in `OFL.txt`.
- Upstream modifications: GB2312/UI subset, removed coding ligatures, renamed
  internal family to UnitySkills CJK. This project makes no further TTF changes.
- `Settings/CombatUIFont.asset` is a static SDF atlas derived from this font,
  covering the V1 HUD text. The font and derived atlas remain under OFL 1.1.

Ship this notice and OFL.txt with distributed builds. No system-installed font
or third-party editor assembly is required by the runtime UI.
