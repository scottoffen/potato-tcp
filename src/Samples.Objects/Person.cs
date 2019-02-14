using System;

namespace Samples.Objects
{
    [Serializable]
    public class Person
    {
        private static Random random = new Random();
        private static Array ecvalues = Enum.GetValues(typeof(EyeColor));

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public EyeColor EyeColor { get; set; }
        public Measurement Height { get; set; }
        public int Weight { get; set; }

        public int GetAgeInYears()
        {
            return DateTime.Now.Year - DateOfBirth.Year;
        }

        public override string ToString()
        {
            var type = this.GetType();
            type = type.DeclaringType ?? type;

            return $"({type.Name}): {LastName}, {FirstName}; {GetAgeInYears()} years old, {EyeColor.ToString().ToLower()} eyes, {Weight} lbs, {Height}";
        }

        public static Action<Guid, Person> Handler = (Guid guid, Person obj) =>
        {
            Console.WriteLine($"Received: {obj.ToString()}");
        };

        public static Person Create()
        {
            return new Person
            {
                FirstName = Constants.FirstNames[random.Next(Constants.FirstNames.Count)],
                LastName = Constants.LastNames[random.Next(Constants.LastNames.Count)],
                DateOfBirth = RandomDate(),
                EyeColor = (EyeColor)ecvalues.GetValue(random.Next(ecvalues.Length)),
                Height = Measurement.Create(),
                Weight = random.Next(50, 250)
            };
        }

        private static DateTime RandomDate()
        {
            var start = DateTime.Now.AddYears(-100);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }
    }
}
