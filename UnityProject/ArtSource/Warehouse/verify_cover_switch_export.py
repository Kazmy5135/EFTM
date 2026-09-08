"""Read exported candidate in isolated Blender and verify metre scale/handedness."""
import argparse
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--candidate', required=True)
parser.add_argument('--report', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
if not bpy.app.background:
    raise RuntimeError('Isolated background Blender only')
candidate = Path(args.candidate)
manifest = json.loads((candidate/'warehouse-manifest.json').read_text(encoding='utf-8'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(candidate/'Warehouse.fbx'))
results = []
for record in manifest['coverSides']:
    obj = bpy.data.objects[record['coverName']]
    corners = [obj.matrix_world @ Vector(p) for p in obj.bound_box]
    unity = [Vector((-p.x,p.z,-p.y)) for p in corners]
    lo = Vector(tuple(min(p[i] for p in unity) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in unity) for i in range(3)))
    expected = next(c for c in manifest['colliders'] if c['name']==record['coverName'])
    center, size = (hi+lo)*.5, hi-lo
    assert (center-Vector(expected['position'])).length < .02, (record['side'],tuple(center))
    assert (size-Vector(expected['size'])).length < .02, (record['side'],tuple(size))
    results.append({'side':record['side'],'unityCenter':list(center),'unitySize':list(size)})
assert 'NearCover' not in bpy.data.objects and 'DoorFrame' not in bpy.data.objects
assert len([o for o in bpy.context.scene.objects if o.type=='MESH']) == manifest['meshCount']
report={'status':'passed','schemaVersion':manifest['schemaVersion'],'covers':results,
        'limitation':'Blender FBX roundtrip verified; Unity ModelImporter check is still required in S3'}
Path(args.report).write_text(json.dumps(report,indent=2),encoding='utf-8')
print('COVER_SWITCH_EXPORT_VERIFIED '+json.dumps(report))
