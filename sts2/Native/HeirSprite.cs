using Godot;
using System.Text.Json;

namespace SpireLegacy;

/// <summary>Original class artwork animated independently of the hidden placeholder Spine body.</summary>
public partial class HeirSprite : AnimatedSprite2D
{
    public string ClassId { get; private set; } = "knight";
    public List<string> Events { get; } = new();
    private static readonly Dictionary<string,int[]> Poses = new()
    {
        ["idle"] = [0,1,2,3], ["attack"] = [4,5,6,7],
        ["skill"] = [8], ["hurt"] = [9], ["dead"] = [10], ["victory"] = [11]
    };
    private int cell;
    private int[] feet = [], insets = [];
    private readonly ShaderMaterial material = new() { Shader = new Shader { Code = "shader_type canvas_item; uniform bool gray = false; void fragment() { vec4 c = texture(TEXTURE, UV) * COLOR; if (c.a < 0.06) { discard; } if (gray) { float g = dot(c.rgb, vec3(0.299,0.587,0.114)); c.rgb = vec3(g); } COLOR = c; }" } };

    public static HeirSprite Create(string classId, float size)
    {
        var art = Design.Get("character_art",classId);
        var sprite = new HeirSprite { ClassId = classId, Centered = false, TextureFilter = TextureFilterEnum.Linear };
        sprite.Material = sprite.material;
        sprite.cell = art.Number("cell");
        sprite.feet = art.Json.GetProperty("feet").EnumerateArray().Select(v => v.GetInt32()).ToArray();
        sprite.insets = art.Json.GetProperty("insets").EnumerateArray().Select(v => v.GetInt32()).ToArray();
        sprite.Scale = Vector2.One * (230f / art.Number("height") * size);
        var atlas = Runtime.Texture(art.Text("path"));
        var regions = art.Json.GetProperty("regions").EnumerateArray().Select(row => row.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
        var frames = new SpriteFrames(); frames.RemoveAnimation("default");
        foreach (var (pose,indices) in Poses)
        {
            frames.AddAnimation(pose);
            frames.SetAnimationLoop(pose,pose is "idle" or "victory");
            frames.SetAnimationSpeed(pose,pose switch { "idle" => 4, "attack" => 10, "skill" => 2.5, "hurt" => 4, _ => 1 });
            foreach (var index in indices)
            {
                var rect = regions[index];
                frames.AddFrame(pose,new AtlasTexture { Atlas = atlas, Region = new Rect2(rect[0],rect[1],rect[2],rect[3]) });
            }
        }
        sprite.SpriteFrames = frames;
        sprite.FrameChanged += sprite.AlignFeet;
        sprite.AnimationChanged += sprite.AlignFeet;
        sprite.AnimationFinished += () => { if (sprite.Animation.ToString() is not ("dead" or "victory")) sprite.PlayPose("idle"); };
        sprite.PlayPose("idle");
        return sprite;
    }
    public void SetGray(bool gray) => material.SetShaderParameter("gray",gray);
    public void PlayPose(string pose)
    {
        if (!Poses.ContainsKey(pose) || (Animation == "dead" && pose != "idle") || (Animation == pose && IsPlaying())) return;
        Events.Add(pose);
        if (Events.Count > 24) Events.RemoveAt(0);
        Play(pose);
        AlignFeet();
    }
    private void AlignFeet()
    {
        if (!Poses.TryGetValue(Animation.ToString(),out var indices) || feet.Length == 0) return;
        var index = indices[Math.Clamp(Frame,0,indices.Length - 1)];
        Offset = new Vector2(-cell / 2f + insets[index],-feet[index]);
    }
}
