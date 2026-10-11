using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using System.Runtime.CompilerServices;

namespace SpireLegacy;

// The native base getter serves canonical and mutable models on both branches.
[HarmonyPatch(typeof(AncientEventModel),"get_DialogueSet")]
public static class HeirAncientConversations
{
    private sealed record Identity(string Entry);
    private static readonly ConditionalWeakTable<AncientDialogueSet,Identity> Sets=new();
    public static void Postfix(AncientEventModel __instance,AncientDialogueSet __result)=>Sets.GetValue(__result,_=>new(__instance.Id.Entry));
    public static bool TryCreate(AncientDialogueSet set,string cls,int visits,out AncientDialogue dialogue)
    {
        dialogue=null!;if(!Sets.TryGetValue(set,out var identity))return false;
        var classId=cls is "knight" or "mage" or "ranger"?cls:"knight";var first=visits<=0;
        var content=AncientConversation.For(identity.Entry,classId,visits);
        dialogue=new AncientDialogue(content.Select(_=>"").ToArray()){VisitIndex=first?0:1,IsRepeating=!first};
        var translations=new Dictionary<string,string>();
        for(var i=0;i<content.Count;i++) {
            var key=$"SPIRELEGACY.ANCIENT.{identity.Entry}.{classId}.{(first?"first":"return")}.{i}";
            translations[key]=content[i].Text;var line=dialogue.Lines[i];
            line.Speaker=content[i].Ancient?AncientDialogueSpeaker.Ancient:AncientDialogueSpeaker.Character;
            line.LineText=new LocString("ancients",key);
            if(i<content.Count-1){translations[key+".next"]=content[i].Ancient?"Respond":"Continue";line.NextButtonText=new LocString("ancients",key+".next");}
        }
        // Only original, namespaced English fallback text enters the active locale.
        LocManager.Instance.GetTable("ancients").MergeWith(translations);return true;
    }
}
