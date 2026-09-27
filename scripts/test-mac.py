#!/usr/bin/env python3
"""Run managed checks outside Rhino; native geometry/UI still need host testing."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--compiler', type=Path, required=True)
p.add_argument('--rhino', type=Path, default=Path('/Applications/Rhino 7.app'))
a = p.parse_args()
root = Path(__file__).resolve().parents[1]
res = a.rhino / 'Contents/Frameworks/RhCore.framework/Versions/A/Resources'
mr = sorted((res.parent / 'Frameworks/Mono64Rhino.framework/Versions').glob('*/Resources'))[0]
gh = res / 'ManagedPlugIns/GrasshopperPlugin.rhp'
work = root / 'obj/Mac/tests'
work.mkdir(parents=True, exist_ok=True)
# Standalone Mono probes .dll, whereas Grasshopper explicitly loads .gha.
shutil.copy2(root / 'bin/Mac/Release/SpaceVisual.gha', work / 'SpaceVisual.dll')
output = work / 'MacManagedSmoke.exe'
refs = [work / 'SpaceVisual.dll', res / 'RhinoCommon.dll', gh / 'Grasshopper.dll', gh / 'GH_IO.dll']
subprocess.run([str(mr / 'bin/mono'), str(a.compiler.resolve()), '-nologo', '-out:' + str(output), '-r:System.Drawing.dll'] + ['-r:' + str(x) for x in refs] + [str(root / 'tests/MacManagedSmoke.cs')], check=True)
env = dict(os.environ, MONO_PATH=os.pathsep.join(map(str, [work, res, gh])))
r = subprocess.run([str(mr / 'bin/mono'), str(output)], env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
print(r.stdout)
(root / 'bin/Mac/Release/managed-test-results.txt').write_text(r.stdout)
raise SystemExit(r.returncode)
