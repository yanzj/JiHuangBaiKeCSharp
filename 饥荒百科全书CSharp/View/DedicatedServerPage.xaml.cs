﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;
using Newtonsoft.Json;
using 饥荒百科全书CSharp.Class;
using 饥荒百科全书CSharp.Class.DedicatedServers.DedicateServer;
using 饥荒百科全书CSharp.Class.DedicatedServers.JsonDeserialize;
using 饥荒百科全书CSharp.Class.DedicatedServers.Tools;
using 饥荒百科全书CSharp.MyUserControl.DedicatedServer;

namespace 饥荒百科全书CSharp.View
{
    /// <summary>
    /// 枚举类型 Message
    /// </summary>
    public enum Message
    {
        保存,
        复活,
        回档,
        重置世界
    }

    /// <summary>
    /// DedicatedServerPage.xaml 的交互逻辑
    /// </summary>
    public partial class DedicatedServerPage : Page
    {
        #region 字段、属性
        private readonly UTF8Encoding _utf8NoBom = new UTF8Encoding(false); // 编码
        private Dictionary<string, string> _hanhua;  // 汉化

        private PathAll _pathAll; // 路径
        private BaseSet _baseSet; // 基本设置
        private Leveldataoverride _overWorld; // 地上世界
        private Leveldataoverride _caves;     // 地下世界
        private Mods _mods;  // mods
        
        private int _saveSlot; // 存档槽
        public int SaveSlot
        {
            get => _saveSlot;
            set
            {
                _saveSlot = value;
                _pathAll.SaveSlot = value;
            }
        }
        #endregion

        /// <summary>
        /// 构造事件
        /// </summary>
        public DedicatedServerPage()
        {
            InitializeComponent();
            #region 设置PathCommon类数据
            // 当前路径
            PathCommon.CurrentDirPath = Environment.CurrentDirectory;
            // 我的文档
            PathCommon.DocumentDirPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // 客户端服务器路径
            PathCommon.ClientFilePath = JsonHelper.ReadClientPath(RegeditRw.RegReadString("platform"));
            PathCommon.ServerFilePath = JsonHelper.ReadServerPath(RegeditRw.RegReadString("platform"));
            #endregion
            //初始化左侧选择存档RadioButton
            for (var i = 0; i < 20; i++)
            {
                // ReSharper disable once PossibleNullReferenceException
                ((RadioButton)DediLeftStackPanel.FindName($"SaveSlotRadioButton{i}")).Tag = i;
            }
            // 初始化服务器面板
            DedicatedServerPanelInitalize();
        }

        /// <summary>
        /// 初始化服务器面板
        /// </summary>
        private void DedicatedServerPanelInitalize()
        {
            string[] gameVersion = { "Steam", "WeGame"};
            DediSettingGameVersionSelect.ItemsSource = gameVersion;
            PathCommon.GamePlatform = JsonHelper.ReadGamePlatform();
            DediSettingGameVersionSelect.Text = PathCommon.GamePlatform;
            DediButtomPanelVisibilityInitialize();

            string[] noYes = { "否", "是" };
            string[] gamemode = { "生存", "荒野", "无尽" };
            var maxPlayer = new string[64];
            for (var i = 1; i <= 64; i++)
            {
                maxPlayer[i - 1] = i.ToString();
            }
            string[] offline = { "在线", "离线" };
            DediBaseSetGamemodeSelect.ItemsSource = gamemode;
            DediBaseSetPvpSelect.ItemsSource = noYes;
            DediBaseSetMaxPlayerSelect.ItemsSource = maxPlayer;
            DediBaseOfflineSelect.ItemsSource = offline;
            DediBaseIsPause.ItemsSource = noYes;
            DediBaseIsCave.ItemsSource = noYes;
            DediBaseSet.Visibility = Visibility.Visible;
        }

        #region "DedicatedServer"

        #region 面板菜单

        #region "面板菜单按钮"
        private void TitleMenuBaseSet_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("BaseSet");
        }

        private void TitleMenuEditWorld_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("EditWorld");
        }

        private void TitleMenuMod_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("Mod");
        }

        private void TitleMenuRollback_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("Rollback");
        }

        //TODO
        // ReSharper disable once UnusedMember.Local
        private void DediTitleBlacklist_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("Blacklist");
        }

        /// <summary>
        /// 打开客户端
        /// </summary>
        private void RunClient()
        {

            if (string.IsNullOrEmpty(_pathAll.ClientModsDirPath))
            {
                MessageBox.Show("客户端路径没有设置");
                return;
            }
            var process = new Process
            {
                StartInfo =
                {
                    Arguments = "",
                    WorkingDirectory = Path.GetDirectoryName(PathCommon.ClientFilePath) ?? throw new InvalidOperationException(),
                    FileName = PathCommon.ClientFilePath
                }
            };
            // 目录,这个必须设置
            process.Start();
        }

        /// <summary>
        /// 打开服务器
        /// </summary>
        private void RunServer()
        {
            if (PathCommon.ServerFilePath == null || PathCommon.ServerFilePath.Trim() == "")
            {
                MessageBox.Show("服务器路径不对,请重新设置服务器路径"); return;
            }
            // 保存世界
            if (_overWorld != null && _caves != null && _mods != null)
            {
                _overWorld.SaveWorld();
                _caves.SaveWorld();
                _mods.SaveListmodsToFile(_pathAll.YyServerDirPath + @"\Master\modoverrides.lua", _utf8NoBom);
                _mods.SaveListmodsToFile(_pathAll.YyServerDirPath + @"\Caves\modoverrides.lua", _utf8NoBom);
            }
            if (PathCommon.GamePlatform == "WeGame")
            {
                var ini1 = new IniHelper(_pathAll.YyServerDirPath + @"\cluster.ini", _utf8NoBom);
                //ini1.write("NETWORK", "offline_cluster", "false", utf8NoBom);
                ini1.Write("NETWORK", "lan_only_cluster", "false", _utf8NoBom);
            }
            if (PathCommon.GamePlatform == "Steam")
            {
                var ini1 = new IniHelper(_pathAll.YyServerDirPath + @"\cluster.ini", _utf8NoBom);
                //ini1.write("NETWORK", "offline_cluster", "false", utf8NoBom);
                ini1.Write("NETWORK", "lan_only_cluster", "false", _utf8NoBom);
            }
            // 打开服务器
            var p = new Process();
            if (PathCommon.GamePlatform != "WeGame")
            {
                p.StartInfo.UseShellExecute = false; // 是否
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(PathCommon.ServerFilePath) ?? throw new InvalidOperationException(); // 目录,这个必须设置
                p.StartInfo.FileName = PathCommon.ServerFilePath;  // 服务器名字

                p.StartInfo.Arguments = "-console -cluster Server_" + PathCommon.GamePlatform + "_" + SaveSlot.ToString() + " -shard Master";
                p.Start();
            }
            // 打开服务器
            if (PathCommon.GamePlatform == "WeGame")
            {
                MessageBox.Show("保存完毕! 请通过WeGame启动,存档文件名为" + PathCommon.GamePlatform + "_" + SaveSlot.ToString());
            }
            if (PathCommon.GamePlatform != "WeGame")
            {
                // 是否开启洞穴
                if (DediBaseIsCave.Text == "是")
                {
                    p.StartInfo.Arguments = "-console -cluster Server_" + PathCommon.GamePlatform + "_" + SaveSlot.ToString() + " -shard Caves";
                    p.Start();
                }
            }
        }
        #endregion

        #region "主面板Visibility属性设置"
        private void DediButtomPanelVisibilityInitialize()
        {
            foreach (UIElement vControl in ButtomGrid.Children)
            {
                vControl.Visibility = Visibility.Collapsed;
            }
            Global.UiElementVisibility(Visibility.Visible, DediButtomBorderH1, DediButtomBorderH2, DediButtomBorderV1, DediButtomBorderV4);
        }

        private void DediButtomPanelVisibility(string obj)
        {
            DediButtomPanelVisibilityInitialize();
            switch (obj)
            {
                case "Setting":
                    DediSetting.Visibility = Visibility.Visible;
                    TitleMenuBaseSet.IsChecked = false;
                    TitleMenuEditWorld.IsChecked = false;
                    TitleMenuMod.IsChecked = false;
                    TitleMenuRollback.IsChecked = false;
                    break;
                case "BaseSet":
                    DediBaseSet.Visibility = Visibility.Visible;
                    break;
                case "EditWorld":
                    DediWorldSet.Visibility = Visibility.Visible;
                    break;
                case "Mod":
                    DediModSet.Visibility = Visibility.Visible;
                    break;
                case "Rollback":
                    DediModRollBack.Visibility = Visibility.Visible;
                    break;
                case "Blacklist":
                    DediModManager.Visibility = Visibility.Visible;
                    break;
            }
        }
        #endregion

        #endregion

        #region "通用设置面板"

        /// <summary>
        /// 游戏平台改变,初始化一切
        /// </summary>
        private void DediSettingGameVersionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 赋值
            PathCommon.GamePlatform = e.AddedItems[0].ToString();
            if (PathCommon.GamePlatform == "WeGame")
            {
                CtrateRunGame.Visibility = Visibility.Collapsed;
                CtrateWorldButton.Content = "保存世界";
            }
            else
            {
                CtrateRunGame.Visibility = Visibility.Visible;
                CtrateWorldButton.Content = "创建世界";
            }
            if (e.RemovedItems.Count != 0)
            {
                // 初始化
                InitServer();
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void InitServer()
        {
            //-1.游戏平台
            PathCommon.GamePlatform = JsonHelper.ReadGamePlatform();
            DediSettingGameVersionSelect.Text = PathCommon.GamePlatform;
            Debug.WriteLine("游戏平台-完");
            // 0.路径信息
            SetPath();
            // 1.检查存档Server是否存在 
            CheckServer();
            // 2.汉化
            _hanhua = JsonHelper.ReadHanhua();
            // 3.读取服务器mods文件夹下所有信息.mod多的话,读取时间也多
            //   此时的mod没有被current覆盖
            _mods = null;
            if (!string.IsNullOrEmpty(_pathAll.ServerModsDirPath))
            {
                _mods = new Mods(_pathAll.ServerModsDirPath);
            }
            // 4. "控制台"
            CreateConsoleButton();
            // 5.clusterToken
            this.DediSettingClusterTokenTextBox.Text = RegeditRw.RegReadString("ClusterToken");
            // 3."基本设置" 等在 点击radioButton后设置
        }

        /// <summary>
        /// 设置"路径"
        /// </summary>
        private void SetPath()
        {
            _pathAll = new PathAll(SaveSlot);
            GameDirSelectTextBox.Text = "";
            if (!string.IsNullOrEmpty(PathCommon.ClientFilePath) && File.Exists(PathCommon.ClientFilePath))
            {
                GameDirSelectTextBox.Text = PathCommon.ClientFilePath;
            }
            else
            {
                PathCommon.ClientFilePath = "";

            }
            DediDirSelectTextBox.Text = "";
            if (!string.IsNullOrEmpty(PathCommon.ServerFilePath) && File.Exists(PathCommon.ServerFilePath))
            {
                DediDirSelectTextBox.Text = PathCommon.ServerFilePath;
            }
            else
            {
                PathCommon.ServerFilePath = "";

            }
            Debug.WriteLine("路径读取-完");
        }

        /// <summary>
        /// "检查"
        /// </summary>
        private void CheckServer()
        {
            if (!Directory.Exists(_pathAll.DoNotStarveTogetherDirPath))
            {
                Directory.CreateDirectory(_pathAll.DoNotStarveTogetherDirPath);
            }
            var directoryInfo = new DirectoryInfo(_pathAll.DoNotStarveTogetherDirPath);
            var directoryInfos = directoryInfo.GetDirectories();
            var serverWeGamePathList = new List<string>();
            foreach (var t in directoryInfos)
            {
                if (t.Name.StartsWith("Server_" + PathCommon.GamePlatform + "_"))
                {
                    serverWeGamePathList.Add(t.FullName);
                }
            }
            // 清空左边
            for (var i = 0; i < 20; i++)
            {
                // ReSharper disable once PossibleNullReferenceException
                ((RadioButton)DediLeftStackPanel.FindName($"SaveSlotRadioButton{i}")).Content = "创建世界";
            }
            // 等于0
            if (serverWeGamePathList.Count == 0)
            {
                // 复制一份过去                  
                //Tool.CopyDirectory(pathAll.ServerMoBanPath, pathAll.DoNotStarveTogether_DirPath);
                CopyServerModel(_pathAll.DoNotStarveTogetherDirPath);

                // 改名字
                if (!Directory.Exists(_pathAll.DoNotStarveTogetherDirPath + "\\Server_" + PathCommon.GamePlatform + "_0"))
                {
                    Directory.Move(_pathAll.DoNotStarveTogetherDirPath + "\\Server", _pathAll.DoNotStarveTogetherDirPath + "\\Server_" + PathCommon.GamePlatform + "_0");
                }
            }
            else
            {
                foreach (var str in serverWeGamePathList)
                {
                    // 取出序号 
                    var num = str.Substring(str.LastIndexOf('_') + 1);
                    // 取出存档名称
                    // ReSharper disable once PossibleNullReferenceException
                    ((RadioButton)DediLeftStackPanel.FindName("SaveSlotRadioButton" + num)).Content = GetHouseName(int.Parse(num));
                }
            }
            // 禁用
            JinYong(false);
            DediSettingGameVersionSelect.IsEnabled = true;
            // 不选择任何一项
            // ReSharper disable once PossibleNullReferenceException
            ((RadioButton)DediLeftStackPanel.FindName("SaveSlotRadioButton" + SaveSlot)).IsChecked = false;
            //// 选择第0个存档
            //((RadioButton)DediLeftStackPanel.FindName("SaveSlotRadioButton0")).IsChecked = true;
            //SaveSlot = 0;
        }

        /// <summary>
        /// 左侧radioButton Click事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveSlotRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            //汉化
            _hanhua = JsonHelper.ReadHanhua();
            //存档
            var saveSlot = int.Parse(((RadioButton)sender).Name.Remove(0, 19));
            if (_pathAll == null)
                _pathAll = new PathAll(SaveSlot);
            // 0.保存之前的
            if (_overWorld != null & _caves != null & _mods != null & Directory.Exists(_pathAll.YyServerDirPath))
            {
                _overWorld.SaveWorld();
                _caves.SaveWorld();

                _mods.SaveListmodsToFile(_pathAll.YyServerDirPath + @"\Master\modoverrides.lua", _utf8NoBom);
                _mods.SaveListmodsToFile(_pathAll.YyServerDirPath + @"\Caves\modoverrides.lua", _utf8NoBom);
            }
            // 1.存档槽
            SaveSlot = saveSlot;
            // 1.5 创建世界
            if (((RadioButton)sender).Content.ToString() == "创建世界")
            {
                // 复制一份过去                  
                //Tool.CopyDirectory(pathAll.ServerMoBanPath, pathAll.DoNotStarveTogether_DirPath);
                CopyServerModel(_pathAll.DoNotStarveTogetherDirPath);
                // 改名字
                if (!Directory.Exists(_pathAll.DoNotStarveTogetherDirPath + "\\Server_" + PathCommon.GamePlatform + "_" + SaveSlot))
                {
                    Directory.Move(_pathAll.DoNotStarveTogetherDirPath + "\\Server", _pathAll.DoNotStarveTogetherDirPath + "\\Server_" + PathCommon.GamePlatform + "_" + SaveSlot);

                }
                ((RadioButton)sender).Content = GetHouseName(SaveSlot);

            }
            // 1.6 复活
            JinYong(true);
            // 2.【基本设置】
            SetBaseSet();
            // 3. "世界设置"
            SetOverWorldSet();
            // 3. "世界设置"
            SetCavesSet();
            // 4. "Mod"
            SetModSet();
        }

        /// <summary>
        /// 复制Server模板到指定位置
        /// </summary>
        /// <param name="path">指定路径</param>
        private void CopyServerModel(string path)
        {
            // 判断是否存在
            if (Directory.Exists(path + @"\Server"))
            {
                Directory.Delete(path + @"\Server", true);
            }
            // 建立文件夹
            Directory.CreateDirectory(path + @"\Server");
            Directory.CreateDirectory(path + @"\Server\Caves");
            Directory.CreateDirectory(path + @"\Server\Master");

            // 填文件
            File.WriteAllText(path + @"\Server\cluster.ini", Tool.ReadResources("Server模板.cluster.ini"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Caves\leveldataoverride.lua", Tool.ReadResources("Server模板.Caves.leveldataoverride.lua"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Caves\modoverrides.lua", Tool.ReadResources("Server模板.Caves.modoverrides.lua"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Caves\server.ini", Tool.ReadResources("Server模板.Caves.server.ini"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Master\leveldataoverride.lua", Tool.ReadResources("Server模板.Master.leveldataoverride.lua"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Master\modoverrides.lua", Tool.ReadResources("Server模板.Master.modoverrides.lua"), _utf8NoBom);
            File.WriteAllText(path + @"\Server\Master\server.ini", Tool.ReadResources("Server模板.Master.server.ini"), _utf8NoBom);

            // clusterToken
            var flag2 = !string.IsNullOrEmpty(RegeditRw.RegReadString("ClusterToken"));
            File.WriteAllText(path + "\\Server\\cluster_token.txt", flag2 ? RegeditRw.RegReadString("ClusterToken") : "",
                _utf8NoBom);
        }

        /// <summary>
        /// 选择游戏exe文件
        /// </summary>
        private void DediSettingGameDirSelect_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择游戏exe文件",
                FileName = PathCommon.GamePlatform == "WeGame"
                    ? "dontstarve_rail"
                    : "dontstarve_steam", //默认文件名
                DefaultExt = ".exe",// 默认文件扩展名
                Filter = PathCommon.GamePlatform == "WeGame"
                    ? "饥荒游戏exe文件(*.exe)|dontstarve_rail.exe"
                    : "饥荒游戏exe文件(*.exe)|dontstarve_steam.exe",
                FilterIndex = 1,
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("dontstarve_"))
                {
                    MessageBox.Show("文件选择错误,请选择正确文件");
                    return;
                }
                PathCommon.ClientFilePath = fileName;
                GameDirSelectTextBox.Text = fileName;
                JsonHelper.WriteClientPath(fileName, PathCommon.GamePlatform);

            }
        }

        /// <summary>
        /// 选择服务器文件
        /// </summary>
        private void DediSettingDediDirSelect_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择服务器exe文件",
                FileName = "dontstarve_dedicated_server_nullrenderer", //默认文件名
                DefaultExt = ".exe",// 默认文件扩展名
                Filter = "饥荒服务器exe文件(*.exe)|dontstarve_dedicated_server_nullrenderer.exe",
                FilterIndex = 1,
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("dontstarve_dedicated_server_nullrenderer"))
                {
                    MessageBox.Show("文件选择错误,请选择正确文件");
                    return;
                }
                PathCommon.ServerFilePath = fileName;
                DediDirSelectTextBox.Text = fileName;
                JsonHelper.WriteServerPath(fileName, PathCommon.GamePlatform);
                // 读取mods
                _mods = null;
                if (!string.IsNullOrEmpty(_pathAll.ServerModsDirPath))
                {
                    _mods = new Mods(_pathAll.ServerModsDirPath);
                }
                SetModSet();
            }
        }

        /// <summary>
        /// 双击打开所在文件夹"客户端"
        /// </summary>
        private void DediSettingGameDirSelectTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(PathCommon.ClientFilePath) && File.Exists(PathCommon.ClientFilePath))
            {
                Process.Start(Path.GetDirectoryName(PathCommon.ClientFilePath) ?? throw new InvalidOperationException());
            }
        }

        /// <summary>
        /// 双击打开所在文件夹"服务端"
        /// </summary>
        private void DediSettingDediDirSelectTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(PathCommon.ServerFilePath) && File.Exists(PathCommon.ServerFilePath))
            {
                Process.Start(Path.GetDirectoryName(PathCommon.ServerFilePath) ?? throw new InvalidOperationException());
            }
        }

        /// <summary>
        /// 通用设置
        /// </summary>
        private void CommonSettingButton_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibility("Setting");
        }

        #endregion

        #region "游戏风格"
        private void DediIntention_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibilityInitialize();
            DediBaseSet.Visibility = Visibility.Visible;
            switch (((Button)sender).Name)
            {
                case "IntentionSocialButton":
                    DediBaseSetIntentionButton.Content = "交际";
                    break;
                case "IntentionCooperativeButton":
                    DediBaseSetIntentionButton.Content = "合作";
                    break;
                case "IntentionCompetitiveButton":
                    DediBaseSetIntentionButton.Content = "竞争";
                    break;
                case "IntentionMadnessButton":
                    DediBaseSetIntentionButton.Content = "疯狂";
                    break;
            }
        }

        private void DediIntention_MouseEnter(object sender, MouseEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "IntentionSocialButton":
                    DidiIntentionTextBlock.Text = "这是一个闲聊&扯蛋的地方。\r\n轻松的游戏风格，只是为了互相沟通&扯蛋。\r\n还等什么，快进来一起扯蛋吧~~";
                    break;
                case "IntentionCooperativeButton":
                    DidiIntentionTextBlock.Text = "一个团队生存的世界。在这个世界，我们要一起合作，尽我们可能来驯服这个充满敌意的世界。";
                    break;
                case "IntentionCompetitiveButton":
                    DidiIntentionTextBlock.Text = "这是一个完美的舞台。\r\n展示你的生存能力，战斗能力、建设能力...吧！";
                    break;
                case "IntentionMadnessButton":
                    DidiIntentionTextBlock.Text = "在这里，你将过着茹毛饮血的生活！\r\n是你吃掉粮食还是被粮食吃掉呢？\r\n让我们拭目以待吧！";
                    break;
            }
        }
        
        private void DediIntention_MouseLeave(object sender, MouseEventArgs e)
        {
            DidiIntentionTextBlock.Text = "";
        }
        #endregion

        #region "基本设置面板"
        /// <summary>
        /// 修改房间名时顶部显示房间名和左侧显示房间名同步修改
        /// </summary>
        private void DediBaseSetHouseName_TextChanged(object sender, TextChangedEventArgs e)
        {
            DediMainTopWorldName.Text = DediBaseSetHouseName.Text;
            if (((RadioButton)DediLeftStackPanel.FindName("SaveSlotRadioButton" + SaveSlot))?.IsChecked == true)
            {
                // ReSharper disable once PossibleNullReferenceException
                ((RadioButton)DediLeftStackPanel.FindName($"SaveSlotRadioButton{SaveSlot}")).Content = DediBaseSetHouseName.Text;
            }
        }

        /// <summary>
        /// 选择游戏风格
        /// </summary>
        private void DediBaseSetIntentionButton_Click(object sender, RoutedEventArgs e)
        {
            DediButtomPanelVisibilityInitialize();
            DediIntention.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 删除当前存档按钮
        /// </summary>
        private void DediMainTop_Delete_Click(object sender, RoutedEventArgs e)
        {
            // 0. 关闭服务器
            var processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                if (item.ProcessName == "dontstarve_dedicated_server_nullrenderer")
                {
                    item.Kill();
                }
            }
            // 1. radioBox 写 创建世界
            // ReSharper disable once PossibleNullReferenceException
            ((RadioButton)DediLeftStackPanel.FindName($"SaveSlotRadioButton{SaveSlot}")).Content = "创建世界";
            // 2. 删除当前存档
            if (Directory.Exists(_pathAll.YyServerDirPath))
            {
                Directory.Delete(_pathAll.YyServerDirPath, true);
            }
           // 2.1 取消选择,谁都不选
            // ReSharper disable once PossibleNullReferenceException
           ((RadioButton)DediLeftStackPanel.FindName($"SaveSlotRadioButton{SaveSlot}")).IsChecked = false;
            // 2.2 
            // DediMainBorder.IsEnabled = false;
            JinYong(false);
            //// 3. 复制一份新的过来                 
            //ServerTools.Tool.CopyDirectory(pathAll.ServerMoBanPath, pathAll.DoNotStarveTogether_DirPath);
            //if (!Directory.Exists(pathAll.DoNotStarveTogether_DirPath + "\\Server_" + PathCommon.GamePlatform + "_" + SaveSlot))
            //{
            //    Directory.Move(pathAll.DoNotStarveTogether_DirPath + "\\Server", pathAll.DoNotStarveTogether_DirPath + "\\Server_" + PathCommon.GamePlatform + "_" + SaveSlot);
            //}
            //// 4. 读取新的存档
            //SetBaseSet();
        }

        /// <summary>
        /// 打开游戏
        /// </summary>
        private void OpenGameButton_Click(object sender, RoutedEventArgs e)
        {
            RunClient();
        }

        /// <summary>
        /// 创建世界按钮
        /// </summary>
        private void CtrateWorldButton_Click(object sender, RoutedEventArgs e)
        {
            RunServer();
        }

        private void DediSettingSaveCluster_Click(object sender, RoutedEventArgs e)
        {
            var flag = string.IsNullOrEmpty(DediSettingClusterTokenTextBox.Text);
            if (flag)
            {
                MessageBox.Show("cluster没填写，不能保存");
            }
            else
            {
                var flag2 = DediSettingClusterTokenTextBox.Text.Trim() == "";
                if (flag2)
                {
                    MessageBox.Show("cluster没填写，不能保存");
                }
                else
                {
                    RegeditRw.RegWrite("ClusterToken", DediSettingClusterTokenTextBox.Text.Trim());
                    MessageBox.Show("保存完毕！");
                }
            }
        }

        #endregion
        #endregion


        /// <summary>
        /// 设置 "Mod"
        /// </summary>
        private void SetModSet()
        {   // 设置
            if (!string.IsNullOrEmpty(_pathAll.ServerModsDirPath))
            {
                // 清空,Enabled变成默认值
                foreach (var item in _mods.ListMod)
                {
                    item.Enabled = false;
                }
                // 细节也要变成默认值,之后再重新读取1
                foreach (var item in _mods.ListMod)
                {
                    foreach (var item1 in item.ConfigurationOptions)
                    {
                        item1.Value.Current = item1.Value.Default1;
                    }
                }
                // 重新读取
                _mods.ReadModsOverrides(_pathAll.ServerModsDirPath, _pathAll.YyServerDirPath + @"\Master\modoverrides.lua");
            }
            // 显示 
            DediModList.Children.Clear();
            DediModXiJie.Children.Clear();
            DediModDescription.Text = "";
            if (_mods != null)
            {
                for (var i = 0; i < _mods.ListMod.Count; i++)
                {
                    // 屏蔽 客户端MOD
                    if (_mods.ListMod[i].ModType == ModType.客户端)
                    {
                        continue;
                    }
                    var dod = new DediModBox
                    {
                        Width = 200,
                        Height = 70,
                        UCTitle = {Content = _mods.ListMod[i].Name},
                        UCCheckBox = {Tag = i},
                        UCConfig =
                        {
                            Source = _mods.ListMod[i].ConfigurationOptions.Count != 0
                                ? new BitmapImage(new Uri(
                                    "/饥荒百科全书CSharp;component/Resources/DedicatedServer/D_mp_mod_config.png",
                                    UriKind.Relative))
                                : null
                        }
                    };
                    dod.UCCheckBox.IsChecked = _mods.ListMod[i].Enabled;
                    dod.UCCheckBox.Checked += CheckBox_Checked;
                    dod.UCCheckBox.Unchecked += CheckBox_Unchecked;
                    dod.PreviewMouseLeftButtonDown += Dod_MouseLeftButtonDown;
                    dod.UCEnableLabel.Content = _mods.ListMod[i].ModType;
                    DediModList.Children.Add(dod);
                }
            }
        }

        /// <summary>
        /// 设置 "Mod" "MouseLeftButtonDown"
        /// </summary>
        private void Dod_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 左边显示
            var n = (int)(((DediModBox)sender).UCCheckBox.Tag);
            var author = "作者:\r\n" + _mods.ListMod[n].Author + "\r\n\r\n";
            var description = "描述:\r\n" + _mods.ListMod[n].Description + "\r\n\r\n";
            var strName = "Mod名字:\r\n" + _mods.ListMod[n].Name + "\r\n\r\n";
            var version = "版本:\r\n" + _mods.ListMod[n].Version + "\r\n\r\n";
            var fileName = "文件夹:\r\n" + _mods.ListMod[n].DirName + "\r\n\r\n";
            DediModDescription.FontSize = 12;
            DediModDescription.TextWrapping = TextWrapping.WrapWithOverflow;
            DediModDescription.Text = strName + author + description + version + fileName;
            if (_mods.ListMod[n].ConfigurationOptions.Count == 0)
            {
                // 没有细节配置项
                Debug.WriteLine(n);
                DediModXiJie.Children.Clear();
                var labelModXiJie = new Label
                {
                    Height = 300,
                    Width = 300,
                    Content = "QQ群: 580332268 \r\n mod类型:\r\n 所有人: 所有人都必须有.\r\n 服务器:只要服务器有就行",
                    FontWeight = FontWeights.Bold,
                    FontSize = 20
                };
                DediModXiJie.Children.Add(labelModXiJie);
            }
            else
            {
                // 有,显示细节配置项
                Debug.WriteLine(n);
                DediModXiJie.Children.Clear();
                foreach (var item in _mods.ListMod[n].ConfigurationOptions)
                {
                    // stackPanel
                    var stackPanel = new StackPanel
                    {
                        Height = 40,
                        Width = 330,
                        Orientation = Orientation.Horizontal
                    };
                    var labelModXiJie = new Label
                    {
                        Height = stackPanel.Height,
                        Width = 180,
                        FontWeight = FontWeights.Bold,
                        Content = string.IsNullOrEmpty(item.Value.Label) ? item.Value.Name : item.Value.Label
                    };
                    // dediComboBox
                    var dod = new DediComboBox
                    {
                        Height = stackPanel.Height,
                        Width = 150,
                        FontSize = 12,
                        Tag = n + "$" + item.Key
                    };
                    // 把当前选择mod的第n个,放到tag里
                    foreach (var item1 in item.Value.Options)
                    {
                        dod.Items.Add(item1.Description);
                    }
                    dod.SelectedValue = item.Value.CurrentDescription;
                    dod.SelectionChanged += Dod_SelectionChanged;
                    // 添加
                    stackPanel.Children.Add(labelModXiJie);
                    stackPanel.Children.Add(dod);
                    DediModXiJie.Children.Add(stackPanel);
                }
            }
        }

        /// <summary>
        /// 设置 "Mod" "SelectionChanged"
        /// </summary>
        private void Dod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine(((DediComboBox)sender).Tag);
            var str = ((DediComboBox)sender).Tag.ToString().Split('$');
            if (str.Length != 0)
            {
                var n = int.Parse(str[0]);
                var name = str[1];
                // 好复杂
                _mods.ListMod[n].ConfigurationOptions[name].Current =
                    _mods.ListMod[n].ConfigurationOptions[name].Options[((DediComboBox)sender).SelectedIndex].Data;

            }
        }

        /// <summary>
        /// 设置 "Mod" "CheckBox_Unchecked"
        /// </summary>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _mods.ListMod[(int)(((CheckBox)sender).Tag)].Enabled = false;
            //Debug.WriteLine(((CheckBox)sender).Tag.ToString());
        }

        /// <summary>
        /// 设置 "Mod" "CheckBox_Checked"
        /// </summary>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _mods.ListMod[(int)((CheckBox)sender).Tag].Enabled = true;
            //Debug.WriteLine(((CheckBox)sender).Tag.ToString());
        }

        /// <summary>
        /// 设置"基本"
        /// </summary>
        private void SetBaseSet()
        {
            var clusterIniFilePath = _pathAll.YyServerDirPath + @"\cluster.ini";
            if (!File.Exists(clusterIniFilePath))
            {
                //MessageBox.Show("cluster.ini不存在");
                return;
            }
            _baseSet = new BaseSet(clusterIniFilePath);

            DediBaseSetGamemodeSelect.DataContext = _baseSet;
            DediBaseSetPvpSelect.DataContext = _baseSet;
            DediBaseSetMaxPlayerSelect.DataContext = _baseSet;
            DediBaseOfflineSelect.DataContext = _baseSet;
            DediBaseSetHouseName.DataContext = _baseSet;
            DediBaseSetDescribe.DataContext = _baseSet;
            DediBaseSetSecret.DataContext = _baseSet;
            DediBaseOfflineSelect.DataContext = _baseSet;
            DediBaseIsPause.DataContext = _baseSet;
            DediBaseSetIntentionButton.DataContext = _baseSet;
            DediBaseIsCave.DataContext = _baseSet;
            Debug.WriteLine("基本设置-完");
        }

        /// <summary>
        /// 设置"地上世界"
        /// </summary>
        private void SetOverWorldSet()
        {
            // 地上 
            _overWorld = new Leveldataoverride(_pathAll, false);
            DediOverWorldWorld.Children.Clear();
            DediOverWolrdFoods.Children.Clear();
            DediOverWorldAnimals.Children.Clear();
            DediOverWorldMonsters.Children.Clear();
            DediOverWorldResources.Children.Clear();
            // 地上 分类

            var overWorldFenLei = JsonHelper.ReadWorldFenLei(false);

            var foods = new Dictionary<string, ShowWorld>();
            var animals = new Dictionary<string, ShowWorld>();
            var monsters = new Dictionary<string, ShowWorld>();
            var resources = new Dictionary<string, ShowWorld>();
            var world = new Dictionary<string, ShowWorld>();

            #region 地上分类方法
            foreach (var item in _overWorld.ShowWorldDic)
            {
                if (overWorldFenLei.ContainsKey(item.Key))
                {
                    switch (overWorldFenLei[item.Key])
                    {
                        case "foods":
                            foods[item.Key] = item.Value;
                            break;
                        case "animals":
                            animals[item.Key] = item.Value;
                            break;
                        case "monsters":
                            monsters[item.Key] = item.Value;
                            break;
                        case "resources":
                            resources[item.Key] = item.Value;
                            break;
                        case "world":
                            world[item.Key] = item.Value;
                            break;
                    }
                }
                else
                {
                    world[item.Key] = item.Value;
                }

            }
            #endregion

            #region "显示" 地上
            //  
            foreach (var item in world)
            {
                if (item.Value.ToolTip == "roads" || item.Value.ToolTip == "layout_mode" || item.Value.ToolTip == "wormhole_prefab")
                {
                    continue;
                }

                var di = new DediComboBoxWithImage()
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60

                };
                di.SelectionChanged += DiOverWorld_SelectionChanged;
                DediOverWorldWorld.Children.Add(di);

            }
            foreach (var item in foods)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiOverWorld_SelectionChanged;
                DediOverWolrdFoods.Children.Add(di);

            }
            foreach (var item in animals)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiOverWorld_SelectionChanged;
                DediOverWorldAnimals.Children.Add(di);

            }
            foreach (var item in monsters)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiOverWorld_SelectionChanged;
                DediOverWorldMonsters.Children.Add(di);

            }
            foreach (var item in resources)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiOverWorld_SelectionChanged;
                DediOverWorldResources.Children.Add(di);

            }
            #endregion

        }

        /// <summary>
        /// 设置"地上世界"
        /// </summary>
        private void DiOverWorld_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //// 测试 用
            var dedi = (DediComboBoxWithImage)sender;
            //List<string> s = new List<string>();
            //s.Add("tag:" + Dedi.Tag.ToString());
            //s.Add("e.source:" + e.Source.ToString());
            //s.Add(e.AddedItems.Count.ToString());
            //s.Add(e.RemovedItems.Count.ToString());
            //s.Add(Dedi.SelectedIndex.ToString());
            //foreach (var item in s)
            //{
            //    Debug.WriteLine(item);
            //}

            // 此时说明修改
            if (e.RemovedItems.Count != 0 && e.AddedItems[0].ToString() == HanHua(_overWorld.ShowWorldDic[dedi.Tag.ToString()].WorldconfigList[dedi.SelectedIndex]))
            {
                _overWorld.ShowWorldDic[dedi.Tag.ToString()].Worldconfig = _overWorld.ShowWorldDic[dedi.Tag.ToString()].WorldconfigList[dedi.SelectedIndex];
                Debug.WriteLine(dedi.Tag + "选项变为:" + _overWorld.ShowWorldDic[dedi.Tag.ToString()].Worldconfig);

                // 保存,这样保存有点卡,换为每次点击radioButton或创建世界时
                //OverWorld.SaveWorld();
                //Debug.WriteLine("保存地上世界");
            }
        }

        /// <summary>
        /// 设置"地下世界"
        /// </summary>
        private void SetCavesSet()
        {
            // 地下
            _caves = new Leveldataoverride(_pathAll, true);
            DediCavesWorld.Children.Clear();
            DediCavesFoods.Children.Clear();
            DediCavesAnimals.Children.Clear();
            DediCavesMonsters.Children.Clear();
            DediCavesResources.Children.Clear();
            // 地下 分类

            var fenleil = JsonHelper.ReadWorldFenLei(true);

            var foods = new Dictionary<string, ShowWorld>();
            var animals = new Dictionary<string, ShowWorld>();
            var monsters = new Dictionary<string, ShowWorld>();
            var resources = new Dictionary<string, ShowWorld>();
            var world = new Dictionary<string, ShowWorld>();


            #region  地下分类方法
            foreach (var item in _caves.ShowWorldDic)
            {
                if (fenleil.ContainsKey(item.Key))
                {
                    switch (fenleil[item.Key])
                    {
                        case "foods":
                            foods[item.Key] = item.Value;
                            break;
                        case "animals":
                            animals[item.Key] = item.Value;
                            break;
                        case "monsters":
                            monsters[item.Key] = item.Value;
                            break;
                        case "resources":
                            resources[item.Key] = item.Value;
                            break;
                        case "world":
                            world[item.Key] = item.Value;
                            break;
                    }
                }
                else
                {
                    world[item.Key] = item.Value;
                }

            }

            #endregion

            #region "显示" 地下
            // animals
            foreach (var item in world)
            {
                if (item.Value.ToolTip == "roads" || item.Value.ToolTip == "layout_mode" || item.Value.ToolTip == "wormhole_prefab")
                {
                    continue;
                }

                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiCaves_SelectionChanged;
                DediCavesWorld.Children.Add(di);

            }
            foreach (var item in foods)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiCaves_SelectionChanged;
                DediCavesFoods.Children.Add(di);

            }
            foreach (var item in animals)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiCaves_SelectionChanged;
                DediCavesAnimals.Children.Add(di);

            }
            foreach (var item in monsters)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiCaves_SelectionChanged;
                DediCavesMonsters.Children.Add(di);

            }
            foreach (var item in resources)
            {
                var di = new DediComboBoxWithImage
                {
                    ImageSource = new BitmapImage(new Uri("/" + item.Value.PicPath, UriKind.Relative)),
                    ItemsSource = HanHua(item.Value.WorldconfigList),
                    SelectedValue = HanHua(item.Value.Worldconfig),
                    ImageToolTip = HanHua(item.Value.ToolTip),
                    Tag = item.Key,
                    Width = 200,
                    Height = 60
                };
                di.SelectionChanged += DiCaves_SelectionChanged;
                DediCavesResources.Children.Add(di);

            }
            #endregion


        }

        /// <summary>
        /// 设置"地下世界"
        /// </summary>
        private void DiCaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //// 测试 用
            var dedi = (DediComboBoxWithImage)sender;

            // 此时说明修改
            if (e.RemovedItems.Count != 0 && e.AddedItems[0].ToString() == HanHua(_caves.ShowWorldDic[dedi.Tag.ToString()].WorldconfigList[dedi.SelectedIndex]))
            {
                _caves.ShowWorldDic[dedi.Tag.ToString()].Worldconfig = _caves.ShowWorldDic[dedi.Tag.ToString()].WorldconfigList[dedi.SelectedIndex];
                Debug.WriteLine(dedi.Tag + "选项变为:" + _caves.ShowWorldDic[dedi.Tag.ToString()].Worldconfig);

                // 保存,这样保存有点卡,换为每次点击radioButton或创建世界时
                //Caves.SaveWorld();
                //Debug.WriteLine("保存地上世界");
            }
        }

        #region 汉化

        private string HanHua(string s)
        {
            return _hanhua.ContainsKey(s) ? _hanhua[s] : s;
        }

        private IEnumerable<string> HanHua(IEnumerable<string> s)
        {
            var r = new List<string>();
            foreach (var item in s)
            {
                r.Add(_hanhua.ContainsKey(item) ? _hanhua[item] : item);
            }
            return r;
        }

        #endregion

        #region 控制台
        /// <summary>
        /// 发送“消息”
        /// </summary>
        /// <param name="messageStr">消息字符串</param>
        private static void SsendMessage(string messageStr)
        {
            var mySendMessage = new MySendMessage();
            // 得到句柄
            var pstr = Process.GetProcessesByName("dontstarve_dedicated_server_nullrenderer");
            // 根据句柄,发送消息
            foreach (var t in pstr)
            {
                mySendMessage.InputStr(t.MainWindowHandle, messageStr);
                mySendMessage.SendEnter(t.MainWindowHandle);
            }
        }

        /// <summary>
        /// 根据分类生产RadioButton
        /// </summary>
        private void CreateConsoleButton()
        {
            DediConsoleFenLei.Children.Clear();
            // otherRadioButton
            var otherRadioButton = new RadioButton
            {
                Content = "其他",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            otherRadioButton.Checked += B_Click;
            otherRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(otherRadioButton);
            // foodRadioButton
            var foodRadioButton = new RadioButton
            {
                Content = "食物",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            foodRadioButton.Checked += B_Click;
            foodRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(foodRadioButton);
            // resourcesRadioButton
            var resourcesRadioButton = new RadioButton
            {
                Content = "资源",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            resourcesRadioButton.Checked += B_Click;
            resourcesRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(resourcesRadioButton);
            // toolsRadioButton
            var toolsRadioButton = new RadioButton
            {
                Content = "工具",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            toolsRadioButton.Checked += B_Click;
            toolsRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(toolsRadioButton);
            // weaponsRadioButton
            var weaponsRadioButton = new RadioButton
            {
                Content = "武器",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            weaponsRadioButton.Checked += B_Click;
            weaponsRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(weaponsRadioButton);
            // giftsRadioButton
            var giftsRadioButton = new RadioButton
            {
                Content = "礼物",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            giftsRadioButton.Checked += B_Click;
            giftsRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(giftsRadioButton);
            // clothesRadioButton
            var clothesRadioButton = new RadioButton
            {
                Content = "衣物",
                Width = 140,
                Height = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("RadioButtonStyle")
            };
            clothesRadioButton.Checked += B_Click;
            clothesRadioButton.IsChecked = true;
            DediConsoleFenLei.Children.Add(clothesRadioButton);
        }

        /// <summary>
        /// 显示具体分类信息
        /// </summary>
        private void B_Click(object sender, RoutedEventArgs e)
        {
            // 读取分类信息
            var itemList = JsonConvert.DeserializeObject<ItemListRootObject>(StringProcess.GetJsonStringDedicatedServer("ItemList.json"));
            // 把当前选择的值放到这里了
            DediConsoleFenLei.Tag = ((RadioButton)sender).Content;
            // 显示具体分类信息
            switch (DediConsoleFenLei.Tag)
            {
                case "其他":
                    foreach (var detail in itemList.Items.Other.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "食物":
                    foreach (var detail in itemList.Items.Food.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "资源":
                    foreach (var detail in itemList.Items.Resources.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "工具":
                    foreach (var detail in itemList.Items.Tools.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "武器":
                    foreach (var detail in itemList.Items.Weapons.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "礼物":
                    foreach (var detail in itemList.Items.Gifts.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
                case "衣物":
                    foreach (var detail in itemList.Items.Clothes.Details)
                    {
                        if (string.IsNullOrEmpty(detail.Chinese))
                        {
                            continue;
                        }
                        CreateBxButton(detail);
                    }
                    break;
            }
        }

        /// <summary>
        /// 创建Bx按钮
        /// </summary>
        /// <param name="detail"></param>
        private void CreateBxButton(Detail3 detail)
        {
            DediConsoleDetails.Children.Clear();
            var codeString = detail.Code;
            var chineseString = detail.Chinese;
            // 按钮
            var button = new Button
            {
                Content = chineseString,
                Width = 115,
                Height = 35,
                Tag = codeString,
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("DediButtonCreateWorldStyle")
            };
            button.Click += Bx_Click;
            DediConsoleDetails.Children.Add(button);
        }

        /// <summary>
        /// Bx按钮Click事件
        /// </summary>
        private void Bx_Click(object sender, RoutedEventArgs e)
        {
            var code = ((Button)sender).Tag.ToString();
            // 如果是其他分类,则直接运行code
            if (DediConsoleFenLei.Tag.ToString() == "其他")
            {
                SsendMessage(code);
                System.Windows.Forms.Clipboard.SetDataObject(code);
            }
            // 如果不是其他
            else
            {
                SsendMessage("c_give(\"" + code + "\", 1)");
                System.Windows.Forms.Clipboard.SetDataObject("c_give(\"" + code + "\", 1)");
            }
        }
        #endregion

        // 获取房间名
        private string GetHouseName(int dSaveSlot)
        {
            var clusterIniPath = _pathAll.DoNotStarveTogetherDirPath + @"\Server_" + PathCommon.GamePlatform + "_" + dSaveSlot.ToString() + @"\cluster.ini";
            if (!File.Exists(clusterIniPath))
            {
                return "创建世界";
            }
            var iniTool = new IniHelper(clusterIniPath, _utf8NoBom);

            var houseName = iniTool.ReadValue("NETWORK", "cluster_name");
            return houseName;
        }

        /// <summary>
        /// 禁用
        /// </summary>
        /// <param name="isDisable">是否禁用</param>
        private void JinYong(bool isDisable)
        {
            // DediMainBorder.IsEnabled = isDisable;
            TitleMenuBaseSet.IsEnabled = isDisable;
            TitleMenuEditWorld.IsEnabled = isDisable;
            TitleMenuMod.IsEnabled = isDisable;
            TitleMenuRollback.IsEnabled = isDisable;

            DediMainTopDelete.IsEnabled = isDisable;
            CtrateWorldButton.IsEnabled = isDisable;
            DediBaseSet.IsEnabled = isDisable;
        }

        private void DediBaseIsCave_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems[0].ToString();
            CaveSettingColumnDefinition.Width = selected == "否" ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        }
    }
}
