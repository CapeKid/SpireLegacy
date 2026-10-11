# Heir Ancient conversations

Neow, Darv, Tezcatara, Orobas, Pael, Nonupeipe, Tanx and Vakuu each have original four-line conversations for Knight, Mage and Ranger, with first-visit and returning-family variations: 48 conversations in total.

The active heir determines the class response. The game's shared Heir visit counter selects first versus return; returning lines address the family rather than claiming a new heir personally remembers an earlier visit. Any later visit can use the returning conversation, including old saves with large visit counts. A future unsupported Ancient receives original family fallback text.

Only the Heir's dialogue selection changes. Native speaker icons, response buttons, controller navigation and reward callbacks remain in use. Architect victory conversations and native characters' dialogue remain unchanged. English fallback text is merged under `SPIRELEGACY.ANCIENT` in the active locale; other localization keys are preserved. No borrowed character voice clips are assigned to these original lines.

`sheets/ancient_dialogue.json` is the editable source; `scripts/author-heir-ancient-dialogue.py` reproduces the original authoring. Core checks validate every class/visit, speaker order, readable line lengths, distinct conversations and future fallback. `scripts/sts2-ancient-dialogue-check.py` verifies the native dialogue selector across visit counts and actual first/return event screens, including controller release callbacks and reward-button focus on both branches. Physical Steam Deck readability/controller acceptance remains pending.
