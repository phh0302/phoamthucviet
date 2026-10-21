using System;
using System.Globalization;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Số lớn cho game idle: giá trị = m × 10^e, với 1 ≤ |m| < 10 (hoặc m = 0).
    /// Đủ chỗ cho mọi con số của game, vượt xa giới hạn của double.
    /// Hiển thị dạng 1.23K, 4.56M, 7.89B, 1.2T, rồi aa, ab, ac...
    /// </summary>
    [Serializable]
    public struct BigNum : IComparable<BigNum>, IEquatable<BigNum>
    {
        // Public để JsonUtility lưu được.
        public double m;
        public long e;

        public static readonly BigNum Zero = new BigNum(0);
        public static readonly BigNum One = new BigNum(1);

        public BigNum(double value)
        {
            m = value;
            e = 0;
            Normalize();
        }

        public BigNum(double mantissa, long exponent)
        {
            m = mantissa;
            e = exponent;
            Normalize();
        }

        private void Normalize()
        {
            if (m == 0 || double.IsNaN(m) || double.IsInfinity(m))
            {
                m = 0;
                e = 0;
                return;
            }
            int shift = (int)Math.Floor(Math.Log10(Math.Abs(m)));
            m /= Math.Pow(10, shift);
            e += shift;
            // Chống sai số làm tròn (ví dụ 9.9999999 → 10).
            if (Math.Abs(m) >= 10) { m /= 10; e += 1; }
            if (Math.Abs(m) < 1) { m *= 10; e -= 1; }
        }

        public bool IsZero => m == 0;
        public bool IsNegative => m < 0;

        /// <summary>log10 của giá trị tuyệt đối (−∞ nếu bằng 0).</summary>
        public double Log10 => m == 0 ? double.NegativeInfinity : Math.Log10(Math.Abs(m)) + e;

        /// <summary>Chuyển về double; trả về ±∞ nếu quá lớn.</summary>
        public double ToDouble()
        {
            if (m == 0) return 0;
            if (e > 308) return m > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            if (e < -308) return 0;
            return m * Math.Pow(10, e);
        }

        /// <summary>base^exponent, tính bằng logarit nên không tràn số.</summary>
        public static BigNum Pow(double baseValue, double exponent)
        {
            if (baseValue <= 0) return Zero;
            double log = exponent * Math.Log10(baseValue);
            double whole = Math.Floor(log);
            return new BigNum(Math.Pow(10, log - whole), (long)whole);
        }

        public static BigNum Max(BigNum a, BigNum b) => a >= b ? a : b;
        public static BigNum Min(BigNum a, BigNum b) => a <= b ? a : b;

        // ---------- Phép tính ----------

        public static BigNum operator +(BigNum a, BigNum b)
        {
            if (a.m == 0) return b;
            if (b.m == 0) return a;
            if (a.e < b.e) { var t = a; a = b; b = t; }
            long diff = a.e - b.e;
            if (diff > 17) return a; // b quá nhỏ so với a
            return new BigNum(a.m + b.m / Math.Pow(10, diff), a.e);
        }

        public static BigNum operator -(BigNum a) => new BigNum(-a.m, a.e);
        public static BigNum operator -(BigNum a, BigNum b) => a + (-b);

        public static BigNum operator *(BigNum a, BigNum b)
        {
            if (a.m == 0 || b.m == 0) return Zero;
            return new BigNum(a.m * b.m, a.e + b.e);
        }

        public static BigNum operator /(BigNum a, BigNum b)
        {
            if (b.m == 0) throw new DivideByZeroException("BigNum chia cho 0");
            if (a.m == 0) return Zero;
            return new BigNum(a.m / b.m, a.e - b.e);
        }

        public static BigNum operator *(BigNum a, double b) => a * new BigNum(b);
        public static BigNum operator *(double a, BigNum b) => new BigNum(a) * b;
        public static BigNum operator /(BigNum a, double b) => a / new BigNum(b);
        public static implicit operator BigNum(double v) => new BigNum(v);

        // ---------- So sánh ----------

        public int CompareTo(BigNum other)
        {
            int sa = Math.Sign(m), sb = Math.Sign(other.m);
            if (sa != sb) return sa.CompareTo(sb);
            if (sa == 0) return 0;
            int byMagnitude = e != other.e ? e.CompareTo(other.e) : Math.Abs(m).CompareTo(Math.Abs(other.m));
            return sa > 0 ? byMagnitude : -byMagnitude;
        }

        public bool Equals(BigNum other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is BigNum b && Equals(b);
        public override int GetHashCode() => HashCode.Combine(Math.Round(m, 9), e);

        public static bool operator <(BigNum a, BigNum b) => a.CompareTo(b) < 0;
        public static bool operator >(BigNum a, BigNum b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigNum a, BigNum b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigNum a, BigNum b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigNum a, BigNum b) => a.CompareTo(b) == 0;
        public static bool operator !=(BigNum a, BigNum b) => a.CompareTo(b) != 0;

        // ---------- Hiển thị ----------

        private static readonly string[] ShortSuffixes = { "", "K", "M", "B", "T" };

        /// <summary>Ví dụ: 950, 1.23K, 45.6M, 789B, 1.2aa.</summary>
        public override string ToString()
        {
            if (m == 0) return "0";
            string sign = m < 0 ? "-" : "";
            double absM = Math.Abs(m);

            if (e < 3)
            {
                double v = absM * Math.Pow(10, e);
                return sign + (v < 10 ? v.ToString("0.#", CultureInfo.InvariantCulture)
                                      : Math.Floor(v).ToString("0", CultureInfo.InvariantCulture));
            }

            long group = e / 3;                    // 1 = K, 2 = M, ...
            double shown = absM * Math.Pow(10, e % 3); // 1 ≤ shown < 1000
            string number = shown >= 100 ? Math.Floor(shown).ToString("0", CultureInfo.InvariantCulture)
                          : shown >= 10 ? (Math.Floor(shown * 10) / 10).ToString("0.#", CultureInfo.InvariantCulture)
                          : (Math.Floor(shown * 100) / 100).ToString("0.##", CultureInfo.InvariantCulture);
            return sign + number + Suffix(group);
        }

        private static string Suffix(long group)
        {
            if (group < ShortSuffixes.Length) return ShortSuffixes[group];
            long k = group - ShortSuffixes.Length; // 0 = aa, 1 = ab, ...
            char first = (char)('a' + (k / 26) % 26);
            char second = (char)('a' + k % 26);
            return new string(new[] { first, second });
        }
    }
}
