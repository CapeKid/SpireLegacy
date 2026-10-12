# Co-op heirs

Every player must install the same Spire Legacy version and BaseLib version, and use the same Slay the Spire 2 game version. The mod supports regular and public-beta, but a regular game cannot join a beta game with a different native version. Native compatibility checks remain enabled.

Host or join through the game's normal co-op menu. Select The Heir and open **Family Manor & Heir Lab** to choose your class and traits and confirm manor purchases. Steam Deck uses the existing Y shortcut or D-pad navigation to the manor button, A to select, B to close, and the shoulder buttons to scroll. Finish your selection before pressing Ready. Unready before changing it.

Ready freezes each player's heir, manor bonuses, banner, motto and Chronicle relic unlocks. Everyone receives those snapshots before embark. Multiple players may choose The Heir with different classes or traits, and Heirs may play alongside native characters. Co-op always uses the native opening flow; solo's first-generation quick start does not skip a co-op room.

Cards, inherited effects, class resources, relic eligibility, dialogue and sprites follow their owning player. A defeated co-op heir still receives the native one-HP revival, including Hero Complex; that required revival is separate from ordinary healing.

Each player's crown bank and actual manor levels stay on their own device. A completed, defeated or abandoned climb settles only that player's matching local active family, once. Purchases and heir changes are locked during the climb; the manor can be browsed without pausing networking. Co-op does not pool crowns or copy another player's family progression.

Use the native save/quit and multiplayer Continue lobby to resume with the same players. The run save carries every heir's frozen effects, including reconnect synchronization. Family progression is stored separately in `SpireLegacy2/family.json` (or your configured `HEIR_DATA_DIR`). If continuing on another device, bring your own family save and backup as well as the game's save. A run whose family ID does not match the local family retains its gameplay effects but cannot credit or overwrite that device's crown bank.

An unfinished local family climb must be resumed or abandoned before starting another Heir co-op run. If a lobby cannot synchronize heir selections, unready and check everyone's mod/version, then ready again.

Version 1.0.29 uses co-op protocol 2. Lobby changes invalidate old selections, unready revokes the frozen snapshot, and native Random must synchronize an heir snapshot before embark. Older co-op protocol versions cannot supply compatible selections.

Owned-game validation uses two, three and four independent ENet peers and disposable saves on each supported branch. See [the co-op audit](adversarial-coop.md) for ownership, malformed-packet and save/rejoin evidence and explicit gaps. Physical Deck/Linux and Deck-to-PC Steam transport remain device playtests.
