"""Non-destructive Blender 008 candidate export from saved Warehouse.blend.

blender --background <Warehouse.blend> --python <this.py> -- --output-root <candidate>
Never touches the interactive Blender scene or the currently installed Unity assets.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(SOURCE))
from cover_switch_layout import COVERS, contract
from validate_cover_switch import validate

parser = argparse.ArgumentParser()
parser.add_argument('--output-root', required=True)
parser.add_argument('--preview-root', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
output = Path(args.output_root).resolve()
previews = Path(args.preview_root).resolve()
original = SOURCE / 'Warehouse.blend'
if not bpy.app.background or Path(bpy.data.filepath).resolve() != original.resolve():
    raise RuntimeError('Run in a separate background process with the saved Warehouse.blend.')
if output == SOURCE or output.is_relative_to(SOURCE.parents[1] / 'Assets'):
    raise RuntimeError('Candidate must not overwrite installed source or Unity assets.')
output.mkdir(parents=True, exist_ok=True)
previews.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
environment = bpy.data.collections['Warehouse - export meshes']
old = bpy.data.objects.get('NearCover')
if old is None or bpy.data.objects.get('DoorFrame') is None:
    raise RuntimeError('Expected original single-sided source geometry.')
material = next((m for m in old.data.materials if m and 'Concrete' in m.name), old.data.materials[0])
source_hash = hashlib.sha256(original.read_bytes()).hexdigest()
for name in ('NearCover', 'DoorFrame'):
    bpy.data.objects.remove(bpy.data.objects[name], do_unlink=True)


def xyz(p):
    return (-p[0], -p[2], p[1])


for record in COVERS:
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz(record['position']))
    obj = bpy.context.object
    obj.name = record['name']
    obj.dimensions = (record['size'][0], record['size'][2], record['size'][1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    environment.objects.link(obj)
    obj.data.materials.append(material)
    bevel = obj.modifiers.new('Concrete edge', 'BEVEL')
    bevel.width = .01
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

art = SOURCE.parents[1] / 'Assets/_Project/Art/Warehouse'
manifest = json.loads((art / 'warehouse-manifest.json').read_text(encoding='utf-8'))
manifest.update(contract())
manifest['sourceBlendSha256'] = source_hash
manifest['generatorSha256'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
manifest['layoutSha256'] = hashlib.sha256((SOURCE / 'cover_switch_layout.py').read_bytes()).hexdigest()
manifest['colliders'] = [c for c in manifest['colliders'] if c['name'] not in ('NearCover', 'DoorFrame')]
manifest['colliders'].extend({k: r[k] for k in ('name', 'position', 'size')} for r in COVERS)
anchors = bpy.data.collections.new('Cover switch - gameplay anchors')
scene.collection.children.link(anchors)
for side in manifest['coverSides']:
    for key in ('playerAnchor', 'hiddenPosition', 'exposedPosition'):
        obj = bpy.data.objects.new(side['side'] + '-' + key, None)
        obj.location = xyz(side[key])
        obj.empty_display_size = .12
        anchors.objects.link(obj)

meshes = [o for o in environment.objects if o.type == 'MESH']
manifest['meshCount'] = len(meshes)
manifest['triangles'] = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
report = validate(manifest, environment)
(previews / 'switch-geometry-validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.export_scene.fbx(filepath=str(output / 'Warehouse.fbx'), use_selection=True,
    object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_space_transform=True, use_mesh_modifiers=True, add_leaf_bones=False,
    mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
(output / 'warehouse-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
scene['coverSwitchSourceRevision'] = manifest['sourceRevision']
for img in bpy.data.images:
    if img.source == 'FILE' and img.has_data and not img.packed_file:
        img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'Warehouse.blend'))

# Preview only: do not replace the source's presentation camera or visibility settings.
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 8
scene.cycles.use_denoising = True
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
camera_data = bpy.data.cameras.new('008-Preview')
camera = bpy.data.objects.new('008-Preview', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.clip_start = .05
camera_data.clip_end = 60
camera_data.lens_unit = 'FOV'
camera_data.sensor_fit = 'VERTICAL'
camera_data.angle = math.radians(54)
for name in ('Ceiling', 'CeilingBeams'):
    if bpy.data.objects.get(name):
        bpy.data.objects[name].hide_render = False


def render(name, position, target, overview=False):
    camera.location = xyz(position)
    camera.rotation_euler = (Vector(xyz(target)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.resolution_x = 540 if not overview else 960
    scene.render.resolution_y = 1080 if not overview else 720
    scene.render.filepath = str(previews / (name + '.png'))
    bpy.ops.render.render(write_still=True)


render('switch-right-hidden', [.73, 1.55, -.85], [.73, 1.55, 18])
render('switch-left-hidden', [-.73, 1.55, -.85], [-.73, 1.55, 18])
render('switch-left-peek', [-.22, 1.495, -.65], [0, 1.495, 18])
render('switch-right-peek', [.22, 1.495, -.65], [0, 1.495, 18])
render('switch-path-middle', [0, 1.55, -.85], [0, 1.55, 18])
for name in ('Ceiling', 'CeilingBeams'):
    if bpy.data.objects.get(name):
        bpy.data.objects[name].hide_render = True
render('switch-layout', [0, 6, -6], [0, 0, .3], True)
print('COVER_SWITCH_CANDIDATE_OK ' + json.dumps({'output': str(output), 'meshes': len(meshes),
      'revision': manifest['sourceRevision'], 'originalSourceSha256': source_hash}))
