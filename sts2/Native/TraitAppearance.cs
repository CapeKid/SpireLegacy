using Godot;

namespace SpireLegacy;

/// <summary>Independent accents keep paired cosmetic traits visible, including grayscale heirs.</summary>
public partial class TraitAppearance : Node2D
{
    private static readonly List<TraitAppearance> instances = [];
    private HeirSprite sprite = null!;
    private HashSet<string> effects = [];
    private readonly List<Polygon2D> motes = [];
    private readonly List<Line2D> arcs = [];
    private readonly List<string> accents = [];
    private float clock, flash;
    public bool Rainbow => effects.Contains("rainbow");
    private static readonly Dictionary<string, Color> AccentColors = new()
    {
        ["blue"] = new(.25f,.55f,1), ["nature"] = new(.3f,.9f,.4f),
        ["medium"] = new(.75f,.45f,1), ["festive"] = new(1,.3f,.35f)
    };
    public static TraitAppearance Attach(Node2D parent, HeirSprite sprite, float size,FamilyProfile.Heir? heir=null)
    {
        var appearance = new TraitAppearance { Name = "HeirTraitAppearance", sprite = sprite,
            effects = (heir==null?Inheritance.Traits:Inheritance.Rows(heir)).Select(r=>r.Text("effect")).ToHashSet(), Scale = Vector2.One * size };
        parent.AddChild(appearance);
        instances.Add(appearance);
        foreach (var (effect,color) in AccentColors.Where(pair=>appearance.effects.Contains(pair.Key)))
        {
            appearance.accents.Add(effect);
            appearance.AddChild(new Polygon2D { Name = effect + "Accent", Color = color,
                Position = new(90 + appearance.accents.Count * 14,-120), Polygon = [new(0,-8),new(6,0),new(0,8),new(-6,0)] });
        }
        if (appearance.Rainbow)
        {
            for (var i=0;i<6;i++)
            {
                var color=Color.FromHsv(i/6f,.8f,1);
                var mote=new Polygon2D { Color=color, Polygon=[new(0,-6),new(5,0),new(0,6),new(-5,0)] };
                appearance.AddChild(mote); appearance.motes.Add(mote);
                var points=Enumerable.Range(0,9).Select(j=>{
                    var a=(i+j/8f)*Mathf.Tau/6;
                    return new Vector2(Mathf.Cos(a)*76,Mathf.Sin(a)*14);
                }).ToArray();
                var arc=new Line2D { Points=points,Width=3,DefaultColor=color,Antialiased=true };
                appearance.AddChild(arc); appearance.arcs.Add(arc);
            }
        }
        appearance.Refresh(0);
        return appearance;
    }
    public void DamageFlash() { if(effects.Contains("histrionic")) flash=.8f; }
    public object Diagnostic() => new { classId=sprite.ClassId,room=sprite.Name=="SpireLegacyRoomHeir",
        rainbow=Rainbow,motes=motes.Count,arcs=arcs.Count,accents=accents.ToArray(),gray=effects.Contains("gray"),
        sepia=effects.Contains("sepia"),flash,clock,tint=sprite.Modulate.ToString(),opacity=sprite.Modulate.A,
        spriteScale=sprite.Scale.ToString(),visible=IsVisibleInTree(),
        motePositions=motes.Select(m=>m.Position.ToString()).ToArray() };
    // External mod nodes do not have Godot-generated virtual dispatch. Use the existing
    // SceneTree frame callback, which also covers rest/shop nodes and prunes freed rooms.
    public static void Update()
    {
        instances.RemoveAll(a=>!GodotObject.IsInstanceValid(a));
        foreach(var appearance in instances)
            if(appearance.IsInsideTree() && !appearance.GetTree().Paused)
                appearance.Refresh((float)appearance.GetProcessDeltaTime());
    }
    private void Refresh(float delta)
    {
        if(!GodotObject.IsInstanceValid(sprite))return;
        clock=(clock+delta)%60; flash=Math.Max(0,flash-delta);
        var color=effects.Contains("sepia")?new Color(.7f,.55f,.3f):Colors.White;
        // Every selected palette also has its own accent, even when another palette wins the body tint.
        if(accents.Count>0 && !effects.Contains("sepia"))color=AccentColors[accents[0]];
        if(Rainbow && accents.Count==0 && !effects.Contains("sepia"))color=Color.FromHsv(clock/6%1,.35f,1);
        // Tint only the character sprite. Banners, health/status UI and the
        // independent cosmetic accents remain fully opaque.
        color.A=effects.Contains("translucent")?.75f:1f;
        sprite.SetGray(effects.Contains("gray")); sprite.SetDamageFlash(flash/.8f);
        sprite.Modulate=color;
        for(var i=0;i<motes.Count;i++)
        {
            // Ten-second orbits and six-second hues both wrap seamlessly at sixty seconds.
            var angle=clock*Mathf.Tau/10+i*Mathf.Tau/6;
            motes[i].Position=new(Mathf.Cos(angle)*102,-120+Mathf.Sin(angle)*130);
            arcs[i].DefaultColor=Color.FromHsv((clock/6+i/6f)%1,.8f,1);
            motes[i].Color=arcs[i].DefaultColor;
        }
    }
}
