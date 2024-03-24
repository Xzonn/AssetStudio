using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudio
{
    public static partial class TypeTreeHelper
    {
        public static void WriteType(OrderedDictionary obj, TypeTree m_Types, BinaryWriter writer)
        {
            // writer.Position = 0;
            var m_Nodes = m_Types.m_Nodes;
            for (int i = 1; i < m_Nodes.Count; i++)
            {
                var m_Node = m_Nodes[i];
                var varNameStr = m_Node.m_Name;
                WriteValue(obj[varNameStr], m_Nodes, writer, ref i);
            }
        }

        private static void WriteValue(object value, List<TypeTreeNode> m_Nodes, BinaryWriter writer, ref int i)
        {
            var m_Node = m_Nodes[i];
            var varTypeStr = m_Node.m_Type;
            var align = (m_Node.m_MetaFlag & 0x4000) != 0;
            switch (varTypeStr)
            {
                case "SInt8":
                    writer.Write((sbyte)value);
                    break;
                case "UInt8":
                    writer.Write((byte)value);
                    break;
                case "char":
                    writer.Write(BitConverter.GetBytes((char)value), 0, 2);
                    break;
                case "short":
                case "SInt16":
                    writer.Write((short)value);
                    break;
                case "UInt16":
                case "unsigned short":
                    writer.Write((ushort)value);
                    break;
                case "int":
                case "SInt32":
                    writer.Write((int)value);
                    break;
                case "UInt32":
                case "unsigned int":
                case "Type*":
                    writer.Write((uint)value);
                    break;
                case "long long":
                case "SInt64":
                    writer.Write((long)value);
                    break;
                case "UInt64":
                case "unsigned long long":
                case "FileSize":
                    writer.Write((ulong)value);
                    break;
                case "float":
                    writer.Write((float)value);
                    break;
                case "double":
                    writer.Write((double)value);
                    break;
                case "bool":
                    writer.Write((bool)value);
                    break;
                case "string":
                    writer.WriteAlignedString((string)value);
                    var toSkip = GetNodes(m_Nodes, i);
                    i += toSkip.Count - 1;
                    break;
                case "map":
                    {
                        if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        var map = GetNodes(m_Nodes, i);
                        i += map.Count - 1;
                        var first = GetNodes(map, 4);
                        var next = 4 + first.Count;
                        var second = GetNodes(map, next);
                        var dic = (List<KeyValuePair<object, object>>)value;
                        var size = dic.Count;
                        writer.Write(size);
                        for (int j = 0; j < size; j++)
                        {
                            var pair = dic[j];
                            int tmp1 = 0;
                            int tmp2 = 0;
                            WriteValue(pair.Key, first, writer, ref tmp1);
                            WriteValue(pair.Value, second, writer, ref tmp2);
                        }
                        break;
                    }
                case "TypelessData":
                    {
                        var size = ((byte[])value).Length;
                        writer.Write(size);
                        writer.Write((byte[])value, 0, size);
                        i += 2;
                        break;
                    }
                default:
                    {
                        if (i < m_Nodes.Count - 1 && m_Nodes[i + 1].m_Type == "Array") //Array
                        {
                            if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                                align = true;
                            var vector = GetNodes(m_Nodes, i);
                            i += vector.Count - 1;
                            var list = (List<object>)value;
                            var size = list.Count;
                            writer.Write(size);
                            for (int j = 0; j < size; j++)
                            {
                                int tmp = 3;
                                WriteValue(list[j], vector, writer, ref tmp);
                            }
                            break;
                        }
                        else //Class
                        {
                            var @class = GetNodes(m_Nodes, i);
                            i += @class.Count - 1;
                            var obj = (OrderedDictionary)value;
                            for (int j = 1; j < @class.Count; j++)
                            {
                                var classmember = @class[j];
                                var name = classmember.m_Name;
                                WriteValue(obj[name], @class, writer, ref j);
                            }
                            break;
                        }
                    }
            }
            if (align)
            {
                writer.AlignStream();
            }
        }
    }
}
