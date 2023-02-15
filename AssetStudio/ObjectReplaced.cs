using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudio
{
    public class ObjectReplaced : ObjectInfo
    {
        public Stream stream;
        ObjectInfo @object;


        public ObjectReplaced(ObjectInfo @object, Stream stream)
        {
            this.@object = @object;
            this.stream = stream;

            byteStart = 0;
            byteSize = (uint)stream.Length;
            typeID = @object.typeID;
            classID = @object.classID;
            isDestroyed = @object.isDestroyed;
            stripped = @object.stripped;

            m_PathID = @object.m_PathID;
            serializedType = @object.serializedType;
        }
    }
}