using ns_SerialPort;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SyncMoviePlay
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 入出力ファイルクラスのインスタンス
        /// </summary>
        ManageInOutFile _InOutFile;

        /// <summary>
        /// USB接続を管理するクラスのインスタンス
        /// </summary>
        SerialPortProcessor _usb;

        private bool _isClosed = false;
        /// <summary>ウィンドウが閉じられたかどうか</summary>
        public bool IsClosed { get { return _isClosed; } }


        private bool _isDataRecieved = false;
        /// <summary>ボードからデータを受信したかどうか　※未使用</summary>
        public bool IsDataRecieved { get { return _isDataRecieved; } }

        private bool _isDataSended = false;
        /// <summary>ボードへデータを送信したかどうか　※未使用</summary>
        public bool IsDataSended { get { return _isDataSended; } }
        
        
        volatile MoviePlay[] _movieDisplay = null;
        int _moviePlayCnt;
        Common.main _main = new Common.main();
        bool isJoin;
        public bool chkIsEndFlg;

        byte[] SendCmd = null;

        public MainWindow()
        {
            InitializeComponent();
            //++++++++++++++++++++++
            // 閉じた後の処理
            //++++++++++++++++++++++
            this.Closing += (s, e) =>
            {
                _isClosed = true;

            };
            //++++++++++++++++++++++
            // Load後の処理
            //++++++++++++++++++++++
            this.Loaded += (s, e) =>
            {
                InitUSBConnect();
                _main.InitdispList();

                _movieDisplay = new MoviePlay[_main._dispCnt];

                lbl_display_1.Content = _main.DebugdispList(0);
                lbl_display_2.Content = _main.DebugdispList(1);
                lbl_display_3.Content = _main.DebugdispList(2);
                lbl_display_4.Content = _main.DebugdispList(3);

                btn_play.IsEnabled = false;
                btn_movie.IsEnabled = false;

                //前回の設定を呼び出す
                InitDisplayInfo();
            };

        }

        /// <summary>
        /// MainWindowのオブジェクト作成後に行う初期化処理 
        /// </summary>
        public void Init()
        {
            _usb = new SerialPortProcessor();

            _usb.OnSpecifiedDeviceRemoved += new EventHandler(this.usb_OnSpecifiedDeviceRemoved);
            _usb.OnDataSend += new EventHandler(this.usb_OnDataSend);
            _usb.OnDataRecieved += new OnDataRecievedEventHandler(this.usb_OnDataRecieved);
            _usb.OnSpecifiedDeviceArrived += new EventHandler(this.usb_OnSpecifiedDeviceArrived);

            SendCmd = null;
        }

        //ログの追加
        public void AddLog(string txt)
        {
#if false
            //前回と同じログの場合は表示しない。
            if (listView_log.Items.Count > 0)
            {
                listView_log.SelectedIndex = 0;
                string[] s = new string[2];
                s = (string[])listView_log.SelectedValue;
                if (txt == s[1]) return;
            }
#endif

            int max = 20;
            string t = System.DateTime.Now.ToString("HH:mm:ss.fff");
            if (txt.Length == 0) t = "";

            //max件のみ表示
            if (listView_log.Items.Count >= max)
            {
                listView_log.Items.RemoveAt(max - 1);
            }


            listView_log.Items.Insert(0, new string[] { t, txt });
        }

        public void SetSendCmd(string _cmd)
        {
            int len = _cmd.Length;
            if (len <= 0) return;
            SendCmd = Encoding.ASCII.GetBytes(_cmd);
            Array.Resize(ref SendCmd, len + 2);
            SendCmd[len] = 0x0d;
            SendCmd[len + 1] = 0x0a;
        }

        /// <summary>
        /// USB接続
        /// </summary>
        void InitUSBConnect()
        {
            //接続中であれば一度閉じる
            _usb.Close();

            //USBと接続する
            ns_IniFile.SettingIni _ini = new ns_IniFile.SettingIni();
            string[] GetData;
            _ini.Func_getIni(ns_IniFile.SettingIni.IniFileID.USB, out GetData);
            try
            {
                _usb.PortNM = GetData[0];
                _usb.Bps = Int32.Parse(GetData[1]);
                _usb.DataBit = Int32.Parse(GetData[2]);
                _usb.Open();
                if(_usb.ErrMsg != string.Empty) AddLog(_usb.ErrMsg);

                //USBに接続できた場合
                if (_usb != null)
                {
                    //センサーを初期化する
                    _InOutFile = new ManageInOutFile();
                }

            }
            catch
            {
                MessageBox.Show("Iniファイルに誤りがあります。\r\n ファイルが存在するか確認して下さい。", "【エラー】デバイスのチェック");
                _isClosed = true;
            }

        }

        /// <summary>
        /// 前回の設定を呼び出す
        /// </summary>
        void InitDisplayInfo()
        {
            ns_IniFile.SettingIni _ini = new ns_IniFile.SettingIni();
            string[] GetData;

            //動画
            _ini.Func_getIni(ns_IniFile.SettingIni.IniFileID.DISPLAY, out GetData);
            if (GetData[0].Length > 0) SetMovieInfo(lbl_movie_1, txt_movie_1, 0, GetData[0]);
            if (GetData[1].Length > 0) SetMovieInfo(lbl_movie_2, txt_movie_2, 1, GetData[1]);
            if (GetData[2].Length > 0) SetMovieInfo(lbl_movie_3, txt_movie_3, 2, GetData[2]);
            if (GetData[3].Length > 0) SetMovieInfo(lbl_movie_4, txt_movie_4, 3, GetData[3]);

            //画像の結合
            _ini.Func_getIni(ns_IniFile.SettingIni.IniFileID.OPTION, out GetData);
            checkBox_display_1.IsChecked = Convert.ToBoolean(GetData[0]);
            checkBox_display_2.IsChecked = Convert.ToBoolean(GetData[1]);
            checkBox_display_3.IsChecked = Convert.ToBoolean(GetData[2]);
            checkBox_display_4.IsChecked = Convert.ToBoolean(GetData[3]);
            radioButton_Horizon.IsChecked = Convert.ToBoolean(GetData[4]);
            radioButton_Vertical.IsChecked = Convert.ToBoolean(GetData[5]);

            //送信オフセット
            _ini.Func_getIni(ns_IniFile.SettingIni.IniFileID.OFFSET, out GetData);
            txt_SendTime.Text = GetData[0];
            
        }

        /// <summary>
        /// 画面の設定を保存する
        /// </summary>
        void SaveDisplayInfo()
        {
            ns_IniFile.SettingIni _ini = new ns_IniFile.SettingIni();
            System.Text.StringBuilder SetData = new System.Text.StringBuilder();

            //動画
            SetData.Clear();
            SetData.Append(SaveDisplayInfo_Movie(0, txt_movie_1.Text));
            SetData.Append(",");
            SetData.Append(SaveDisplayInfo_Movie(1, txt_movie_2.Text));
            SetData.Append(",");
            SetData.Append(SaveDisplayInfo_Movie(2, txt_movie_3.Text));
            SetData.Append(",");
            SetData.Append(SaveDisplayInfo_Movie(3, txt_movie_4.Text));
            _ini.Func_SetIni(ns_IniFile.SettingIni.IniFileID.DISPLAY, SetData.ToString());

            //画面の結合
            SetData.Clear();
            SetData.Append(checkBox_display_1.IsChecked.ToString());
            SetData.Append(",");
            SetData.Append(checkBox_display_2.IsChecked.ToString());
            SetData.Append(",");
            SetData.Append(checkBox_display_3.IsChecked.ToString());
            SetData.Append(",");
            SetData.Append(checkBox_display_4.IsChecked.ToString());
            SetData.Append(",");
            SetData.Append(radioButton_Horizon.IsChecked.ToString());
            SetData.Append(",");
            SetData.Append(radioButton_Vertical.IsChecked.ToString());
            _ini.Func_SetIni(ns_IniFile.SettingIni.IniFileID.OPTION, SetData.ToString());

            //送信オフセット
            SetData.Clear();
            SetData.Append(txt_SendTime.Text);
            _ini.Func_SetIni(ns_IniFile.SettingIni.IniFileID.OFFSET, SetData.ToString());
        }
        /// <summary>
        /// 正しいパスになっているかチェックする
        /// </summary>
        /// <param name="i"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        string SaveDisplayInfo_Movie(int i,string path)
        {
            string s = string.Empty;
            if (_main.SetMoviePath(i, path).Length > 0) s = path;
            return s;
        }

        public void Send()
        {
            if (_usb == null) return;
            if (_InOutFile == null) return;
            if (SendCmd == null) return;

            _InOutFile.Func_SendCommand(_usb, SendCmd);

            //末尾についている0x0d,0x0aを削除して、無駄な改行が入らないようにしている。
            string _tmp = System.Text.Encoding.ASCII.GetString(SendCmd);
            AddLog("[送信]" + _tmp.Remove(_tmp.IndexOf((char)0x0d)));

            SendCmd = null;
        }

        public void Receive()
        {
            _usb.Sync_ReceiveData();
        }

        /// <summary>
        /// USB接続された時のイベントを受け取る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void usb_OnSpecifiedDeviceArrived(object sender, EventArgs e)
        {
            AddLog("ボードに接続しました");
        }

        /// <summary>
        /// USBが取り外れた時のイベントを受け取る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void usb_OnSpecifiedDeviceRemoved(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler(usb_OnSpecifiedDeviceRemoved), new object[] { sender, e });
            }
            else
            {
                AddLog("ボードとの接続を切断しました");
            }
        }

        /// <summary>
        /// データ受信時にイベントを受け取る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void usb_OnDataRecieved(object sender, OnDataRecievedEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(new OnDataRecievedEventHandler(usb_OnDataRecieved), new object[] { sender, args });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                string s = "";
                foreach (byte b in args.data)
                {
                    s += string.Format("{0,3:X2}", b) + " ";
                }
                //                Console.WriteLine("Input             :{0}", s);
                //                Console.WriteLine("Input ASCII Code  : {0}", System.Text.Encoding.ASCII.GetString(args.data));

                string ErrLog = "";
                ErrLog = _InOutFile.Func_Receive(_usb, args.data);
                if (ErrLog.Length > 0)
                {
                    AddLog(ErrLog);
                }
                else
                {
                    //実測データを反映する
                    if (s != "")
                    {
//                        string _s = "[受信]" + s;
//                        AddLog(_s);
                        //    MessageBox.Show(s);
                    }
                    
                }

                //_isDataSended = false;
            }

        }

        /// <summary>
        /// データ送信時にイベントを受け取る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void usb_OnDataSend(object sender, EventArgs e)
        {
            //_isDataSended = true;
        }



        //+++++++++++++++++++++++++++++++++++++++++++++++
        //入力値のチェックなど、画面に関する処理
        //+++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// ドラッグしながらテキストボックスにカーソルが行った時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_1_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }
        /// <summary>
        /// テキストボックスにドロップした時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_1_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                SetMovieInfo(lbl_movie_1, txt_movie_1, 0, files[0]);
            }
        }

        /// <summary>
        /// ドラッグしながらテキストボックスにカーソルが行った時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_2_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }
        /// <summary>
        /// テキストボックスにドロップした時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_2_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                SetMovieInfo(lbl_movie_2, txt_movie_2, 1, files[0]);
            }
        }
        /// <summary>
        /// ドラッグしながらテキストボックスにカーソルが行った時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_3_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }
        /// <summary>
        /// テキストボックスにドロップした時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_3_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                SetMovieInfo(lbl_movie_3, txt_movie_3, 2, files[0]);
            }
        }
        /// <summary>
        /// ドラッグしながらテキストボックスにカーソルが行った時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_4_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ファイルをドロップされた場合のみ e.Handled を True にする
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }
        /// <summary>
        /// テキストボックスにドロップした時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_movie_4_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                SetMovieInfo(lbl_movie_4, txt_movie_4, 3, files[0]);
            }
        }

        /// <summary>
        /// テキストボックスにドロップしたときにムービー情報をセットする
        /// </summary>
        /// <param name="lbl"></param>
        /// <param name="txt"></param>
        /// <param name="i">DISPLAYクラス</param>
        /// <param name="fileInfo">セットする情報</param>
        private void SetMovieInfo(System.Windows.Controls.Label lbl, System.Windows.Controls.TextBox txt, int i,string fileInfo)
        {
            txt.Text = fileInfo;
            lbl.Content = _main.SetMoviePath(i, txt.Text);
            AddLog(_main.JdgReady());
            btn_play.IsEnabled = _main.dispReady;
            btn_movie.IsEnabled = _main.dispReady;
        }

        private void Chk(object sender, MouseEventArgs e)
        {
            //_main.JdgReady();
            //btn_play.IsEnabled = _main.dispReady;
        }
        

        private void btn_playSetting_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START1;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_play_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START2;
            MoviePlay();

        }

        private void btn_movie_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = string.Empty;
            MoviePlay();
        }

        private void btn_ptn1_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START3;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_ptn2_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START4;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_ptn3_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START5;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_ptn4_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START6;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_ptn5_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START7;
            SetSendCmd(_main.sendStartCmd);
        }

        private void btn_ptn6_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.START8;
            SetSendCmd(_main.sendStartCmd);
        }


        /// <summary>
        /// 動画再生
        /// </summary>
        async void MoviePlay()
        {
            _moviePlayCnt = 0;


            //リピートの有無
            _main.repeat = (bool)chk_Repeat.IsChecked;

            //送信オフセットの設定
            if (!_main.chkSendTime(txt_SendTime.Text))
            {
                AddLog("送信オフセットに誤りがあります");
            }

            close_movieDisplay();

            //画面を結合するかどうかで処理を分ける。
            isJoin = chkDisplayJoin();
            if (!isJoin)
            {
                MoviePlay_Single();
            }
            else
            {
                MoviePlay_Join();
            }

            if (_main.sendStartCmd != string.Empty)
            {
                SetSendCmd(_main.sendStartCmd);

                //終了時のコマンドをセットしておく
                //※複数画面を開いてもコマンドは1回だけ送信するようにしているため、
                //　あえて変数を使っている。
                _main.sendEndCmd = Common.main.STOP;
            }
            System.Windows.Forms.Cursor.Hide();
            

            //動画再生中のチェック
            chkIsEndFlg = true;
            while (chkIsEndFlg)
            {
                //AddLog("[jdgMovieEnd]全ての動画が最後まで再生されるまで待つ");
                await Task.Run(() => waitAllMovie(_movieDisplay, _moviePlayCnt));

                if (!chkIsEndFlg) break;

                if (!_main.repeat) break;

                //AddLog("[jdgMovieEnd]リピートする場合、一定期間待ってからコマンドを送信する。");
                AddLog("リピート待ち・・・");
                //await Task.Run(() => Thread.Sleep(5000));

                if (!chkIsEndFlg) break;

                if (_main.sendStartCmd != string.Empty)
                {
                    SetSendCmd(_main.sendStartCmd);
                    await Task.Run(() => Thread.Sleep(_main.sendOfs));
                }

                if (!chkIsEndFlg) break;

                //AddLog("[jdgMovieEnd]各動画にリピートor停止の指示を出す。");
                if (!isJoin)
                {
                    for (int i = 0; i < _moviePlayCnt; i++)
                    {
                        _movieDisplay[i].isEndChk();
                    }
                }
                else
                {
                    _movieDisplay[_main.joinDisplay_no].isEndChk();
                }


            }

            //AddLog("動画再生中のチェックを抜ける");
            close_movieDisplay();
        }

        void waitAllMovie(MoviePlay[] _movieDisplay, int cnt)
        {
            bool sw = false;
            while (!sw)
            {
                if (!chkIsEndFlg) break;

                sw = true;
                for (int i = 0; i < cnt; i++)
                {
                    if (_movieDisplay[i].isPlay)
                    {
                        sw = false;
                    }
                }
            }
        }
        

        bool chkDisplayJoin()
        {
            bool _findSw = false;
            bool _base = false;  //基準となる画面が見つかるとtrue

            //画面１の結合有無をチェック
            if ((bool)checkBox_display_1.IsChecked)
            {
                _findSw = true;
                _base = true;
                _main.joinDisplay_no = 0;
                _main.joinDisplay_pos[0] = _main.dispList[_main.joinDisplay_no]._pos[0];
                _main.joinDisplay_pos[1] = _main.dispList[_main.joinDisplay_no]._pos[1];
                _main.joinDisplay_size[0] = _main.dispList[_main.joinDisplay_no]._size[0];
                _main.joinDisplay_size[1] = _main.dispList[_main.joinDisplay_no]._size[1];
            }

            //画面２の結合有無をチェック
            _findSw = chkDisplayJoin_Parts(checkBox_display_2, _findSw, ref _base, 1);
            //画面３の結合有無をチェック
            _findSw = chkDisplayJoin_Parts(checkBox_display_3, _findSw, ref _base, 2);
            //画面４の結合有無をチェック
            _findSw = chkDisplayJoin_Parts(checkBox_display_4, _findSw, ref _base, 3);

            //AddLog("chkDisplayJoin _findSw " + _findSw);

            return _findSw;
        }

        bool chkDisplayJoin_Parts(System.Windows.Controls.CheckBox chk,bool _findSw, ref bool _base, int i)
        {

            if (i >= _main._dispCnt) return _findSw;

            if (!(bool)chk.IsChecked) return _findSw;

            if (!_base)
            {
                _base = true;
                _main.joinDisplay_no = i;
                _main.joinDisplay_pos[0] = _main.dispList[i]._pos[0];
                _main.joinDisplay_pos[1] = _main.dispList[i]._pos[1];
                _main.joinDisplay_size[0] = _main.dispList[i]._size[0];
                _main.joinDisplay_size[1] = _main.dispList[i]._size[1];
            }
            else
            {
                //画面サイズを追加
                if ((bool)radioButton_Horizon.IsChecked)
                {
                    //横方向に連結　※幅は加算する、高さは大きい方に合わせる
                    _main.joinDisplay_size[0] += _main.dispList[i]._size[0];
                    if (_main.joinDisplay_size[1] < _main.dispList[i]._size[1])
                    {
                        _main.joinDisplay_size[1] = _main.dispList[i]._size[1];
                    }

                }
                else
                {
                    //縦方向に連結　※幅は大きい方に合わせる、高さは加算する
                    if (_main.joinDisplay_size[0] < _main.dispList[i]._size[0])
                    {
                        _main.joinDisplay_size[0] = _main.dispList[i]._size[0];
                    }
                    _main.joinDisplay_size[1] += _main.dispList[i]._size[1];
                }
            }


            return true;
        }


        /// <summary>
        /// 動画をそれぞれ指定した画面で再生
        /// </summary>
        void MoviePlay_Single()
        {
            
            for (int i = 0; i < _main._dispCnt; i++)
            {
                if (_main.dispList[i]._moviePath == string.Empty) continue;

                _movieDisplay[_moviePlayCnt] = new MoviePlay(_main, this, i);
                _movieDisplay[_moviePlayCnt].Left = _main.dispList[i]._pos[0];
                _movieDisplay[_moviePlayCnt].Top = _main.dispList[i]._pos[1];
                _movieDisplay[_moviePlayCnt].Width = _main.dispList[i]._size[0];
                _movieDisplay[_moviePlayCnt].Height = _main.dispList[i]._size[1];
                _movieDisplay[_moviePlayCnt].SetMovie();
                _moviePlayCnt++;
            }

            for (int i = 0; i < _moviePlayCnt; i++)
            {
                _movieDisplay[i].Show();
                //AddLog("[" + i + "] MovieDisplaySingle Show");
            }

        }

        /// <summary>
        /// 画面を連結して動画を再生
        /// </summary>
        void MoviePlay_Join()
        {
            int i = _main.joinDisplay_no;
            if (_main.dispList[i]._moviePath == string.Empty) return;

            _movieDisplay[0] = new MoviePlay(_main, this, i);
            _movieDisplay[0].Left = _main.joinDisplay_pos[0];
            _movieDisplay[0].Top = _main.joinDisplay_pos[1];
            _movieDisplay[0].Width = _main.joinDisplay_size[0];
            _movieDisplay[0].Height = _main.joinDisplay_size[1];
            _movieDisplay[0].SetMovie();
            _moviePlayCnt = 1;

            _movieDisplay[i].Show();
            //AddLog("[" + i + "] MovieDisplayJoin Show");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                //エスケープキーで画面閉じる
                case Key.Escape:
                    _isClosed = true;
                    break;

                //その他のキーで***
                default:
                    break;
            }
        }

        /// <summary>
        /// 停止ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {

            _main.sendStartCmd = Common.main.STOP;
            SetSendCmd(_main.sendStartCmd);
            close_movieDisplay();

        }

        /// <summary>
        /// 初期化ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_init_Click(object sender, RoutedEventArgs e)
        {
            _main.sendStartCmd = Common.main.INIT;
            SetSendCmd(_main.sendStartCmd);
            close_movieDisplay();
        }

        void close_movieDisplay()
        {
            //AddLog("close_movieDisplay");

            chkIsEndFlg = false;
            
            foreach (Window win in Application.Current.Windows)
            {
                if (win == this) continue;

                Window.GetWindow(win).Close();
            }

            for (int i = 0; i < _moviePlayCnt; i++)
            {
                _movieDisplay[i] = null;
            }
        }

        private void btn_movie_clear_1_Click(object sender, RoutedEventArgs e)
        {
            xaml_movie_clear(lbl_movie_1, txt_movie_1, 0);
        }
        private void btn_movie_clear_2_Click(object sender, RoutedEventArgs e)
        {
            xaml_movie_clear(lbl_movie_2, txt_movie_2, 1);
        }
        private void btn_movie_clear_3_Click(object sender, RoutedEventArgs e)
        {
            xaml_movie_clear(lbl_movie_3, txt_movie_3, 2);
        }
        private void btn_movie_clear_4_Click(object sender, RoutedEventArgs e)
        {
            xaml_movie_clear(lbl_movie_4, txt_movie_4, 3);
        }
        
        private void xaml_movie_clear(System.Windows.Controls.Label lbl, System.Windows.Controls.TextBox txt , int i)
        {
            lbl.Content = string.Empty;
            txt.Text = "再生動画をドラッグ＆ドロップ";
            _main.dispList[i].err = true;
            _main.dispList[i].errMsg = "[" + i + "]設定無し";
            _main.dispList[i]._movieName = string.Empty;
            _main.dispList[i]._moviePath = string.Empty;

            AddLog(_main.JdgReady());
            btn_play.IsEnabled = _main.dispReady;
            btn_movie.IsEnabled = _main.dispReady;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDisplayInfo();
        }

    }
}
