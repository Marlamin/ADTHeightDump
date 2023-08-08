# ADTHeightDump
Dumps tileset metadata from World of Warcraft ADT files to a JSON file.

## Download
Binaries (and source) for the latest release can be found on the [releases page](https://github.com/Marlamin/ADTHeightDump/releases).

## Before running
Download the latest community-listfile.csv listfile from [the wow-listfile repo](https://github.com/wowdev/wow-listfile/releases), rename it to `listfile.csv` and place it in the same folder as the executable.

## Usage
Arguments:  `ADTHeightDump.exe <wowProd> (wowDir)`  

### Examples
 `ADTHeightDump.exe wowt` (streams from CDN, slower)    
 `ADTHeightDump.exe wowt "C:\World of Warcraft"` (uses locally installed client)
