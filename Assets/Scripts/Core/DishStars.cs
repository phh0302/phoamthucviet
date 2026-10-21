namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Sao của món ăn: mỗi mốc x2 giá món (cấp 10, 25, 50, 75, 100, 150, 200, 250) là 1 sao và thưởng 1 đá quý.
    /// Mỗi màu sao có 3 sao; qua sao thứ 3 thì đổi màu (xanh lục → lam → vàng → tím → đỏ) và đếm lại từ 1.
    /// </summary>
    public static class DishStars
    {
        public const int StarsPerTier = 3;
        public const int GemsPerStar = 1;

        /// <summary>Số sao đang sáng (0–3) với số mốc x2 đã đạt.</summary>
        public static int Lit(int doublings) => doublings <= 0 ? 0 : (doublings - 1) % StarsPerTier + 1;

        /// <summary>Màu sao: 0 xanh lục, 1 lam, 2 vàng, 3 tím, 4 đỏ.</summary>
        public static int Tier(int doublings) => doublings <= 0 ? 0 : (doublings - 1) / StarsPerTier;

        /// <summary>
        /// Số ô sao cần hiện ở màu hiện tại: tối đa 3, nhưng nếu quầy chỉ còn đạt được ít mốc hơn
        /// (ví dụ cấp tối đa 25 chỉ có 2 mốc) thì chỉ hiện đúng số đó.
        /// </summary>
        public static int Slots(int doublings, int maxDoublings)
        {
            int tier = Tier(doublings);
            return System.Math.Max(0, System.Math.Min(StarsPerTier, maxDoublings - tier * StarsPerTier));
        }

        public static readonly string[] TierNames = { "xanh lục", "lam", "vàng", "tím", "đỏ" };
    }
}
