# CUETools Discogs Metadata Plugin for EAC

## Overview

This project is a Discogs-based metadata plugin for **Exact Audio Copy (EAC)**, built as a COM-visible class library in C# and modeled closely on the open-source **CUETools CTDB EAC plugin**. It lets EAC query Discogs for album/track metadata and cover art when ripping CDs.

The core implementation is in `Metadata.cs`, which follows the same `IMetadataRetriever` pattern used by `CUETools.CTDB.EACPlugin`, but talks directly to the Discogs API instead of going via CTDB.

## Features

- Uses CD TOC to look up releases on Discogs.
- Lets the user select the correct Discogs release (similar flow to CTDB plugin).
- Fills in:
  - Album artist and title
  - Track titles and artists
  - Year / basic release info
  - Optional cover art download
- Installs as an EAC metadata plugin, selectable in **EAC → Metadata Options (F12)**.

## Dependencies

This plugin is designed to integrate with **CUETools** and **EAC** and therefore depends on several DLLs that ship with them.

From CUETools / CTDB (shipped with CUETools / CTDB plugin):

- `CUETools.CDImage.dll`
- `CUETools.AccurateRip.dll`
- `CUETools.Codecs.dll`
- `CUETools.CTDB.dll`
- `CUETools.CTDB.Types.dll`

From EAC install folder (typically `C:\Program Files (x86)\Exact Audio Copy`):

- `HelperFunctions.dll`
- `Interop.HelperFunctionsLib.dll`
- `Newtonsoft.Json.dll`

From .NET Framework:

- .NET Framework 4.8 (target framework)
- `System.Windows.Forms` (for plugin UI dialogs)
- Other standard .NET assemblies referenced by the project file.

You can see the exact references in `CUETools.Discogs.EACPlugin.csproj` and in the Solution Explorer when opened inside the `cuetools.net` solution.

## Build instructions

1. Install prerequisites

   - Windows 10 or later (32-bit EAC).
   - .NET Framework 4.8.
   - Visual Studio 2022 (Community is fine) with **“.NET desktop development”** workload.
   - Exact Audio Copy (EAC) 32-bit.
   - CUETools (for the CTDB EAC plugin and supporting DLLs).

2. Get the source

   You can either:

   - Clone this repo alone:

     ```bash
     git clone https://github.com/USERNAME/cuetools-discogs-plugin.git
     ```

   - Or clone the full `cuetools.net` repo and add this project into its `Interop` folder, mirroring the structure of `CUETools.CTDB.EACPlugin`.

3. Open the project

   - Open `CUETools.sln` from the `cuetools.net` source tree (if integrating there), or open `CUETools.Discogs.EACPlugin.csproj` directly in Visual Studio.
   - Make sure the project targets:
     - `.NET Framework 4.8`
     - Platform: `x86`

4. Add references

   If Visual Studio reports missing references, add them as follows:

   - Right-click `References` under `CUETools.Discogs.EACPlugin` → **Add Reference…**
   - Add project / DLL references to:
     - `CUETools.CDImage`
     - `CUETools.AccurateRip`
     - `CUETools.Codecs`
     - `CUETools.CTDB`
     - `CUETools.CTDB.Types`
   - Use the **Browse…** button to add from the EAC folder:
     - `HelperFunctions.dll`
     - `Interop.HelperFunctionsLib.dll`
     - `Newtonsoft.Json.dll`

5. Discogs configuration

   In `Metadata.cs` there is a constant for the Discogs user token / user agent:

   ```csharp
   private const string UserToken = "YOUR_DISCOGS_TOKEN_HERE";
   private const string UserAgent = "CUEToolsDiscogsPlugin/1.0 (https://github.com/USERNAME/cuetools-discogs-plugin)";
