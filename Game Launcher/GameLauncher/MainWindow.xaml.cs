using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace GameLauncher
{
    internal enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private readonly string Bulid;
        private LauncherStatus _status;
        private static readonly string Filename = "GameLauncher.exe";
        private readonly string ErrorLog = "ErrorLog";
        private static readonly string Shortcutname = "Poshy's Game Launcher";
        private readonly string Version_URL = "https://onedrive.live.com/download?cid=97C8437F9F5C8CC6&resid=97C8437F9F5C8CC6%213993&authkey=ADbLJx7jtGtU_YM";
        private readonly string Game_Download_URL = "https://drive.google.com/uc?export=download&id=16PxeCK4vpHUF-962d_-amFKMoJgARGHe";

        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;

                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;

                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;

                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;

                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            MakeBaseFolders();
            Progressbar.Visibility = (Visibility)1;
            Bulid = "Bulid";
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, Bulid + ".zip");
            gameExe = Path.Combine(rootPath, Bulid, "3D City Tycoon.exe");
        }

        private void CheckForUpdates()
        {
            if (System.IO.File.Exists(versionFile))
            {
                Version localVersion = new Version(System.IO.File.ReadAllText(versionFile));
                VersionText.Content = localVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(Version_URL));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        Progressbar.Visibility = (Visibility)0;
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Progressbar.Visibility = (Visibility)1;
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    CreateDebugFile(ex);
                    Status = LauncherStatus.failed;
                    MessageBox.Show("Error checking for game updates");
                }
            }
            else
            {
                Progressbar.Visibility = (Visibility)0;
                InstallGameFiles(false, Version.zero);
            }
        }

        private void AddToProgress(float amount)
        {
            Progressbar.Value += Progressbar.Maximum / amount;
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(Version_URL));
                }
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                AddToProgress(10);
                webClient.DownloadFileAsync(new Uri(Game_Download_URL), gameZip, _onlineVersion);
                AddToProgress(10);
            }
            catch (Exception ex)
            {
                CreateDebugFile(ex);
                Status = LauncherStatus.failed;
                MessageBox.Show("Error installing game files");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            int retryattempts = 0;
            int allowedretryattempts = 2;
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                AddToProgress(20);
                new WaitForChangedResult();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                AddToProgress(30);
                System.IO.File.Delete(gameZip);
                AddToProgress(10);
                System.IO.File.WriteAllText(versionFile, onlineVersion);
                VersionText.Content = onlineVersion;
                AddToProgress(100);
                Status = LauncherStatus.ready;
                Progressbar.Visibility = (Visibility)1;
            }
            catch (Exception ex)
            {
                retryattempts++;
                if (retryattempts >= allowedretryattempts)
                {
                    CreateDebugFile(ex);
                    Status = LauncherStatus.failed;
                    MessageBox.Show("Error finishing download");
                }
                else
                {
                    CheckForUpdates();
                }
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Status == LauncherStatus.downloadingGame || Status == LauncherStatus.downloadingUpdate)
            {
                PlayButton.Content = "Chill It's downloading";
            }
            else
            {
                if (System.IO.File.Exists(gameExe) && Status == LauncherStatus.ready)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(gameExe) { WorkingDirectory = Path.Combine(rootPath, Bulid) };
                    Process.Start(startInfo);
                    Close();
                }
                else if (Status == LauncherStatus.failed)
                {
                    CheckForUpdates();
                }
            }
        }

        private void CreateDebugFile(Exception ex = null /*, int i = 0*/)
        {
            string TempFileName = DateTime.Now.ToString("MM_dd_yyyy__HH-mm-ss");
            string content = DateTime.Now + ": " + ex;
            using StreamWriter outputFile = new StreamWriter(Directory.GetCurrentDirectory() + @"\" + ErrorLog + @"\" + TempFileName + ".txt");
            outputFile.WriteLine(content);
            outputFile.Close();
        }

        public static void MakeShortcut()
        {
            {
                IShellLink link = (IShellLink)new ShellLink();

                link.SetDescription("A Game launcher by Poshy");
                link.SetPath(Path.Combine(Directory.GetCurrentDirectory(), Filename));
                IPersistFile file = (IPersistFile)link;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                file.Save(Path.Combine(desktopPath, Shortcutname + ".lnk"), true);
            }
        }

        #region Shortcut

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);

            void GetIDList(out IntPtr ppidl);

            void SetIDList(IntPtr pidl);

            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            void GetHotkey(out short pwHotkey);

            void SetHotkey(short wHotkey);

            void GetShowCmd(out int piShowCmd);

            void SetShowCmd(int iShowCmd);

            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

            void Resolve(IntPtr hwnd, int fFlags);

            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        #endregion Shortcut

        private void MakeBaseFolders()
        {
            if (!File.Exists(ErrorLog))
            {
                System.IO.Directory.CreateDirectory(ErrorLog);
            }
        }
    }

    internal struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}