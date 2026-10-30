# Xbox360Convert

<p align="center"><b>Batch convert Xbox and Xbox 360 ISO, CCI and GOD images to ISO or CCI, split for FATX</b></p>

<p align="center">
  <a href="https://github.com/Team-Resurgent/Xbox360Convert/actions/workflows/main.yml"><img src="https://github.com/Team-Resurgent/Xbox360Convert/actions/workflows/main.yml/badge.svg" alt="Build"></a>
  <a href="https://discord.gg/VcdSfajQGK"><img src="https://img.shields.io/badge/chat-on%20discord-7289da.svg?logo=discord" alt="Discord"></a>
</p>

<p align="center">
  <a href="https://ko-fi.com/J3J7L5UMN"><img src="https://img.shields.io/badge/ko--fi-Support-FF5E5B?style=for-the-badge&logo=ko-fi&logoColor=white" alt="ko-fi"></a>
  <a href="https://www.patreon.com/teamresurgent"><img src="https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white" alt="Patreon"></a>
</p>

<p align="center">
  <a href="https://github.com/Team-Resurgent/Xbox360Convert/releases/latest"><img src="https://img.shields.io/badge/download-latest-brightgreen.svg?style=for-the-badge&logo=github" alt="Download"></a>
</p>

A command-line tool that converts **ISO**, **CCI**, **GOD** and **ZIP** inputs to **ISO** or **CCI** using [XboxToolkit](https://github.com/Team-Resurgent/XboxToolkit).
## What it does
- Converts a **single file**, or a whole **input folder** in one batch.
- In a folder, it picks up files ending in `.zip`, `.iso`, or `.cci`, and **GOD** (Games on Demand) containers in the folder or any subfolder.
- For each input:
  - **ZIP**: extracts the first **`.iso`** entry from the archive into a temp folder.
  - **ISO / CCI / GOD**: converts to **CCI** or **ISO**.
  - **GOD** output is named after the title in the GOD header, with ` (Disc N)` added for multi-disc titles.
- Writes the result to the **output directory** (optionally deletes the source file after success).
## Splitting
The Xbox hard drive (FATX) can't hold files of 4GB or more, so output is **split by default**:
- Output that would be 4GB or larger is written as numbered parts: `name.1.cci`, `name.2.cci`, ... (or `name.1.iso`, `name.2.iso`, ...). Each part is just under 4GB (`0xFF000000` bytes).
- Output smaller than that is written as a single `name.cci` / `name.iso`.
- Pass `-n` / `--nosplit` to always write a single file, e.g. when the output won't go on a FATX drive.

Split images can also be used as **input**: put all the parts in the input folder (or pass any one part as a single file) and the set is converted once, from part 1, to a file named without the part number. With `-d`, every part is deleted, and for a GOD container the header file and its `.data` folder are deleted.
## Converting XEX files
`-m` / `--machine` re-signs every XEX on an Xbox 360 disc for a **DEVKIT** or **RETAIL** console, and `-u` / `--unrestrict` removes their media, region and other limits. Both use [XexTool](https://github.com/Team-Resurgent/XexTool), which is downloaded for your OS the first time it's needed (or put `XexTool` next to `xbox360convert` or on your PATH).
- XEX files inside **STFS packages** on the disc (such as Arcade titles) are converted too, and the package is rebuilt around them.
- The disc is extracted, converted and built again, since converted files can change size.
- The disc's system update and title update are left as they are.
- A rebuilt package no longer matches its Microsoft signature, so it is written as a console-signed (CON) package with a blank signature. [360Attach](https://github.com/Team-Resurgent/360Attach) signs it with the console's own key when it mounts the disc, so the package opens on a stock devkit as well as on RGH/JTAG.
- For a retail-kernel console (RGH/JTAG/Peer Pressure), convert without `-m`: it already runs retail-signed discs. `-m DEVKIT` output only runs on a devkit.
## Requirements
- **Input** must be an existing file or folder, and **output** an existing folder.
- **Output** must **not** be the input folder, or the folder that holds the input file.
- Uses **SharpCompress** for ZIP handling and **XboxToolkit** for container read/write.
## Command-line options
| Option | Description |
|--------|-------------|
| `-i`, `--input=` | A **folder** of `.zip` / `.iso` / `.cci` files and GOD containers, or a **single file** (for GOD, the header file next to its `.data` folder) |
| `-o`, `--output=` | Destination **folder** for converted files |
| `-f`, `--format=` | Output format: **`ISO`** or **`CCI`** (default: **`CCI`**) |
| `-d`, `--delete` | Delete the original file after a successful conversion |
| `-n`, `--nosplit` | Write a single file instead of splitting at the FATX 4GB limit |
| `-m`, `--machine=` | Re-sign every XEX, including those in STFS packages, for **`DEVKIT`** or **`RETAIL`** |
| `-u`, `--unrestrict` | Remove media, region and other limits from every XEX, including those in STFS packages |
| `-h`, `--help` | Show help |
## Usage examples
```bash
# Convert all supported files in .\in to CCI in .\out (default format)
xbox360convert -i C:\path\to\input -o C:\path\to\output
# Same, explicit format
xbox360convert -i C:\path\to\input -o C:\path\to\output -f CCI
# Convert a single file
xbox360convert -i C:\path\to\input\game.iso -o C:\path\to\output
# Produce ISO instead
xbox360convert -i C:\path\to\input -o C:\path\to\output -f ISO
# Don't split output larger than 4GB
xbox360convert -i C:\path\to\input -o C:\path\to\output -n
# Re-sign for a devkit and remove XEX limits
xbox360convert -i C:\path\to\input -o C:\path\to\output -m DEVKIT -u
# Delete originals after success
xbox360convert -i C:\path\to\input -o C:\path\to\output -d
```
