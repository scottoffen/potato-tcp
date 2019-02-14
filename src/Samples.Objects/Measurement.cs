using System;

namespace Samples.Objects
{
    [Serializable]
    public class Measurement
    {
        private static Random random = new Random();

        public int Feet { get; set; }
        public int Inches { get; set; }

        public override string ToString()
        {
            return $"{Feet}'{Inches}\"";
        }

        public static Measurement Create()
        {
            return new Measurement { Feet = random.Next(3, 6), Inches = random.Next(12) };
        }
    }
}