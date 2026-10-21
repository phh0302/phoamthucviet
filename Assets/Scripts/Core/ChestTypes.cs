using System;

namespace PhoAmThucViet.Core
{
    /// <summary>Một loại rương bán bằng đá quý: số món, giá và tỉ lệ độ hiếm của mỗi món.</summary>
    public class ChestType
    {
        public string id, name;
        public int items, price;
        /// <summary>Trọng số Thường / Hiếm / Sử thi / Huyền thoại (tổng 100 = phần trăm).</summary>
        public float[] weights;

        /// <summary>Tỉ lệ (0–1) một món ra đúng độ hiếm này.</summary>
        public double Rate(Rarity r)
        {
            float total = 0;
            foreach (float w in weights) total += Math.Max(0, w);
            int i = (int)r;
            return total > 0 && i < weights.Length ? Math.Max(0, weights[i]) / total : 0;
        }

        /// <summary>Tỉ lệ cả rương có ít nhất 1 món độ hiếm này.</summary>
        public double AtLeastOne(Rarity r) => 1 - Math.Pow(1 - Rate(r), items);
    }

    public static class ChestTypes
    {
        /// <summary>Rương nhỏ: 2 món, 30 ♦, không ra Huyền thoại.</summary>
        public static readonly ChestType Small = new ChestType
        {
            id = "small", name = "Rương nhỏ", items = 2, price = 30,
            weights = new[] { 52f, 35f, 13f, 0f },
        };

        /// <summary>Rương lớn: 6 món, 80 ♦, mỗi món 5% ra Huyền thoại.</summary>
        public static readonly ChestType Big = new ChestType
        {
            id = "big", name = "Rương lớn", items = 6, price = 80,
            weights = new[] { 50f, 32f, 13f, 5f },
        };

        public static readonly ChestType[] All = { Small, Big };
    }
}
