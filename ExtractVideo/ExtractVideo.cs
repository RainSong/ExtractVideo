using Emgu.CV;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtractVideo
{
    public partial class FormExtractVideo : Form
    {
        #region 定义字段
        private FileInfo fileInfo = null;
        private Capture capture = null;
        /// <summary>
        /// 总帧数
        /// </summary>
        private int FreamCount = 0;
        /// <summary>
        /// 帧高度
        /// </summary>
        private int FreamHeight = 0;
        /// <summary>
        /// 帧宽度
        /// </summary>
        private int FreamWidth = 0;
        /// <summary>
        /// 帧速
        /// </summary>
        private int FPS = 0;
        /// <summary>
        /// 需要保存的帧数
        /// </summary>
        private int NeedSaveCount = 0;
        /// <summary>
        /// 文件保存完成后更新窗体空间的委托
        /// </summary>
        private Action<int> acUpUi = null;
        #endregion
        #region 定义方法
        public FormExtractVideo()
        {
            InitializeComponent();
            this.labelFileSize.Text =
                this.labelVideoLength.Text =
                this.labelFPS.Text =
                this.labelFreamCount.Text =
                this.labelVideoSize.Text =
                this.labelNeedSave.Text =
                this.labelSavedCount.Text = string.Empty;
            this.openFileDialog1.Filter = "MP4|*.mp4|AVI|*.avi|RMVB|*.rmvb|MKV|*.mkv";

            acUpUi = (savedCount) =>
            {
                this.labelSavedCount.Text = savedCount.ToString() + "张";
                this.progressBar1.Value = savedCount;
            };
        }
        /// <summary>
        /// 获取图片保存的路径
        /// </summary>
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
        /// <summary>
        /// 显示视频信息
        /// </summary>
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
        /// <summary>
        /// 每帧都保存未一张图片
        /// </summary>
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
        /// <summary>
        /// 每秒保存一张图片
        /// </summary>
        private void ExtractEverySecond()
        {
            var length = (this.FreamCount.ToString()).Length;
            
            var currentFreamIndex = 0;

            var isEnd = false;
            var savedCount = 1;
            while (!isEnd)
            {
                if (currentFreamIndex > this.FreamCount)
                {
                    currentFreamIndex = this.FreamCount - 1;
                    isEnd = true;
                }
                if (acUpUi != null)
                    Invoke(acUpUi, savedCount);


                this.capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, currentFreamIndex);
                var mat = this.capture.QueryFrame();
                if (mat == null) continue;
                var fileName = (savedCount + 1).ToString().PadLeft(length, '0') + ".jpg";
                var fileFullName = Path.Combine(this.txtImagePath.Text, fileName);
                mat.Bitmap.Save(fileFullName);
                //this.progressBar1.Value = i / this.FreamCount;
                currentFreamIndex += this.FPS;
                savedCount++;
            }
        }

        private void CreateFloder(string path)
        {
            try
            {
                if (!Directory.Exists(this.txtImagePath.Text))
                    Directory.CreateDirectory(this.txtImagePath.Text);
            }

            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 分割保存
        /// </summary>
        private void Extract()
        {
            if (this.rbFream.Checked) ExtractEveryFream();
            else if (this.rbSecond.Checked) ExtractEverySecond();
            var dialogResult = MessageBox.Show("图片分解完成，是否打在图片目录？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                Process.Start("explorer.exe", this.txtImagePath.Text);
            }
        }
        /// <summary>
        /// 计算并显示要保存的图片数量
        /// </summary>
        private void ComputNeedSaveCount()
        {
            if (this.rbFream.Checked) this.NeedSaveCount = this.FreamCount;
            else if (this.rbSecond.Checked)
            {
                if ((this.FreamCount % this.FPS) == 0)
                    this.NeedSaveCount = (this.FreamCount / this.FPS);
                else
                {
                    var count = (1.0 * this.FreamCount / this.FPS) + 1;
                    var intCount = (int)count;
                    if (count > intCount)
                    {
                        intCount++;
                    }
                    this.NeedSaveCount = intCount;
                }
            }
            this.labelNeedSave.Text = this.NeedSaveCount.ToString() + "张";
            this.progressBar1.Maximum = this.NeedSaveCount;
        }

        private void ShowMessage(string title, string message)
        {
            
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
        #region 定义事件处理
        /// <summary>
        /// 浏览按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowser_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtFile.Text = this.openFileDialog1.FileName;
                this.fileInfo = new FileInfo(this.txtFile.Text);
                this.capture = new Capture(this.txtFile.Text);

                this.progressBar1.Value = 0;
                this.rbFream.Checked = true;
                this.rbSecond.Checked = false;
                this.labelSavedCount.Text = string.Empty;

                ShowVideoInfo();
                GetImageDir();
                ComputNeedSaveCount();
            }
        }
        /// <summary>
        /// 分割按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExtract_Click(object sender, EventArgs e)
        {
            var task = new Task(new Action(Extract));
            task.Start();
        }

        private void rbFream_CheckedChanged(object sender, EventArgs e)
        {
            ComputNeedSaveCount();
        }
        #endregion
    }
}
