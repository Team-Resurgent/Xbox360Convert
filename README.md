# Simple360CCI

<p align="center"><b>Batch convert Xbox and Xbox 360 game images between ISO and CCI, split for FATX</b></p>

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

CLI tool from [Team-Resurgent/Simple360CCI](https://github.com/Team-Resurgent/Simple360CCI) that converts between **ISO** and **CCI** (and can unpack **ZIP** inputs) using **`XboxToolkit`** (`ISOContainerReader`, `CCIContainerReader`, `ContainerUtility`).
Source: [`Simple360CCI/Program.cs`](https://github.com/Team-Resurgent/Simple360CCI/blob/main/Simple360CCI/Program.cs).
## What it does
- Scans an **input directory** for files ending in `.zip`, `.iso`, or `.cci`.
- For each file:
  - **ZIP**: extracts the first **`.iso`** entry from the archive into a temp folder.
  - **ISO / CCI**: opens via `ISOContainerReader` or `CCIContainerReader`, mounts with `TryMount()`, then converts with:
    - `ContainerUtility.ConvertContainerToCCI(...)` when output format is **CCI**, or  
    - `ContainerUtility.ConvertContainerToISO(...)` when output format is **ISO**.
- By default, splits output that would exceed the **FATX 4GB** file size limit into parts named `name.1.cci`, `name.2.cci`, ... (or `.iso`). Output that fits is written as a single `name.cci`.
- Writes the result to the **output directory** (optionally deletes the source file after success).
## Requirements
- **Input** and **output** must be **existing directories** and must **not** be the same path.
- Uses **SharpCompress** for ZIP handling and **XboxToolkit** for container read/write.
## Command-line options
| Option | Description |
|--------|-------------|
| `-i`, `--input=` | Source **folder** containing `.zip` / `.iso` / `.cci` files |
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
