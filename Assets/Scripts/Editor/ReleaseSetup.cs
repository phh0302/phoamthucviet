using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PhoAmThucViet.EditorTools
{
    /// <summary>
    /// Menu "Phố Ẩm Thực/Phát hành": áp cài đặt Android cho Google Play và build AAB / APK một chạm.
    /// Ký app (keystore) người làm game tự tạo trong Project Settings → Player → Android → Publishing Settings.
    /// </summary>
    public static class ReleaseSetup
    {
        public const string ProductName = "Phố Ẩm Thực Việt";
        public const string CompanyName = "Pho Am Thuc Studio";
        /// <summary>Mã app trên Google Play. Đổi TRƯỚC lần tải lên đầu tiên; sau đó không đổi được nữa.</summary>
        public const string PackageName = "com.phoamthuc.phoamthucviet";
        public const string IconPath = "Assets/Art/Icon/app_icon.png";
        public const string BuildDir = "Builds";

        [MenuItem("Phố Ẩm Thực/Phát hành/1. Áp dụng cài đặt Android")]
        public static void ApplySettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // Màn dọc, không xoay
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Google Play bắt buộc 64-bit và API mới nhất
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "0.1.0";

            // Màn hình chờ: nền đỏ son như icon
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.84f, 0.19f, 0.15f);
            PlayerSettings.SplashScreen.showUnityLogo = false;

            ApplyIcon();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phát hành] Đã áp cài đặt: {ProductName} · {PackageName} · v{PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})");
            EditorUtility.DisplayDialog("Phát hành", $"Đã áp cài đặt Android.\n\nTên app: {ProductName}\nMã app: {PackageName}\n" +
                $"Phiên bản: {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})\n\n" +
                "Nhớ: mã app không đổi được sau lần tải lên Google Play đầu tiên.", "OK");
        }

        private static void ApplyIcon()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) { Debug.LogWarning($"[Phát hành] Không thấy icon ở {IconPath}"); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        [MenuItem("Phố Ẩm Thực/Phát hành/2. Build AAB (gửi Google Play)")]
        public static void BuildAab() => Build(appBundle: true);

        [MenuItem("Phố Ẩm Thực/Phát hành/3. Build APK (cài thử trên điện thoại)")]
        public static void BuildApk() => Build(appBundle: false);

        [MenuItem("Phố Ẩm Thực/Phát hành/Mở thư mục Builds")]
        public static void OpenBuilds()
        {
            Directory.CreateDirectory(BuildDir);
            EditorUtility.RevealInFinder(Path.GetFullPath(BuildDir));
        }

        private static void Build(bool appBundle)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Thiếu Android Build Support",
                    "Unity chưa cài module Android.\n\nMở Unity Hub → Installs → bánh răng của bản Unity đang dùng → Add modules → " +
                    "tích \"Android Build Support\" (kèm OpenJDK và Android SDK & NDK Tools) → Install, rồi mở lại project.", "OK");
                return;
            }

            if (appBundle && !PlayerSettings.Android.useCustomKeystore &&
                !EditorUtility.DisplayDialog("Chưa có keystore",
                    "AAB gửi Google Play phải được ký bằng keystore riêng của bạn (giữ kỹ, mất là không cập nhật app được).\n\n" +
                    "Tạo ở: Project Settings → Player → Android → Publishing Settings → Keystore Manager.\n\n" +
                    "Vẫn build bằng khóa debug (chỉ để thử, Google Play sẽ từ chối)?", "Vẫn build", "Hủy"))
                return;

            ApplySettings();
            if (appBundle) PlayerSettings.Android.bundleVersionCode++; // mỗi bản gửi lên Play phải tăng số này

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { EditorUtility.DisplayDialog("Phát hành", "Build Settings chưa có scene nào.", "OK"); return; }

            Directory.CreateDirectory(BuildDir);
            string file = $"PhoAmThucViet-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}{(appBundle ? ".aab" : ".apk")}";
            EditorUserBuildSettings.buildAppBundle = appBundle;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(BuildDir, file),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None, // bản phát hành: tự ẩn công cụ thử (DebugTools)
            });

            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build xong", $"{file}\n{summary.totalSize / (1024f * 1024f):0.0} MB · {summary.totalTime.TotalMinutes:0.0} phút\n\nNằm trong thư mục {BuildDir}/ của project.", "OK");
                OpenBuilds();
            }
            else EditorUtility.DisplayDialog("Build lỗi", $"Kết quả: {summary.result}, {summary.totalErrors} lỗi.\nXem chi tiết trong Console.", "OK");
        }
    }
}
