using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core
{
    public class LocalStore<T>
    {
        private readonly StreamReader _reader;
        private readonly FileStream _stream;
        private readonly StreamWriter _writer;

        public LocalStore()
        {
            Guid name = typeof (T).GUID;
            string path = string.Format(@"c:\LocalStore\{0}.dat", name);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream);
        }

        public T ReadOne(string id = null, int? row = null)
        {
            // read one row, populate object
            lock (_stream)
            {
                _stream.Seek(0, SeekOrigin.Begin);

                if (row == null)
                    row = int.MaxValue;

                var rowData = new string[0];

                for (int i = 1; i <= row; i++)
                {
                    if (_reader.EndOfStream)
                        return default(T);

                    string data = _reader.ReadLine();

                    rowData = data.Split(new[] {';'}, 2);

                    if (!string.IsNullOrEmpty(id) && rowData[0] == id)
                        return JsonSerializer.DeserializeFromString<T>(rowData[1]);
                }

                return JsonSerializer.DeserializeFromString<T>(rowData[1]);
            }
        }

        public List<T> ReadAll()
        {
            string full = string.Empty;

            // read all rows, populate object
            lock (_stream)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                full = _reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(full))
                return null;

            List<string> records = full.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ToList();

            if (records.Count > 0)
                records.RemoveAt(records.Count - 1);

            List<T> objects = records.Select(s => { return JsonSerializer.DeserializeFromString<T>(s.Split(new[] {';'}, 2)[1]); }).ToList();

            return objects;
        }

        public bool WriteOne(string id, T value)
        {
            string serializedObj = id + ";" + JsonSerializer.SerializeToString(value);

            lock (_stream)
            {
                _stream.Seek(0, SeekOrigin.End);
                _writer.WriteLine(serializedObj);
                _writer.Flush();
            }

            return true;
        }

        public bool Clear()
        {
            lock (_stream)
            {
                // clear file
                _stream.SetLength(0);
                _stream.Flush();
            }
            return true;
        }

        public bool Close()
        {
            _stream.Close();

            try
            {
                _reader.Close();
                _writer.Close();
            }
            catch (Exception ex)
            {
            }

            return true;
        }
    }
}