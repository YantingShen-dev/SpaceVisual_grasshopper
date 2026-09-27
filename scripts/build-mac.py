#!/usr/bin/env python3
"""Build against an installed Rhino 7 for Mac; no system .NET installation needed."""
import argparse
import hashlib
import json
import pathlib
import plistlib
import subprocess
import xml.etree.ElementTree as ET

root = pathlib.Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--rhino', type=pathlib.Path, default=pathlib.Path('/Applications/Rhino 7.app'))
p.add_argument('--compiler', type=pathlib.Path, required=True, help='Roslyn 4.8 tasks/net472/csc.exe')
a = p.parse_args()
resources = a.rhino / 'Contents/Frameworks/RhCore.framework/Versions/A/Resources'
gh = resources / 'ManagedPlugIns/GrasshopperPlugin.rhp'
mono_resources = sorted((resources.parent / 'Frameworks/Mono64Rhino.framework/Versions').glob('*/Resources'))
if not mono_resources:
    p.error('Rhino 7 bundled Mono was not found')
mono = mono_resources[0]
api = mono / 'lib/mono/4.8-api'
compiler = a.compiler.resolve()
for required in [compiler, mono / 'bin/mono', api / 'mscorlib.dll', resources / 'RhinoCommon.dll', gh / 'Grasshopper.dll']:
    if not required.exists():
        p.error('Missing: ' + str(required))
out = root / 'bin/Mac/Release'
obj = root / 'obj/Mac'
out.mkdir(parents=True, exist_ok=True)
obj.mkdir(parents=True, exist_ok=True)
project = ET.parse(root / 'SpaceVisual.csproj').getroot()
meta = obj / 'AssemblyInfo.cs'
meta.write_text('\n'.join('[assembly: System.Reflection.%s(%s)]' % (attr, json.dumps(project.findtext('.//' + prop))) for attr, prop in [('AssemblyVersion', 'AssemblyVersion'), ('AssemblyFileVersion', 'FileVersion'), ('AssemblyTitle', 'Title'), ('AssemblyCompany', 'Company'), ('AssemblyCopyright', 'Copyright')]) + '\n[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]\n')
refs = [api / (name + '.dll') for name in ['mscorlib', 'System', 'System.Core', 'System.Drawing', 'System.Windows.Forms']]
refs += [resources / 'RhinoCommon.dll', gh / 'Grasshopper.dll', gh / 'GH_IO.dll']
sources = sorted(f for f in root.rglob('*.cs') if not set(f.relative_to(root).parts).intersection({'bin', 'obj', 'tests'}))
args = ['-nologo', '-target:library', '-platform:anycpu', '-langversion:12', '-nullable:enable', '-optimize+', '-deterministic+', '-nostdlib+', '-out:' + str(out / 'SpaceVisual.gha')]
args += ['-reference:' + str(f) for f in refs]
args += ['-resource:' + str(f) + ',icons.' + f.name for f in sorted((root / 'icon').glob('*.png'))]
args += [str(f) for f in sources] + [str(meta)]
subprocess.run([str(mono / 'bin/mono'), str(compiler)] + args, check=True, cwd=root)
version = plistlib.loads((a.rhino / 'Contents/Info.plist').read_bytes()).get('CFBundleShortVersionString')
manifest = {'source_commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(), 'local_changes': subprocess.check_output(['git', 'status', '--short'], cwd=root, text=True).strip(), 'rhino_sdk': str(resources), 'rhino_version': version, 'framework': 'net48 / Rhino 7 Mono', 'platform': 'AnyCPU', 'compiler': 'Microsoft.Net.Compilers.Toolset 4.8.0', 'sha256': hashlib.sha256((out / 'SpaceVisual.gha').read_bytes()).hexdigest(), 'runtime_status': 'Pending in-Rhino testing; Rhino 8 and Apple Silicon not verified'}
(out / 'build-info.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(out / 'SpaceVisual.gha')
