using System.Text;
using System.Threading.Tasks;

namespace MotifSeeker.Ga
{
    public static class InfoCombi
    {
        /// <summary>
        /// ln(C_{P+N}^{p+n} / (C_P^p * C_N^n)), FET, Fisher's Exact Test, Критерий Фишера
        /// </summary>
        public static double GetInfo(int p, int n, int P, int N)
        {
            //if (p == 0)
            //	return 0;
            if (p * N < P * n)
                return 0;
            return BiCombi.lnC(P + N, p + n) - BiCombi.lnC(P, p) - BiCombi.lnC(N, n);
        }
    }
}
