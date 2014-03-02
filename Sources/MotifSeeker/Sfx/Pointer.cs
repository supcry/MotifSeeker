namespace MotifSeeker.Sfx
{
    public class Pointer
    {
        public int Left;
        public int Right;
        public int Depth;

        public Pointer(int depth, int left,int right)
        {
            Depth = depth;
            Left = left;
            Right = right;
        }
    }
}