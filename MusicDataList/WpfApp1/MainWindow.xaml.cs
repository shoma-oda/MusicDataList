using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace MusicDataList
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<Music> BindDataList = new ObservableCollection<Music>();  //DataGridにバインドするためリスト
        private string textBoxPostProc;                                                       //参照してきたパスを代入する変数
        public IniFile filePath;                                                              //参照してきたファイルのパス情報を格納する変数
        public bool isReferenced = false;                                                     //DataGridの更新が発生したか判断するための変数
        public bool isRead = false;                                                           //TextBoxの更新が発生したか判断するための変数

        public class IniFile //INIファイルを読み書きするクラス
        {
             [DllImport("KERNEL32.DLL")] 
             private static extern int GetPrivateProfileString(
             string lpAppName,
             string lpKeyName,
             string lpDefault,
             StringBuilder lpReturnedString,
             int nSize,
             string lpFileName);

             [DllImport("KERNEL32.DLL")]
             private static extern int WritePrivateProfileString(
             string lpAppName,
             string lpKeyName,
             string lpString,
             string lpFileName);
 
             string filePath;

            public IniFile(string a_filePath)
            {
                this.filePath = a_filePath;
            }

            public string this[string section, string key]
            {
                set
                {             
                    WritePrivateProfileString(section, key, value, filePath);
                }
                get
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetPrivateProfileString(section, key, string.Empty, sb, sb.Capacity, filePath);
                    return sb.ToString();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            FilePath.AddHandler(TextBox.DragOverEvent, new DragEventHandler(TbDragOver), true);
            FilePath.AddHandler(TextBox.DropEvent, new DragEventHandler(TbDrop), true);

            //前回参照していたパスを表示する。
            filePath = new IniFile("./dirName.ini"); 
            string value = filePath["section", "key"];
            if(value != null)  
            {
                FilePath.Text = value; //前回参照した絶対パスを代入する
                textBoxPostProc = FilePath.Text;
            }

            //参照ファイルを読み出すまでボタンを無効化する
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            FileSaveButton.IsEnabled = false;
        }


        //参照ボタン
        private void FileAccessButton_Click(object sender, RoutedEventArgs e)
        {
            if (isReferenced == false || isRead == false)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.FileName = "";
                dialog.DefaultExt = "*.*";
                if (dialog.ShowDialog() == true)
                {
                    textBoxPostProc = dialog.FileName;
                    FilePath.Text = textBoxPostProc;
                }
                filePath["section", "key"] = textBoxPostProc;    //参照した絶対パスを保存する
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("編集を破棄しますか？","確認",MessageBoxButton.YesNo,MessageBoxImage.Information);

                if( result.ToString() == "Yes")
                {
                    isReferenced = false;                  //再度読み出しができるようにする
                    DataGrid.ItemsSource = null;           //DataGridへのバインドを外す
                    FilePath.Text = null;

                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.FileName = "";
                    dialog.DefaultExt = "*.*";
                    if (dialog.ShowDialog() == true)
                    {
                        textBoxPostProc = dialog.FileName;
                        FilePath.Text = textBoxPostProc;
                    }
                    filePath["section", "key"] = textBoxPostProc;    //参照した絶対パスを保存する
                }
            }
        }

        //参照したいファイルをドラッグオーバーするための関数
        private void TbDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        //参照したいファイルをドロップするための関数
        private void TbDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // TextBoxの中身をクリアする。
                FilePath.Text = string.Empty;
                // ドロップしたファイル名を全部取得する。
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                FilePath.Text = filenames[0];
            }
        }

        //Readボタン
        public void FileReadButton_Click(object sender, RoutedEventArgs e)
        {


            //ボタンを有効化
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;
            FileSaveButton.IsEnabled = true;

            if (textBoxPostProc == null)
            {   //参照パスが指定されていない場合
                MessageBox.Show("参照したいファイルを指定してください。", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (textBoxPostProc.Contains(".csv") == false)
            {   //ファイル名が間違っている場合
                MessageBox.Show("ファイル名を確認してください", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                int musicIndex = 0;
                string line = string.Empty;
                string[] values = string.Empty;

                StreamReader sr = new StreamReader(textBoxPostProc);
                if (isReferenced == false && isRead == false ) //初めて読み出しする場合
                {
                    BindDataList.Clear();
                    do
                    {
                        line = sr.ReadLine();
                        if ( musicIndex != 0) //1行目は表示しない
                        {
                            values = line.Split(',');
                            BindDataList.Add(new Music()
                            {
                                Number = musicIndex,
                                Song   = values[1],
                                Artist = values[2],
                                Genre  = values[3],
                                BPM    = values[4],
                            });
                        }
                        line = null;
                        values = null;
                        musicIndex++;
                    }
                    while (!sr.EndOfStream);
                    sr.Close();
                    DataGrid.Items.Refresh();  //すでにバインドしているデータをリフレッシュする。
                    DataGrid.ItemsSource = BindDataList;
                    isRead = true;
                }
                else //既に読み出しを行っている場合
                {
                    MessageBoxResult result = MessageBox.Show("編集を破棄しますか？","確認",MessageBoxButton.YesNo,MessageBoxImage.Information);

                    if (result.ToString() == "Yes")     //編集を破棄する場合
                    {
                        DataGrid.ItemsSource = null;     //DataGridへのバインドを外す
                        isReferenced = false;                     //再度読み出しができるようにする

                        if (BindDataList.Count != 0)
                        {
                            BindDataList.Clear(); //２回目以降の呼び出しの場合、前回のリストをクリアする
                        }

                        do
                        {
                            line = sr.ReadLine();
                            if (musicIndex != 0)
                            {
                                values = line.Split(',');
                                BindDataList.Add(new Music()
                                {
                                    Number = musicIndex,
                                    Song   = values[1],
                                    Artist = values[2],
                                    Genre  = values[3],
                                    BPM    = values[4],
                                });
                            }
                            line = null;
                            values = null;
                            musicIndex++;
                        }
                        while (!sr.EndOfStream);
                        sr.Close();
                        DataGrid.Items.Refresh();  //すでにバインドしているデータをリフレッシュする。
                        DataGrid.ItemsSource = BindDataList;
                    }
                }
            }
        }

        //削除ボタン
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DataGrid.SelectedIndex;
            if (selectedIndex > -1)
            {
                int musicIndex = 1;
                var newBindDataList = new ObservableCollection<Music>();
                BindDataList.RemoveAt(selectedIndex);

                foreach (Music music in BindDataList)
                {
                    newBindDataList.Add(new Music()
                    {
                        Number = musicIndex,
                        Song   = music.Song,
                        Artist = music.Artist,
                        Genre  = music.Genre,
                        BPM    = music.BPM,
                    });
                    musicIndex++;
                }
                DataGrid.Items.Refresh();  //すでにバインドしているデータをリフレッシュする。
                DataGrid.ItemsSource = newBindDataList;
            }
        }

        //追加ボタン
        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            var newMusicDataList = DataGrid.ItemsSource as ObservableCollection<Music>;
            var prefix           = newMusicDataList.Count+1;
            var newMusicData     = CreateNewRow(prefix);
            newMusicDataList.Add(newMusicData);    
        }

        private  Music CreateNewRow( int a_prefix){
            var newData = new Music{ Number = a_prefix, Song = "", Artist = "", Genre = "", BPM = ""};
            return newData;
        }

        //保存ボタン
        private void FileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            using( var sw = new StreamWriter( textBoxPostProc, false, Encoding.Unicode)){
                sw.WriteLine(string.Join(",", DataGrid.Columns.Select(x => 
                {
                    if (x.Header is string headerText)
                    {
                        return headerText;
                    }
                    else
                    {
                        return string.Empty;
                    }
                })));
                // セルの内容
                foreach (var item in DataGrid.Items)
                {
                    sw.WriteLine(string.Join(",", DataGrid.Columns.Select(x => x.OnCopyingCellClipboardContent(item)?.ToString())));
                }
            }
        }

        //データグリッドにバインドするリストの内容
        public class Music : INotifyPropertyChanged
        {
            private int _number;
            public int Number { get { return _number; } set { _number = value; OnPropertyChanged("Number"); } }

            private string _song;
            public string Song { get { return _song; } set { _song = value; OnPropertyChanged("Song"); } }

            private string _artist;
            public string Artist { get { return _artist; } set { _artist = value; OnPropertyChanged("Artist"); } }

            private string _genre;
            public string Genre { get { return _genre; } set { _genre = value; OnPropertyChanged("Genre"); } }

            private string _bpm;
            public string BPM { get { return _bpm; } set { _bpm = value; OnPropertyChanged("BPM"); } }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}