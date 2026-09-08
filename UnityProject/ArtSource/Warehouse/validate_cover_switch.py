"""Blender-side 008 source checks. Does not substitute Unity encounter/UI tests."""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

# Existing 5d0e3cb encounter positions and 20 equal visibility samples.
POSITIONS = [(-.42, 0, 19), (.1, 0, 20), (.42, 0, 18.6), (.85, 0, 19.6), (-.15, 0, 14)]
SAMPLES = [(-.055,1.74,-.07),(.055,1.68,-.07),(-.2,1.48,0),(.2,1.48,0),
    (-.12,1.38,-.1),(.12,1.38,-.1),(-.12,1.2,-.1),(.12,1.2,-.1),
    (-.28,1.32,0),(.28,1.32,0),(-.28,1.08,0),(.28,1.08,0),
    (-.1,.93,0),(.1,.93,0),(-.12,.72,0),(.12,.72,0),
    (-.12,.45,0),(.12,.45,0),(-.12,.15,0),(.12,.15,0)]


def xyz(p):
    return Vector((-p[0], -p[2], p[1]))


def validate(manifest, collection):
    bpy.context.view_layer.update()
    meshes = [o for o in collection.objects if o.type == 'MESH']
    trees = [(o.name, BVHTree.FromPolygons([o.matrix_world @ v.co for v in o.data.vertices],
              [list(p.vertices) for p in o.data.polygons])) for o in meshes]

    def visible(start, point, only_near=False):
        a, b = xyz(start), xyz(point)
        delta = b-a
        return not any(tree.ray_cast(a, delta.normalized(), delta.length-.001)[0] is not None
            for name, tree in trees if not only_near or name.startswith('NearCover'))

    hidden, exposed = {}, {}
    for side in manifest['coverSides']:
        h, e = side['hiddenPosition'], side['exposedPosition']
        hidden[side['side']], exposed[side['side']] = [], []
        for p in POSITIONS:
            points = [tuple(p[i]+s[i] for i in range(3)) for s in SAMPLES]
            # Require near-cover geometry, not merely off-screen targets or distant props.
            h_count = sum(visible(h, point, True) for point in points)
            assert h_count == 0, (side['side'], p, 'hidden position leaks', h_count)
            hidden[side['side']].append(h_count)
            e_count = sum(visible(e, point) for point in points)
            exposed[side['side']].append(e_count)
        assert max(exposed[side['side']]) >= 2, 'Each side needs observable targets'
    for i in range(5):
        assert max(exposed[s][i] for s in exposed) >= 2, ('No side can observe position', i)

    # Conservative body AABB encloses the intended capsule. Flat path permits
    # continuous segment clearance by interval intersection, not just point checks.
    body_sweeps = 0
    for path in manifest['switchPaths']:
        points = path['controlPoints']
        bounds_min = [min(p[i] for p in points) + (-.25 if i != 1 else .001) for i in range(3)]
        bounds_max = [max(p[i] for p in points) + (.25 if i != 1 else 1.8) for i in range(3)]
        for c in manifest['colliders']:
            lo = [c['position'][i]-c['size'][i]/2 for i in range(3)]
            hi = [c['position'][i]+c['size'][i]/2 for i in range(3)]
            overlaps = all(bounds_max[i] > lo[i] and bounds_min[i] < hi[i] for i in range(3))
            assert not overlaps, ('Player swept body intersects', c['name'])
        body_sweeps += 1

    # Near-plane conservative swept envelope during +/-60-degree turn and traverse.
    radius = math.sqrt(.05**2 + (.05*math.tan(math.radians(27)))**2 * 1.25)
    assert radius < .06
    for c in manifest['colliders']:
        lo = [c['position'][i]-c['size'][i]/2 for i in range(3)]
        hi = [c['position'][i]+c['size'][i]/2 for i in range(3)]
        a, b = [-.73-radius,1.55-radius,-.85-radius], [.73+radius,1.55+radius,-.85+radius]
        assert not all(b[i] > lo[i] and a[i] < hi[i] for i in range(3)), ('Camera envelope intersects', c['name'])
    return {'status':'passed', 'revision':manifest['sourceRevision'], 'bodySweepDirections':body_sweeps,
        'hiddenVisibleSamplesOutOf20':hidden, 'environmentOnlyExposedSamplesOutOf20':exposed,
        'cameraNearEnvelopeRadius':radius,
        'limitations':['Unity enemy cover primitives, actor renderers and viewport projection not included',
                      'No runtime movement, pointer input, gameplay or Unity import claimed']}
