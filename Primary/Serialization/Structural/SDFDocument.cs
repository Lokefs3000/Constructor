using System.Collections;
using System.Diagnostics;

namespace Primary.Serialization.Structural
{
    public sealed class SDFDocument : IReadOnlyList<SDFObject>
    {
        private List<SDFObject> _objects;

        public SDFDocument()
        {
            _objects = new List<SDFObject>();
        }

        public static SDFDocument Parse(ref SDFReader reader)
        {
            reader.Reset();

            SDFDocument document = new SDFDocument();
            while (reader.Read())
            {
                if (reader.TokenType == SDFTokenType.ObjectBegin)
                    document._objects.Add(DeserializeRoutineObject(ref reader));
            }

            return document;
        }

        private static SDFObject DeserializeRoutineObject(ref SDFReader reader)
        {
            Debug.Assert(reader.TokenType == SDFTokenType.ObjectBegin);

            SDFObject sdf = new SDFObject();
            sdf.Name = reader.GetString();

            while (reader.Read())
            {
                if (reader.TokenType == SDFTokenType.ObjectEnd)
                    break;
                else
                {
                    if (reader.TokenType == SDFTokenType.ObjectBegin)
                    {
                        SDFObject @object = DeserializeRoutineObject(ref reader);
                        sdf.Add(@object.Name!, @object);
                    }
                    else if (reader.TokenType == SDFTokenType.Property)
                    {
                        string propertyName = reader.GetString();

                        bool r = reader.Read();
                        Debug.Assert(r);

                        if (r)
                        {
                            if (reader.TokenType == SDFTokenType.ObjectBegin)
                            {
                                SDFObject @object = DeserializeRoutineObject(ref reader);
                                sdf.Add(propertyName, @object);
                            }
                            else if (reader.TokenType == SDFTokenType.ArrayBegin)
                            {
                                SDFArray array = DeserializeRoutineArray(ref reader);
                                sdf.Add(propertyName, array);
                            }
                            else
                            {
                                SDFProperty property = new SDFProperty();
                                property.Value = reader.Slice.ToString();

                                sdf.Add(propertyName, property);
                            }
                        }
                    }
                }
            }

            Debug.Assert(reader.TokenType == SDFTokenType.ObjectEnd);
            return sdf;
        }

        private static SDFArray DeserializeRoutineArray(ref SDFReader reader)
        {
            Debug.Assert(reader.TokenType == SDFTokenType.ArrayBegin);

            SDFArray array = new SDFArray();
            while (reader.Read())
            {
                if (reader.TokenType == SDFTokenType.ArrayEnd)
                    break;

                if (reader.TokenType == SDFTokenType.ObjectBegin)
                {
                    array.Add(DeserializeRoutineObject(ref reader));
                }
                else if (reader.TokenType == SDFTokenType.ObjectEnd)
                {
                    array.Add(DeserializeRoutineObject(ref reader));
                }
                else if (reader.TokenType == SDFTokenType.Number || reader.TokenType == SDFTokenType.Boolean || reader.TokenType == SDFTokenType.String)
                {
                    SDFProperty property = new SDFProperty();
                    property.Value = reader.Slice.ToString();

                    array.Add(property);
                }
            }

            Debug.Assert(reader.TokenType == SDFTokenType.ArrayEnd);
            return array;
        }

        #region Auto-generated
        public SDFObject this[int index] => _objects[index];
        public int Count => _objects.Count;

        public IEnumerator<SDFObject> GetEnumerator() => _objects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
