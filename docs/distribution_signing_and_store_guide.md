# LightPad Distribution, Signing, and Store Guide (Beginner)

This guide is for your current MAUI app setup (`net10.0-android` + `net10.0-windows10.0.19041.0`) and explains:

1. How to distribute to Android tablets and Surface/PC.
2. When stores are required vs optional.
3. How to create/get certificates and signing keys.
4. How to wire signing into GitHub Actions.

---

## 1. Decision First: Where You Will Distribute

### Android tablets
1. Internal testing only: sideload signed `.apk`.
2. Public users: publish in Google Play (recommended).

### Windows PC / Surface
1. Internal testing: zipped build output or signed MSIX sideload.
2. Public users: Microsoft Store (recommended) or direct signed installer download.

### iPad tablets (if needed later)
1. You must use Apple signing/distribution paths (App Store/TestFlight/enterprise).
2. You cannot distribute to normal users by simple sideload like Android.

---

## 2. Do You Need Digital Signing?

Short answer: yes.

1. Android requires app signing to install/update.
2. Windows should be code-signed for trust and SmartScreen reputation.
3. Microsoft Store + MSIX can handle signing at Store submission time, but direct distribution still requires your own signing.

---

## 3. Android Step-by-Step (Google Play + CI)

## 3.1 Create Google Play Console account
1. Go to Play Console and create a developer account.
2. Create a new app entry.
3. Complete required policy/profile fields.

## 3.2 Create Android upload keystore (one-time)
Run this locally once and back it up offline:

```powershell
keytool -genkeypair `
  -v `
  -keystore lightpad-upload.jks `
  -alias lightpad_upload `
  -keyalg RSA `
  -keysize 4096 `
  -validity 10000
```

Keep safe:
1. `lightpad-upload.jks`
2. Keystore password
3. Key alias (`lightpad_upload`)
4. Key password

## 3.3 Enable Play App Signing
1. In Play Console, open your app.
2. Go to `Release > Setup > App signing`.
3. Enroll in Play App Signing.
4. Use your upload key for CI uploads.

## 3.4 Create Google service account for CI upload
1. In Google Cloud, create/select a project linked to Play Console.
2. Create a Service Account.
3. Create JSON key and download it.
4. In Play Console API access, link this service account.
5. Grant least required permissions (typically release management for your app).

## 3.5 Add GitHub secrets (Android)
1. `ANDROID_KEYSTORE_BASE64`
2. `ANDROID_KEYSTORE_PASSWORD`
3. `ANDROID_KEY_ALIAS`
4. `ANDROID_KEY_PASSWORD`
5. `PLAY_SERVICE_ACCOUNT_JSON`

Create base64 for keystore:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("lightpad-upload.jks")) | Set-Clipboard
```

---

## 4. Windows Step-by-Step (Surface/PC + CI)

## 4.1 Choose your Windows distribution model
1. Microsoft Store + MSIX (recommended for broad public release).
2. Direct download (you sign and host installer/packages yourself).

## 4.2 Understand signing options
1. Testing only: self-signed certificate is fine.
2. Public direct distribution: use trusted signing (Azure Trusted/Artifact Signing or CA-issued certificate).
3. Microsoft Store (MSIX route): Store signs package during submission.

## 4.3 For direct distribution: obtain certificate
Pick one:
1. Buy OV/EV code-signing certificate from CA.
2. Use Azure Trusted/Artifact Signing service.

You will need:
1. `.pfx` certificate (or cloud signing profile).
2. Certificate password.
3. Timestamp URL.

## 4.4 Add GitHub secrets (Windows direct signing)
1. `WINDOWS_CERT_PFX_BASE64`
2. `WINDOWS_CERT_PASSWORD`
3. `WINDOWS_TIMESTAMP_URL` (example: `http://timestamp.digicert.com`)

Create base64 for PFX:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("lightpad-signing.pfx")) | Set-Clipboard
```

---

## 5. Microsoft Store Step-by-Step (Windows)

## 5.1 Create Partner Center developer account
1. Register a developer account.
2. Choose account type (Individual or Company).
3. Complete identity/verification steps.

## 5.2 Reserve app name
1. In Partner Center, create new app product.
2. Reserve the LightPad name.

## 5.3 Package app for Store
Important for your current project:
1. Your `.csproj` currently has `<WindowsPackageType>None</WindowsPackageType>`.
2. For Store submission, switch to MSIX packaging (packaged build path).

## 5.4 Create submission
1. Upload package.
2. Fill listing metadata (description, screenshots, category, age rating, privacy URL).
3. Set availability/pricing.
4. Submit for certification.
5. Publish after approval.

---

## 6. GitHub Actions Template (Android + Windows)

This is a starter workflow you can add as `.github/workflows/release-mobile-windows.yml`.

```yaml
name: Release Android and Windows

on:
  workflow_dispatch:
    inputs:
      release_tag:
        description: Release tag (e.g. v1.0.0)
        required: true
        type: string

permissions:
  contents: write

jobs:
  android:
    runs-on: windows-latest
    env:
      DOTNET_VERSION: 10.0.x
      PROJECT: src/LightPad.App/LightPad.App.csproj
      CONFIGURATION: Release
      TFM: net10.0-android
      RID: android-arm64
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install MAUI workload
        run: dotnet workload install maui

      - name: Restore
        run: dotnet restore LightPad.sln

      - name: Decode Android keystore
        shell: pwsh
        run: |
          [IO.File]::WriteAllBytes("$env:GITHUB_WORKSPACE\lightpad-upload.jks", [Convert]::FromBase64String("${{ secrets.ANDROID_KEYSTORE_BASE64 }}"))

      - name: Build signed Android AAB
        run: >
          dotnet publish $env:PROJECT
          -c $env:CONFIGURATION
          -f $env:TFM
          -p:AndroidPackageFormat=aab
          -p:AndroidKeyStore=true
          -p:AndroidSigningKeyStore=${{ github.workspace }}\lightpad-upload.jks
          -p:AndroidSigningStorePass=${{ secrets.ANDROID_KEYSTORE_PASSWORD }}
          -p:AndroidSigningKeyAlias=${{ secrets.ANDROID_KEY_ALIAS }}
          -p:AndroidSigningKeyPass=${{ secrets.ANDROID_KEY_PASSWORD }}

      - name: Upload Android artifact
        uses: actions/upload-artifact@v4
        with:
          name: android-aab
          path: src/LightPad.App/bin/Release/net10.0-android/**/*.aab

      # Optional: add Google Play upload action after validating first manual build.

  windows:
    runs-on: windows-latest
    env:
      DOTNET_VERSION: 10.0.x
      PROJECT: src/LightPad.App/LightPad.App.csproj
      CONFIGURATION: Release
      TFM: net10.0-windows10.0.19041.0
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install MAUI workload
        run: dotnet workload install maui

      - name: Restore
        run: dotnet restore LightPad.sln

      - name: Build Windows artifact
        run: >
          dotnet build LightPad.sln
          -c $env:CONFIGURATION
          -f $env:TFM

      - name: Decode cert
        if: ${{ secrets.WINDOWS_CERT_PFX_BASE64 != '' }}
        shell: pwsh
        run: |
          [IO.File]::WriteAllBytes("$env:GITHUB_WORKSPACE\lightpad-signing.pfx", [Convert]::FromBase64String("${{ secrets.WINDOWS_CERT_PFX_BASE64 }}"))

      # Add SignTool or Trusted Signing step here when packaging as MSIX/installer.
      # Keep your existing artifact/release upload steps from build-windows-artifact.yml.
```

---

## 7. Immediate Changes You Should Make in This Repo

1. Keep current workflow for quick Surface internal testing.
2. Add a new release workflow for Android signing/building.
3. Add a packaged Windows/MSIX workflow for Store submission.
4. Keep direct-download workflow as fallback.

---

## 8. First-Time Checklist (Order to Follow)

1. Create Android upload keystore and back it up.
2. Set up Play Console app + Play App Signing.
3. Create Play service account + JSON key.
4. Add Android secrets to GitHub.
5. Run Android workflow and produce signed `.aab`.
6. Upload to Play internal testing.
7. Create Microsoft Partner Center account.
8. Reserve app name and prepare Store listing assets.
9. Decide Windows distribution route:
10. If Store-first: create MSIX packaging workflow and submit.
11. If direct-first: buy/setup code-signing and sign installer/MSIX.

---

## 9. References (Official Docs)

1. Android app signing and Play App Signing:
https://developer.android.com/studio/publish/app-signing.html
2. MSIX signing guidance:
https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide
3. Windows code-signing options:
https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
4. Publish Windows apps to Microsoft Store:
https://learn.microsoft.com/en-us/windows/apps/publish/
5. Publish your first Windows app:
https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app

