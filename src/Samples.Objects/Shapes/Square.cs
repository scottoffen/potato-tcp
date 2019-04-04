namespace Samples.Objects.Shapes
{
    public class Square : Rectangle
    {
        public static Square GetTinySquare()
        {
            return new TinySquare();
        }
    }

    internal class TinySquare : Square
    {

    }
}