﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox;
using System.Windows.Forms;
using Toolbox.Library;
using System.IO;
using Toolbox.Library.IO;
using Toolbox.Library.Forms;

namespace FirstPlugin
{
    public class GFPAK : IArchiveFile, IFileFormat
    {
        public FileType FileType { get; set; } = FileType.Archive;

        public bool CanSave { get; set; }
        public string[] Description { get; set; } = new string[] { "Graphic Package" };
        public string[] Extension { get; set; } = new string[] { "*.gfpak" };
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public IFileInfo IFileInfo { get; set; }

        private string FindMatch(byte[] f)
        {
            if (f.Matches("SARC")) return ".szs";
            else if (f.Matches("Yaz")) return ".szs";
            else if (f.Matches("YB") || f.Matches("BY")) return ".byaml";
            else if (f.Matches("FRES")) return ".bfres";
            else if (f.Matches("Gfx2")) return ".gtx";
            else if (f.Matches("FLYT")) return ".bflyt";
            else if (f.Matches("CLAN")) return ".bclan";
            else if (f.Matches("CLYT")) return ".bclyt";
            else if (f.Matches("FLIM")) return ".bclim";
            else if (f.Matches("FLAN")) return ".bflan";
            else if (f.Matches("FSEQ")) return ".bfseq";
            else if (f.Matches("VFXB")) return ".ptcl";
            else if (f.Matches("AAHS")) return ".sharc";
            else if (f.Matches("BAHS")) return ".sharcb";
            else if (f.Matches("BNTX")) return ".bntx";
            else if (f.Matches("BNSH")) return ".bnsh";
            else if (f.Matches("FSHA")) return ".bfsha";
            else if (f.Matches("FFNT")) return ".bffnt";
            else if (f.Matches("CFNT")) return ".bcfnt";
            else if (f.Matches("CSTM")) return ".bcstm";
            else if (f.Matches("FSTM")) return ".bfstm";
            else if (f.Matches("STM")) return ".bstm";
            else if (f.Matches("CWAV")) return ".bcwav";
            else if (f.Matches("FWAV")) return ".bfwav";
            else if (f.Matches("CTPK")) return ".ctpk";
            else if (f.Matches("CGFX")) return ".bcres";
            else if (f.Matches("AAMP")) return ".aamp";
            else if (f.Matches("MsgStdBn")) return ".msbt";
            else if (f.Matches("MsgPrjBn")) return ".msbp";
            else if (f.Matches(0x00000004)) return ".gfbanm";
            else if (f.Matches(0x00000014)) return ".gfbanm";
            else if (f.Matches(0x00000018)) return ".gfbanmcfg";
            else if (f.Matches(0x00000020)) return ".gfbmdl";
            else if (f.Matches(0x00000044)) return ".gfbpokecfg";
            else return "";
        }

        //For BNTX, BNSH, etc
        private string GetBinaryHeaderName(byte[] Data)
        {
            using (var reader = new FileReader(Data))
            {
                reader.Seek(0x10, SeekOrigin.Begin);
                uint NameOffset = reader.ReadUInt32();

                reader.Seek(NameOffset, SeekOrigin.Begin);
                return reader.ReadString(Syroot.BinaryData.BinaryStringFormat.ZeroTerminated);
            }
        }

        public bool Identify(System.IO.Stream stream)
        {
            using (var reader = new Toolbox.Library.IO.FileReader(stream, true))
            {
                return reader.CheckSignature(8, "GFLXPACK");
            }
        }

        public Type[] Types
        {
            get
            {
                List<Type> types = new List<Type>();
                return types.ToArray();
            }
        }

        public List<FileEntry> files = new List<FileEntry>();
        public IEnumerable<ArchiveFileInfo> Files => files;

        public void ClearFiles() { files.Clear(); }

        public bool CanAddFiles { get; set; } = false;
        public bool CanRenameFiles { get; set; } = false;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public void Load(System.IO.Stream stream)
        {
            CanSave = true;

            Read(new FileReader(stream));
        }

        public void Unload()
        {
            foreach (var file in files)
            {
                if (file.FileFormat != null)
                    file.FileFormat.Unload();

                file.FileData = null;
            }

            files.Clear();

            GC.SuppressFinalize(this);
        }

        public void Save(System.IO.Stream stream)
        {
            Write(new FileWriter(stream));
        }

        private void Save(object sender, EventArgs args)
        {
            List<IFileFormat> formats = new List<IFileFormat>();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Utils.GetAllFilters(formats);
            sfd.FileName = FileName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                STFileSaver.SaveFileFormat(this, sfd.FileName);
            }
        }

        private void CallRecursive(TreeView treeView)
        {
            // Print each node recursively.  
            TreeNodeCollection nodes = treeView.Nodes;
            foreach (TreeNode n in nodes)
            {
                PrintRecursive(n);
            }
        }
        private void PrintRecursive(TreeNode treeNode)
        {
            // Print each node recursively.  
            foreach (TreeNode tn in treeNode.Nodes)
            {
                PrintRecursive(tn);
            }
        }

        public ushort BOM;
        public uint Version;
        public List<Folder> folders = new List<Folder>();

        public int version;
        public int FolderCount;

        public void Read(FileReader reader)
        {
            string Signature = reader.ReadString(8, Encoding.ASCII);
            if (Signature != "GFLXPACK")
                throw new Exception($"Invalid signature {Signature}! Expected GFLXPACK.");

            version = reader.ReadInt32();
            uint padding = reader.ReadUInt32();
            uint FileCount = reader.ReadUInt32();
            FolderCount = reader.ReadInt32();
            ulong FileInfoOffset = reader.ReadUInt64();
            ulong hashArrayPathsOffset = reader.ReadUInt64();
            ulong FolderArrayOffset = reader.ReadUInt64();

            reader.Seek((long)FolderArrayOffset, SeekOrigin.Begin);

            List<UInt64> hashes = new List<UInt64>();

            List<HashIndex> FolderFiles = new List<HashIndex>();
            for (int i = 0; i < FolderCount; i++)
            {
                Folder folder = new Folder();
                folder.Read(reader);
                folders.Add(folder);

                foreach (var hash in folder.hashes)
                    FolderFiles.Add(hash);
            }

            reader.Seek((long)hashArrayPathsOffset, SeekOrigin.Begin);
            for (int i = 0; i < FileCount; i++)
            {
                ulong hash = reader.ReadUInt64();
                hashes.Add(hash);
            }

            reader.Seek((long)FileInfoOffset, SeekOrigin.Begin);
            for (int i = 0; i < FileCount; i++)
            {
                FileEntry fileEntry = new FileEntry(this);
                fileEntry.Read(reader);
                fileEntry.FileName = GetString(hashes[i], fileEntry.FileData);
                fileEntry.FilePathHash = hashes[i];

                for (int f = 0; f < FolderFiles.Count; f++)
                    if (FolderFiles[f].Index == f)
                        fileEntry.FolderHash = FolderFiles[f];

                string baseName = Path.GetFileName(fileEntry.FileName.Replace("\r", ""));

                switch (Utils.GetExtension(fileEntry.FileName))
                {
                    case ".gfbanm":
                     //   fileEntry.OpenFileFormatOnLoad = true;
                        fileEntry.FileName = $"Animations/{baseName}";
                        break;
                    case ".gfbanmcfg":
                        fileEntry.FileName = $"AnimationConfigs/{baseName}";
                        break;
                    case ".gfbmdl":
                        fileEntry.FileName = $"Models/{baseName}";
                        break;
                    case ".gfbpokecfg":
                        fileEntry.FileName = $"PokeConfigs/{baseName}";
                        break;
                    case ".bntx":
                      //  fileEntry.OpenFileFormatOnLoad = true;
                        fileEntry.FileName = $"Textures/{baseName}";
                        break;
                    case ".bnsh":
                        fileEntry.FileName = $"Shaders/{baseName}";
                        break;
                    case ".ptcl":
                        fileEntry.FileName = $"Effects/{baseName}";
                        break;
                    default:
                        fileEntry.FileName = $"OtherFiles/{baseName}";
                        break;
                }



           //     Console.WriteLine($"{fileEntry.FileName} {fileEntry.FolderHash.hash.ToString("X")} {suffix64.ToString("X")}");

                files.Add(fileEntry);
            }
        }

        private Dictionary<ulong, string> hashList;
        public Dictionary<ulong, string> HashList
        {
            get
            {
                if (hashList == null) {
                    hashList = new Dictionary<ulong, string>();
                    GenerateHashList();
                }
                return hashList;
            }
        }

        private void GenerateHashList()
        {
            foreach (string hashStr in Properties.Resources.Pkmn.Split('\n'))
            {
                string HashString = hashStr.TrimEnd();

                ulong hash = FNV64A1.Calculate(HashString);
                if (!hashList.ContainsKey(hash))
                    hashList.Add(hash, HashString);

                if (HashString.Contains("pm0000"))
                    GeneratePokeStrings(HashString);

                string[] hashPaths = HashString.Split('/');
                for (int i = 0; i < hashPaths?.Length; i++)
                {
                    hash = FNV64A1.Calculate(hashPaths[i]);
                    if (!hashList.ContainsKey(hash))
                        hashList.Add(hash, HashString);
                }
            }
        }

        private void GeneratePokeStrings(string hashStr)
        {
            //Also check file name just in case
            if (FileName.Contains("pm"))
            {
                string baseName = Path.GetFileNameWithoutExtension(FileName);
                string pokeStrFile = hashStr.Replace("pm0000_00", baseName);

                ulong hash = FNV64A1.Calculate(pokeStrFile);
                if (!hashList.ContainsKey(hash))
                    hashList.Add(hash, pokeStrFile);
            }

            for (int i = 0; i < 1000; i++)
            {
                string pokeStr = hashStr.Replace("pm0000", $"pm{i.ToString("D4")}");

                ulong hash = FNV64A1.Calculate(pokeStr);
                if (!hashList.ContainsKey(hash))
                    hashList.Add(hash, pokeStr);
            }
        }

        private string GetString(ulong Hash, byte[] Data)
        {
            string ext = FindMatch(Data);
            if (ext == ".bntx" || ext == ".bfres" || ext == ".bnsh" || ext == ".bfsha")
                return GetBinaryHeaderName(Data) + ext;
            else
            {
                if (HashList.ContainsKey(Hash))
                    return HashList[Hash];
                else
                    return $"{Hash.ToString("X")}{ext}";
            }
        }

        public void Write(FileWriter writer)
        {
            writer.WriteSignature("GFLXPACK");
            writer.Write(version);
            writer.Write(0);
            writer.Write(files.Count);
            writer.Write(FolderCount);
            long FileInfoOffset = writer.Position;
            writer.Write(0L);
            long HashArrayOffset = writer.Position;
            writer.Write(0L);
            long folderArrOffset = writer.Position;

            //Reserve space for folder offsets
            for (int f = 0; f < FolderCount; f++)
                writer.Write(0L);

            //Now write all sections
            writer.WriteUint64Offset(HashArrayOffset);
            foreach (var fileTbl in files)
                writer.Write(fileTbl.FilePathHash);

            //Save folder sections
            List<long> FolderSectionPositions = new List<long>();
            foreach (var folder in folders)
            {
                FolderSectionPositions.Add(writer.Position);
                folder.Write(writer);
            }
            //Write the folder offsets back
            using (writer.TemporarySeek(folderArrOffset, SeekOrigin.Begin))
            {
                foreach (long offset in FolderSectionPositions)
                    writer.Write(offset);
            }

            //Now file data
            writer.WriteUint64Offset(FileInfoOffset);
            foreach (var fileTbl in files)
                fileTbl.Write(writer);

            //Save data blocks
            foreach (var fileTbl in files)
            {
                fileTbl.WriteBlock(writer);
            }

            writer.Align(16);
        }

        public class Folder
        {
            public ulong hash;
            public uint FileCount => (uint)hashes.Count;
            public uint unknown;

            public List<HashIndex> hashes = new List<HashIndex>();

            public void Read(FileReader reader)
            {
                hash = reader.ReadUInt64();
                uint fileCount = reader.ReadUInt32();
                unknown = reader.ReadUInt32();

                for (int f = 0; f < fileCount; f++)
                {
                    HashIndex hash = new HashIndex();
                    hash.Read(reader, this);
                    hashes.Add(hash);
                }
            }
            public void Write(FileWriter writer)
            {
                writer.Write(hash);
                writer.Write(FileCount);
                writer.Write(unknown);

                foreach (var hash in hashes)
                    hash.Write(writer);
            }
        }

        public class HashIndex
        {
            public ulong hash;
            public int Index;
            public uint unknown;

            public Folder Parent { get; set; }

            public void Read(FileReader reader, Folder parent)
            {
                Parent = parent;
                hash = reader.ReadUInt64();
                Index = reader.ReadInt32();
                unknown = reader.ReadUInt32(); //Always 0xCC?
            }
            public void Write(FileWriter writer)
            {
                writer.Write(hash);
                writer.Write(Index);
                writer.Write(unknown);
            }
        }
        public class FileEntry : ArchiveFileInfo
        {
            public HashIndex FolderHash;

            public UInt64 FilePathHash;

            public uint unkown;
            public uint CompressionType;
            private long DataOffset;

            public uint CompressedFileSize;
            public uint padding;

            private IArchiveFile ArchiveFile;

            public FileEntry(IArchiveFile archiveFile) {
                ArchiveFile = archiveFile;
            }

            private bool IsTexturesLoaded = false;
            public override IFileFormat OpenFile()
            {
                var FileFormat = base.OpenFile();
                bool IsModel = FileFormat is GFBMDL;

                if (IsModel && !IsTexturesLoaded)
                {
                    IsTexturesLoaded = true;
                    foreach (var file in ArchiveFile.Files)
                    {
                        if (Utils.GetExtension(file.FileName) == ".bntx")
                        {
                            file.FileFormat = file.OpenFile();
                        }
                    }
                }


                return base.OpenFile();
            }

            public void Read(FileReader reader)
            {
                unkown = reader.ReadUInt16(); //Usually 9?
                CompressionType = reader.ReadUInt16();
                uint DecompressedFileSize = reader.ReadUInt32();
                CompressedFileSize = reader.ReadUInt32();
                padding = reader.ReadUInt32();
                ulong FileOffset = reader.ReadUInt64();

                using (reader.TemporarySeek((long)FileOffset, SeekOrigin.Begin))
                {
                    FileData = reader.ReadBytes((int)CompressedFileSize);
                    FileData = STLibraryCompression.Type_LZ4.Decompress(FileData, 0, (int)CompressedFileSize, (int)DecompressedFileSize);
                }
            }

            byte[] CompressedData;
            public void Write(FileWriter writer)
            {
                this.SaveFileFormat();

                CompressedData = Compress(FileData, CompressionType);

                writer.Write((ushort)unkown);
                writer.Write((ushort)CompressionType);
                writer.Write(FileData.Length);
                writer.Write(CompressedData.Length);
                writer.Write(padding);
                DataOffset = writer.Position;
                writer.Write(0L);
            }
            public void WriteBlock(FileWriter writer)
            {
                writer.Align(16);
                writer.WriteUint64Offset(DataOffset);
                writer.Write(CompressedData);
            }
            public static byte[] Compress(byte[] data, uint Type)
            {
                if (Type == 2)
                {
                    return STLibraryCompression.Type_LZ4.Compress(data);
                }
                else
                    throw new Exception("Unkown compression type?");
            }
        }

        public static void ReplaceNode(TreeNode node, TreeNode replaceNode, TreeNode NewNode)
        {
            if (NewNode == null)
                return;

            int index = node.Nodes.IndexOf(replaceNode);
            node.Nodes.RemoveAt(index);
            node.Nodes.Insert(index, NewNode);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            int index = 0;
            foreach (FileEntry file in files)
            {
                //Remove folder references first
                //Regenerate the indices after
                foreach (var folder in folders)
                {
                    for (int f = 0; f < folder.FileCount; f++)
                        if (folder.hashes[f].Index == index)
                            folder.hashes.RemoveAt(f);
                }


                index++;
            }

            files.Remove((FileEntry)archiveFileInfo);

            return true;
        }

        private void RegenerateFileIndices()
        {
            foreach (var folder in folders)
            {
                int index = 0;
                foreach (FileEntry file in files)
                {
                    for (int f = 0; f < folder.FileCount; f++)
                    {
                        if (file.FolderHash == folder.hashes[f])
                            folder.hashes[f].Index = index;
                    }

                    index++;
                }
            }
        }
    }
}
