using System;
using System.IO;
using System.Text;

namespace Marked.Serializer
{
    public class BinaryDataReader : IDataReader
    {
        public Stream Stream
        {
            get => ms;
            set
            {
                ms = new MemoryStream();
                value.CopyTo(ms);
                reader = new BinaryReader(ms, Encoding.UTF8, false);
            }
        }

        private BinaryReader reader;
        private MemoryStream ms;

        public bool IsEmptyElement => throw new NotImplementedException();

        public void Dispose()
        {
            reader.Close();
            reader.Dispose();
        }

        public long ReadArrayLength()
        {
            if (PeekAndReadToken(BinaryDataToken.ArrayLength))
            {
                return reader.ReadInt64();
            }
            else
            {
                return 0;
            }
        }

        public object ReadContent(Type type)
        {
            if (PeekAndReadToken(BinaryDataToken.Content))
            {
                throw new NotImplementedException();
            }
            else
            {
                return null;
            }
        }

        public bool ReadEmptyNode(string name)
        {
            return ReadStartNode(name) && ReadEndNode(name);
        }

        public bool ReadEndNode(string name)
        {
            throw new NotImplementedException();
        }

        public int ReadId()
        {
            if (PeekAndReadToken(BinaryDataToken.Id))
            {
                return reader.ReadInt32();
            }
            else
            {
                return -1;
            }
        }

        public int ReadReference()
        {
            if (PeekAndReadToken(BinaryDataToken.RefId))
            {
                return reader.ReadInt32();
            }
            else
            {
                return -1;
            }
        }

        public bool ReadStartNode(string name)
        {
            throw new NotImplementedException();
        }

        public Type ReadType()
        {
            if (PeekAndReadToken(BinaryDataToken.Type))
            {
                return Type.GetType(reader.ReadString());
            }
            else
            {
                return null;
            }
        }

        private bool PeekAndReadToken(BinaryDataToken expected)
        {
            var peek = (BinaryDataToken)reader.ReadByte();
            if (peek == expected)
            {
                return true;
            }
            else
            {
                ms.Position--;
                return false;
            }
        }

        private bool PeekAndReadEmptyNode()
        {
            long start = ms.Position;
            bool empty = false;
            if (PeekAndReadToken(BinaryDataToken.NodeStart))
            {
                string name = reader.ReadString();
                var type = ReadType();
                var id = ReadId();
                var refId = ReadReference();

                if (PeekAndReadToken(BinaryDataToken.NodeEnd))
                {

                }
            }

            ms.Position = start;
            return empty;
        }
    }
}
