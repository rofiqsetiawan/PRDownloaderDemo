// Created by Rofiq Setiawan (rofiqsetiawan@gmail.com)

#nullable enable

using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Downloader;
using Pub.DevRel.EasyPermissionsLib;
using Environment = Android.OS.Environment;
using R = SimpleDemo.Resource;

namespace SimpleDemo
{
    [Register("kid.prdownloader.simpledemo.MainActivity")]
    [Activity(MainLauncher = true, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class MainActivity : AppCompatActivity
    {
        private const string _tag = "PRDownloader";
        private int _currDownloadId = -1;
        private const string _url = "https://file-examples-com.github.io/uploads/2017/04/file_example_MP4_480_1_5MG.mp4";

        private TextView? _tvProgress;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(R.Layout.activity_main);

            _tvProgress = FindViewById<TextView>(R.Id.tv_progress)!;

            var config = PRDownloaderConfig.NewBuilder()
                .SetDatabaseEnabled(true)
                .SetReadTimeout(30_000)
                .SetConnectTimeout(30_000)
                .Build();

            PRDownloader.Initialize(ApplicationContext, config);

            FindViewById<Button>(R.Id.btn_download)!.Click += BtnDownloadOnClick;

            FindViewById<Button>(R.Id.btn_pause)!.Click += BtnPauseOnClick;

            FindViewById<Button>(R.Id.btn_resume)!.Click += BtnResumeOnClick;

            FindViewById<Button>(R.Id.btn_cancel)!.Click += BtnCancelOnClick;
        }

        private void BtnDownloadOnClick(object sender, EventArgs e)
        {
            var canContinue = IsStorageAccessGranted();
            if (!canContinue)
            {
                ShowToast(GetString(R.String.give_access_to_storage));
                return;
            }

            var fileName = System.IO.Path.GetFileName(_url);
            var savingDirPath = GetSavingDirPath(this);
            var fileSavingPath = System.IO.Path.Combine(savingDirPath, fileName);

            // Just simple checking
            // in production you should use `PRDownloader.GetStatus(_currDownloadId))`
            if (System.IO.File.Exists(fileSavingPath))
            {
                ShowToast($"File already downloaded at {fileSavingPath}");
                return;
            }

            _currDownloadId = PRDownloader.Download(_url, savingDirPath, fileName)
                .Build()
                .SetOnStartOrResumeListener(
                    new StartOrResumeListener(
                        () =>
                        {
                            var msg = "OnStartOrResume";
                            LogD(msg);
                            ShowToast(msg);
                        }
                    )
                )
                .SetOnPauseListener(
                    new PauseListener(
                        () =>
                        {
                            var msg = "OnPause";
                            LogD(msg);
                            ShowToast(msg);
                        }
                    )
                )
                .SetOnCancelListener(
                    new CancelListener(
                        () =>
                        {
                            var msg = "OnCancel";
                            LogD(msg);
                            ShowToast(msg);
                        }
                    )
                )
                .SetOnProgressListener(
                    new ProgressListener(
                        progress =>
                        {
                            var percentage = progress.CurrentBytes * 100 / progress.TotalBytes;

                            _tvProgress!.Text = $"{percentage}% ({progress.CurrentBytes} of {progress.TotalBytes} bytes)";
                        }
                    )
                )
                .Start(
                    new DownloadListener(
                        () =>
                        {
                            var msg = "Download completed";
                            LogD(msg);
                            ShowToast(msg);
                        },
                        error =>
                        {
                            var msg = $"Download error! {error.ServerErrorMessage}";
                            LogD(msg);
                            ShowToast(msg);
                        }
                    )
                );
        }

        private void BtnPauseOnClick(object sender, EventArgs e)
        {
            if (_currDownloadId == -1)
            {
                ShowToast("Nothing to pause!");
                return;
            }

            PRDownloader.Pause(_currDownloadId);

            ShowToast("Download paused!");
        }

        private void BtnResumeOnClick(object sender, EventArgs e)
        {
            if (_currDownloadId == -1)
            {
                ShowToast("Nothing to resume!");
                return;
            }

            PRDownloader.Pause(_currDownloadId);

            ShowToast("Download resumed!");
        }

        private void BtnCancelOnClick(object sender, EventArgs e)
        {
            if (_currDownloadId == -1)
            {
                ShowToast("Nothing to cancel");
                return;
            }

            PRDownloader.Cancel(_currDownloadId);

            ShowToast("Download cancelled!");
        }

        public override bool OnCreateOptionsMenu(IMenu? menu)
        {
            MenuInflater.Inflate(R.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return item.ItemId switch
            {
                R.Id.action_settings => true,
                _ => base.OnOptionsItemSelected(item)
            };
        }

        // ~/Download/PRDownloader
        public static string GetSavingDirPath(Context context)
        {
            // TODO: on Android 10 (Q): Android.OS.Environment.ExternalStorageDirectory is obsolete
            return System.IO.Path.Combine(
                Environment.ExternalStorageDirectory.AbsolutePath, Environment.DirectoryDownloads, _tag
            );
        }

        private void LogD(string msg) => Log.Debug(_tag, msg);

        private void ShowToast(string msg) => Toast.MakeText(ApplicationContext, msg, ToastLength.Short)!.Show();

        private const int RcStoragePerm = 48;

        private bool IsStorageAccessGranted()
        {
            var perms = new[] {
                Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.WriteExternalStorage
            };

            if (EasyPermissions.HasPermissions(this, perms))
                return true;

            EasyPermissions.RequestPermissions(
                this, GetString(R.String.give_access_to_storage), RcStoragePerm, perms
            );

            return false;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            EasyPermissions.OnRequestPermissionsResult(requestCode, permissions, grantResults.Cast<int>().ToArray(), this);
        }
    }
}
