using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SyncMoviePlay
{
    /// <summary>
    /// MoviePlay.xaml の相互作用ロジック
    /// </summary>
    public partial class MoviePlay : Window
    {
        Common.main _main;
        MainWindow _mainWindow;
        int dispNo; //表示するディスプレイ番号

        public bool isPlay;
        
        public MoviePlay(Common.main _m, MainWindow _mainWindow, int i)
        {
            _main = _m;
            this._mainWindow = _mainWindow;
            dispNo = i;
            InitializeComponent();
            //Loaded += LoadedProc;
            ContentRendered += (s, e) => { PlayMovie(); };
        }

        private async void LoadedProc(object sender, RoutedEventArgs e)
        {
            // WindowがActiveになるまで待つ
            await Task.Run(() =>
            {
                do
                {
                    Thread.Sleep(100);
                } while (!Application.Current.Dispatcher.Invoke(() => { return IsActive; }));
            });
            //動画を読み込む
            //SetMovie();
        }

        public void SetMovie()
        {
            try
            {
                me_movie.Source = new Uri(_main.dispList[dispNo]._moviePath);
                me_movie.Position = new TimeSpan(0, 0, 0, 0, 1);
                me_movie.ScrubbingEnabled = true;
                me_movie.Pause();
                isPlay = true;
                // _mainWindow.AddLog("[" + dispNo + "]動画の読み込み完了" + me_movie.Source);
            }
            catch (Exception e)
            {
                _mainWindow.AddLog("[" + dispNo + "]動画を読み込めませんでした。" + "\n" + "[メッセージ]" + e.Message);
                Window.GetWindow(this).Close();
            }

        }

        async void PlayMovie()
        {
            try
            {
                if(_main.sendStartCmd != string.Empty) await Task.Run(() => Thread.Sleep(_main.sendOfs));

                if (!isPlay) return;

                me_movie.Play();
                _mainWindow.AddLog("[" + dispNo + "]動画再生開始");
            }
            catch (Exception e)
            {
                _mainWindow.AddLog("[" + dispNo + "]動画の再生に失敗しました" + "\n" + "[メッセージ]" + e.Message);
                Window.GetWindow(this).Close();
            }
        }


        public void CloseMovie()
        {
            isPlay = false;
            try
            {
                //動画を止める
                //※停止コマンドは送信しない
                me_movie.Clock = null;
                me_movie.Close();
                me_movie.Source = null;

                _mainWindow.AddLog("[" + dispNo + "]動画再生終了");


            }
            catch (Exception e)
            {
            }
        }
        
        private void EndMovie(object sender, RoutedEventArgs e)
        {
            isPlay = false;
        }

        public void isEndChk()
        {
            if (_main.repeat)
            {
                isPlay = true;
                _mainWindow.AddLog("[" + dispNo + "]動画再生開始(リピート)");
                me_movie.Position = new TimeSpan(0, 0, 0, 0, 1);
            }
            else
            {
                Window.GetWindow(this).Close();
            }
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                //エスケープキーで画面閉じる
                case Key.Escape:
                    //同名のウインドウ全て閉じる。
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win == this) continue;

                        if (win.Title == this.Title)
                        {
                            Window.GetWindow(win).Close();
                        }
                    }
                    
                    Window.GetWindow(this).Close();
                    
                    //リピートのチェックなどがいらなくなるので、フラグを初期化する
                    _mainWindow.chkIsEndFlg = false;

                    break;

                //その他のキーで***
                default:
                    break;
            }
        }

        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //_mainWindow.AddLog("画面を閉じる");
            CloseMovie();
            System.Windows.Forms.Cursor.Show();
        }
    }
}
