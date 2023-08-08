using System;
using System.Numerics;
namespace ADTHeightDump
{
    public enum ADTChunks
    {
        // Root ADT
        MVER = 'M' << 24 | 'V' << 16 | 'E' << 8 | 'R' << 0,
        MCNK = 'M' << 24 | 'C' << 16 | 'N' << 8 | 'K' << 0,

        // TEX
        MTEX = 'M' << 24 | 'T' << 16 | 'E' << 8 | 'X' << 0,
        MTXP = 'M' << 24 | 'T' << 16 | 'X' << 8 | 'P' << 0,
        MAMP = 'M' << 24 | 'A' << 16 | 'M' << 8 | 'P' << 0,
        MDID = 'M' << 24 | 'D' << 16 | 'I' << 8 | 'D' << 0,
        MHID = 'M' << 24 | 'H' << 16 | 'I' << 8 | 'D' << 0,
    }

    public struct ADT
    {
        public MTEX textures;
        public MTXP[] texParams;
        public uint[] diffuseTextureFileDataIDs;
        public uint[] heightTextureFileDataIDs;
    }

    public struct MCLY
    {
        public uint textureId;
        public mclyFlags flags;
        public uint offsetInMCAL;
        public int effectId;
    }

    [Flags]
    public enum mclyFlags : uint
    {
        Flag_0x1 = 0x1,
        Flag_0x2 = 0x2,
        Flag_0x4 = 0x4,
        Flag_0x8 = 0x8,
        Flag_0x10 = 0x10,
        Flag_0x20 = 0x20,
        Flag_0x40 = 0x40,
        Flag_0x80 = 0x80,
        Flag_0x100 = 0x100,
        Flag_0x200 = 0x200,
        Flag_0x400 = 0x400,
        Flag_0x800 = 0x800,
        Flag_0x1000 = 0x1000
    }

    public struct MTEX
    {
        public string[] filenames;
    }

    public struct MTXP
    {
        public uint flags;
        public float height;
        public float offset;
        public uint unk3;
    }
}