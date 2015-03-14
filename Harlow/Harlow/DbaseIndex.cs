using System;
using System.IO;

namespace Harlow
{
    /// <summary>
    /// The possible types of fields that we'll come across in a dbf file.
    /// All are handled the same way right now, as strings.
    /// </summary>
    public enum FieldType : byte
    {
        C = 0x43, D = 0x44, F = 0x46, N = 0x4E, L = 0x4C,
        c = 0x63, d = 0x64, f = 0x66, n = 0x6E, l = 0x6C,
    };

    /// <summary>
    /// Loads the relevant data structures by reading the header of the .dbf
    /// This is an Abstract class and is not directly instantiated, it is 
    /// inherited by the Dbase class.
    /// </summary>
    abstract public class DbaseIndex
    {

        protected string[] _FieldNames;
        protected FieldType[] _FieldTypes;
        protected byte[] _FieldLengths;
        protected uint _RecordCount;
        protected ushort _RecordLength;
        protected uint _RecordStart;
        protected uint _FieldCount;
        protected string _Filename;

        private byte _DbType;
        private byte _UpdateYear;
        private byte _UpdateMonth;
        private byte _UpdateDay;
        private ushort _HeaderLength;

        public DbaseIndex(string filename)
        {
            ReadHeader(filename);
        }

        private void ReadHeader(string filename)
        {

            filename = filename.Remove(filename.Length - 4, 4);
            filename += ".dbf";
            _Filename = filename;

            FileStream fs = new FileStream(filename, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            byte checkByte;
            byte bbuffer;

            _DbType = br.ReadByte();

            if (_DbType == 0x03)
            {
                _UpdateYear = br.ReadByte();
                _UpdateMonth = br.ReadByte();
                _UpdateDay = br.ReadByte();
                _RecordCount = br.ReadUInt32();
                _HeaderLength = br.ReadUInt16();
                _RecordLength = br.ReadUInt16();

                // Header size minus terminator char divided by length of
                // a field descriptor minus 1 to adjust for the file descriptor.
                _FieldCount = (uint)((_HeaderLength - 1) / 32) - 1;

                _FieldNames = new string[_FieldCount];
                _FieldTypes = new FieldType[_FieldCount];
                _FieldLengths = new byte[_FieldCount];

                for (int a = 0; a < _FieldCount; ++a)
                {
                    fs.Seek(32 + (32 * a), 0);

                    // Has to be read as byte and converted to char
                    // because a 0x9e is appended to the end of some
                    // field descriptors and it throws off br.ReadChars( x )
                    for (int b = 0; b < 11; ++b)
                    {
                        bbuffer = br.ReadByte();
                        if (bbuffer == 0) { continue; } // ignore nulls
                        
                        _FieldNames[a] += (char)bbuffer;
                    }

                    _FieldTypes[a] = (FieldType)br.ReadByte();
                    br.ReadBytes(4); // field address in memory...??
                    _FieldLengths[a] = br.ReadByte();
                }
            }

            // Field descriptors are 32 bytes each + 32 bytes for the file 
            // descriptor + 1 byte for the header record terminator (0x0D)
            _RecordStart = (_FieldCount * 32) + 32 + 1;

            fs.Seek(_RecordStart - 1, 0);

            checkByte = br.ReadByte(); // should be 0x0D (13)

            br.Close();
            fs.Close();
        }


        /// <summary>
        /// Gets the names of the fields in the database.
        /// </summary>
        /// @returns A string array containing the field names
        public string[] FieldNames
        {
            get
            {
                return _FieldNames;
            }
        }
    }
}