# Trait effect audit — 1.0.19

Reviewed all 61 selectable traits and the two preserved historical IDs against StS2 runtime consumers. Crown bonuses alone do not count as an effect. Numeric stats are consumed by Inheritance and BloodlineRelic; size is applied to combat, rest and merchant sprites. Healing uses the scoped InheritedHealing patch, which preserves Neow initialization.

Fixed cosmetic suppression by the old exclusive tint chain: Synesthesia now has animated rainbow motes and a colored floor ring independent of the body shader. Palette traits have separate visible accents when paired. Grayscale and sepia remain visible alongside the accents. Rest sites and shops now use the same trait appearance and inherited size. Histrionic produces a brief damage flash and returns to its normal palette, including when paired with Colorblind.

Synesthesia remains cosmetic and has no crown bonus. This audit does not change card balance, trait crown bonuses or saved IDs. Existing nonvisual platforming adaptations remain bounded gameplay effects.

| Trait | Status | Observable effect | Runtime consumer |
|---|---|---|---|
| Colorblind (`nocolor`) | Selectable | Your heir is rendered in gray. | Native/TraitAppearance.cs: Refresh |
| C.I.P (`nohealthbar`) | Selectable | Your health is hidden. | Native/HiddenHeirHealth.cs; normal maximum HP |
| Clumsy (`easybreakables`) | Selectable | After your first Attack each turn, lose 2 Block (minimum 0). | Native/BloodlineRelic.cs: AfterCardPlayed |
| Dwarfism (`small`) | Selectable | Less health, but more Dexterity. | Inheritance stats / BloodlineRelic / HeirCharacter; HeirVisuals.Attach / AttachRoomPortrait |
| Endomorph (`playerknockedlow`) | Selectable | Gain 1 Dexterity for steadier defense. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Ectomorph (`playerknockedfar`) | Selectable | Lose 1 Dexterity; gain extra legacy crowns. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Nature (`disposition`) | Selectable | Your heir wears a green family accent. | Native/TraitAppearance.cs: Attach / Refresh |
| Gigantism (`large`) | Selectable | More health, but less Dexterity. | Inheritance stats / BloodlineRelic / HeirCharacter; HeirVisuals.Attach / AttachRoomPortrait |
| IBS (`fart`) | Selectable | Start each combat with 1 Weak. | Native/BloodlineRelic.cs: BeforeCombatStart |
| Muscle Weakness (`enemyknockedlow`) | Selectable | Lose 1 Strength. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Breaker (`breakpropsformana`) | Selectable | The first Skill played each turn draws 1 card. | Native/BloodlineRelic.cs: AfterCardPlayed |
| Fragile (`onehitdeath`) | Selectable | Maximum HP is 1, overriding all other HP bonuses. | Core/Inheritance.cs: MaxHp |
| Pacifist (`cantattack`) | Selectable | Your Basic Attack cards become Skills that apply 2 Poison (3 upgraded), with no damage or Block. Other Attacks cannot be played. | Native/BloodlineRelic.cs: ShouldPlay / Core.CardDesign.Mercy |
| Superfluid (`omnidash`) | Selectable | Gain 2 Block at the start of each turn. | Native/BloodlineRelic.cs: AfterPlayerTurnStart |
| High Jumper (`highjump`) | Selectable | Gain 2 Dexterity, but lose 8 maximum HP. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Aerodynamic (`gaindownstrike`) | Selectable | The first Attack played each turn deals 1 additional damage per hit. | Native/BloodlineRelic.cs: InheritedDamageAdditive |
| Combative (`weapon`) | Selectable | More Strength, but less health. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Bookish (`magic`) | Selectable | Draw another card each turn, but lose health. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Hypergonadism (`enemyknockedfar`) | Selectable | The first Attack each turn applies 1 Weak to each enemy it hits, before damage. | Native/BloodlineRelic.cs: BeforeDamageReceived |
| Exhausted (`longercd`) | Selectable | Lose 1 Energy on even-numbered combat turns. | Native/BloodlineRelic.cs: AfterPlayerTurnStart |
| Healthy (`health`) | Historical only | More health, but less Strength. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Strong (`strength`) | Historical only | More Strength, but less Dexterity. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Crippling Intellect (`bonusmagicstrength`) | Selectable | Draw 1 extra card each turn; lose 18 maximum HP. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Methodical (`fmffan`) | Selectable | The first Skill played each turn grants 2 Block. | Native/BloodlineRelic.cs: AfterCardPlayed |
| Unempathetic (`noenemyhealthbar`) | Selectable | Each enemy starts with 4 Block. | Native/BloodlineRelic.cs: BeforeCombatStart |
| Overcompensation (`manacostanddamageup`) | Selectable | Attacks deal 50% more damage and cost 1 extra Energy when drawn. | Native/BloodlineRelic.cs: AfterCardDrawn / InheritedDamageMultiplicative |
| Spelunker (`revealallchests`) | Selectable | Lose 6 maximum HP. Gain 10 gold when entering a treasure room. | Inheritance stats / BloodlineRelic / HeirCharacter; Native/BloodlineRelic.cs: AfterRoomEntered |
| Clownanthropy (`bounceterrain`) | Selectable | Lose 12 maximum HP. The first Attack played each turn grants 2 Block. | Inheritance stats / BloodlineRelic / HeirCharacter; Native/BloodlineRelic.cs: AfterCardPlayed |
| Charismatic (`lowerstoreprice`) | Selectable | Shop cards, relics, and potions cost 20% less. | Native/BloodlineRelic.cs: ModifyMerchantPrice |
| Histrionic (`fakeselfdamage`) | Selectable | Your heir flashes red after taking damage; HP reports stay accurate. | Native/TraitAppearance.cs: DamageFlash / Refresh |
| Nostalgic (`oldyellowtint`) | Selectable | Your heir is rendered in a nostalgic sepia palette. | Native/TraitAppearance.cs: Refresh |
| Puritan (`enemiescensored`) | Selectable | Enemies start with 5 Block, representing difficulty reading opponents. | Native/BloodlineRelic.cs: BeforeCombatStart |
| Associative Agnosia (`enemiesblackfill`) | Selectable | Enemies start with 1 Strength, representing harder-to-read opponents. | Native/BloodlineRelic.cs: BeforeCombatStart |
| Hollow Bones (`lowergravity`) | Selectable | Lose 12 maximum HP; gain 2 Dexterity and a translucent appearance. | Inheritance stats / BloodlineRelic / HeirCharacter; HeirVisuals.Attach / AttachRoomPortrait |
| Stressed (`bluronhit`) | Selectable | After losing HP to an enemy attack, gain 1 Weak. | Native/BloodlineRelic.cs: AfterDamageReceived |
| Synesthesia (`colortrails`) | Selectable | Rainbow motes orbit your heir above a shifting colorful ring, including at rest sites and shops. Visible alongside other palette traits. | Native/TraitAppearance.cs: Attach / Refresh |
| Vegan (`nomeat`) | Selectable | All healing is halved, rounded down. | Core/Inheritance.cs: Healing |
| Only Heart (`smallhitbox`) | Selectable | Excellent Dexterity, but fragile. | Inheritance stats / BloodlineRelic / HeirCharacter; HeirVisuals.Attach / AttachRoomPortrait |
| Diva (`cheeronkills`) | Selectable | Gain 2 gold after each enemy you defeat; start combat Vulnerable for 1 turn. | Native/BloodlineRelic.cs: BeforeCombatStart / AfterDeath |
| Super IBS (`superfart`) | Selectable | At combat start, apply 3 Poison to every enemy. | Native/BloodlineRelic.cs: BeforeCombatStart |
| Vampirism (`vampire`) | Selectable | Heal 3 after combat, but have less health. | Inheritance stats / BloodlineRelic / HeirCharacter |
| Cartographer (`mapreveal`) | Selectable | On the first turn of each combat, draw 1 extra card but lose 1 Energy. | Native/BloodlineRelic.cs: ModifyHandDraw / AfterPlayerTurnStart |
| Algesia (`noimmunitywindow`) | Selectable | Each enemy attack that penetrates Block deals 2 extra damage. | Native/BloodlineRelic.cs: ModifyHpLostAfterOsty |
| Limitless (`nomanacap`) | Selectable | Gain 1 extra Energy each turn, but start each turn with 1 Vulnerable. | Native/BloodlineRelic.cs: ModifyMaxEnergy / AfterPlayerTurnStart |
| Hero Complex (`megahealth`) | Selectable | Gain 40 maximum HP. Healing is disabled. Earn 100% more legacy crowns. | Inheritance stats / BloodlineRelic / HeirCharacter; Core/Inheritance.cs: Healing |
| Butter Fingers (`itemsgoflying`) | Selectable | Lose up to 5 gold after each battle. | Native/BloodlineRelic.cs: AfterCombatVictory |
| Masochism (`manafromhurt`) | Selectable | After losing HP to an enemy attack, gain 1 Energy for your next turn. | Native/BloodlineRelic.cs: AfterDamageReceived |
| Lootbox Addict (`bonuschestgold`) | Selectable | Lose 8 maximum HP. Gain 20 gold when entering a treasure room. | Inheritance stats / BloodlineRelic / HeirCharacter; Native/BloodlineRelic.cs: AfterRoomEntered |
| Super Healer (`superhealer`) | Selectable | Lose 15 maximum HP; all healing is doubled. | Inheritance stats / BloodlineRelic / HeirCharacter; Core/Inheritance.cs: Healing |
| Hoarder (`twinrelics`) | Selectable | Start the run with two random common relics. | Native/BloodlineRelic.cs: AfterActEntered |
| Mushroom Man (`mushroomgrow`) | Selectable | Lose 6 maximum HP. After the first enemy Attack that damages you each turn, gain 3 Block. Your heir has a compact appearance. | Inheritance stats / BloodlineRelic / HeirCharacter; HeirVisuals.Attach / AttachRoomPortrait; Native/BloodlineRelic.cs: AfterDamageReceived |
| Blue (`youareblue`) | Selectable | Your heir is rendered blue. | Native/TraitAppearance.cs: Refresh |
| Perfectionist (`skillcritsonly`) | Selectable | Attacks deal 50% more damage while you have Block, and 25% less otherwise. | Native/BloodlineRelic.cs: InheritedDamageMultiplicative |
| Shocked (`disarmonhurt`) | Selectable | After an enemy attack damages you, the first Attack drawn next turn is discarded. | Native/BloodlineRelic.cs: AfterDamageReceived |
| Explosive Chests (`explosivechests`) | Selectable | Lose 5 HP when entering a treasure room; this can kill the heir. | Native/BloodlineRelic.cs: AfterRoomEntered |
| Explosive Enemies (`explosiveenemies`) | Selectable | When an enemy dies, lose 2 HP from its explosion. | Native/BloodlineRelic.cs: AfterDeath |
| Medium (`halloweenholiday`) | Selectable | Your heir wears a ghostly violet palette. | Native/TraitAppearance.cs: Attach / Refresh |
| Festive (`christmasholiday`) | Selectable | Your heir wears a festive red palette. | Native/TraitAppearance.cs: Attach / Refresh |
| Innovator (`randomizekit`) | Selectable | Your starting deck is a seeded mixture of all three heir classes. | Core/Inheritance.cs: Deck |
| Kanganthropy (`superspinkick`) | Selectable | The first Attack played each turn deals 2 additional damage per hit. | Native/BloodlineRelic.cs: InheritedDamageAdditive |
| Forgetful (`cointimer`) | Selectable | Lose up to 10 gold after each battle. | Native/BloodlineRelic.cs: AfterCombatVictory |
| Inter-dimensional (`projectilesnowalls`) | Selectable | Lose 6 maximum HP. The first Attack each turn removes up to 3 Block from each enemy it hits, before damage. | Inheritance stats / BloodlineRelic / HeirCharacter; Native/BloodlineRelic.cs: BeforeDamageReceived |
| Antique (`antique`) | Selectable | Start the run with one random common relic. | Native/BloodlineRelic.cs: AfterActEntered |

Verification: `scripts/sts2-trait-appearance-check.py` exercises actual native combat and room nodes on both branches, paired palettes, all three classes, animated motion, HP-loss flashes and flash recovery. Existing core and native trait-mechanics checks cover stat, healing, attack, room and settlement hooks. Physical Deck visual review and publication remain separate delivery checks.
