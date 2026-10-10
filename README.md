# Spire Legacy — Slay the Spire 2

Every climb ends. Your bloodline grows.

A solo custom character by **CapeKid** for the regular and public-beta branches of **Slay the Spire 2**. Knight, Mage and Ranger heirs inherit traits and altered starters; crowns from each climb rebuild a permanent Family Manor.

- **82 reward cards per class on regular, 85 on public-beta**, plus three basics. Each includes 34 shared family cards alongside class specialties. All 360 historical definitions, saved IDs and original illustrations are retained.
- All 61 eligible Rogue Legacy 2 traits, compatible inheritance, three new heirs after each climb and the first family's quick start.
- Six manor upgrades, family naming, visible banner emblems, full trait descriptions, confirmed purchases and a temporary crown editor.
- Native card, reward, shop and save integration; atomic family saves and backup recovery.
- Original trait icons, class badges, portraits, sprites and card art included in the mod. No game assets or extraction tools are distributed; Rogue Legacy 2 is not required.
- Synesthesia's animated rainbow ring and paired cosmetic accents appear in combat, rest sites and shops. See the [complete trait-effect audit](docs/trait-effect-audit.md).

Supports **Slay the Spire 2 regular v0.107.1 and public-beta v0.111.0**. Requires **BaseLib 3.4.7**. The StS1 implementation remains as historical source; active releases now target StS2.

[Install, Steam Deck, Melty, Workshop, saves and build instructions](docs/sts2-port.md)

The port is a development release. Native automated and visual checks cover card registration, upgrades, inherited save round trips, combat effects, class mechanics, rewards, shops and manor controls. Full campaign balance, physical Deck gameplay and a Melty-installed launch still require verification. The Melty package is prepared with a Windows launch mapping; the Workshop package supports native Windows/Linux.

## Source

`sts2/` contains the native C# port. Design remains in `sheets/*.json`. Generate stable models with `scripts/sts2-generate.py`, build `sts2/SpireLegacy.csproj` and package with `scripts/sts2-package.py`. Your owned StS2 assemblies are local build dependencies and are not in Git. Original SVG trait/class icons are reproducible with `scripts/author-heir-icons.py`; see [icon provenance](docs/original-icons.md).

The active pools contain 187 distinct cards on regular and 196 on public-beta; the other historical definitions remain available to native model lookup and compatibility tools. [Pool guide](docs/card-pools.md), [card catalogue](docs/card-catalogue.md), [class mechanics and starter genetics](docs/classes-and-genes.md), [trait adaptations](docs/traits.md) and [manor balance](docs/manor-and-balance.md) describe the shared design. Their historical StS1 test results do not substitute for StS2 validation.

All 360 original illustrations and their generation records are preserved. Review sheets: [Knight](media/card-art-knight.png), [Mage](media/card-art-mage.png), [Ranger](media/card-art-ranger.png).

Private game files, extracted content, logs, test saves and release archives are ignored. Never distribute owned DLLs/PCKs or extracted RL2 images. The old StS1 Workshop item remains separate from the StS2 port.

## License and credits

Original mod code and illustrations: **CapeKid, MIT**. Remixes with attribution are allowed. Slay the Spire 2 belongs to Mega Crit; Rogue Legacy 2 belongs to Cellar Door Games. The standalone StS2 package contains original mod assets; neither game's extracted assets are distributed.

BaseLib: Alchyr and contributors, MIT. universal-modder was used for development. Content-reader code is retained only as historical StS1 source; active StS2 packages do not include it. Historical StS1 source credits ModTheSpire and BaseMod; neither is required by the StS2 mod.
