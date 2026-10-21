using System.Collections.Generic;
using System.Text;
using PhoAmThucViet.Core;
using UnityEditor;
using UnityEngine;

namespace PhoAmThucViet.EditorTools
{
    /// <summary>
    /// Mô phỏng một người chơi tích cực: mỗi giây mua lựa chọn có hiệu quả tốt nhất
    /// (thu nhập tăng thêm / giá) cho tới khi nâng hết quầy, đầu bếp và phục vụ. In ra số phút hoàn thành
    /// và thời điểm mở từng quầy, để chỉnh số liệu về đúng 15–20 phút mỗi thành phố.
    /// Giả định tiền tự thu và chưa tính tiền thuê quản lý.
    /// </summary>
    public class PacingSimulator : EditorWindow
    {
        private CityData _city;
        private float _profitPct, _cookPct, _discountPct;
        private string _report = "";
        private Vector2 _scroll;

        [MenuItem("Phố Ẩm Thực/Mô phỏng nhịp chơi")]
        public static void Open() => GetWindow<PacingSimulator>("Nhịp chơi");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Chọn một thành phố rồi bấm Mô phỏng. Mục tiêu mỗi mốc: 1) 2–4 phút, 2) 4–6 phút, 3) 10–15 phút, " +
                "4) 15–20 phút, 5) 15–20 phút; cả thành phố khoảng 54 phút. Cấp tối đa các quầy theo mốc: 25 / 50 / 150 / 250 / 250.", MessageType.Info);
            _city = (CityData)EditorGUILayout.ObjectField("Thành phố", _city, typeof(CityData), false);

            EditorGUILayout.LabelField("Trang bị đầu bếp giả định (%)", EditorStyles.boldLabel);
            _profitPct = EditorGUILayout.Slider("Lợi nhuận +", _profitPct, 0, (float)(ChefBonuses.MaxProfit * 100));
            _cookPct = EditorGUILayout.Slider("Nấu nhanh", _cookPct, 0, 80f);
            _discountPct = EditorGUILayout.Slider("Giảm giá", _discountPct, 0, (float)(ChefBonuses.MaxDiscount * 100));
            var bonuses = new ChefBonuses { profit = _profitPct / 100.0, cookSpeed = _cookPct / 100.0, discount = _discountPct / 100.0 };

            using (new EditorGUI.DisabledScope(_city == null))
            {
                if (GUILayout.Button("Mô phỏng thành phố này", GUILayout.Height(28))) _report = SimulateCity(_city, bonuses);
                if (GUILayout.Button("Mô phỏng cả hành trình từ thành phố này", GUILayout.Height(28)))
                {
                    var sb = new StringBuilder();
                    for (var c = _city; c != null; c = c.nextCity) sb.AppendLine(SimulateCity(c, bonuses)).AppendLine();
                    _report = sb.ToString();
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        public static string SimulateCity(CityData city, ChefBonuses b)
        {
            var sb = new StringBuilder();
            float total = 0;
            for (int i = 0; i < city.venues.Count; i++)
            {
                sb.Append($"[Mốc {i + 1}] ");
                sb.AppendLine(Simulate(city.venues[i], b, out float minutes));
                total += minutes;
            }
            sb.Insert(0, $"##### {city.displayName}: tổng {total:0.0} phút cho {city.venues.Count} mốc #####\n\n");
            return sb.ToString();
        }

        public static string Simulate(VenueData venue, ChefBonuses b, out float minutes, int maxSeconds = 3600)
        {
            int n = venue.stations.Count;
            var level = new int[n];
            var chefs = new int[n];
            var unlockedAt = new int[n];
            for (int i = 0; i < n; i++) { unlockedAt[i] = -1; chefs[i] = 1; }
            level[0] = 1; unlockedAt[0] = 0;
            int waiters = 1;

            BigNum money = BigNum.Zero, earned = BigNum.Zero;
            int t = 0, lastBuy = 0, longestWait = 0, longestWaitAt = 0;
            bool done = false;

            BigNum Income(int[] lv, int[] ch, int w)
            {
                BigNum sum = BigNum.Zero;
                double speed = EconomyService.ServiceSpeed(venue, w);
                for (int i = 0; i < n; i++)
                    if (lv[i] > 0) sum += EconomyService.IncomePerSecond(venue.stations[i], venue, lv[i], b, ch[i], speed);
                return sum;
            }

            for (; t < maxSeconds; t++)
            {
                bool bought = false;
                // Mua liên tục trong giây này cho tới khi không đủ tiền cho lựa chọn tốt nhất
                // (lựa chọn tốt nhất = thu nhập tăng thêm / giá).
                while (true)
                {
                    BigNum current = Income(level, chefs, waiters);
                    int bestKind = -1, bestIndex = -1; double bestScore = -1; BigNum bestCost = BigNum.Zero;

                    void Consider(int kind, int index, BigNum cost, BigNum gain)
                    {
                        double score = cost.IsZero ? double.MaxValue : (gain / cost).ToDouble();
                        if (score > bestScore) { bestScore = score; bestKind = kind; bestIndex = index; bestCost = cost; }
                    }

                    for (int i = 0; i < n; i++)
                    {
                        var s = venue.stations[i];
                        if (level[i] == 0)
                        {
                            if (i > 0 && level[i - 1] == 0) continue;
                            level[i] = 1;
                            Consider(0, i, EconomyService.UnlockCost(s, venue, b), Income(level, chefs, waiters) - current);
                            level[i] = 0;
                            continue;
                        }
                        if (level[i] < s.maxLevel)
                        {
                            level[i]++;
                            BigNum gain = Income(level, chefs, waiters) - current;
                            level[i]--;
                            Consider(1, i, EconomyService.UpgradeCost(s, venue, level[i], b), gain);
                        }
                        if (chefs[i] < s.maxChefs)
                        {
                            chefs[i]++;
                            BigNum gain = Income(level, chefs, waiters) - current;
                            chefs[i]--;
                            Consider(2, i, EconomyService.ChefCost(s, venue, chefs[i], b), gain);
                        }
                    }
                    if (waiters < venue.MaxWaiters)
                        Consider(3, 0, EconomyService.WaiterCost(venue, waiters, b), Income(level, chefs, waiters + 1) - current);

                    if (bestKind < 0 || bestCost > money) break;
                    money -= bestCost;
                    bought = true;
                    switch (bestKind)
                    {
                        case 0: level[bestIndex] = 1; unlockedAt[bestIndex] = t; break;
                        case 1: level[bestIndex]++; break;
                        case 2: chefs[bestIndex]++; break;
                        case 3: waiters++; break;
                    }
                }

                if (bought)
                {
                    if (t - lastBuy > longestWait) { longestWait = t - lastBuy; longestWaitAt = t; }
                    lastBuy = t;
                }

                BigNum income = Income(level, chefs, waiters);
                money += income;
                earned += income;

                bool all = waiters >= venue.MaxWaiters;
                for (int i = 0; i < n && all; i++)
                    all = level[i] >= venue.stations[i].maxLevel && chefs[i] >= venue.stations[i].maxChefs;
                if (all) { done = true; t++; break; }
            }

            minutes = done ? t / 60f : maxSeconds / 60f;
            var sb = new StringBuilder();
            sb.AppendLine($"== {venue.displayName} ==");
            sb.AppendLine(done ? $"Nâng hết sau {t / 60f:0.0} phút" : $"CHƯA nâng hết sau {maxSeconds / 60} phút");
            sb.AppendLine($"Lần chờ lâu nhất giữa 2 lần mua: {longestWait} giây (ở phút {longestWaitAt / 60f:0.0})");
            sb.AppendLine($"Tiền kiếm được: {earned}  ·  Phục vụ: {waiters}/{venue.MaxWaiters}");
            for (int i = 0; i < n; i++)
            {
                var s = venue.stations[i];
                string open = unlockedAt[i] < 0 ? "chưa mở" : $"mở ở phút {unlockedAt[i] / 60f:0.0}";
                sb.AppendLine($"  {s.displayName,-14} cấp {level[i],3}/{s.maxLevel}  ·  đầu bếp {chefs[i]}/{s.maxChefs}  ·  {open}");
            }
            sb.AppendLine("(Chưa tính tiền thuê quản lý và thời gian bấm Thu tiền.)");
            return sb.ToString();
        }
    }
}
