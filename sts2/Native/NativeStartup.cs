using System.Reflection;
using HarmonyLib;

namespace SpireLegacy;

public static class NativeStartup
{
    public const string Owner="CapeKid.SpireLegacy2";
    public static Type[] PatchClasses=>typeof(Runtime).Assembly.GetTypes().Where(t=>t.GetCustomAttributes<HarmonyPatch>().Any())
        .Where(t=>t!=typeof(CaptureCopiedOwnerInheritance)||CaptureCopiedOwnerInheritance.Prepare())
        .Where(t=>t!=typeof(CaptureTransferredOwnerInheritance)||CaptureTransferredOwnerInheritance.Prepare())
        .OrderBy(t=>t.FullName,StringComparer.Ordinal).ToArray();
    public static int Install(string owner,IEnumerable<Type> classes,string? failBefore=null)
    {
        var harmony=new Harmony(owner);var count=0;string? current=null;
        try{
            foreach(var type in classes){current=type.FullName;
                if(type.Name==failBefore)throw new MissingMethodException("Planted unsupported native API for startup rollback audit: "+current);
                var applied=harmony.CreateClassProcessor(type).Patch();
                if(applied==null||applied.Count==0)throw new MissingMethodException("Patch resolved no native targets.");
                count+=applied.Count;
            }
            return count;
        }catch(Exception error){Remove(owner);throw new InvalidOperationException("Spire Legacy patch installation failed at "+current+"; removed this mod's partial patches. "+error.Message,error);}
    }
    public static void Remove(string owner)
    {
        var harmony=new Harmony(owner);
        foreach(var method in Harmony.GetAllPatchedMethods().ToArray())if(Harmony.GetPatchInfo(method)!.Owners.Contains(owner))harmony.Unpatch(method,HarmonyPatchType.All,owner);
    }
    public static void ValidateApis()
    {
        BranchCommands.Validate();
        ApiContract.Require(typeof(MegaCrit.Sts2.Core.Models.CardPools.IroncladCardPool),"GenerateAllCards",typeof(MegaCrit.Sts2.Core.Models.CardModel[]),Type.EmptyTypes);
    }
    public static object Inventory()=>Harmony.GetAllPatchedMethods().Select(method=>new{method=method.DeclaringType?.FullName+"."+method.Name,signature=method.ToString(),patches=Harmony.GetPatchInfo(method)!.Prefixes.Concat(Harmony.GetPatchInfo(method)!.Postfixes).Concat(Harmony.GetPatchInfo(method)!.Finalizers).Where(p=>p.owner==Owner).Select(p=>p.PatchMethod.DeclaringType?.FullName+"."+p.PatchMethod.Name).ToArray()}).Where(row=>row.patches.Length>0).OrderBy(row=>row.method).ToArray();
}
