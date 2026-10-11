"""Original Heir conversations; no copied game dialogue."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
# Opening/followup belong to the Ancient; each pair of replies is Knight, Mage, Ranger.
scripts={
'NEOW':[
 ('Another name on an old family tree. What will this branch become?','Then carry more home than your scars.',
  [('A shield for those who follow.','I will leave them a house that stands.'),('A record of what we learn here.','The next heir will have my notes.'),('A trail back to the people waiting for me.','I will mark the dangerous turns.')]),
 ("Your family's banner again. Does each heir remember the climb?",'Let memory guide you, but take your own steps.',
  [('We remember the promises.','This oath is mine to keep.'),('We trade journals, including the mistakes.','I have room for another correction.'),('We learn the old tracks, then choose our route.','This trail still has surprises.')])],
'DARV':[
 ('You look like someone with an attic full of heirlooms. Anything interesting?','A family that keeps everything! We should get along.',
  [('Only this sword. I promised to bring it back.','A gift would be easier to explain than a missing sword.'),('My family keeps experiments in the attic.','I could label anything you give me. For safety.'),('Mostly maps and arrows with stories attached.','Something light would fit beside the maps.')]),
 ('More of that family! Did the last present make it home?','Good. A proper collection needs a little excitement.',
  [('Its story did. The armory remembers.','I will try to bring back both this time.'),('There is a detailed entry in the family journal.','Preferably excitement that survives a jar.'),('It left a mark on the trail.','I am getting better at finding the way back.')])],
'TEZCATARA':[
 ('Come in, heir. Even an old family cannot climb on an empty stomach.','A place at the table is worth inheriting too.',
  [('I was taught to eat after everyone else.','I suppose the others need me strong.'),('I packed books and forgot the provisions.','I will add that to the packing list.'),('I have been living on whatever fits in my cloak.','This smells much better than trail rations.')]),
 ("Another of your house. I hope someone taught you to rest.",'Then take a little warmth with you for the climb.',
  [('They did. I am still learning to listen.','I will tell the next heir that rest is not surrender.'),('The family journal now has a chapter on supper.','Some lessons deserve to be repeated.'),('I know when a trail needs a campfire.','I will save the warmth for a cold stretch.')])],
'OROBAS':[
 ('A little heir with a long shadow. How much of your family do you carry?','The tide keeps some things and returns others. Choose what you hold.',
  [('Their hopes are heavier than the armor.','I can put down the weight without dropping the promise.'),('Questions that have passed through many hands.','An unanswered question can still be a useful inheritance.'),('Their routes, their warnings, and a few knots.','I know which knots to loosen.')]),
 ('Your house sends another traveler to the edge of the water.','Then let this meeting be a new current, not an old anchor.',
  [('We are learning which burdens to leave behind.','I will carry what helps us stand.'),('The journals disagree about you.','I will leave room for a different answer.'),('The old tracks ended here, but mine can continue.','I will watch where the current bends.')])],
'PAEL':[
 ('Your blood remembers shapes that your mind has forgotten. Will you change?','Growth does not ask the old branches for permission.',
  [('I can change my stance without losing my oath.','Then I will learn to guard the new growth.'),('Inheritance is a beginning, not a finished formula.','I will observe carefully before calling it progress.'),('Every season changes the hunter and the trail.','I can follow a living trail.')]),
 ('The family returns, and no two branches grow alike.','Good. A bloodline should become more than its roots.',
  [('We share a banner, not the same armor.','There is room beneath it for someone different.'),('The differences are where the discoveries begin.','I will write down what this branch learns.'),('The next heir may see tracks I miss.','I will leave space for their path.')])],
'NONUPEIPE':[
 ('Oh! A family hero! Is that your inherited look, or did you make it yourself?','Then let us find something worthy of the family portrait!',
  [('The dents in the armor are definitely mine.','Preferably something that survives the next dent.'),('The hat is tradition. The scorch marks are new.','I should take the portrait before the next experiment.'),('The cloak is old. I chose the feathers.','A portrait is easier to carry than another cloak.')]),
 ('A new face from the same splendid family! Shall we improve the collection?','Wonderful! A family should never run out of beautiful stories.',
  [('The armory has a shelf for the things we cannot wear.','I will try to give this one a good ending.'),('The portrait gallery is becoming an excellent study of variation.','And perhaps fewer scorch marks.'),('I have been collecting stories along the trail.','I know a few worth bringing home.')])],
'TANX':[
 ('An heir! Show me what your family calls a weapon.','Then find something here that deserves another story.',
  [('A sword kept sharp through generations.','The next heir should inherit a blade, not just a boast.'),('A staff, and the patience to aim it properly.','I will test the grip before testing the explosion.'),('A bow that knows my hands, and arrows that do not come back.','I will make the first shot count.')]),
 ("Your family's weapons have found their way here again!",'Good! An heir should learn a new trick before handing things down.',
  [('We repair them between climbs.','I will make sure the lesson survives the battle.'),('We improve the designs between disasters.','The family workshop could use a safer trick.'),('We remember every shot that missed.','I have been saving a space in the quiver.')])],
'VAKUU':[
 ('You dress for a battle, but your family has sent you into a story.','Every house needs someone willing to step onto the stage.',
  [('Then I will play the part that gets them home.','I can stand in the light without forgetting who is behind me.'),('I would rather understand the machinery behind the scenery.','I suppose someone must also take notes.'),('I prefer a quiet entrance and a clear exit.','I will keep an eye on the wings.')]),
 ('Another heir takes the stage. Does your house know how this story ends?','Then make the next scene worth remembering.',
  [('We know how to stand up after a bad ending.','I will give the next heir something to build on.'),('We have revised the ending several times.','This draft may still surprise us.'),('We know a few exits, and none of them are easy.','I will leave the trail visible.')])]
}
rows=[]
for ancient,visits in scripts.items():
    for visit,(opening,followup,replies) in zip(['first','return'],visits):
        for cls,(reply,closing) in zip(['knight','mage','ranger'],replies):
            rows.append(dict(id=ancient.lower()+'_'+cls+'_'+visit,ancient=ancient,classId=cls,visit=visit,opening=opening,reply=reply,followup=followup,closing=closing))
(root/'sheets/ancient_dialogue.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Authored',len(rows),'original four-line conversations')
