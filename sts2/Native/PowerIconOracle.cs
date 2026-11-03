using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace SpireLegacy;

// Called only through the opt-in, isolated native test bridge.
public static class PowerIconOracle
{
    public static object Inspect()
    {
        var powers=typeof(LegacyPower).Assembly.GetTypes()
            .Where(t=>!t.IsAbstract&&typeof(LegacyPower).IsAssignableFrom(t)).OrderBy(t=>t.Name);
        var records=new List<object>();
        foreach(var type in powers)
        {
            var power=(LegacyPower)ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            var packed=PreloadManager.Cache.GetTexture2D(power.PackedIconPath);
            var big=PreloadManager.Cache.GetTexture2D(power.ResolvedBigIconPath);
            if(packed.GetWidth()<=0||big.GetWidth()<=0)
                throw new InvalidDataException("Missing power texture: "+power.Key);
            if(power.PackedIconPath==ModelDb.Power<StrengthPower>().PackedIconPath)
                throw new InvalidDataException("Unrelated Strength icon: "+power.Key);
            if(power.Key=="retained_draw"&&
               (power.PackedIconPath!=ModelDb.Power<DrawCardsNextTurnPower>().PackedIconPath||
                power.ResolvedBigIconPath!=ModelDb.Power<DrawCardsNextTurnPower>().ResolvedBigIconPath))
                throw new InvalidDataException("Eternal Bookmark must use the native draw icons.");
            var image=big.GetImage();
            var path=Path.Combine(Runtime.DataDirectory,"power-icon-"+power.Key+".png");
            if(image.SavePng(path)!=Godot.Error.Ok)throw new IOException("Cannot save power icon evidence.");
            records.Add(new{key=power.Key,packedIcon=power.PackedIconPath,bigIcon=power.ResolvedBigIconPath,
                packedWidth=packed.GetWidth(),bigWidth=big.GetWidth()});
        }
        return records;
    }
}
