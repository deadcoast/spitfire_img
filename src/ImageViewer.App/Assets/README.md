# App assets

`spitfire-icon.svg` is the source icon asset for `spitfire.img`.

Derived executable, window, and package assets are generated from the repository-controlled asset design with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-app-assets.ps1
```

The generated outputs are committed because they are consumed by Windows app identity and release packaging:

- `spitfire.ico`
- `Square44x44Logo.png`
- `Square150x150Logo.png`
- `Wide310x150Logo.png`
