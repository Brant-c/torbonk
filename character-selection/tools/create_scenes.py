from pathlib import Path
R=Path(__file__).resolve().parents[1]
scene='''[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://Frontend/Scripts/{script}.cs" id="1"]

[node name="{name}" type="Control"]
{layout}
script = ExtResource("1")
{extra}
'''
for name in ['MainMenu','Faction','BaseClass','AdvancedClass','Customization','Galaxy','Ready','CharacterPreview']:
    script='FrontendFlow' if name=='MainMenu' else 'CharacterPreview' if name=='CharacterPreview' else 'SelectionPage'
    extra=('StepId = "'+name+'"') if script=='SelectionPage' else ''
    layout='layout_mode = 3\nanchors_preset = 15\nanchor_right = 1.0\nanchor_bottom = 1.0\ngrow_horizontal = 2\ngrow_vertical = 2' if name=='MainMenu' else 'layout_mode = 0'
    (R/'Frontend/Scenes'/f'{name}.tscn').write_text(scene.format(script=script,name=name,extra=extra,layout=layout))
