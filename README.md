# Simple360CCI

<p align="center"><b>Batch convert Xbox and Xbox 360 ISO, CCI and GOD images to ISO or CCI, split for FATX</b></p>

<p align="center">
  <a href="https://github.com/Team-Resurgent/Simple360CCI/actions/workflows/main.yml"><img src="https://github.com/Team-Resurgent/Simple360CCI/actions/workflows/main.yml/badge.svg" alt="Build"></a>
  <a href="https://discord.gg/VcdSfajQGK"><img src="https://img.shields.io/badge/chat-on%20discord-7289da.svg?logo=discord" alt="Discord"></a>
</p>

<p align="center">
  <a href="https://ko-fi.com/J3J7L5UMN"><img src="https://img.shields.io/badge/ko--fi-Support-FF5E5B?style=for-the-badge&logo=ko-fi&logoColor=white" alt="ko-fi"></a>
  <a href="https://www.patreon.com/teamresurgent"><img src="https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white" alt="Patreon"></a>
</p>

<p align="center">
  <a href="https://github.com/Team-Resurgent/Simple360CCI/releases/latest"><img src="https://img.shields.io/badge/download-latest-brightgreen.svg?style=for-the-badge&logo=github" alt="Download"></a>
</p>

A command-line tool that converts **ISO**, **CCI**, **GOD** and **ZIP** inputs to **ISO** or **CCI** using [XboxToolkit](https://github.com/Team-Resurgent/XboxToolkit).
## What it does
- Scans an **input directory** for files ending in `.zip`, `.iso`, or `.cci`, and for **GOD** (Games on Demand) containers in it or any subfolder.
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

Split images can also be used as **input**: put all the parts in the input folder and the set is converted once, from part 1, to a file named without the part number. With `-d`, every part is deleted, and for a GOD container the header file and its `.data` folder are deleted.
## Requirements
- **Input** and **output** must be **existing directories** and must **not** be the same path.
- Uses **SharpCompress** for ZIP handling and **XboxToolkit** for container read/write.
## Command-line options
| Option | Description |
|--------|-------------|
| `-i`, `--input=` | Source **folder** containing `.zip` / `.iso` / `.cci` files or GOD containers |
| `-o`, `--output=` | Destination **folder** for converted files |
| `-f`, `--format=` | Output format: **`ISO`** or **`CCI`** (default: **`CCI`**) |
| `-d`, `--delete` | Delete the original file after a successful conversion |
| `-n`, `--nosplit` | Write a single file instead of splitting at the FATX 4GB limit |
| `-h`, `--help` | Show help |
## Usage examples
```bash
# Convert all supported files in .\in to CCI in .\out (default format)
Simple360CCI -i C:\path\to\input -o C:\path\to\output
# Same, explicit format
Simple360CCI -i C:\path\to\input -o C:\path\to\output -f CCI
# Produce ISO instead
Simple360CCI -i C:\path\to\input -o C:\path\to\output -f ISO
# Don't split output larger than 4GB
Simple360CCI -i C:\path\to\input -o C:\path\to\output -n
# Delete originals after success
Simple360CCI -i C:\path\to\input -o C:\path\to\output -d
```
