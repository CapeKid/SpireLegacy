"""Add 45 individually designed cards per class without replacing existing IDs."""
import json, re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
read = lambda n: json.loads((ROOT/'sheets'/f'{n}.json').read_text())
def write(n, rows): (ROOT/'sheets'/f'{n}.json').write_text(json.dumps(rows, indent=2)+'\n')

# name|type|cost|build|role|effects|original illustration scene
RECIPES = {
'knight': {
'COMMON': '''Buckler Jab|A|1|armor|bridge|damage=5 block=5|a knight punches a bat with a round buckler
Layered Mail|S|1|armor|setup|block=4 nextBlock=6|a knight fastens two overlapping plates of armor
Shield Drill|S|0|armor|support|block=3 special=exhaust_one specialAmount=1|a knight feeds a dented shield to a forge
Hot Rivets|A|1|exhaust|bridge|damage=8 special=exhaust_one specialAmount=1|a knight hurls glowing rivets while melting a broken sword
Burning Orders|S|0|exhaust|support|special=exhaust_one specialAmount=1 nextDraw=2|a knight burns a scroll and reveals tomorrow's battle map
Blood Oath|S|1|blood|setup|strength=2 hpLoss=2 exhaust=True|a knight swears an oath over a crimson sword
Grit Teeth|S|0|blood|bridge|block=7 hpLoss=2|a knight braces behind a chipped shield with clenched teeth
Redoubled Blow|A|1|strength|payoff|damage=2 hits=4|a knight spins a short sword through four separate slashes
Measured Cut|A|1|strength|support|damage=7 vigor=4|a knight draws a sword back after a measured slash
Splintered Pike|A|1|status|setup|damage=13 special=wounds specialAmount=1|a knight shatters a wooden pike into splinters
Heavy Harness|S|1|status|setup|block=15 special=wounds specialAmount=1|a knight buckles an immense spiked armor harness
Rationed Assault|A|1|delayed|bridge|damage=8 nextDraw=1|a knight charges past a supply cart toward a distant battle
Stand Ready|S|1|delayed|support|nextBlock=10 nextEnergy=1|a knight plants a shield and waits beneath a dawn banner
Flanking March|A|0|delayed|bridge|damage=4 nextBlock=4|a knight circles behind a castle pillar with shield raised
Siege Volley|A|2|strength|payoff|damage=4 hits=3 aoe=True|a knight launches three fans of crossbow bolts across a castle hall''',
'UNCOMMON': '''Cathedral Guard|S|2|armor|setup|block=12 plated=2|a knight guards a cathedral door with layered radiant armor
Rampart Rush|A|2|armor|payoff|special=block_damage hits=2 exhaust=True|a knight rams two enemies with an enormous stone shield
Guard Rotation|S|1|armor|support|block=6 special=retrieve specialAmount=1 retain=True|a knight retrieves a shield from a rack while holding another
Armor Cache|S|1|delayed|setup|nextBlock=12 draw=1 exhaust=True|a knight opens a chest packed with spare shields
Cast Off|S|1|exhaust|enabler|special=exhaust_nonattacks specialAmount=3|a knight tosses maps and broken armor into a blazing furnace
Forge Sparks|P|1|exhaust|payoff|power=exhaust_damage magic=3|a knight's forge sends sparks into surrounding monsters
Ashen Advance|A|1|exhaust|payoff|damage=5 special=exhaust_damage specialAmount=2|a knight charges with a blade assembled from glowing ashes
Salvage Guard|S|1|exhaust|payoff|special=exhaust_block specialAmount=2 block=3|a knight forms a shield from burnt scraps of steel
Blood Temper|A|1|blood|bridge|damage=6 hits=2 hpLoss=2|a knight dips twin blades in a crimson forge
Crimson Banner|P|2|blood|payoff|power=rupture magic=2|a knight raises a battle banner beside a blood-red anvil
Painful Lesson|S|1|blood|support|draw=3 hpLoss=2 exhaust=True|a knight studies a battle scroll while tending a wound
Warrior Feast|S|2|blood|recovery|heal=7 vigor=7 exhaust=True|a knight eats a hearty stew and lifts a glowing sword
Serrated Barrage|A|2|strength|payoff|damage=2 hits=6|a knight unleashes six rapid dagger cuts
Weapons Master|S|1|strength|setup|strength=1 vigor=5 exhaust=True|a knight selects a sharpened sword from six weapon racks
Focused Defense|S|1|strength|support|block=9 nextDraw=1|a knight studies an opponent over the rim of a shield
Scar Tissue|P|1|status|payoff|power=evolve magic=1|a knight's damaged armor repairs itself around old battle scars
Furnace Breath|P|1|status|payoff|power=fire_breathing magic=5|a knight breathes a cone of furnace flame through a visor
Jagged Charge|A|2|status|setup|damage=12 aoe=True special=wounds specialAmount=2|a knight smashes through splintering wooden barricades
Ready Arsenal|S|2|delayed|payoff|nextEnergy=2 nextDraw=2 exhaust=True|a knight lays weapons and maps out for tomorrow's siege
Patient Riposte|A|2|retained|payoff|damage=12 retain=True special=retain_damage specialAmount=4|a knight waits with an increasingly radiant poised sword''',
'RARE': '''Castle Heart|P|2|armor|payoff|power=metallicize magic=7|a knight is surrounded by a miniature living castle wall
Iron Avalanche|A|3|armor|payoff|damage=8 special=guard_bonus specialAmount=24 aoe=True exhaust=True|a knight pushes a wall of shields down a steep stair
Phoenix Forge|P|2|exhaust|payoff|power=exhaust_damage magic=6|a knight's burning anvil rises as a fiery phoenix
Ancestral Salvage|S|2|exhaust|support|special=exhume block=16 exhaust=True|a knight retrieves an ancestral shield from sacred ashes
King's Ransom|S|0|blood|payoff|energy=3 hpLoss=8 exhaust=True|a knight sacrifices a crimson crown to fuel three bright torches
Bloodfire Mantle|P|1|blood|payoff|power=combust magic=7|a knight stands in a circle of blood-red fire
Grand Melee|A|3|strength|payoff|damage=3 hits=7 aoe=True|a knight spins seven swords through a mob of castle monsters
Warlord's Command|S|2|strength|support|power=double_tap magic=2 strength=2 exhaust=True|a knight commands spectral soldiers to repeat his charge
Iron Furnace|P|2|status|payoff|power=fire_breathing magic=10|a knight opens an enormous furnace shield toward monsters
Siege Tomorrow|S|2|delayed|payoff|nextEnergy=3 nextBlock=20 exhaust=True|a knight oversees a prepared siege engine at sunrise'''
},
'mage': {
'COMMON': '''Scripted Spark|A|1|sequencing|bridge|damage=5 draw=1 nextBlock=3|a mage draws a rune that launches a spark and leaves a tiny shield
Warding Verse|S|0|sequencing|enabler|block=3 retain=True|a mage writes a protective verse onto a floating parchment
Page Turner|S|1|sequencing|support|draw=1 nextDraw=2|a mage flips a spellbook to a glowing bookmarked page
Slow Incantation|S|1|delayed|setup|nextEnergy=1 nextDraw=1 nextBlock=5|a mage prepares two candles beneath a ticking rune clock
Ice Cocoon|S|1|delayed|setup|block=4 nextBlock=8|a mage wraps himself in two layers of crystalline ice
Tomorrow's Flame|A|1|delayed|bridge|damage=7 nextEnergy=1 retain=True|a mage holds a sleeping ember inside an hourglass
Ash Reading|S|0|exhaust|enabler|special=exhaust_one specialAmount=1 draw=1 hpLoss=1|a mage reads glowing letters in the ashes of a burnt scroll
Paper Shield|S|1|exhaust|bridge|block=11 exhaust=True|a mage unfolds a paper shield that burns after absorbing a blow
Fading Star|A|0|exhaust|enabler|damage=7 ethereal=True|a mage catches a star dissolving into violet smoke
Stored Ember|A|1|retained|payoff|damage=5 retain=True special=retain_damage specialAmount=3|a mage stores a growing fireball in a glass lantern
Crystal Seed|S|1|retained|payoff|block=4 retain=True special=retain_block specialAmount=3|a mage holds a seed growing into a crystalline shield
Patient Study|S|1|retained|support|draw=2 retain=True exhaust=True|a mage sits beside a carefully bookmarked tower of books
Corrosive Ink|A|1|debuff|bridge|damage=4 poison=4|a mage splashes green acid ink from a quill
Chilling Words|S|1|debuff|support|weak=2 block=5|a mage speaks frozen speech-shaped runes toward a monster
Rune Scatter|A|1|sequencing|payoff|damage=2 hits=4 special=random_attack|a mage scatters four bright rune stones toward monsters''',
'UNCOMMON': '''Woven Incantation|P|1|sequencing|payoff|power=skill_block magic=4|a mage weaves three scrolls together into a protective tapestry
Arcane Meter|P|1|sequencing|payoff|power=skill_energy magic=1|a mage fills three rune circles that ignite tomorrow's mana crystal
Triple Formula|S|1|sequencing|enabler|draw=2 special=charge_gain specialAmount=1 exhaust=True|a mage combines three diagrams into a bright formula
Conduit Lance|A|2|sequencing|payoff|damage=4 hits=5|a mage channels a lightning lance through five glowing crystal rings
Bookmark Ward|S|1|sequencing|bridge|block=5 power=equilibrium magic=1|a mage wedges a magical shield-shaped bookmark into an open book
Hourglass Array|S|2|delayed|setup|nextEnergy=2 nextDraw=3 exhaust=True|a mage aligns three hourglasses with two mana crystals
Delayed Nova|A|2|delayed|bridge|damage=12 aoe=True nextBlock=8|a mage releases a circular nova while a second shield rune waits
Winter Reserve|S|1|delayed|support|nextBlock=14 retain=True|a mage stores a block of glowing ice in a cabinet
Ashen Chorus|P|1|exhaust|payoff|power=exhaust_damage magic=2|a mage conducts singing burnt scrolls that shower monsters with sparks
Spell Pyre|S|1|exhaust|enabler|special=exhaust_nonattacks specialAmount=4|a mage burns a tower of spell scrolls on a ritual pyre
Phoenix Notes|S|1|exhaust|support|special=retrieve specialAmount=1 nextDraw=2 exhaust=True|a mage recovers a phoenix feather bookmark from a discarded book
Ashbolt|A|2|exhaust|payoff|damage=6 special=exhaust_damage specialAmount=3|a mage builds a missile out of exhausted scroll ashes
Tower Library|P|2|retained|payoff|power=establishment magic=1|a mage keeps floating books on shelves that brighten as they wait
Wax Seal|S|1|retained|support|special=discount_hand block=4 exhaust=True|a mage seals prepared scrolls with a mana-saving wax stamp
Moonstone Lance|A|2|retained|payoff|damage=9 retain=True special=retain_damage specialAmount=6|a mage waits beneath a moon with a growing crystal lance
Geode Ward|S|2|retained|payoff|block=9 retain=True special=retain_block specialAmount=5|a mage watches a geode slowly grow into a defensive wall
Hex Rain|A|2|debuff|enabler|damage=6 aoe=True weak=2|a mage summons a shower of dark weakening runes
Viper Script|S|1|debuff|setup|poison=4 vulnerable=1|a mage writes a green serpent rune that cracks a monster's armor
Alchemist's Patience|P|1|debuff|payoff|power=noxious magic=2|a mage brews a slowly bubbling green cauldron
Rune Familiar|P|2|sequencing|support|power=power_draw magic=1|a tiny animated spellbook delivers scrolls to a mage''',
'RARE': '''Archmage Thesis|P|2|sequencing|payoff|power=skill_block magic=8|a mage finishes a vast three-part spell diagram forming a shield
Mirror Manuscript|S|1|sequencing|payoff|power=burst magic=1 retain=True exhaust=True|a mage holds two identical glowing scrolls in a mirrored chamber
Comet Calendar|S|2|delayed|payoff|nextEnergy=3 nextDraw=4 exhaust=True|a mage schedules four comets across a celestial calendar
Stasis Dome|S|2|delayed|payoff|block=12 nextBlock=20 exhaust=True|a mage freezes a protective dome inside a giant clock
Book of Embers|P|2|exhaust|payoff|power=exhaust_damage magic=5|a mage opens a flaming spellbook sending embers in every direction
Returned Prophecy|S|2|exhaust|support|special=exhume draw=3 exhaust=True|a mage recovers a prophecy scroll from a ring of ash
Eternal Bookmark|P|2|retained|support|power=well_laid magic=3|a mage uses three golden bookmarks to hold floating spells in place
Sun in a Bottle|A|3|retained|payoff|damage=18 aoe=True retain=True special=retain_damage specialAmount=8|a mage uncorks a growing miniature sun over a castle hall
Plague Constellation|P|2|debuff|payoff|power=noxious magic=4|a mage connects green stars above a bubbling plague cauldron
Grand Incantation|A|3|sequencing|payoff|damage=4 hits=8 special=random_attack exhaust=True|a mage's eight rune rings unleash a storm of tiny meteors'''
},
'ranger': {
'COMMON': '''Loose Fletching|A|1|discard|payoff|damage=6 special=discard_draw specialAmount=1|a ranger loses a loose arrow feather and reveals a fresh arrow
Pocket Wind|S|1|discard|payoff|block=5 special=discard_energy specialAmount=1|a ranger releases a bottled gust from an overstuffed pouch
Shed Cloak|S|1|discard|payoff|block=6 special=discard_block specialAmount=5|a ranger sheds a cloak that catches an enemy sword
Sort Quiver|S|0|discard|enabler|draw=2 discard=2|a ranger sorts two arrows into separate quivers
Quick Exchange|A|1|discard|enabler|damage=8 draw=1 discard=1|a ranger shoots while swapping arrows between belt and hand
Trail Reset|S|1|discard|enabler|special=refill_hand|a ranger empties a pouch and refills it with new supplies
Hissing Arrow|A|1|poison|bridge|damage=7 poison=3|a ranger fires a green serpent-tipped arrow
Toxic Snare|S|1|poison|support|poison=3 weak=1|a ranger sets a venom-coated rope snare
Silent Mixture|S|1|poison|setup|poison=4 retain=True|a ranger holds a sealed green poison vial for later
Held Shot|A|1|retained|payoff|damage=6 retain=True special=retain_damage specialAmount=3|a ranger holds a drawn bow as its arrow gradually glows
Hidden Buckler|S|1|retained|payoff|block=5 retain=True special=retain_block specialAmount=2|a ranger unfolds a growing leaf-shaped buckler from a cloak
Pine Needle|A|0|chain|enabler|damage=3 poison=1|a ranger flicks a tiny green pine needle toward a bat
Running Shot|A|1|chain|bridge|damage=5 nextDraw=1|a ranger runs along a branch while firing an arrow
Trail Sign|S|0|scry|enabler|special=scry specialAmount=2 block=2|a ranger reads a carved trail sign while hiding behind a twig shield
Marked Path|A|1|scry|bridge|damage=6 special=scry specialAmount=2|a ranger shoots an arrow along a magically revealed path''',
'UNCOMMON': '''Light Pack|P|1|discard|payoff|power=discard_block magic=3|a ranger travels lightly while discarded gear forms protective cover
Tailwind|P|1|discard|payoff|power=discard_energy magic=1|a ranger discards a heavy pouch and catches a bright tailwind
Spring Nock|A|1|discard|payoff|damage=8 special=discard_draw specialAmount=2|a ranger discards an old bowstring and fits a spring-loaded nock
Spare Provisions|S|1|discard|payoff|draw=1 special=discard_energy specialAmount=2 exhaust=True|a ranger tosses a ration pouch that releases two bright energy sparks
Briar Retreat|S|2|discard|enabler|block=14 draw=2 discard=2|a ranger retreats through briars while swapping arrow bundles
Quiver Repack|S|1|discard|enabler|special=refill_hand nextBlock=6|a ranger empties and repacks a quiver behind a fallen log
Scatter Supplies|A|1|discard|enabler|damage=5 aoe=True discard=1|a ranger hurls a spray of sharp camping supplies at several monsters
Mire Volley|A|2|poison|bridge|damage=3 hits=3 poison=4|a ranger fires three swamp-green arrows across a marsh
Spreading Blight|S|1|poison|enabler|poison=3 aoe=True weak=1|a ranger pours poison down a branching root network
Serpent Trail|P|1|poison|payoff|power=noxious magic=2|a ranger follows a green serpent trail circling enemy tracks
Venom Reserve|S|1|poison|payoff|special=double_poison retain=True exhaust=True|a ranger saves two concentrated green vials for a venomous strike
Perch Discipline|P|2|retained|payoff|power=establishment magic=1|a ranger settles on a perch with arrows growing cheaper as they wait
Eagle Focus|A|2|retained|payoff|damage=10 retain=True special=retain_damage specialAmount=5|a ranger aims beside an eagle with a steadily glowing arrow
Camouflage Nest|S|1|retained|payoff|block=7 retain=True special=retain_block specialAmount=4|a ranger waits inside a growing shield of woven leaves
Conserve Arrows|S|1|retained|support|power=equilibrium magic=1 block=6|a ranger gathers unused arrows behind a leaf shield
Thread the Needle|A|0|chain|enabler|damage=2 hits=2|a ranger fires two tiny arrows through a narrow needle-shaped gap
Pursuit Volley|A|1|chain|payoff|damage=2 hits=2 special=marked_bonus specialAmount=4|a ranger fires two arrows along a marked monster's trail
Tracking Rhythm|P|1|chain|payoff|power=attack_vigor magic=3|a ranger's three arrow footprints awaken a glowing fourth arrow
Owl Watch|P|1|scry|support|power=foresight magic=3|a ranger and owl inspect three ghostly future arrows
Chosen Trail|A|2|scry|bridge|damage=15 special=scry specialAmount=3 retain=True|a ranger fires a long arrow down one of three revealed paths''',
'RARE': '''Empty Quiver|S|1|discard|payoff|special=refill_hand energy=1 exhaust=True|a ranger empties a golden quiver and refills it in a rushing wind
Ghost Luggage|P|2|discard|payoff|power=discard_block magic=6|a ranger's discarded travel bags become a spectral barricade
Falcon Courier|A|2|discard|payoff|damage=14 special=discard_energy specialAmount=3 exhaust=True|a ranger sends an energy crystal by falcon while firing a heavy arrow
Venom Eclipse|S|2|poison|payoff|poison=10 aoe=True exhaust=True|a ranger spreads ten drops of green venom beneath a dark moon
King Cobra|P|2|poison|payoff|power=noxious magic=4|a ranger raises a huge spectral cobra from a green vial
Long Vigil|A|3|retained|payoff|damage=20 retain=True special=retain_damage specialAmount=9|a ranger waits through moonrise with an enormous glowing arrow
Ancient Canopy|S|2|retained|payoff|block=15 retain=True special=retain_block specialAmount=7|a ranger grows a vast protective canopy while waiting in a tree
Arrow Cascade|A|2|chain|payoff|special=chain_damage specialAmount=3 aoe=True exhaust=True|a ranger's earlier arrow trails join into a cascading storm
Hunt Without End|P|2|chain|payoff|power=attack_vigor magic=6|a ranger follows repeating golden arrow tracks into a dark forest
Forest Oracle|P|2|scry|payoff|power=scry_block magic=6|a ranger consults a glowing ancient tree showing five future paths'''
}}

POWERS = {
'exhaust_damage':'Whenever you Exhaust a card, deal !M! damage to ALL enemies.',
'skill_block':'Every third Skill you play each turn grants !M! Block.',
'skill_energy':'Every third Skill you play each turn grants !M! Energy NEXT turn.',
'discard_block':'Whenever you discard a card during your turn, gain !M! Block.',
'discard_energy':'The first time you discard a card each turn, gain !M! Energy.',
'attack_vigor':'Every third Attack you play each turn grants !M! Vigor for your NEXT Attack.',
'power_draw':'Whenever you play a Power, draw !M! card(s).',
'evolve':'Whenever you draw a Status, draw !M! card(s).',
'fire_breathing':'Whenever you draw a Status or Curse, deal !M! damage to ALL enemies.',
'combust':'At the end of your turn, lose 1 HP and deal !M! damage to ALL enemies.',
'establishment':'Whenever a card is Retained, reduce its cost by !M! this combat.',
'burst':'Your next !M! Skill(s) this turn are played twice.',
'noxious':'At the start of your turn, apply !M! Poison to ALL enemies.',
'foresight':'At the start of your turn, Scry !M!.',
'scry_block':'Whenever you Scry, gain !M! Block.'}
SPECIALS = {
'discard_draw':'When discarded during your turn, draw {n} card(s).',
'discard_energy':'When discarded during your turn, gain {n} Energy.',
'discard_block':'When discarded during your turn, gain {n} Block.',
'retain_damage':'Whenever this is Retained, gain {n} damage this combat.',
'retain_block':'Whenever this is Retained, gain {n} Block this combat.',
'refill_hand':'Discard your hand, then draw that many cards.',
'wounds':'Shuffle {n} Wound(s) into your draw pile.',
'exhaust_nonattacks':'Exhaust all non-Attacks in your hand. Gain {n} Block per card Exhausted.',
'exhaust_damage':'Deal {n} additional damage per card in your Exhaust pile (up to 10 cards).',
'exhaust_block':'Gain {n} Block per card in your Exhaust pile (up to 10 cards).'}

def main():
    cards=read('cards');art=read('card_art');classes={r['id']:r for r in read('classes')}
    existing={r['id'] for r in cards};added=[];builds=read('card_builds') if (ROOT/'sheets/card_builds.json').exists() else []
    prefix=art[0]['prompt'].split('\nCard:')[0]
    for cls,tiers in RECIPES.items():
        for rarity,recipes in tiers.items():
            lines=recipes.splitlines();assert len(lines)=={'COMMON':15,'UNCOMMON':20,'RARE':10}[rarity]
            for line in lines:
                name,kind,cost,build,role,values,scene=line.split('|')
                key=cls+'_'+re.sub(r'[^a-z0-9]+','_',name.lower()).strip('_')
                if key in existing:continue
                row=dict(cards[0])
                for f,v in list(row.items()):
                    if isinstance(v,bool):row[f]=False
                    elif isinstance(v,int):row[f]=0
                row.update(id=key,name=name,classId=cls,type={'A':'ATTACK','S':'SKILL','P':'POWER'}[kind],cost=int(cost),rarity=rarity,asset=classes[cls]['asset'],art=key,power='none',special='none',hits=1)
                for token in values.split():
                    field,value=token.split('=');row[field]=True if value=='True' else int(value) if re.fullmatch(r'-?\d+',value) else value
                row['upgradeDamage']=3 if row['damage'] else 0;row['upgradeBlock']=3 if row['block'] else 0
                row['upgradeMagic']=1 if any(row[f] for f in ('draw','energy','strength','dexterity','weak','vulnerable','magic','poison','vigor','thorns','plated','nextEnergy','nextDraw','nextBlock','heal')) else 0
                row['upgradeSpecial']=1 if row['special'] in ('discard_draw','discard_energy','discard_block','retain_damage','retain_block','exhaust_damage','exhaust_block','exhaust_nonattacks','scry') else 0
                row['upgradeCost']=row['cost']>0 and not(row['upgradeDamage'] or row['upgradeBlock'] or row['upgradeMagic'] or row['upgradeSpecial'])
                # Persistent cost reduction/draw engines upgrade setup cost, not their multiplicative payoff.
                if row['power'] in ('establishment','evolve','power_draw','discard_energy','skill_energy'):
                    row['upgradeMagic']=0;row['upgradeCost']=True
                cards.append(row);added.append(row)
                builds.append(dict(id=key,classId=cls,build=build,role=role))
                prompt=prefix+'\nCard: '+name+'. Original distinct scene: '+scene+'. Rogue Legacy 2-inspired chunky chibi proportions, bold black outlines, flat cel shading, readable silhouette and warm parchment backdrop. Landscape card illustration with crop-safe central composition. No text, border, lettering or numbers.'
                art.append(dict(id=key,cardId=key,name=name,path=f'heir/cards/{key}.png',width=250,height=190,prompt=prompt,generator='built-in imagegen',sha256='pending'))
    powers={r['id']:r for r in read('power_effects')};specials={r['id']:r for r in read('special_effects')}
    for k,v in POWERS.items():powers[k]=dict(id=k,summary=v)
    for k,v in SPECIALS.items():specials[k]=dict(id=k,summary=v)
    for cls in classes:
        pool=[r for r in cards if r['classId']==cls];assert len(pool)==120
        assert [sum(r['rarity']==tier for r in pool) for tier in ('BASIC','COMMON','UNCOMMON','RARE')]==[3,35,56,26]
    write('cards',cards);write('card_art',art);write('card_builds',builds)
    write('power_effects',list(powers.values()));write('special_effects',list(specials.values()))
    write('pool_targets',[dict(id=c,reference='Expanded class-specific library; 45 additions over 0.2.0',basic=3,common=35,uncommon=56,rare=26,total=120) for c in classes])
    (ROOT/'build/build-choice-art-plan.json').write_text(json.dumps([dict(id=r['id'],classId=r['classId'],prompt=next(a['prompt'] for a in art if a['id']==r['id'])) for r in added],indent=2))
    print(f'{len(cards)} cards; {len(added)} added; 120 per class')
if __name__=='__main__':main()
