namespace WadTool.WadLib
{
    public class FolderInfo
    {
        public byte[] Name; // 0x20 bytes
        public UInt32 Unknown1;
        public UInt32 FileSize;
        public UInt32 Unknown2;
        public UInt32 Unknown3;
        public FolderEntry RootFolder;
        public FolderInfo(byte[] ind, byte[] wad) : this(new BinaryReader(new MemoryStream(ind)), new BinaryReader(new MemoryStream(wad))) {}
        public FolderInfo(string ind, string wad) : this(File.OpenRead(ind), File.OpenRead(wad)) {}
        public FolderInfo(FileInfo ind, FileInfo wad) : this(ind.OpenRead(), wad.OpenRead()) {}
        public FolderInfo(FileStream ind, FileStream wad) : this(new BinaryReader(ind), new BinaryReader(wad)) {}
        public FolderInfo(BinaryReader ind, BinaryReader wad)
        {
            Name = ind.ReadBytes(32);
            Unknown1 = ind.ReadUInt32();
            FileSize = ind.ReadUInt32();
            Unknown2 = ind.ReadUInt32();
            Unknown3 = ind.ReadUInt32();
            RootFolder = new FolderEntry(ind, wad);
        }
    }
    public class FolderEntry // 16 bytes
    {
        public byte[] Name; // 8 bytes
        public byte[] LongName;
        public byte[] ParentLongName;
        public Int16 Index;
        public Int16 NumChildren;
        public OffsetSize Offset;
        public List<FolderEntry> Folders;
        public FileList Files;
        public uint Level;
        public bool IsFileFolder {
            get => NumChildren < 0;
        }
        public FolderEntry(byte[] ind, byte[] wad) : this(new BinaryReader(new MemoryStream(ind)), new BinaryReader(new MemoryStream(wad))) {}
        public FolderEntry(BinaryReader ind, BinaryReader wad)
        {
            Read(ind, wad, 0);
        }
        public FolderEntry(BinaryReader ind, BinaryReader wad, long offset, uint level)
        {
            ind.BaseStream.Position = offset;
            Read(ind, wad, level);
        }
        void Read(BinaryReader ind, BinaryReader wad, uint level)
        {
            long CurOffset = ind.BaseStream.Position;
            Name = ind.ReadBytes(8);
            Index = ind.ReadInt16();
            NumChildren = ind.ReadInt16();
            Offset = new OffsetSize(ind.ReadUInt32());
            Level = level;
            if(!IsFileFolder)
            {
                Folders = new List<FolderEntry>();
                for(short i = Index; i < Index+NumChildren; i++)
                {
                    FolderEntry child = new FolderEntry(ind, wad, CurOffset+16*i, level+1);
                    Folders.Add(child);
                    if(child.ParentLongName != null) LongName = child.ParentLongName;
                }
            }
            else
            {
                Files = new FileList(wad, Offset.Offset);
                LongName = Files.Name;
                ParentLongName = Files.ParentName;
            }
        }
        public FolderEntry this[string index]
        {
            get => Folders.Where(f => WadUtils.Decode(f.Name) == index || WadUtils.Decode(f.LongName!) == index).Single();
        }
    }
    public class FileList
    {
        public byte[] Name; // 32 bytes
        public byte[] ParentName; // 32 bytes
        public UInt32 NumFiles;
        public List<FileEntry> Files;
        public FileList(BinaryReader wad, long offset)
        {
            wad.BaseStream.Position = offset;
            NumFiles = wad.ReadUInt32();
            Files = new List<FileEntry>((int)NumFiles);
            for(uint i = 0; i < NumFiles; i++)
            {
                FileEntry file = new FileEntry(wad);
                Files.Add(file);
            }
            if(NumFiles > 0)
            {
                wad.BaseStream.Position = Files[0].Offset;
                ParentName = wad.ReadBytes(32);
                Name = wad.ReadBytes(32);
                for(int i = 1; i < NumFiles; i++)
                {
                    Files[i].LongName = wad.ReadBytes(32);
                }
            }
        }
        public FileEntry this[string index]
        {
            get => Files.Where(f => WadUtils.Decode(f.Name) == index || WadUtils.Decode(f.LongName!) == index).Single();
        }
    }
    public class FileEntry
    {
        public long Pointer;
        public byte[] Name; // 8 bytes
        public byte[] LongName; // 32 bytes
        public UInt32 Offset;
        public UInt32 Size;
        public FileEntry(BinaryReader wad)
        {
            Pointer = wad.BaseStream.Position;
            Name = wad.ReadBytes(8);
            Offset = wad.ReadUInt32() << 11;
            Size = wad.ReadUInt32();
        }
        public byte[] ReadFile(BinaryReader wad)
        {
            wad.BaseStream.Position = Offset;
            return wad.ReadBytes((int)Size);
        }
    }
}