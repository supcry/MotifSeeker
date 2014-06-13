using System;
using System.Collections.Generic;
using System.Linq;

namespace MotifSeeker.Ga
{
    /// <summary>
    /// C_n^k. Сохраняются предпросчитанные значения логаифмов от факториалов. Потокобезопасен.
    /// </summary>
    public static class BiCombi
    {
        public static List<double> factorials = new List<double> { 0 };

        public static void UpgradeFactorialsTo(int n)
        {
            if (factorials.Count > n)
                return;
            lock (factorials)
            {
                if (factorials.Count > n)
                    return;
                while (factorials.Count <= n)
                    factorials.Add(factorials.Last() + Math.Log(factorials.Count));
            }
        }

        public static double C(int n, int k)
        {
            return Math.Exp(lnC(n, k));
        }

        public static double Cc(int n1, int k1, int n2, int k2)
        {
            return Math.Exp(lnC(n1, k1) + lnC(n2, k2));
        }

        public static double lnC(int n, int k)
        {
            return lnFact(n) - lnFact(n - k) - lnFact(k);
        }

        public static double lnFact(int n)
        {
            UpgradeFactorialsTo(n);
            return factorials[n];
        }
    }
}