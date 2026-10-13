"""Original vector geometry and synthesized SFX; no source-game artwork or recordings."""
import hashlib,json,math,random,struct,wave
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'src/main/resources/heir/presentation';out.mkdir(parents=True,exist_ok=True);rows=[]
def record(path,kind):
    rows.append(dict(id=path.stem+'-'+kind,path=path.relative_to(root/'src/main/resources').as_posix(),kind=kind,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),author='CapeKid',license='MIT',provenance='Original geometry / deterministic sine-and-noise synthesis; scripts/author-heir-presentation.py'))
def svg(name,body,size=128):
    p=out/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 128 128">'+body+'</svg>');record(p,'vector')
for cls,color in [('knight','#dfb968'),('mage','#bb94e9'),('ranger','#83c690')]:
    rgb=tuple(int(color[i:i+2],16)/255 for i in [1,3,5]);tint=', '.join(str(round(v,5)) for v in rgb)
    shader='shader_type canvas_item; uniform float threshold = 0.0; uniform vec4 tint : source_color; void fragment() { float edge = UV.x * 0.65 + UV.y * 0.25 + abs(fract(UV.y * 7.0) - 0.5) * 0.1; float a = threshold < 0.001 ? 0.0 : smoothstep(edge - 0.015, edge + 0.015, threshold); COLOR = vec4(tint.rgb * 0.22, a); }'
    p=out/f'{cls}-transition.tres';p.write_text('[gd_resource type="ShaderMaterial" load_steps=2 format=3]\n\n[sub_resource type="Shader" id="Shader_family"]\ncode = '+json.dumps(shader)+'\n\n[resource]\nshader = SubResource("Shader_family")\nshader_parameter/threshold = 0.0\nshader_parameter/tint = Color('+tint+', 1)\n');record(p,'material')
    shape={'knight':'-20, -24, 20, -24, 20, 6, 0, 24, -20, 6','mage':'0, -28, 7, -7, 28, 0, 7, 7, 0, 28, -7, 7, -28, 0, -7, -7','ranger':'-25, -7, 5, -7, 5, -18, 28, 0, 5, 18, 5, 7, -25, 7'}[cls]
    p=out/f'{cls}-trail.tscn';p.write_text('''[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://src/Core/Nodes/Vfx/NCardTrailVfx.cs" id="1"]
[ext_resource type="Script" path="res://src/Core/Nodes/Vfx/NCardTrail.cs" id="2"]
[sub_resource type="Gradient" id="Gradient_ribbon"]
colors = PackedColorArray('''+tint+''', 0, '''+tint+''', 1)

[node name="HeirCardTrail" type="Node2D"]
script = ExtResource("1")

[node name="Sprites" type="Node2D" parent="."]

[node name="Trails" type="Node2D" parent="."]

[node name="FamilyRibbon" type="Line2D" parent="Trails"]
width = 16.0
gradient = SubResource("Gradient_ribbon")
begin_cap_mode = 2
end_cap_mode = 2
antialiased = true
script = ExtResource("2")

[node name="Crest" type="Polygon2D" parent="Sprites"]
polygon = PackedVector2Array('''+shape+''')
color = Color('''+tint+''', 0.45)
''');record(p,'scene')
    layers=[f'<circle cx="64" cy="64" r="57" fill="#1d2631" stroke="#111820" stroke-width="8"/><circle cx="64" cy="64" r="51" fill="none" stroke="{color}" stroke-width="5"/>',
      f'<circle cx="64" cy="64" r="43" fill="none" stroke="{color}" stroke-width="2" stroke-dasharray="14 8"/>',
      f'<path d="M64 7l3 5-3 5-3-5zM64 111l3 5-3 5-3-5zM7 64l5-3 5 3-5 3zM111 64l5-3 5 3-5 3z" fill="{color}"/>',
      f'<path d="M44 23l-3-16 14 8 9-12 9 12 14-8-3 16z" fill="{color}" stroke="#111820" stroke-width="3"/>',
      f'<path d="M41 105q23 16 46 0l-4 11q-19 9-38 0z" fill="{color}" stroke="#111820" stroke-width="2"/>']
    for i,b in enumerate(layers,1):svg(f'{cls}-energy-{i}',b)
    energy_symbol=f'<circle cx="64" cy="64" r="54" fill="#28313b" stroke="{color}" stroke-width="10"/><path d="M39 35l-4-16 19 8 10-18 10 18 19-8-4 16z" fill="{color}"/>'
    svg(cls+'-energy-icon',energy_symbol)
    # Unsized [img] tags use the intrinsic dimensions, including native text measurement.
    # Match the native sprite-font icons without shrinking card costs or counter layers.
    svg(cls+'-energy-text',energy_symbol,size=24)
    symbol={'knight':'<path d="M48 48h32v24L64 85 48 72z"/>','mage':'<path d="M64 43l6 16 17 5-17 6-6 17-6-17-17-6 17-5z"/>','ranger':'<path d="M46 82l32-32m-19 0h19v19m-30 5l7 7" fill="none" stroke-width="7"/>'}[cls]
    svg(cls+'-cookie',f'<circle cx="64" cy="64" r="51" fill="#c99558" stroke="#533628" stroke-width="6"/><circle cx="64" cy="64" r="40" fill="{color}"/><g fill="#654531" stroke="#654531">{symbol}</g><g fill="#593a29"><circle cx="34" cy="39" r="4"/><circle cx="96" cy="48" r="4"/><circle cx="30" cy="84" r="4"/><circle cx="91" cy="94" r="4"/></g>')
    svg(cls+'-cookie-outline','<circle cx="64" cy="64" r="56" fill="white"/>')
    svg(cls+'-outline','<path d="M28 19h72v62L64 113 28 81z" fill="white"/>')
    for role in ['attack','cast','die','select','transition']:
        rate=22050;duration={'attack':.25,'cast':.42,'die':.65,'select':.6,'transition':.5}[role]
        rng=random.Random(cls+role);data=bytearray();base={'knight':220,'mage':440,'ranger':330}[cls]
        for i in range(int(rate*duration)):
            t=i/rate;x=t/duration;env=math.sin(math.pi*x)**1.5
            if role=='attack':v=(rng.uniform(-1,1)*.45+math.sin(2*math.pi*(base*t+60*t*t))*.25)*env*(1-x)
            elif role=='die':v=(math.sin(2*math.pi*base*(t-.45*t*t))*.45+math.sin(2*math.pi*base*.5*t)*.16)*env*(1-x)
            elif role=='transition':v=(rng.uniform(-1,1)*.15+math.sin(2*math.pi*(base*t+220*t*t))*.32)*env
            else:v=sum(math.sin(2*math.pi*base*f*t)*a for f,a in [(1,.35),(1.5,.18),(2,.1)])*env
            data.extend(struct.pack('<h',int(max(-.8,min(.8,v))*20000)))
        p=out/f'{cls}-{role}.wav'
        with wave.open(str(p),'wb') as w:w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes(data)
        record(p,'audio')
(root/'sheets/presentation_art.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Authored',len(rows),'original presentation assets')
