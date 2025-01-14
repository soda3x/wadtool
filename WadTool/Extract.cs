using WadTool.WadLib;

namespace WadTool
{
    partial class Program
    {
        public static void Extract(FileInfo ind, FileInfo wad, FileSystemInfo output, bool namelist, bool bogus, bool dryrun, string file)
        {
            var wp = new WadPackage(ind, wad);

            if(output == null)
            {
                output = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            if(file != null)
            {
                FileEntry fe = wp.GetFile(file);
                if(wp.WadFile.Length < fe.Offset+fe.Size)
                {
                    Console.Error.WriteLine("{0} @ 0x{1:X} seems to lie outside the WAD, an empty file will be written unless -b is given", fe.LongName, fe.Offset);
                    if(bogus) return;
                }
                byte[] buf = wp.GetBytes(fe);
                if(!dryrun)
                {
                    if(output.Name == "-")
                    {
                        using (Stream stdout = Console.OpenStandardOutput())
                        {
                            stdout.Write(buf, 0, buf.Length);
                        }
                    }
                    else
                    {
                        if(output is FileInfo)
                        {
                            using (Stream fileout = ((FileInfo)output).Create())
                            {
                                fileout.Write(buf, 0, buf.Length);
                            }
                        }
                        else
                        {
                            var name = file.Split('/').Last();
                            var f = new FileInfo(Path.Combine(output.FullName, name));

                            using (Stream fileout = f.Create())
                            {
                                fileout.Write(buf, 0, buf.Length);
                            }
                        }
                    }
                }
            }
            else
            {
                FolderEntry node = wp.Index.RootFolder;

                if(file != null)
                {
                    string[] dirs = file.Split('/');
                    for(int i = 0; !node.IsFileFolder && i < dirs.Length; i++)
                    {
                        node = node[dirs[i]];
                    }
                }

                DirectoryInfo dir;
                if(output is DirectoryInfo)
                {
                    dir = (DirectoryInfo)output;
                    ExtractTree(wp.WadReader, node, dir, namelist, dryrun, bogus);
                }
                else
                {
                    Console.Error.WriteLine("{0} is not a directory", output.FullName);
                }
            }
        }
        public static void ExtractTree(BinaryReader wad, FolderEntry tree, DirectoryInfo root, bool namelist, bool dryrun, bool bogus)
        {
            string name = tree.LongName != null ? WadUtils.Decode(tree.LongName) : WadUtils.Decode(tree.Name);

            DirectoryInfo subdir;
            if(name == "" || dryrun)
                subdir = root;
            else
                subdir = root.CreateSubdirectory(name);

            if(tree.IsFileFolder)
            {
                FileList list = tree.Files;
                for(int i = 0; i < list.Files.Count; i++)
                    if(WadUtils.Decode(list.Files[i].Name) != "NAMELIST" || namelist)
                        ExtractTree(wad, list.Files[i], subdir, dryrun, bogus);
            }
            else
            {
                for(int i = 0; i < tree.Folders.Count; i++)
                    ExtractTree(wad, tree.Folders[i], subdir, namelist, dryrun, bogus);
            }
        }
        public static void ExtractTree(BinaryReader wad, FileEntry tree, DirectoryInfo root, bool dryrun, bool bogus)
        {
            string name = tree.LongName != null ? WadUtils.Decode(tree.LongName) : WadUtils.Decode(tree.Name);

            if(wad.BaseStream.Length < tree.Offset+tree.Size)
            {
                if(bogus) return;
            }

            var buf = tree.ReadFile(wad);

            if(!dryrun)
            {
                var file = new FileInfo(Path.Combine(root.FullName, name));
                using(Stream fs = file.Create())
                {
                    fs.Write(buf, 0, buf.Length);
                }
            }
        }
    }
}