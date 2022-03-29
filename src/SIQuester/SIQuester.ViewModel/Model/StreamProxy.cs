using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace SIQuester.Model
{
    [Serializable]
    public sealed class StreamProxy : ISerializable, IDisposable
    {
        public Stream Stream { get; }

        public StreamProxy(Stream stream)
        {
            Stream = stream;
        }

        private StreamProxy(SerializationInfo info, StreamingContext context)
        {
            var type = info.GetInt32("type");
            if (type == 0)
            {
                var path = info.GetString("path");
                Stream = File.OpenRead(path);
            }
            else
            {
                Stream = (MemoryStream)info.GetValue("stream", typeof(MemoryStream));
            }
        }

        public async void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                if (Stream.Length > 4194304)
                {
                    var temp = Path.GetTempFileName();
                    using (var fs = File.Create(temp))
                    {
                        await Stream.CopyToAsync(fs);
                    }

                    info.AddValue("path", temp, typeof(string));
                    info.AddValue("type", 0);
                }
                else
                {
                    var ms = new MemoryStream();
                    await Stream.CopyToAsync(ms);
                    info.AddValue("stream", ms, typeof(MemoryStream));
                    info.AddValue("type", 1);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"GetObjectData error: {ex}");
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
