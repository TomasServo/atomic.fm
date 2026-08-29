# Atomic.FM — use existing repo `TomasServo/atomic.fm`

No new repo. Push this plugin into the GitHub repo you already have.

## 1. Make the repo public (required for Pulsar)

1. Open https://github.com/TomasServo/atomic.fm
2. **Settings** → scroll to **Danger Zone**
3. **Change repository visibility** → **Public**

PluginHub cannot use a private repo.

## 2. Put this code into that repo (GitHub Desktop)

1. Open **GitHub Desktop**
2. Top-left: click the current repo name → choose **`atomic.fm`**
3. Click **Show in Explorer**
4. Download [AtomicFM-PluginHub-Ready.zip](/opt/cursor/artifacts/AtomicFM-PluginHub-Ready.zip)
5. Extract the zip → copy **all files inside** into the Explorer folder → **Replace**
6. Back in GitHub Desktop: you should see many changes
7. Summary: `Atomic.FM release`
8. **Commit to main** → **Push origin**

## 3. Submit to PluginHub

1. Fork https://github.com/StarCpt/PluginHub
2. In your fork: **Add file → Create new file**
3. Path: `Plugins/atomic.fm.xml`
4. Paste contents from this project’s `PluginHub/Plugins/atomic.fm.xml`
5. Commit → **Open pull request**

Do not use `Plugins/Mods` (that’s for Workshop mods only).
