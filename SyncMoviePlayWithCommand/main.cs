using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Common
{
    public class main
    {
        public const string START1 = "COM_START1";
        public const string START2 = "COM_START2";
        public const string START3 = "COM_START3";
        public const string START4 = "COM_START4";
        public const string START5 = "COM_START5";
        public const string START6 = "COM_START6";
        public const string START7 = "COM_START7";
        public const string START8 = "COM_START8";
        public const string STOP = "COM_STOP";
        public const string INIT = "COM_INIT";

        public int joinDisplay_no;
        public int[] joinDisplay_pos = new int[2];
        public int[] joinDisplay_size = new int[2];
        
        public bool dispReady;  //true…準備完了、false…何かしら問題あり
        public int _dispCnt;
        public DISPLAY[] dispList;

        //再生開始時に送信するコマンド
        public string sendStartCmd = string.Empty;
        //再生終了時に送信するコマンド
        public string sendEndCmd = string.Empty;
        //リピートの指定
        public bool repeat = false;
        //再生タイミングのオフセット
        public int sendOfs = 0;

        public class DISPLAY
        {
            public string _name = string.Empty;
            public int _no = 0;
            public int[] _pos = new int[2];     //左上の座標
            public int[] _size = new int[2];    //幅・高さ
            public string _movieName = string.Empty;
            public string _moviePath = string.Empty;
            public bool err = true;
            public string errMsg = string.Empty;

        }



        public main()
        {
            dispReady = false;
        }

        public string SetMoviePath(int i,string s)
        {   
            
            if (i >= _dispCnt) return string.Empty;


            //動画のチェック&セット
            dispList[i].err = !chkMovieExist(i,s);

            if (dispList[i].err) return string.Empty;
            return dispList[i]._movieName;
        }
        
        public bool chkSendTime(string s)
        {
            try
            {
                int num = 0;
                sendOfs = 0;
                //入力値が数値であるかチェック、数値の場合保存する
                if (int.TryParse(s, out num))
                {
                    sendOfs = num;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }

            //合格
            return true;
        }

        /// <summary>
        /// 動画の存在チェック
        /// </summary>
        /// <returns></returns>
        bool chkMovieExist(int i, String _path)
        {
            bool sw = false;
            dispList[i]._movieName = string.Empty;
            dispList[i]._moviePath = string.Empty;
            dispList[i].errMsg = "[" + i + "]準備完了 ";
            try
            {
                FileInfo _info = new FileInfo(@_path);
                string _f = _info.Name;
                DirectoryInfo _d = _info.Directory;

                //ファイルの存在をチェック
                if (!_info.Exists)
                {
                    dispList[i].errMsg = "[" + i + "]指定したファイルは存在しません。";
                    return sw;
                }

                //ファイルが動画かチェック
                if (!chkFile(_f))
                {
                    dispList[i].errMsg = "[" + i + "]" + _f + "は再生できません。";
                    return sw;
                }

                //合格
                dispList[i]._movieName = _info.Name;
                dispList[i]._moviePath = _info.FullName;
                dispList[i].errMsg += _info.Name;
                sw = true;

//                MessageBox.Show( " [info.Name]" + _info.Name + "\n" + " [DirectoryInfo]" + _d.FullName + "\n" + "[movie_path]" + movie_path + "\n", "TEST");

            }
            catch (Exception e)
            {
                dispList[i].errMsg = "[" + i + "]ファイル名に誤りがあります。";
                //MessageBox.Show(_path + " [ERR]" + e.Message, "TEST");
            }


            return sw;
        }

        /// <summary>
        /// 指定したファイルが動画であるかチェックする
        /// </summary>
        /// <param name="_file"></param>
        /// <returns></returns>
        bool chkFile(string _file)
        {
            bool sw = true;

            if (_file.IndexOf(".mov") > 0) return sw;
            if (_file.IndexOf(".wmv") > 0) return sw;

            sw = false;
            return sw;
        }

        /// <summary>
        /// 準備完了有無のチェック　＆　準備出来ているかメッセージを返す
        /// </summary>
        /// <returns></returns>
        public string JdgReady()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            dispReady = false;
            //一つでもムービーがセットされていたら準備完了とする。
            for(int i=0; i < _dispCnt; i++)
            {
                if (!dispList[i].err) dispReady = true;

                sb.Append(dispList[i].errMsg);
                if (i != _dispCnt - 1) sb.Append("\n");
            }


            return sb.ToString();
        }

        /// <summary>
        /// ディスプレイリストを初期化する
        /// </summary>
        public void InitdispList()
        {
            sendStartCmd = string.Empty;
            repeat = false;
            sendOfs = 0;

            _dispCnt = Screen.AllScreens.Length;
            dispList = new DISPLAY[_dispCnt];
            int i = 0;
            foreach(Screen item in Screen.AllScreens)
            {
                dispList[i] = new DISPLAY();
                dispList[i]._name = item.DeviceName;
                dispList[i]._no = i;
                dispList[i]._pos[0] = item.Bounds.X;
                dispList[i]._pos[1] = item.Bounds.Y;
                dispList[i]._size[0] = item.Bounds.Width;
                dispList[i]._size[1] = item.Bounds.Height;
                dispList[i]._moviePath = string.Empty;
                dispList[i].err = true;
                dispList[i].errMsg = "[" + i + "]設定無し";
                
                i++;
            }

        }

        public string DebugdispList(int i)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if ( i >= _dispCnt)
            {
                sb.Append("[");
                sb.Append(i);
                sb.Append("]");
                sb.Append("ディスプレイ無し");

                return sb.ToString();
            }

            sb.Append("[");
            sb.Append(dispList[i]._no);
            sb.Append("]");
//            sb.Append(" Name:");
            sb.Append(dispList[i]._name);
            sb.Append(" Size:");
            sb.Append(dispList[i]._size[0]);
            sb.Append(",");
            sb.Append(dispList[i]._size[1]);
            sb.Append(" Pos:(");
            sb.Append(dispList[i]._pos[0]);
            sb.Append(",");
            sb.Append(dispList[i]._pos[1]);
            sb.Append(")");

            return sb.ToString();
        }
    }
}
