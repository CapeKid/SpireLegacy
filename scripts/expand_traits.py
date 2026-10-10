"""Author turn-based adaptations from a private installed-game catalog audit.

Names and enum identifiers are factual references; descriptions below are original.
The private audit and extracted game text are never packaged.
"""
import json
import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
catalog=json.loads((ROOT/'private/trait-catalog.json').read_text())
old=json.loads((ROOT/'sheets/traits.json').read_text())
legacy=dict(YouAreLarge='large',YouAreSmall='small',DamageBoost='weapon',MagicBoost='magic',Vampire='vampire',BonusHealth='health',BonusStrength='strength',SmallHitbox='smallhitbox')
# Each adaptation lists exact modifiers and a behavior implemented by TraitRules/Power.
design={
'NoColor':dict(effect='gray',summary='Your heir is rendered in gray.'),
'NoHealthBar':dict(hp=-8,summary='Reduced maximum HP; health remains visible for accessible card play.'),
'EasyBreakables':dict(effect='first_attack_block_loss', amount=2, summary='After your first Attack each turn, lose 2 Block (minimum 0).'),
'PlayerKnockedLow':dict(dexterity=1,summary='Gain 1 Dexterity for steadier defense.'),
'PlayerKnockedFar':dict(dexterity=-1,summary='Lose 1 Dexterity; gain extra legacy crowns.'),
'Disposition':dict(effect='nature',summary='Your heir wears a green family accent.'),
'Fart':dict(effect='none', amount=0, summary='No combat modifier. IBS does not poison enemies.'),
'EnemyKnockedLow':dict(strength=-1,summary='Lose 1 Strength; gain extra legacy crowns.'),
'BreakPropsForMana':dict(effect='skill_draw',amount=1,summary='The first Skill played each turn draws 1 card.'),
'OneHitDeath':dict(effect='fragile',summary='Maximum HP is 1, overriding all other HP bonuses.'),
'CantAttack':dict(effect='pacifist',amount=5,summary='Cannot play Attacks. Apply 5 Poison to each enemy at combat start.'),
'OmniDash':dict(effect='turn_block',amount=2,summary='Gain 2 Block at the start of each turn.'),
'HighJump':dict(dexterity=2,hp=-8,summary='Gain 2 Dexterity, but lose 8 maximum HP.'),
'GainDownStrike':dict(effect='first_attack_damage', amount=1, summary='The first Attack played each turn deals 1 additional damage per hit.'),
'BonusMagicStrength':dict(hp=-18,draw=1,summary='Draw 1 extra card each turn; lose 18 maximum HP.'),
'FMFFan':dict(effect='none', amount=0, hp=0, goldBonus=0, summary='A personal preference; no combat or health modifier.'),
'NoEnemyHealthBar':dict(effect='enemy_guard',amount=4,summary='Each enemy starts with 4 Block; enemy HP remains visible.'),
'ManaCostAndDamageUp':dict(effect='costly',amount=50,summary='Attacks deal 50% more damage and cost 1 extra Energy when drawn.'),
'RevealAllChests':dict(effect='treasure_gold', amount=10, hp=-6, summary='Lose 6 maximum HP. Gain 10 gold when entering a treasure room.'),
'BounceTerrain':dict(effect='first_attack_block', amount=2, hp=-12, summary='Lose 12 maximum HP. The first Attack played each turn grants 2 Block.'),
'LowerStorePrice':dict(effect='shop',amount=20,summary='Shop cards, relics, and potions cost 20% less.'),
'FakeSelfDamage':dict(effect='histrionic',summary='Your heir flashes red after taking damage; HP reports stay accurate.'),
'OldYellowTint':dict(effect='sepia',summary='Your heir is rendered in a nostalgic sepia palette.'),
'EnemiesCensored':dict(effect='enemy_guard',amount=5,summary='Enemies start with 5 Block, representing difficulty reading opponents.'),
'EnemiesBlackFill':dict(effect='enemy_strength',amount=1,summary='Enemies start with 1 Strength, representing harder-to-read opponents.'),
'LowerGravity':dict(hp=-12,dexterity=2,scale=.9,summary='Lose 12 maximum HP; gain 2 Dexterity and a lighter appearance.'),
'BlurOnHit':dict(effect='hurt_weak',amount=1,summary='After losing HP to an enemy attack, gain 1 Weak.'),
'ColorTrails':dict(effect='rainbow',summary='Your heir cycles through colorful hues.'),
'NoMeat':dict(effect='vegan',summary='All healing is halved, rounded down.'),
'CheerOnKills':dict(effect='diva',amount=2,summary='Gain 2 gold after each enemy you defeat; start combat Vulnerable for 1 turn.'),
'SuperFart':dict(effect='poison',amount=3,summary='At combat start, apply 3 Poison to every enemy.'),
'MapReveal':dict(effect='none', amount=0, summary='No combat modifier. The Spire map is already visible; no extra gold is awarded.'),
'NoImmunityWindow':dict(effect='algesia',amount=2,summary='Each enemy attack that penetrates Block deals 2 extra damage.'),
'NoManaCap':dict(effect='overcharge', amount=1, hp=0, summary='Gain 1 extra Energy each turn, but start each turn with 1 Vulnerable.'),
'MegaHealth':dict(goldBonus=100, hp=40,effect='no_heal',summary='Gain 40 maximum HP. Healing is disabled. Earn 100% more legacy crowns.'),
'ItemsGoFlying':dict(effect='coin_loss',amount=5,summary='Lose up to 5 gold after each battle.'),
'ManaFromHurt':dict(effect='hurt_energy',amount=1,summary='After losing HP to an enemy attack, gain 1 Energy for your next turn.'),
'BonusChestGold':dict(effect='treasure_gold', amount=20, hp=-8, summary='Lose 8 maximum HP. Gain 20 gold when entering a treasure room.'),
'SuperHealer':dict(hp=-15,effect='super_heal',amount=2,summary='Lose 15 maximum HP; all healing is doubled.'),
'TwinRelics':dict(effect='relics',amount=2,summary='Start the run with two random common relics.'),
'MushroomGrow':dict(effect='none', amount=0, scale=.85, summary='A compact appearance; no Thorns or combat bonus.'),
'YouAreBlue':dict(effect='blue',summary='Your heir is rendered blue.'),
'SkillCritsOnly':dict(effect='perfectionist',amount=50,summary='Attacks deal 50% more damage while you have Block, and 25% less otherwise.'),
'DisarmOnHurt':dict(effect='shock',summary='After an enemy attack damages you, the first Attack drawn next turn is discarded.'),
'ExplosiveChests':dict(effect='chest',amount=5,summary='Lose 5 HP when entering a treasure room; this can kill the heir.'),
'ExplosiveEnemies':dict(effect='explosions',amount=2,summary='When an enemy dies, lose 2 HP from its explosion.'),
'HalloweenHoliday':dict(effect='medium',summary='Your heir wears a ghostly violet palette.'),
'ChristmasHoliday':dict(effect='festive',summary='Your heir wears a festive red palette.'),
'RandomizeKit':dict(effect='kit',summary='Your starting deck is a seeded mixture of all three heir classes.'),
'SuperSpinKick':dict(effect='first_attack_damage', amount=2, summary='The first Attack played each turn deals 2 additional damage per hit.'),
'CoinTimer':dict(effect='coin_loss',amount=10,summary='Lose up to 10 gold after each battle.'),
'ProjectilesNoWalls':dict(effect='none', amount=0, summary='No combat modifier. Passing through walls has no equivalent in card combat.'),
'Antique':dict(effect='relics',amount=1,summary='Start the run with one random common relic.'),
'LongerCD':dict(effect='exhausted',amount=1,summary='Lose 1 Energy on even-numbered combat turns.'),
}
objects=json.loads((ROOT/'private/rl_asset_index.json').read_text())
sprites={r['name']:r for r in objects if r['type']=='Sprite'}
icon_alias=dict(NoImmunityWindow='ManaFromHurt',OldYellowTint='Retro',NoHealthBar='PlayerNoHealthBar',DamageBoost='WeaponDamageBoost',MagicBoost='MagicDamageBoost',BonusMagicStrength='MagicDamageBoost',NoEnemyHealthBar='EnemyNoHealthBar',LowerStorePrice='LowerStorePrices',ColorTrails='ColorTrail',MegaHealth='BonusHealth2',DisarmOnHurt='DisableOnHurt',ExplosiveChests='ExplosiveChest',ChristmasHoliday='WinterHoliday',Disposition='Gay',NoManaCap='NoManaCap 1',Antique='FreeRelic')
assets=json.loads((ROOT/'sheets/assets.json').read_text());asset_ids={r['id'] for r in assets}
traits=[];coverage=[]
for c in catalog:
    enabled=c['rarity'] in (1,2,3)
    key=legacy.get(c['source'],c['source'].lower())
    included=enabled or c['source'] in legacy
    # Unity rich text and runtime relic-name slots are not trait labels in StS.
    display_name=re.sub(r'<[^>]+>', '', c['name'])
    display_name=re.sub(r'\s*[-–—:]?\s*\{\d+\}', '', display_name).strip()
    coverage.append(dict(id=c['source'].lower(),source=c['source'],name=display_name,sourceRarity=c['rarity'],status='adapted' if included else 'disabled_in_source',traitId=key if included else 'none'))
    if not included:continue
    base=next((dict(r) for r in old if r['id']==key),dict(id=key,name=c['name'],asset='none',hp=0,strength=0,dexterity=0,draw=0,goldBonus=round(c['gold']*100),scale=1.0,summary='none',heal=0))
    base.update(source=c['source'],enabled=enabled,effect='none',amount=0,excludes=[next(x['source'].lower() for x in catalog if x['number']==n) for n in c['excludes']])
    base['excludes']=[legacy.get(next(x['source'] for x in catalog if x['source'].lower()==n),n) for n in base['excludes']]
    if c['source'] in design:base.update(design[c['source']])
    base['name']=re.sub(r'<[^>]+>', '', base['name'])
    base['name']=re.sub(r'\s*[-–—:]?\s*\{\d+\}', '', base['name']).strip()
    if c['source']=='SmallHitbox':base['name']='Only Heart'
    if c['source']=='DamageBoost':base['name']='Combative'
    if c['source']=='MagicBoost':base['name']='Bookish'
    source_name='Icons_Traits_'+icon_alias.get(c['source'],c['source'])
    if source_name not in sprites:raise ValueError('Missing genuine icon '+source_name)
    base['asset']=source_name.replace(' ','_')
    if base['asset'] not in asset_ids:
        asset_ids.add(base['asset']);assets.append(dict(id=base['asset'],sourceFile=sprites[source_name]['file'],sourceType='Sprite',sourceName=source_name,output=base['asset']+'.png'))
    traits.append(base)
for name,rows in [('traits',traits),('trait_coverage',coverage),('assets',assets)]:
    (ROOT/'sheets'/f'{name}.json').write_text(json.dumps(rows,indent=2)+'\n')
print(len(traits),'adaptations;',sum(r['enabled'] for r in traits),'eligible entries;',len(coverage),'audited library entries')


