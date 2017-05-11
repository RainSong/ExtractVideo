using Emgu.CV;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractVideo
{
    public partial class Form1 : Form
    {
        private FileInfo fileInfo = null;
        private Capture capture = null;
        private int FreamCount = 0;
        private int FreamHeight = 0;
        private int FreamWidth = 0;
        private int FPS = 0;
        private int NeedSaveCount = 0;

        private Action<int> acUpUi = null;
        private Task ExtractTask = null;

        public Form1()
        {
            InitializeComponent();
            this.labelFileSize.Text =
                this.labelVideoLength.Text =
                this.labelFPS.Text =
                this.labelFreamCount.Text =
                this.labelVideoSize.Text =
                this.labelNeedSave.Text =
                this.labelSavedCount.Text = string.Empty;
            this.Text = "视频文件分割为图片";
            this.openFileDialog1.Filter = "MP4|*.mp4|AVI|*.avi|RMVB|*.rmvb|MKV|*.mkv";

            acUpUi = (savedCount) =>
            {
                this.labelSavedCount.Text = savedCount.ToString() + "张";
                this.progressBar1.Value = savedCount;
            };

            this.ExtractTask = new Task(new Action(Extract));
        }

        private void btnBrowser_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtFile.Text = this.openFileDialog1.FileName;
                this.fileInfo = new FileInfo(this.txtFile.Text);
                this.capture = new Capture(this.txtFile.Text);

                //定位到某一帧
                //this.capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, 100);
                ////获取当前帧
                //var mat = this.capture.QueryFrame();
                //mat.Bitmap.Save("D://123.jpg");

                ShowVideoInfo();
                GetImageDir();
            }
        }

        private void GetImageDir()
        {
            if (this.fileInfo == null) return;
            var path = Path.GetDirectoryName(fileInfo.FullName);
            var fileName = fileInfo.Name;
            path = Path.Combine(path, fileName);

            var index = path.LastIndexOf(".");
            if (index > 0)
                path = path.Remove(index, path.Length - index) + " - images - " + DateTime.Now.ToString("yyyyMMddHHmmssffffff");

            this.txtImagePath.Text = path;
            //Directory.CreateDirectory(path);
        }

        private void ShowVideoInfo()
        {
            if (this.fileInfo == null || this.capture == null) return;

            this.FreamCount = (int)this.capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
            this.FreamHeight = (int)this.capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight);
            this.FreamWidth = (int)this.capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth);
            this.FPS = (int)this.capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
            var videoLength = this.FPS == 0 ? 0 : this.FreamCount / this.FPS;

            this.labelFPS.Text = this.FPS.ToString() + "帧/秒";
            this.labelFreamCount.Text = this.FreamCount.ToString();
            this.labelVideoSize.Text = string.Format("{0}(W)*{1}(H)", this.FreamWidth, this.FreamHeight);

            #region 计算时间长度
            string strVideoLength = string.Empty;
            if (videoLength >= 3600)
            {
                var hour = videoLength / 3600;
                var min = (videoLength - (hour * 3600)) / 60;
                var second = videoLength - (hour * 3600) - (min * 60);
                strVideoLength = string.Format("{0}时{1}分{2}秒", hour, min, second);
            }
            else if (videoLength >= 60 && videoLength < 3600)
            {
                var min = videoLength / 60;
                var second = videoLength - (min * 60);
                strVideoLength = string.Format("{0}分{1}秒", min, second);
            }
            else
            {
                strVideoLength = ((int)videoLength).ToString() + "秒";
            }
            this.labelVideoLength.Text = strVideoLength;
            #endregion

            #region 计算文件大小
            var fileLength = this.fileInfo.Length;
            if (fileLength >= (1024 * 1024 * 1024))
            {
                var gb = fileLength / 1024D / 1024D / 1024D;
                this.labelFileSize.Text = gb.ToString("f2") + "GB";
            }
            else if (fileLength >= (1024 * 1024) && fileLength < (1024 * 1024 * 1024))
            {
                var mb = fileLength / 1024D / 1024D;
                this.labelFileSize.Text = mb.ToString("f2") + "MB";
            }
            else if (fileLength >= 1024 && fileLength < (1024 * 1024))
            {
                var kb = fileLength / 1024D;
                this.labelFileSize.Text = kb.ToString("f2") + "KB";
            }
            else
            {
                this.labelFileSize.Text = fileLength.ToString() + "B";
            }
            #endregion

        }

        private void ExtractEveryFream()
        {
            var length = (this.FreamCount.ToString()).Length;
            if (!Directory.Exists(this.txtImagePath.Text))
                Directory.CreateDirectory(this.txtImagePath.Text);
            for (var i = 0; i < this.FreamCount; i++)
            {
                //this.capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, i);
                var mat = this.capture.QueryFrame();
                var fileName = (i + 1).ToString().PadLeft(length, '0') + ".jpg";
                var fileFullName = Path.Combine(this.txtImagePath.Text, fileName);
                mat.Bitmap.Save(fileFullName);
                //this.progressBar1.Value = i / this.FreamCount;
                Invoke(acUpUi, i + 1);
            }
        }

        private void ExtractEverySecond()
        {
            var length = (this.FreamCount.ToString()).Length;
            if (!Directory.Exists(this.txtImagePath.Text))
                Directory.CreateDirectory(this.txtImagePath.Text);
            var currentFreamIndex = 0;

            var isEnd = false;
            var savedCount = 1;
            while (!isEnd)
            {
                if (currentFreamIndex > this.FreamCount)
                {
                    currentFreamIndex = this.FreamCount;
                    isEnd = true;
                }
                if (acUpUi != null)
                    Invoke(acUpUi, savedCount);


                this.capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, currentFreamIndex);
                var mat = this.capture.QueryFrame();
                var fileName = (currentFreamIndex + 1).ToString().PadLeft(length, '0') + ".jpg";
                var fileFullName = Path.Combine(this.txtImagePath.Text, fileName);
                mat.Bitmap.Save(fileFullName);
                //this.progressBar1.Value = i / this.FreamCount;
                currentFreamIndex += this.FPS;
                savedCount++;
            }
        }

        private void Extract()
        {
            if (this.rbFream.Checked) ExtractEveryFream();
            else if (this.rbSecond.Checked) ExtractEverySecond();
            var dialogResult = MessageBox.Show("图片分解完成，是否打在图片目录？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                
            }
        }

        private void ComputNeedSaveCount()
        {
            if (this.rbFream.Checked) this.NeedSaveCount = this.FreamCount;
            else if (this.rbSecond.Checked)
            {
                if ((this.FreamCount % this.FPS) == 0)
                    this.NeedSaveCount = (this.FreamCount / this.FPS);
                else
                    this.NeedSaveCount = (this.FreamCount / this.FPS) + 1;
            }
            this.labelNeedSave.Text = this.NeedSaveCount.ToString() + "张";
            this.progressBar1.Maximum = this.NeedSaveCount;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.ExtractTask.Start();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void rbFream_CheckedChanged(object sender, EventArgs e)
        {
            ComputNeedSaveCount();
        }

    }
}
