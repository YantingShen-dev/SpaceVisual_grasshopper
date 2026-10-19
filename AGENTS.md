# Local workflow

- Do not push to GitHub, push tags, or publish a release unless the user has personally run the updated plugin and explicitly authorized uploading that update. Earlier publication permissions do not carry forward.
- Name distributed plugin files `SpaceVisual_<version>.gha`, for example `SpaceVisual_0.2.3.gha`. Keep only the current distributed binary in `bin/Release/`; retain the stable internal assembly identity.
- Do not launch Rhino automatically. Use an already open session for validation; ask the user to save and close Rhino before replacing an installed plugin.
- Keep local candidates and validation outputs under `artifacts/`, separate from installed/released builds.
