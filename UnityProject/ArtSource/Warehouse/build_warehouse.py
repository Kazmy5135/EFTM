"""Build the approved EFTM warehouse in Blender; export FBX, textures and previews.

Run: blender --background --factory-startup --python <this file>
Coordinates in helpers use Unity metres (X, Y up, Z down corridor).
The editable Blender scene uses -X, -Z, Y, exported -Z forward / Y up.
"""
import bpy
import math
import json
import random
from pathlib import Path
from mathutils import Vector
import numpy as np

SOURCE = Path(__file__).resolve().parent
PROJECT = SOURCE.parents[1]
ART = PROJECT / 'Assets/_Project/Art/Warehouse'
TEXTURES = ART / 'Textures'
PREVIEWS = PROJECT.parent / 'codex-chat-images'
for directory in (ART / 'Models', TEXTURES, PREVIEWS):
    directory.mkdir(parents=True, exist_ok=True)
random.seed(7107)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.engine = 'CYCLES'
scene.cycles.samples = 40
scene.cycles.use_denoising = True
scene.render.image_settings.file_format = 'PNG'
scene.render.resolution_percentage = 100
scene.world.color = (0.16, 0.18, 0.20)
scene.view_settings.view_transform = 'AgX'
scene.view_settings.exposure = 0.0
scene.render.film_transparent = False
bpy.context.preferences.filepaths.save_version = 0
try:
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type = 'CUDA'
    prefs.get_devices()
    for device in prefs.devices:
        device.use = device.type == 'CUDA'
    if any(d.use for d in prefs.devices):
        scene.cycles.device = 'GPU'
except Exception as error:
    print('CPU rendering fallback:', error)

environment = bpy.data.collections.new('Warehouse - export meshes')
scene.collection.children.link(environment)
presentation = bpy.data.collections.new('Presentation - cameras and lighting')
scene.collection.children.link(presentation)
groups = {}
materials = {}
manifest = {'units': 'meters', 'colliders': [], 'materials': [], 'previewOnlyAnchors': []}


def xyz(p):
    return (-p[0], -p[2], p[1])


def smooth_noise(n, cells, rng):
    small = rng.random((cells + 1, cells + 1))
    small[-1, :] = small[0, :]
    small[:, -1] = small[:, 0]
    q = np.arange(n) * cells / n
    index = q.astype(int)
    t = q - index
    t = t * t * (3 - 2 * t)
    a = small[index[:, None], index[None, :]]
    b = small[index[:, None] + 1, index[None, :]]
    c = small[index[:, None], index[None, :] + 1]
    d = small[index[:, None] + 1, index[None, :] + 1]
    return (a * (1-t[:, None]) + b*t[:, None]) * (1-t[None, :]) + (c*(1-t[:, None]) + d*t[:, None])*t[None, :]


def material(name, color, roughness=0.8, metallic=0.0, texture=True):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    shader = nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    record = {'name': name, 'color': list(color), 'roughness': roughness, 'metallic': metallic}
    if texture:
        n = 1024
        rng = np.random.default_rng(107 + len(materials))
        noise = sum(smooth_noise(n, scale, rng)*strength for scale, strength in [(5,.35),(22,.23),(80,.15),(230,.08)])
        fine = rng.random((n,n))
        tone = .68 + noise * .63 + (fine-.5)*.12
        if name == 'WH_Wood':
            row = np.arange(n)[:,None]
            col = np.arange(n)[None,:]
            grain = np.sin(col*(2*math.pi*46/n) + smooth_noise(n, 18, rng)*7 + np.sin(row*(2*math.pi*2/n)))
            tone += grain*.12
            tone -= (np.sin(col*(2*math.pi*15/n) + smooth_noise(n, 8, rng)*3) > .94)*.16
        if name == 'WH_Paint':
            chipped = smooth_noise(n, 65, rng) > .81
            tone[chipped] *= 1.3
        if name in ('WH_Concrete', 'WH_Floor'):
            tone[fine > .986] *= .62
        pixels = np.ones((n,n,4), dtype=np.float32)
        # Texture values are sRGB, while material inputs are scene-linear.
        srgb = np.power(np.array(color), 1/2.2)
        pixels[:,:,:3] = np.clip(tone[:,:,None]*srgb, 0, 1)
        image = bpy.data.images.new(name+'_Albedo', width=n, height=n)
        image.pixels.foreach_set(pixels.ravel())
        image.filepath_raw = str(TEXTURES / (name+'_Albedo.png'))
        image.file_format = 'PNG'
        image.save()
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = image
        links.new(tex.outputs['Color'], shader.inputs['Base Color'])
        bump = nodes.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .22
        bump.inputs['Distance'].default_value = .018
        links.new(tex.outputs['Color'], bump.inputs['Height'])
        links.new(bump.outputs['Normal'], shader.inputs['Normal'])
        # Tangent normals are exported as actual portable bitmap assets.
        gy, gx = np.gradient(tone)
        normal = np.stack((-gx*2.3, -gy*2.3, np.ones_like(tone)),axis=-1)
        normal /= np.linalg.norm(normal,axis=-1,keepdims=True)
        pixels[:,:,:3] = normal*.5+.5
        normal_image = bpy.data.images.new(name+'_Normal', width=n,height=n)
        normal_image.colorspace_settings.name = 'Non-Color'
        normal_image.pixels.foreach_set(pixels.ravel())
        normal_image.filepath_raw = str(TEXTURES/(name+'_Normal.png'))
        normal_image.file_format = 'PNG'
        normal_image.save()
        record['albedo'] = name+'_Albedo.png'
        record['normal'] = name+'_Normal.png'
    materials[name] = mat
    manifest['materials'].append(record)
    return mat


concrete = material('WH_Concrete', (.24,.245,.23))
floor = material('WH_Floor', (.19,.20,.185), .78)
paint = material('WH_Paint', (.075,.108,.085), .82)
wood = material('WH_Wood', (.16,.105,.055), .84)
steel = material('WH_Steel', (.10,.12,.115), .52, .65)
dark = material('WH_DarkMetal', (.035,.045,.042), .48, .7, False)
rust = material('WH_Rust', (.20,.115,.062), .85, .25, False)
safety = material('WH_SafetyPaint', (.34,.27,.12), .9, 0, False)
lamp = material('WH_Lamp', (.74,.82,.78), .32, 0, False)
lamp.node_tree.nodes.get('Principled BSDF').inputs['Emission Color'].default_value = (.74,.82,.78,1)
lamp.node_tree.nodes.get('Principled BSDF').inputs['Emission Strength'].default_value = 4


def track(obj, group):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    environment.objects.link(obj)
    groups.setdefault(group, []).append(obj)
    return obj


def box(name, pos, size, mat, bevel=.012, group=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz(pos))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (size[0],size[2],size[1])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mat)
    uv = obj.data.uv_layers.active
    for polygon in obj.data.polygons:
        axis = max(range(3),key=lambda i: abs(polygon.normal[i]))
        axes = [i for i in range(3) if i != axis]
        for loop_index in polygon.loop_indices:
            v = obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
            uv.data[loop_index].uv = (v[axes[0]]/2, v[axes[1]]/2)
    if bevel:
        mod = obj.modifiers.new('Small manufactured edge bevel','BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod = obj.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL')
        mod.keep_sharp = True
    return track(obj,group or name)


def cylinder(name, start, end, radius, mat, group):
    a,b = Vector(xyz(start)), Vector(xyz(end))
    direction = b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=direction.length,location=(a+b)/2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat('Z','Y').to_euler()
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return track(obj,group)


def collider(name, pos, size):
    manifest['colliders'].append({'name':name, 'position':list(pos), 'size':list(size)})


def wall(name, pos, size, stripe=True):
    box(name,pos,size,concrete,.01,name)
    collider(name,pos,size)
    if stripe:
        # Thin paint facing strips are separate geometry, not coplanar surfaces.
        x,y,z=pos
        sx,sy,sz=size
        if sx < sz:
            inside=x + (.006+sx/2)*(1 if x<0 else -1)
            box(name+'_paint',(inside,.64,z),(.005,1.26,sz-.02),paint,0,name)
            box(name+'_skirting',(inside,.075,z),(.028,.15,sz-.02),dark,.003,name)
        else:
            box(name+'_paint',(x,.64,z-sz/2-.004),(sx-.02,1.26,.005),paint,0,name)
            if name=='NearCover':
                box(name+'_rearPaint',(x,.64,z+sz/2+.004),(sx-.02,1.26,.005),paint,0,name)


box('Floor',(0,-.12,10),(4,.24,24),floor,.008)
collider('Floor',(0,-.12,10),(4,.24,24))
wall('LeftWall',(-2,1.5,10),(.24,3,24))
wall('RightWall',(2,1.5,10),(.24,3,24))
wall('FarWall',(0,1.5,22),(4,3,.24))
# Interior edge and door frame exactly match the accepted U2 scene geometry.
wall('NearCover',(1.14,1.5,-.02),(1.7,3,.32))
wall('DoorFrame',(.24,1.5,-.02),(.18,3,.46),False)
box('Ceiling',(0,3.12,10),(4,.24,24),concrete,.008)
collider('Ceiling',(0,3.12,10),(4,.24,24))

# Expansion joints, drainage strip and restrained worn floor markings.
for z in (2,6,10,14,18):
    box('FloorJoint',(0,.001,z),(3.75,.003,.009),dark,0,'FloorDetails')
for z in np.arange(.5,21,.19):
    box('DrainSlot',(1.68,.003,float(z)),(.13,.004,.025),dark,0,'FloorDetails')
for z in np.arange(1,19,1.05):
    box('WornLine',(-1.63,.005,float(z)),(.035,.004,.72),safety,0,'FloorDetails')

# Industrial columns, upper beams and trunk conduits.
for i,z in enumerate((2.6,5.85,9.1,12.35,15.6,18.85)):
    box('CeilingBeam',(0,2.89,z),(3.76,.18,.16),steel,.008,'CeilingBeams')
    for side in (-1,1):
        x=side*1.855
        box('WallPillar',(x,1.5,z),(.08,3,.14),steel,.008,'WallStructure')
        box('PillarFoot',(x,.12,z),(.13,.24,.22),dark,.005,'WallStructure')
        box('LampBase',(side*1.80,2.48,z+.45),(.12,.18,.34),dark,.015,'Fixtures')
        box('LampGlass',(side*1.728,2.48,z+.45),(.027,.11,.25),lamp,.014,'Fixtures')
        for dz in (-.11,.11):
            box('LampGuard',(side*1.71,2.48,z+.45+dz),(.025,.13,.009),steel,.003,'Fixtures')
for x,y,r in ((1.78,2.13,.044),(1.78,2.29,.025),(-1.78,2.62,.024)):
    cylinder('ServicePipe',(x,y,.5),(x,y,21.7),r,steel,'Pipework')
    for z in (1,4,7,10,13,16,19,21):
        cylinder('PipeCoupling',(x,y,z-.035),(x,y,z+.035),r*1.25,dark,'Pipework')
        box('PipeBracket',(x,y,z),(.17,.15,.022),dark,.002,'Pipework')
cylinder('PipeRiser',(1.78,.15,2),(1.78,2.14,2),.044,steel,'Pipework')
box('ElectricalCabinet',(-1.79,1.52,6),(.18,.63,.42),steel,.02,'WallDetails')
box('ElectricalLatch',(-1.69,1.46,5.86),(.045,.12,.025),dark,.004,'WallDetails')

# Closed steel door on the far wall, with frame, inset panels, hinges and handle.
box('FarDoor',(0,1.14,21.83),(1.08,2.28,.10),steel,.013,'FarDoor')
for x in (-.59,.59):
    box('DoorJamb',(x,1.2,21.77),(.09,2.4,.13),dark,.006,'FarDoor')
box('DoorHeader',(0,2.39,21.77),(1.25,.09,.13),dark,.006,'FarDoor')
for y in (.53,1.59):
    box('DoorPanel',(0,y,21.768),(.86,.88,.018),steel,.009,'FarDoor')
cylinder('DoorHandle',(.37,1.02,21.69),(.37,1.20,21.69),.018,dark,'FarDoor')
for y in (.35,1.1,1.95):
    cylinder('DoorHinge',(-.53,y,21.73),(-.53,y+.10,21.73),.025,dark,'FarDoor')
box('DoorLight',(0,2.67,21.74),(.38,.18,.12),dark,.012,'FarDoor')
box('DoorLightGlass',(0,2.67,21.67),(.30,.10,.022),lamp,.008,'FarDoor')


def crate(name,x,z,w=1.0,d=.85,h=.95,bottom=.12):
    g=name
    box('CrateCore',(x,bottom+h/2,z),(w-.035,h-.035,d-.035),dark,.01,g)
    # Real individual planks with small gaps, all four sides and lid.
    count=max(4,round(w/.15))
    for i in range(count):
        px=x-w/2+(i+.5)*w/count
        for side in (-1,1):
            box('FacePlank',(px,bottom+h/2,z+side*d/2),(w/count-.007,h,.035),wood,.004,g)
        box('LidPlank',(px,bottom+h,z),(w/count-.006,.04,d),wood,.004,g)
    count=max(4,round(d/.15))
    for i in range(count):
        pz=z-d/2+(i+.5)*d/count
        for side in (-1,1):
            box('SidePlank',(x+side*w/2,bottom+h/2,pz),(.035,h,d/count-.007),wood,.004,g)
    for side in (-1,1):
        for y in (bottom+.11,bottom+h-.11):
            box('FrameRail',(x,y,z+side*(d/2+.027)),(w+.04,.11,.05),wood,.006,g)
        for px in (x-w/2+.07,x+w/2-.07):
            box('FrameStile',(px,bottom+h/2,z+side*(d/2+.045)),(.095,h,.052),wood,.006,g)
            for y in (bottom+.11,bottom+h-.11):
                cylinder('Nail',(px,y,z+side*(d/2+.073)),(px,y,z+side*(d/2+.078)),.008,dark,g)
    for dx in (-w*.32,w*.32):
        box('PalletRunner',(x+dx,bottom-.075,z),(.12,.12,d+.1),wood,.008,g)
    collider(name,(x,(bottom+h)/2,z),(w+.10,bottom+h+.025,d+.15))


crate('CrateTallLower',-1.12,17.7,.93,.9,.98)
crate('CrateTallUpper',-1.12,17.7,.93,.9,.98,1.23)
crate('CrateMid',1.12,15.4,.91,.85,.94)
crate('CrateLow',1.10,19.8,.95,.72,.51)
box('ConcreteCover',(1.06,.48,12.4),(1.04,.96,.66),concrete,.025,'ConcreteCover')
box('ConcreteCoverFoot',(1.06,.06,12.4),(1.11,.12,.74),concrete,.012,'ConcreteCover')
collider('ConcreteCover',(1.06,.48,12.4),(1.11,.96,.74))
box('TallCabinet',(-1.42,.91,15.0),(.63,1.82,.74),steel,.02,'TallCabinet')
box('CabinetDoor',(-1.42,.94,14.618),(.54,1.63,.028),steel,.012,'TallCabinet')
box('CabinetHandle',(-1.23,1.03,14.586),(.025,.14,.035),dark,.006,'TallCabinet')
for y in (1.49,1.53,1.57,1.61):
    box('CabinetVent',(-1.42,y,14.599),(.37,.013,.008),dark,.003,'TallCabinet')
collider('TallCabinet',(-1.42,.91,15.0),(.63,1.82,.78))

# Debris is restricted to wall bases; sparse enough for the phone viewport.
for i in range(48):
    side=random.choice((-1,1))
    x=side*random.uniform(1.72,1.83)
    z=random.uniform(.6,21.5)
    chip=box('ConcreteChip',(x,.015,z),(random.uniform(.02,.07),.025,random.uniform(.03,.12)),concrete,.004,'Debris')
    chip.rotation_euler.z=random.random()*math.pi

# Merge only within meaningful editable modules to keep Unity renderer count low.
export_objects=[]
for group,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=group
    # World origin makes imported axis validation unambiguous.
    scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    export_objects.append(obj)
    if group in ('Ceiling','CeilingBeams'):
        obj.hide_render=True

bpy.ops.object.select_all(action='DESELECT')
for obj in export_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active=export_objects[0]
bpy.ops.export_scene.fbx(filepath=str(ART/'Models/Warehouse.fbx'),use_selection=True,
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False,
    mesh_smooth_type='FACE',path_mode='STRIP',bake_anim=False)
manifest['meshCount']=len(export_objects)
manifest['triangles']=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in export_objects)
(ART/'warehouse-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')


def aim(obj,point):
    obj.rotation_euler=(Vector(xyz(point))-obj.location).to_track_quat('-Z','Y').to_euler()


def area(name,pos,power,size,color,target):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power
    data.shape='DISK'
    data.size=size
    data.color=color
    obj=bpy.data.objects.new(name,data)
    presentation.objects.link(obj)
    obj.location=xyz(pos)
    aim(obj,target)
    return obj


area('OverviewKey',(-3,12,7),2000,14,(.86,.91,1),(0,0,10))
area('OverviewFill',(5,7,16),1400,10,(1,.92,.78),(0,0,12))
for z in (2.5,7,11.5,16,20):
    area('CeilingSoftLight',(0,2.83,z),100,2.3,(.86,.92,1),(0,0,z))

camera_data=bpy.data.cameras.new('OverviewCamera')
camera=bpy.data.objects.new('OverviewCamera',camera_data)
presentation.objects.link(camera)
camera.location=xyz((0,25,-12))
aim(camera,(0,.55,10))
camera_data.type='ORTHO'
camera_data.ortho_scale=24
camera_data.lens=45
scene.camera=camera
scene.render.resolution_x=1100
scene.render.resolution_y=1650
scene.render.filepath=str(PREVIEWS/'warehouse-blender-overview.png')
# Store a useful solid/material viewport and pack source textures for portability.
for screen in bpy.data.screens:
    for space in screen.areas:
        if space.type=='VIEW_3D':
            space.spaces.active.region_3d.view_perspective='CAMERA'
for img in bpy.data.images:
    if img.source=='FILE' and img.has_data:
        img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Warehouse.blend'))
bpy.ops.render.render(write_still=True)
perspective = camera.copy()
perspective.data = camera_data.copy()
perspective.name = 'PresentationCamera'
presentation.objects.link(perspective)
perspective.data.type = 'PERSP'
perspective.data.lens = 38
perspective.location = xyz((0,10,-7))
aim(perspective,(0,0,7))
scene.camera = perspective
scene.render.filepath = str(PREVIEWS/'warehouse-blender-perspective.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Warehouse.blend'))
bpy.ops.render.render(write_still=True)
print('WAREHOUSE_BUILD_OK',json.dumps({'meshes':manifest['meshCount'],'triangles':manifest['triangles']}))
