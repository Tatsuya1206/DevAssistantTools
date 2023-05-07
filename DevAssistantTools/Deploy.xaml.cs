using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.IO;
using System.Xml.Linq;
using Path = System.IO.Path;
using System.Diagnostics;

namespace DevAssistantTools
{
    /// <summary>
    /// Deploy.xaml の相互作用ロジック
    /// </summary>
    public partial class Deploy : Page
    {
        private const string INFO_CAPTION = "インフォメーション";
        
        // 実行ファイルと同じ場所に保存
        private readonly string inputXmlFilePath = Path.Combine(Environment.CurrentDirectory, "input.xml");
        private readonly string systemSettingXmlFilePath = Path.Combine(Environment.CurrentDirectory, "SystemSetting.xml");
        private struct StandardModule
        {
            public const string ELEMENT_NAME = "StandardModule";
            public const string ELEMENTS_NAME = "Item";
        }
        private bool isAllDeployExec = false;
        private string successPhase = string.Empty;

        public Deploy()
        {
            InitializeComponent();
            LoadInpuDate();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClearErrs();
            //エラーチェック
            if (hasErros())
            {
                return;
            }
            else
            {
                SaveInputData();
                MessageBox.Show("デプロイ実行処理", INFO_CAPTION, MessageBoxButton.OK);
            }


        }
        /// <summary>
        /// エラーチェック
        /// </summary>
        /// <returns>true:エラー有り/false:エラー無し</returns>
        private bool hasErros()
        {
            bool ret = false;

            // 必須チェック
            ret = hasRequiredErr();

            if (!ret)
            {
                // 条件チェック
                ret = hasConditionsErr();
            }

            return ret;
        }
        /// <summary>
        /// 必須項目チェック
        /// </summary>
        /// <returns>true:エラー有り/false:エラー無し</returns>
        private bool hasRequiredErr()
        {
            bool ret = false;

            // 開発環境パス
            if (string.IsNullOrEmpty(DevSourcePassText.Text))
            {
                ShowErrMsg(DevSourcePassLabel.Content.ToString() + "を指定してください。");
                DevSourcePassText.Focus();
                ret = true;
                return ret;
            }

            // 実行環境パス
            if (string.IsNullOrEmpty(ExecSourcePassText.Text))
            {
                ShowErrMsg(ExecSourcePassLabel.Content.ToString() + "を指定してください。");
                ExecSourcePassText.Focus();
                ret = true;
                return ret;
            }

            // 拡張子
            if (TargetFileBin.IsChecked == false &&
                TargetFileAspx.IsChecked == false &&
                TargetFileJs.IsChecked == false &&
                TargetFileRpt.IsChecked == false)
            {
                ShowErrMsg("拡張子を指定してください。");
                TargetFileBin.Focus();
                ret = true;
                return ret;
            }
            return ret;
        }
        /// <summary>
        /// 条件チェック
        /// </summary>
        /// <returns>true:エラー有り/false:エラー無し</returns>
        private bool hasConditionsErr()
        {
            bool ret = false;
            if (string.IsNullOrEmpty(TargetIdText.Text))
            {
                // 機能IDの指定がない場合、
                // 全ファイルが対象かメッセージ表示。
                ret = false;
                isAllDeployExec = isAllDeploy();
            }
            if (TargetIdText.Text.Length < 2 && !isAllDeployExec)
            {
                // 個別実行かつ、機能IDが2文字以下の場合、エラー
                ShowErrMsg("機能IDは２文字以上入力してください。");
                TargetIdText.Focus();
                ret = true;
            }
            else if(!isAllDeployExec)
            {
                // 個別実行の場合、標準モジュールかチェック
                ret = isInputModuleText();
            }
            return ret;
        }
        /// <summary>
        /// モジュール入力チェック
        /// </summary>
        /// <returns></returns>
        private bool isInputModuleText()
        {
            bool ret = false;
            string targetInitial = TargetIdText.Text.Substring(0, 2);

            if (isStandardModule(targetInitial))
            {
                if (string.IsNullOrEmpty(ModuleNameText.Text))
                {
                    ret = true;
                    ShowErrMsg("アドオン機能の場合、モジュール名を指定してください。");
                    ModuleNameText.Focus();
                }
            }
            return ret;
        }
        private bool isStandardModule(string targetInitial)
        {
            bool ret = false;
            List<string> standardModule = GetStandardModule();
            if(string.IsNullOrEmpty(standardModule.FirstOrDefault(x => x == targetInitial)))
            {
                ret = true;
            }
            return ret;
        }
        /// <summary>
        /// 実行対象の確認
        /// </summary>
        /// <returns>true:全ファイル実行/false:個別ファイル実行</returns>
        private bool isAllDeploy()
        {
            bool isExec = false;

            string msg = "機能IDが指定されていません。" + Environment.NewLine + "すべてのファイルをデプロイしてよろしいですか？";
            string caption = INFO_CAPTION;
            MessageBoxButton btn = MessageBoxButton.OKCancel;
            MessageBoxImage img = MessageBoxImage.Information;
            ShowDialogMsg(msg, caption, btn, img, out isExec);

            return isExec;
        }
        /// <summary>
        /// エラーメッセージ表示
        /// </summary>
        /// <param name="errMsg"></param>
        private void ShowErrMsg(string errMsg)
        {
            MsgLabel.Visibility = Visibility.Visible;
            MsgLabel.Content = errMsg;
            MsgLabel.Background = Brushes.Red;
            MsgLabel.Foreground = Brushes.White;
        }
        /// <summary>
        /// ダイアログメッセージ表示
        /// </summary>
        /// <param name="msg">メッセージ</param>
        /// <param name="caption">メッセージタイトル</param>
        /// <param name="btn">ボタンの種類</param>
        /// <param name="icon">表示アイコン</param>
        /// <param name="isExec">OUT:実行状態</param>
        private void ShowDialogMsg(string msg,string caption, MessageBoxButton btn, MessageBoxImage icon,out bool isExec)
        {
            isExec = false;
            MessageBoxResult ret = MessageBox.Show(msg, caption, btn, icon);
            switch (ret)
            {
                case MessageBoxResult.Yes:
                case MessageBoxResult.OK:
                    isExec = true;
                    break;
                case MessageBoxResult.None:
                case MessageBoxResult.No:
                case MessageBoxResult.Cancel:
                    isExec = false;
                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// エラーメッセージの非表示
        /// </summary>
        private void ClearErrs()
        {
            MsgLabel.Visibility = Visibility.Hidden;
            // 一度、全実行状態にすると値が引き継がれるため、
            // デプロイボタン押下時に初期値に戻す。
            isAllDeployExec = false;
        }

        /// <summary>
        /// 入力内容の保存
        /// </summary>
        private void SaveInputData()
        {
            // XMLファイルを作成または開く
            XDocument xml = new XDocument();
            XElement root = new XElement("Root");
            xml.Add(root);

            // 画面入力項目をXMLに保存
            foreach (var control in InputGrid.Children)
            {
                if (control is TextBox textBox)
                {
                    root.Add(new XElement(textBox.Name, textBox.Text));
                }
                else if (control is ComboBox comboBox)
                {
                    root.Add(new XElement(comboBox.Name, comboBox.SelectedItem?.ToString()));
                }
                else if (control is CheckBox checkBox)
                {
                    root.Add(new XElement(checkBox.Name, checkBox.IsChecked.ToString()));
                }
            }

            xml.Save(inputXmlFilePath);
        }
        /// <summary>
        /// 前回入力内容の読込
        /// </summary>
        private void LoadInpuDate()
        {
            if (File.Exists(inputXmlFilePath))
            {
                // XMLファイルから画面入力項目を読み込み
                XDocument xml = XDocument.Load(inputXmlFilePath);
                foreach (var element in xml.Root.Elements())
                {
                    if (InputGrid.FindName(element.Name.LocalName) is TextBox textBox)
                    {
                        textBox.Text = element.Value;
                    }
                    else if (InputGrid.FindName(element.Name.LocalName) is ComboBox comboBox)
                    {
                        comboBox.SelectedItem = element.Value;
                    }
                    else if (InputGrid.FindName(element.Name.LocalName) is CheckBox checkBox)
                    {
                        if (bool.TryParse(element.Value, out bool isChecked))
                        {
                            checkBox.IsChecked = isChecked;
                        }
                    }
                }
            }
        }
        private void CallDeployBat()
        {
            string binfileExeFlg = (bool)TargetFileBin.IsChecked ? "1" : "0";

            // バッチファイルのパスと引数を設定する
            string batchFilePath = "C:\\path\\to\\batchfile.bat";
            string argument = DevSourcePassText.Text +
                              Environment.NewLine +
                              ExecSourcePassText.Text +
                              Environment.NewLine;

            // Processオブジェクトを作成する
            Process process = new Process();

            // 起動するファイルのパスを指定する
            process.StartInfo.FileName = batchFilePath;

            // 引数を設定する
            process.StartInfo.Arguments = argument;

            // 新しいウィンドウを作成せずに実行する
            process.StartInfo.CreateNoWindow = true;

            // コンソールウィンドウを使わずに実行する
            process.StartInfo.UseShellExecute = false;

            // 標準出力をリダイレクトする
            process.StartInfo.RedirectStandardOutput = true;

            // バッチファイルを実行する
            process.Start();

            // バッチファイルの終了まで待機する
            process.WaitForExit();

            // 標準出力を読み取る
            string output = process.StandardOutput.ReadToEnd();

            // プロセスを閉じる
            process.Close();
        }
        private List<string> GetStandardModule()
        {
            // TODO:XMLファイルの存在チェック、StandardModuleの存在チェック？

            List<string> moduleList = new List<string>();
            // XMLファイルを読み込み
            XDocument doc = XDocument.Load(systemSettingXmlFilePath);

            // StandardModule要素からItem要素を取得
            IEnumerable<XElement> items = doc.Element(StandardModule.ELEMENT_NAME).Elements(StandardModule.ELEMENTS_NAME);

            // 各Item要素の値を表示
            foreach (XElement item in items)
            {
                moduleList.Add(item.Value);
            }
            return moduleList;
        }
    }
}
