namespace MotifSeeker.Sfx
{
	public class Pointer
	{
		public int SrcId;

		public int LastLeft;
		public int LastRigth;
		public int LastDepth;

		public int Left;
		public int Rigth;

		public int Depth;

		public Pointer(int depth, int left, int right)
			: this(depth, left, right, 0)
		{
		}

		public Pointer(int depth, int left, int rigth, int srcId)
		{
			Depth = depth;
			Left = left;
			Rigth = rigth;
			SrcId = srcId;
		}

		public void Reset(int rigth, int srcId)
		{
			Left = 0;
			Rigth = rigth;
			Depth = 0;

			LastLeft = 0;
			LastRigth = rigth;
			LastDepth = 0;

			SrcId = srcId;
		}
	}
}