using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO.Ports;
using System.IO;
using System.Windows;

namespace ns_SerialPort
{
    //入出力ファイル管理
    public class ManageInOutFile
    {
        public void Func_SendCommand(SerialPortProcessor _usb, byte[] _b)
        {
            if (_usb == null) return;
            if (_b == null) return;
            if (_b.Length == 0) return;
            
            _usb.WriteData(_b);
        }
        
        /// <summary>
        /// dataのサイズが想定外の場合、破棄する
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        string Func_ErrDataSize(byte[] data, int size)
        {
            string ret_t = "";
            if (data.Length != size)
            {
                ret_t = "【受信データサイズエラー】" + " DataSize :" + data.Length;
            }
            return ret_t;
        }

        string Func_JdgReceiveData(SerialPortProcessor _usb, byte[] data)
        {
            string ret_t = "";

            try
            {
            }
            catch (Exception ex)
            {
                ret_t = ex.Message;
            }


            return ret_t;
        }

        //受信したデータを判別する
        public string Func_Receive(SerialPortProcessor _usb, byte[] data)
        {
            string ret_t = "";
            
            return ret_t;
        }

        string Func_SetCH(byte[] s)
        {
            string ret_t = "";

            
            return ret_t;
        }

    }

    public delegate void OnDataRecievedEventHandler(object sender, OnDataRecievedEventArgs args);
    public class OnDataRecievedEventArgs : EventArgs
    {
        public readonly byte[] data;

        public OnDataRecievedEventArgs(byte[] data)
        {
            this.data = data;
        }
    }


    public class SerialPortProcessor
    {

        SerialPort myPort = null;

        public string PortNM;   //ポート名
        public int Bps; //ボーレート
        public int DataBit; //データビット
        public string ErrMsg;

        /// <summary>
        /// コンストラクターの宣言
        /// </summary>
        public SerialPortProcessor()
        {
            myPort = null;
            PortNM = string.Empty;
            Bps = 0;
            DataBit = 0;
            ErrMsg = string.Empty;
        }


        /// <summary>
        /// 指定されたデバイスが見つかった場合にイベントを発行します。
        /// </summary>
        [Description("指定されたデバイスが見つかった場合にイベントを発行します。")]
        [Category("Embedded Event")]
        [DisplayName("OnSpecifiedDeviceArrived")]
        public event EventHandler OnSpecifiedDeviceArrived;

        /// <summary>
        /// 指定されたデバイスが削除された場合にイベントを発行します。
        /// </summary>
		[Description("指定されたデバイスが削除された場合にイベントを発行します。")]
        [Category("Embedded Event")]
        [DisplayName("OnSpecifiedDeviceRemoved")]
        public event EventHandler OnSpecifiedDeviceRemoved;


        /// <summary>
        /// 指定したデバイスからデータを受信した際に登録されたイベントを実行。
        /// </summary>
        [Description("デバイスからデータを受信した時、イベントを発行")]
        [Category("Embedded Event")]
        [DisplayName("OnDataRecieved")]
        public event OnDataRecievedEventHandler OnDataRecieved;

        /// <summary>
        /// データ送信時にイベントを発行します。
        /// </summary>
        [Description("データ送信時にイベントを発行します。")]
        [Category("Embedded Event")]
        [DisplayName("OnDataSend")]
        public event EventHandler OnDataSend;

        /// <summary>
        /// USB接続
        /// </summary>
        public void Open()
        {
            ErrMsg = string.Empty;
            try
            {
                myPort = new SerialPort(PortNM, Bps, Parity.None, DataBit, StopBits.One);
                myPort.Open();

                if (myPort != null)
                {
                    OnSpecifiedDeviceArrived(this, new EventArgs()); 
                }
                else
                {
                    OnSpecifiedDeviceRemoved(this, new EventArgs());
                }
            }
            catch
            {
                ErrMsg = "ポート番号[" + PortNM + "]  デバイスの接続に失敗しました。USBが繋がっているか確認してください。";
            }
        }

        /// <summary>
        /// 送信
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteData(byte[] buffer)
        {
            if (myPort == null) return;
            if (!myPort.IsOpen) return;

            myPort.Write(buffer, 0, buffer.Length);
            OnDataSend(this, new EventArgs());
        }

        /// <summary>
        /// 受信　※非同期実行
        /// </summary>
        public async void Sync_ReceiveData()
        {
            if (myPort == null) return;
            if (!myPort.IsOpen) return;
            
            await Task.Run(() =>
            {
                try
                {
                    int rbyte = myPort.BytesToRead;
                    byte[] buffer = new byte[rbyte];
                    int read = 0;
                    while (read < rbyte)
                    {
                        int length = myPort.Read(buffer, read, rbyte - read);
                        read += length;
                    }
                    if (rbyte > 0)
                    {
                        OnDataRecieved(this, new OnDataRecievedEventArgs(buffer));
                    }
                }
                catch
                {

                }
            });

        }

        public void Close()
        {
            if (myPort != null)
            {
                myPort.Close();
                myPort = null;
                OnSpecifiedDeviceRemoved(this, new EventArgs());
            }
        }
    }
}
