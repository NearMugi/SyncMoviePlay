using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace SyncMoviePlay
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {

        /// <summary>二重起動禁止用mutex</summary>
        private static System.Threading.Mutex _mutex;

        /// <summary>アプリケーション初期化処理</summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 二重起動禁止
            _mutex = new System.Threading.Mutex(false, Application.ResourceAssembly.FullName);
            if (!_mutex.WaitOne(0, false))
            {
                _mutex.Close();
                _mutex = null;
                this.Shutdown();
            }


            // 開始
            Start();
        }

        /// <summary>終了処理</summary>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
        

        /// <summary>メインウィンドウ</summary>
        private static MainWindow _win;


        /// <summary>メイン処理を開始する</summary>
        private async void Start()
        {
            //+++++++++++++++
            //初期処理
            //+++++++++++++++
            // メイン画面の表示
            _win = new MainWindow();
            _win.Init();
            _win.Show();

            //+++++++++++++++
            //メインループ処理
            //+++++++++++++++
            while (!_win.IsClosed)
            {
                // UIメッセージ処理
                DoEvents();
            }

            await Task.Delay(1);

            //メイン画面を閉じた時に全て止める
            _mutex.Close();
            _mutex = null;
            this.Shutdown();
        }



        /// <summary>現在メッセージ待ち行列の中にある全UIメッセージを処理する</summary>
        private void DoEvents()
        {
            // 新しくネスト化されたメッセージ ポンプを作成
            DispatcherFrame frame = new DispatcherFrame();

            // DispatcherFrame (= 実行ループ) を終了させるコールバック
            DispatcherOperationCallback exitFrameCallback = (f) =>
            {
                // ネスト化されたメッセージ ループを抜ける
                ((DispatcherFrame)f).Continue = false;
                return null;
            };

            // 非同期で実行する
            // 優先度を Background にしているので、このコールバックは
            // ほかに処理するメッセージがなくなったら実行される
            DispatcherOperation exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background, exitFrameCallback, frame);

            // 実行ループを開始する
            Dispatcher.PushFrame(frame);

            // コールバックが終了していない場合は中断
            if (exitOperation.Status != DispatcherOperationStatus.Completed)
            {
                exitOperation.Abort();
            }
        }
    }
}
