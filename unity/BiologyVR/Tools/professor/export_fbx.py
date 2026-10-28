import bpy, os
bpy.ops.wm.open_mainfile(filepath=os.path.abspath("professor_v4.blend"))
for o in bpy.data.objects:
    if o.type == 'MESH' and o.data.shape_keys:
        for k in o.data.shape_keys.key_blocks: k.value = 0
os.makedirs("unity", exist_ok=True)
bpy.ops.export_scene.fbx(filepath=os.path.abspath("unity/Professor.fbx"), use_selection=False, object_types={'MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
for o in bpy.data.objects:
    if o.type == 'MESH': print(o.name, len(o.data.vertices), [m.name for m in o.data.materials if m], [k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [])
