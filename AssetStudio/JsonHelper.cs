using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AssetStudio
{
    public static class JsonHelper
    {
        public static OrderedDictionary ReadType(TypeTree m_Types, JObject @object)
        {
            var obj = new OrderedDictionary();
            var m_Nodes = m_Types.m_Nodes;
            for (int i = 1; i < m_Nodes.Count; i++)
            {
                var m_Node = m_Nodes[i];
                var varNameStr = m_Node.m_Name;
                obj[varNameStr] = ReadValue(m_Nodes, @object[varNameStr], ref i);
            }
            return obj;
        }

        private static object ReadValue(List<TypeTreeNode> m_Nodes, JToken token, ref int i)
        {
            var m_Node = m_Nodes[i];
            var varTypeStr = m_Node.m_Type;
            object value;
            var align = (m_Node.m_MetaFlag & 0x4000) != 0;
            switch (varTypeStr)
            {
                case "SInt8":
                    value = (sbyte)token;
                    break;
                case "UInt8":
                    value = (byte)token;
                    break;
                case "char":
                    value = (char)token;
                    break;
                case "short":
                case "SInt16":
                    value = (short)token;
                    break;
                case "UInt16":
                case "unsigned short":
                    value = (ushort)token;
                    break;
                case "int":
                case "SInt32":
                    value = (int)token;
                    break;
                case "UInt32":
                case "unsigned int":
                case "Type*":
                    value = (uint)token;
                    break;
                case "long long":
                case "SInt64":
                    value = (long)token;
                    break;
                case "UInt64":
                case "unsigned long long":
                case "FileSize":
                    value = (ulong)token;
                    break;
                case "float":
                    value = (float)token;
                    break;
                case "double":
                    value = (double)token;
                    break;
                case "bool":
                    value = (bool)token;
                    break;
                case "string":
                    value = (string)token;
                    var toSkip = GetNodes(m_Nodes, i);
                    i += toSkip.Count - 1;
                    break;
                case "map":
                    {
                        JObject @object = (JObject)token;
                        if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                            align = true;
                        var map = GetNodes(m_Nodes, i);
                        i += map.Count - 1;
                        var first = GetNodes(map, 4);
                        var next = 4 + first.Count;
                        var second = GetNodes(map, next);
                        var size = @object.Count;
                        var dic = new List<KeyValuePair<object, object>>(size);
                        foreach (var kv in @object)
                        {
                            int tmp1 = 0;
                            int tmp2 = 0;
                            dic.Add(new KeyValuePair<object, object>(ReadValue(first, kv.Key, ref tmp1), ReadValue(second, kv.Value, ref tmp2)));
                        }
                        value = dic;
                        break;
                    }
                case "TypelessData":
                    {
                        value = (byte[])token;
                        i += 2;
                        break;
                    }
                default:
                    {
                        if (i < m_Nodes.Count - 1 && m_Nodes[i + 1].m_Type == "Array") //Array
                        {
                            JArray array = (JArray)token;
                            if ((m_Nodes[i + 1].m_MetaFlag & 0x4000) != 0)
                                align = true;
                            var vector = GetNodes(m_Nodes, i);
                            i += vector.Count - 1;
                            var size = array.Count;
                            var list = new List<object>(size);
                            for (int j = 0; j < size; j++)
                            {
                                int tmp = 3;
                                list.Add(ReadValue(vector, array[j], ref tmp));
                            }
                            value = list;
                            break;
                        }
                        else //Class
                        {
                            JObject @object = (JObject)token;
                            var @class = GetNodes(m_Nodes, i);
                            i += @class.Count - 1;
                            var obj = new OrderedDictionary();
                            for (int j = 1; j < @class.Count; j++)
                            {
                                var classmember = @class[j];
                                var name = classmember.m_Name;
                                obj[name] = ReadValue(@class, @object[name], ref j);
                            }
                            value = obj;
                            break;
                        }
                    }
            }
            return value;
        }

        private static List<TypeTreeNode> GetNodes(List<TypeTreeNode> m_Nodes, int index)
        {
            var nodes = new List<TypeTreeNode>
            {
                m_Nodes[index]
            };
            var level = m_Nodes[index].m_Level;
            for (int i = index + 1; i < m_Nodes.Count; i++)
            {
                var member = m_Nodes[i];
                var level2 = member.m_Level;
                if (level2 <= level)
                {
                    return nodes;
                }
                nodes.Add(member);
            }
            return nodes;
        }
    }
}