using System;
using System.IO;
using System.Linq;

namespace Samples.Objects
{
    [Serializable]
    public class LargeDataObject
    {
        private static readonly string largeText = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LargeDataFile.txt"));

        public string Payload { get; set; } = string.Concat(Enumerable.Repeat(largeText, 1));

        public override string ToString()
        {
            return Payload;
        }

        public static Action<Guid, LargeDataObject> Handler = (Guid guid, LargeDataObject obj) =>
        {
            Console.WriteLine($"Received: {obj.ToString().Length} characters of text.");
        };
    }
}