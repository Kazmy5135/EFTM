"""008 geometry contract, authored in Unity metres and exported by Blender."""
REVISION = 'cover-switch-008-r1'
COVERS = [
    {'side': side, 'name': 'NearCover' + side,
     'position': [sign * 1.25, 1.5, -.02], 'size': [1.26, 3.0, .32]}
    for side, sign in [('Right', 1), ('Left', -1)]
]


def contract():
    sides = []
    for side, sign in [('Right', 1), ('Left', -1)]:
        sides.append({
            'side': side, 'coverName': 'NearCover' + side,
            'playerAnchor': [sign * .73, 0, -.85],
            'hiddenPosition': [sign * .73, 1.55, -.85],
            'hiddenEuler': [0, 0, 0],
            'exposedPosition': [sign * .22, 1.495, -.65],
            'exposedEuler': [0, sign * 1.03, sign * 7.16],
        })
    paths = []
    for source, target, sign in [('Right', 'Left', 1), ('Left', 'Right', -1)]:
        paths.append({'source': source, 'target': target,
                      'controlPoints': [[sign * x, 0, -.85] for x in [.73, .24, -.24, -.73]],
                      'lookYawDegrees': -sign * 60})
    return {'schemaVersion': 2, 'sourceRevision': REVISION, 'units': 'meters',
            'coordinateSystem': 'Unity-X-right-Y-up-Z-forward',
            'coverSides': sides, 'switchPaths': paths,
            'motion': {'turnSeconds': .15, 'moveSeconds': .8,
                       'landingSeconds': .2, 'lookBackSeconds': .55}}
