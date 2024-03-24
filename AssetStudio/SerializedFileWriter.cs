using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AssetStudio
{
    internal class ObjectSortHelper
    {
        public int index;
        public ObjectInfo @object;
        public long offset;
    }

    public static class SerializedFileWriter
    {
        public static void SaveAs(this SerializedFile assetsFile, string path, Dictionary<long, Stream> replacedStreams = null)
        {
            using (var stream = File.Create(path))
            {
                assetsFile.SaveAs(stream, replacedStreams);
            }
        }
        public static void SaveAs(this SerializedFile assetsFile, Stream stream, Dictionary<long, Stream> replacedStreams = null)
        {
            using (var writer = new EndianBinaryWriter(stream))
            {
                assetsFile.SaveAs(writer, replacedStreams);
            }
        }

        public static void SaveAs(this SerializedFile assetsFile, EndianBinaryWriter writer, Dictionary<long, Stream> replacedStreams = null)
        {
            var newObjects = assetsFile.m_Objects.Select((x, i) => new ObjectSortHelper()
            {
                index = i,
                @object = x,
                offset = x.byteStart
            }).ToList();
            newObjects.Sort((x, y) => x.offset.CompareTo(y.offset));
            long objectOffset = newObjects[0].offset;
            for (int i = 0; i < newObjects.Count; i++)
            {
                var @object = newObjects[i];
                if (replacedStreams != null && replacedStreams.ContainsKey(@object.@object.m_PathID))
                {
                    @object.@object = new ObjectReplaced(@object.@object, replacedStreams[@object.@object.m_PathID]);
                }
                @object.offset = objectOffset;
                objectOffset += @object.@object.byteSize;
                objectOffset += (8 - objectOffset % 8) % 8;
            }
            newObjects.Sort((x, y) => x.index.CompareTo(y.index));

            writer.Position = 0;
            var header = assetsFile.header;
            // Write Header
            if (header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                writer.Write((long)0);
                writer.Write((uint)header.m_Version);
                writer.Write(0);
            }
            else
            {
                writer.Write(header.m_MetadataSize);
                writer.Write((uint)header.m_FileSize);
                writer.Write((uint)header.m_Version);
                writer.Write((uint)header.m_DataOffset);
            }

            byte m_FileEndianess;
            if (header.m_Version >= SerializedFileFormatVersion.Unknown_9)
            {
                writer.Write(header.m_Endianess);
                writer.Write(header.m_Reserved);
                m_FileEndianess = header.m_Endianess;
            }
            else
            {
                writer.Position = header.m_FileSize - header.m_MetadataSize;
                m_FileEndianess = 0x00;
                writer.Write(m_FileEndianess);
            }

            if (header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                writer.Write(header.m_MetadataSize);
                writer.Write(header.m_FileSize);
                writer.Write(header.m_DataOffset);
                writer.Write((long)0x00); // unknown
            }

            // Write Metadata
            if (m_FileEndianess == 0)
            {
                writer.Endian = EndianType.LittleEndian;
            }
            if (header.m_Version >= SerializedFileFormatVersion.Unknown_7)
            {
                writer.WriteStringToNull(assetsFile.unityVersion);
            }
            if (header.m_Version >= SerializedFileFormatVersion.Unknown_8)
            {
                writer.Write((int)assetsFile.m_TargetPlatform);
            }
            bool m_EnableTypeTree = false;
            long m_EnableTypeTree_Position = writer.Position;
            if (header.m_Version >= SerializedFileFormatVersion.HasTypeTreeHashes)
            {
                writer.Write(m_EnableTypeTree);
            }

            // Write Types
            int typeCount = assetsFile.m_Types.Count;
            writer.Write(typeCount);
            foreach (var type in assetsFile.m_Types)
            {
                WriteSerializedType(type, false);
            }

            if (header.m_Version >= SerializedFileFormatVersion.Unknown_7 && header.m_Version < SerializedFileFormatVersion.Unknown_14)
            {
                writer.Write(assetsFile.bigIDEnabled);
            }

            // Write Object Infos
            int objectCount = assetsFile.m_Objects.Count;
            writer.Write(objectCount);
            for (int i = 0; i < objectCount; i++)
            {
                var objectInfo = newObjects[i].@object;

                if (assetsFile.bigIDEnabled != 0)
                {
                    writer.Write(objectInfo.m_PathID);
                }
                else if (header.m_Version < SerializedFileFormatVersion.Unknown_14)
                {
                    writer.Write((int)objectInfo.m_PathID);
                }
                else
                {
                    writer.AlignStream();
                    writer.Write(objectInfo.m_PathID);
                }

                if (header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
                {
                    writer.Write(newObjects[i].offset - header.m_DataOffset);
                }
                else
                {
                    writer.Write((uint)(newObjects[i].offset - header.m_DataOffset));
                }

                writer.Write(objectInfo.byteSize);
                writer.Write(objectInfo.typeID);
                if (header.m_Version < SerializedFileFormatVersion.RefactoredClassId)
                {
                    writer.Write((ushort)objectInfo.classID);
                }
                if (header.m_Version < SerializedFileFormatVersion.HasScriptTypeIndex)
                {
                    writer.Write(objectInfo.isDestroyed);
                }
                if (header.m_Version >= SerializedFileFormatVersion.HasScriptTypeIndex && header.m_Version < SerializedFileFormatVersion.RefactorTypeData)
                {
                    var m_ScriptTypeIndex = (short)0;
                    if (objectInfo.serializedType != null)
                    {
                        m_ScriptTypeIndex = objectInfo.serializedType.m_ScriptTypeIndex;
                    }
                    writer.Write(m_ScriptTypeIndex);
                }
                if (header.m_Version == SerializedFileFormatVersion.SupportsStrippedObject || header.m_Version == SerializedFileFormatVersion.RefactoredClassId)
                {
                    writer.Write(objectInfo.stripped);
                }

                var pos = writer.Position;
                writer.Position = newObjects[i].offset;
                BinaryReader reader = assetsFile.reader;
                if (objectInfo is ObjectReplaced replaced)
                {
                    reader = new BinaryReader(replaced.stream);
                }
                reader.BaseStream.Position = objectInfo.byteStart;
                writer.Write(reader.ReadBytes((int)objectInfo.byteSize));
                writer.Position = pos;
            }

            // Write Script Types 
            if (header.m_Version >= SerializedFileFormatVersion.HasScriptTypeIndex)
            {
                int scriptCount = assetsFile.m_ScriptTypes.Count;
                writer.Write(scriptCount);
                foreach (var m_ScriptType in assetsFile.m_ScriptTypes)
                {
                    writer.Write(m_ScriptType.localSerializedFileIndex);
                    if (header.m_Version < SerializedFileFormatVersion.Unknown_14)
                    {
                        writer.Write((int)m_ScriptType.localIdentifierInFile);
                    }
                    else
                    {
                        writer.AlignStream();
                        writer.Write(m_ScriptType.localIdentifierInFile);
                    }
                }
            }

            // Write Externals
            int externalsCount = assetsFile.m_Externals.Count;
            writer.Write(externalsCount);
            foreach (var m_External in assetsFile.m_Externals)
            {
                if (header.m_Version >= SerializedFileFormatVersion.Unknown_6)
                {
                    writer.WriteStringToNull(m_External.tempEmpty);
                }
                if (header.m_Version >= SerializedFileFormatVersion.Unknown_5)
                {
                    writer.Write(m_External.guidBytes, 0, 16);
                    writer.Write(m_External.type);
                }
                writer.WriteStringToNull(m_External.pathName);
            }

            // Write Ref Types
            if (header.m_Version >= SerializedFileFormatVersion.SupportsRefObject)
            {
                int refTypesCount = assetsFile.m_RefTypes.Count;
                writer.Write(refTypesCount);
                foreach (var refType in assetsFile.m_RefTypes)
                {
                    WriteSerializedType(refType, true);
                }
            }

            if (header.m_Version >= SerializedFileFormatVersion.Unknown_5)
            {
                writer.WriteStringToNull(assetsFile.userInformation);
            }

            writer.AlignStream(16);

            if (header.m_Version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                writer.Position = 0x18;
                writer.WriteBE(writer.BaseStream.Length);
            }
            else
            {
                writer.Position = 0x04;
                writer.WriteBE((uint)writer.BaseStream.Length);
            }

            void WriteSerializedType(SerializedType type, bool isRefType)
            {
                writer.Write(type.classID);

                if (header.m_Version >= SerializedFileFormatVersion.RefactoredClassId)
                {
                    writer.Write(type.m_IsStrippedType);
                }

                if (header.m_Version >= SerializedFileFormatVersion.RefactorTypeData)
                {
                    writer.Write(type.m_ScriptTypeIndex);
                }

                if (header.m_Version >= SerializedFileFormatVersion.HasTypeTreeHashes)
                {
                    if (isRefType && type.m_ScriptTypeIndex >= 0)
                    {
                        writer.Write(type.m_ScriptID, 0, 16);
                    }
                    else if ((header.m_Version < SerializedFileFormatVersion.RefactoredClassId && type.classID < 0) || (header.m_Version >= SerializedFileFormatVersion.RefactoredClassId && type.classID == 114))
                    {
                        writer.Write(type.m_ScriptID, 0, 16);
                    }
                    writer.Write(type.m_OldTypeHash, 0, 16);
                }

                if (type.m_Type != null)
                {
                    if (!m_EnableTypeTree)
                    {
                        m_EnableTypeTree = true;
                        var _ = writer.Position;
                        writer.Position = m_EnableTypeTree_Position;
                        if (header.m_Version >= SerializedFileFormatVersion.HasTypeTreeHashes)
                        {
                            writer.Write(m_EnableTypeTree);
                        }
                        writer.Position = _;
                    }
                    if (header.m_Version >= SerializedFileFormatVersion.Unknown_12 || header.m_Version == SerializedFileFormatVersion.Unknown_10)
                    {
                        TypeTreeBlobWrite(type.m_Type);
                    }
                    else
                    {
                        // TODO: WriteTypeTree
                        // WriteTypeTree(type.m_Type);
                    }
                    if (header.m_Version >= SerializedFileFormatVersion.StoresTypeDependencies)
                    {
                        if (isRefType)
                        {
                            writer.WriteStringToNull(type.m_KlassName);
                            writer.WriteStringToNull(type.m_NameSpace);
                            writer.WriteStringToNull(type.m_AsmName);
                        }
                        else
                        {
                            writer.WriteArray(type.m_TypeDependencies);
                        }
                    }
                }
            }

            void TypeTreeBlobWrite(TypeTree m_Type)
            {
                writer.Write(m_Type.m_Nodes.Count);
                writer.Write(m_Type.m_StringBuffer.Length);
                foreach (var typeTreeNode in m_Type.m_Nodes)
                {
                    writer.Write((ushort)typeTreeNode.m_Version);
                    writer.Write((byte)typeTreeNode.m_Level);
                    writer.Write((byte)typeTreeNode.m_TypeFlags);
                    writer.Write(typeTreeNode.m_TypeStrOffset);
                    writer.Write(typeTreeNode.m_NameStrOffset);
                    writer.Write(typeTreeNode.m_ByteSize);
                    writer.Write(typeTreeNode.m_Index);
                    writer.Write(typeTreeNode.m_MetaFlag);
                    if (header.m_Version >= SerializedFileFormatVersion.TypeTreeNodeWithTypeFlags)
                    {
                        writer.Write(typeTreeNode.m_RefTypeHash);
                    }
                }
                writer.Write(m_Type.m_StringBuffer);
            }
        }
    }
}
