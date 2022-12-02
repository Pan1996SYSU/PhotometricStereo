using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using NumSharp.Backends.Unmanaged;
using NumSharp;

namespace PSAppTools
{
    public partial class Form : System.Windows.Forms.Form
    {
        // UI参数
        string CalibInputPath = null;
        string CalibOutputPath = null;
        string InputDirPath = null;
        string OutputDirPath = null;
        string LightFilePath = null;
        int ThreadNum;
        List<string> ImgExtList = new List<string>() { "保持", ".bmp", ".gif", ".jpeg", ".jpg", ".pbm", ".png", ".tif", ".tiff" };
        // 逻辑参数
        int CalibStep = 0;
        int ScaleHeight = 0;
        int ScaleWidth = 0;
        List<List<int>> PointsKeeper = new List<List<int>>();
        List<int> center;
        int radius;
        NDArray EyeVec = np.array(0, 0, 1);
        List<NDArray> LightVecList = new List<NDArray>();
        List<Task> FeedList = new List<Task>();
        public Form()
        {
            InitializeComponent();
            setOutExtCombText();
            ThreadNum = getProcessorCount();
            this.procNumSpin.Value = ThreadNum;
            this.procNumSpin.Maximum = ThreadNum;
            this.procNumSpin.Minimum = 1;
            this.calibPBar.Maximum = 5;
            this.calibBackBtn.Enabled = false;
            /*
            this.CalibInputPath = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例";
            this.CalibOutputPath = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例";
            this.calibInEdit.Text = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例";
            this.calibOutEdit.Text = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例";
            this.InputDirPath = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\合成示例";
            this.inDirEdit.Text = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\合成示例";
            this.outDirEdit.Text = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\合成示例";
            this.OutputDirPath = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\合成示例";
            this.LightFilePath = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例\calib_result.txt";
            this.lightEdit.Text = @"D:\桌面\YoutuPSTool\YoutuPSTool\data\标定示例\calib_result.txt";
            */
        }
        public int getProcessorCount()
        {
            int ThreadNum;
            try
            {
                ThreadNum = Environment.ProcessorCount;
            }
            catch
            {
                ThreadNum = 1;
            }
            return ThreadNum;
        }
        public void setOutExtCombText()
        {
            for (int i = 0; i < ImgExtList.Count; i++)
            {
                this.outExtComb.Items.Add(ImgExtList[i]);
            }
            this.outExtComb.SelectedIndex = 0;
        }
        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void calibBackBtn_Click(object sender, EventArgs e)
        {
            this.CalibStep = 0;
            this.PointsKeeper.Clear();
            this.LightVecList.Clear();

            this.calibGoBtn.Text = "开始";
            this.calibBackBtn.Enabled = false;
            this.calibPBar.Value = 0;
        }

        private void openCalibInBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "选择标定图片文件所在文件夹目录";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                this.CalibInputPath = folder.SelectedPath;
                this.calibInEdit.Text = this.CalibInputPath;
            }
        }

        private void openCalibOutBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "选择输出光源标定txt文件夹目录";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                this.CalibOutputPath = folder.SelectedPath;
                this.calibOutEdit.Text = this.CalibOutputPath;
            }
        }

        private void openInDirBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "选择输入合成图片所在文件夹目录";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                this.InputDirPath = folder.SelectedPath;
                this.inDirEdit.Text = this.InputDirPath;
            }
        }

        private void openOutDirBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.Description = "选择输出合成图片文件夹目录";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                this.OutputDirPath = folder.SelectedPath;
                this.outDirEdit.Text = this.OutputDirPath;
            }
        }

        private void openLightBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;      //该值确定是否可以选择多个文件
            dialog.Title = "选择光源参数文件";     //弹窗的标题
            dialog.Filter = "MicroSoft txt文件(*.txt)|*.txt|所有文件(*.*)|*.*";       //筛选文件
            dialog.ShowHelp = true;     //是否显示“帮助”按钮
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.LightFilePath = dialog.FileName;
                this.lightEdit.Text = this.LightFilePath;
            }
        }

        private void calibGoBtn_Click(object sender, EventArgs e)
        {
            if (this.CalibInputPath == null)
            {
                MessageBox.Show("请选择输入路径！", "警告");
                return;
            }
            if (this.CalibOutputPath == null)
            {
                MessageBox.Show("请选择输出路径！", "警告");
                return;
            }
            var CalibFiles = Directory.GetFiles(this.CalibInputPath, ".", SearchOption.AllDirectories).Where(s => s.EndsWith(".bmp") || s.EndsWith(".gif")
            || s.EndsWith(".jpeg") || s.EndsWith(".jpg") || s.EndsWith(".pbm") || s.EndsWith(".png") || s.EndsWith(".tif") || s.EndsWith(".tiff"));
            if (CalibFiles.Count() != 4)
            {
                return;
            }
            if (this.CalibStep == 5)
            {
                var SavePath = Path.Combine(this.CalibOutputPath, "calib_result.txt");
                StreamWriter sw = new StreamWriter(SavePath, false);
                // 光源信息列表待处理
                for (int i = 0; i < this.LightVecList.Count(); i++)
                {
                    sw.WriteLine("{0}\t{1}\t{2}", (double)this.LightVecList[i][0], (double)this.LightVecList[i][1], (double)this.LightVecList[i][2]);
                }
                sw.Close();
                MessageBox.Show("恭喜！标定已完成，结果保存在输出文件夹！", "完成");
                this.LightFilePath = SavePath;
                this.lightEdit.Text = SavePath;
                return;
            }
            // 这个区域不包括任务栏
            Rectangle ScreenArea = Screen.GetWorkingArea(this);
            int ScreenWidth = ScreenArea.Width;
            int ScreenHeight = ScreenArea.Height;
            if (this.CalibStep == 0)
            {
                this.PointsKeeper.Clear();
                Mat Img = new Mat(CalibFiles.ElementAt(0), ImreadModes.Color);
                int height = Img.Height;
                int width = Img.Width;
                if (ScreenHeight / ScreenWidth > height / width)
                {
                    this.ScaleWidth = (int)(ScreenWidth * 0.8);
                    this.ScaleHeight = (int)(ScreenWidth * 0.8 * height / width);
                }
                else
                {
                    this.ScaleWidth = (int)(ScreenHeight * 0.8 * width / height);
                    this.ScaleHeight = (int)(ScreenHeight * 0.8);
                }
                openWindow("Mark 3 points on the circumference", Img, this.ScaleWidth, this.ScaleHeight);
                if (this.PointsKeeper.Count < 3) return;
                (var center, var radius) = get_circle();
                if (center == null) return;
                this.center = center;
                this.radius = radius;
            }
            else
            {
                this.PointsKeeper.Clear();
                Mat Img = new Mat(CalibFiles.ElementAt(this.CalibStep - 1), ImreadModes.Color);
                openWindow("Mark the reflective center", Img, this.ScaleWidth, this.ScaleHeight);
                if (this.PointsKeeper.Count < 1) return;
                double mx = this.PointsKeeper[this.PointsKeeper.Count - 1][0];
                double my = this.PointsKeeper[this.PointsKeeper.Count - 1][1];
                double x = (mx - (double)this.center[0]) / (double)this.radius;
                double y = -(my - (double)this.center[1]) / (double)this.radius;
                double z = Math.Sqrt(1.0 - Math.Pow(x, 2) - Math.Pow(y, 2));
                NDArray normal = np.array(x, y, z);
                NDArray LightVec = 2.0 * z * normal - this.EyeVec;
                LightVec = (normal + LightVec) / 2;
                double norm = np.sqrt(LightVec[0] * LightVec[0] + LightVec[1] * LightVec[1] + LightVec[2] * LightVec[2]);
                LightVec /= norm;
                this.LightVecList.Add(LightVec);
                this.calibBackBtn.Enabled = true;
            }

            this.CalibStep++;
            if (this.CalibStep != 5) this.calibGoBtn.Text = "下一步";
            else this.calibGoBtn.Text = "完成";
            this.calibPBar.Value = this.CalibStep;
        }
        public (List<int>, int) get_circle()
        {
            List<int> p1, p2, p3;
            p1 = this.PointsKeeper[this.PointsKeeper.Count() - 3];
            p2 = this.PointsKeeper[this.PointsKeeper.Count() - 2];
            p3 = this.PointsKeeper[this.PointsKeeper.Count() - 1];
            double temp = p2[0] * p2[0] + p2[1] * p2[1];
            double bc = (p1[0] * p1[0] + p1[1] * p1[1] - temp) / 2;
            double cd = (temp - p3[0] * p3[0] - p3[1] * p3[1]) / 2;
            double det = (p1[0] - p2[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p2[1]);

            if (Math.Abs(det) < 1.0e-6)
            {
                return (null, -1);
            }
            double cx = (bc * (p2[1] - p3[1]) - cd * (p1[1] - p2[1])) / det;
            double cy = ((p1[0] - p2[0]) * cd - (p2[0] - p3[0]) * bc) / det;
            double radius = Math.Sqrt(Math.Pow((cx - p1[0]), 2) + Math.Pow((cy - p1[1]), 2));
            int x = (int)(cx);
            int y = (int)(cy);
            int r = (int)(radius);
            List<int> center = new List<int> { x, y };
            return (center, r);
        }
        public void openWindow(string title, Mat img, int width, int height)
        {
            string winTitle = title;
            Cv2.NamedWindow(winTitle, WindowFlags.KeepRatio);
            Cv2.ResizeWindow(winTitle, width, height);
            Cv2.ImShow(winTitle, img);
            IntPtr inp = img.CvPtr;
            Cv2.SetMouseCallback(winTitle, mouseCallback, inp);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();
        }

        private void mouseCallback(MouseEventTypes @event, int x, int y, MouseEventFlags flags, IntPtr userData)
        {
            if (@event == MouseEventTypes.LButtonDown)
            {
                Mat img = new Mat(userData);
                Cv2.Circle(img, x, y, 10, new Scalar(0, 0, 255), -1);
                if (this.CalibStep == 0)
                {
                    Cv2.ImShow("Mark 3 points on the circumference", img);
                }
                else
                {
                    Cv2.ImShow("Mark the reflective center", img);
                }
                List<int> points = new List<int>() { x, y };
                this.PointsKeeper.Add(points);

            }
        }

        private void genBtn_Click(object sender, EventArgs e)
        {
            if (this.InputDirPath == null)
            {
                MessageBox.Show("请选择输入路径！", "警告");
                return;
            }
            if (this.OutputDirPath == null)
            {
                MessageBox.Show("请选择输出路径！", "警告");
                return;
            }
            if (this.LightFilePath == null)
            {
                MessageBox.Show("请选择光源文件路径！", "警告");
                return;
            }
            this.numProBar.Value = 0;
            var InputDirPathList = Directory.GetFiles(this.InputDirPath, ".", SearchOption.AllDirectories).Where(s => s.EndsWith(".bmp") || s.EndsWith(".gif")
            || s.EndsWith(".jpeg") || s.EndsWith(".jpg") || s.EndsWith(".pbm") || s.EndsWith(".png") || s.EndsWith(".tif") || s.EndsWith(".tiff"));
            Dictionary<string, List<string>> InputDirPathDict;
            InputDirPathDict = new Dictionary<string, List<string>>();
            var pattern = @".+(?=_L\d_)";
            for (int i = 0; i < InputDirPathList.Count(); i++)
            {
                var InputDir = InputDirPathList.ElementAt(i);
                var stem = Path.GetFileName(InputDir);
                var suffix = Path.GetExtension(InputDir);
                if (stem.Contains("_L0") || stem.Contains("_L5")) continue;
                if (!stem.Contains("_L")) continue;
                if (!stem.Contains(".bmp") && !stem.Contains(".jpg") && !stem.Contains(".jpeg") && !stem.Contains(".png")) continue;
                Match match = Regex.Match(stem, pattern);
                if (InputDirPathDict.ContainsKey(match.ToString()))
                {
                    InputDirPathDict[match.ToString()].Add(InputDir);
                }
                else
                {
                    InputDirPathDict[match.ToString()] = new List<string>() { InputDir };
                }
            }
            var L = loadTxt(this.LightFilePath);
            for (int j = 0; j < InputDirPathDict.Count; j++)
            {
                Task t = new Task();
                t.Light = L;
                t.cpath = InputDirPathDict.Keys.ElementAt(j);
                t.paths = InputDirPathDict[t.cpath];
                t.outExt = this.outExtComb.Text;
                this.FeedList.Add(t);
            }
            this.numProBar.Maximum = this.FeedList.Count;
            ThreadPool.SetMaxThreads((int)this.procNumSpin.Value, (int)this.procNumSpin.Value);
            for (int k = 0; k < this.FeedList.Count; k++)
            {
                ThreadPool.QueueUserWorkItem(GenerateSingle, this.FeedList[k]);
            }
            this.numProBar.Value = this.numProBar.Maximum;
        }
        private void GenerateSingle(object task)
        {
            try
            {
                Task t = (Task)task;
                var L = t.Light;
                var cpath = t.cpath;
                var paths = t.paths;
                var outExt = t.outExt;
                var (M, h, w) = loadImages(paths);
                var (normal, albedo) = SolvePS(L, M, h, w);

                if (outExt == "保持")
                {
                    outExt = Path.GetExtension(paths[0]);
                }
                string parent = Path.GetDirectoryName(paths[0]);
                string NormalPath = parent + '/' + cpath + "_L0_normal" + outExt;
                string AlbedoPath = parent + '/' + cpath + "_L0_albedo" + outExt;
                Cv2.ImWrite(NormalPath, normal);
                Cv2.ImWrite(AlbedoPath, albedo);
                if (this.numProBar.Value < this.numProBar.Maximum) this.numProBar.Value++;
            }
            catch
            {
                if (this.numProBar.Value < this.numProBar.Maximum) this.numProBar.Value++;
            }
        }
        public (Mat, Mat) SolvePS(double[,] L, NDArray M, int h, int w)
        {
           
            NDArray ndarrL = np.array(L);
            var ndarrLT = ndarrL.T;
            var A = np.matmul(ndarrLT, ndarrL);
            var b = np.matmul(ndarrLT, M);
            double[,] B = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for(int j = 0; j < 3; j++)
                {
                    B[i, j] = A[i, j];
                }
            }
            var BT = MatInverse(B);
            var N = np.matmul(BT, b);
            var a = np.square(N)[0] + np.square(N)[1] + np.square(N)[2];
            var R = np.sqrt(a);
            R[R == 0] = 1;
            N = N / R;
            N = N.T;

            N = N.reshape(h, w, 3);
            var dimB = N[":, :, 0"].copy();
            var dimR = N[":, :, 2"].copy();
            N[":, :, 0"] = dimR;
            N[":, :, 2"] = dimB;
            N = (N + 1.0) / 2.0;
            N = N * 255;
            N = N.astype(np.uint8);
            Mat n = new Mat(h, w, MatType.CV_8UC3, (Array)N);

            R = R.reshape(h, w);
            R = R / R.max();
            R = R * 255;
            R = R.astype(np.uint8);
            Mat r = new Mat(h, w, MatType.CV_8UC1, (Array)R);

            return (n, r);
        }
        public (NDArray, int, int) loadImages(List<string> ImgPaths)
        {
            if (ImgPaths.Count != 4) return (null, -1, -1);
            Mat Img1 = new Mat(ImgPaths.ElementAt(0), ImreadModes.Grayscale);
            Mat Img2 = new Mat(ImgPaths.ElementAt(1), ImreadModes.Grayscale);
            Mat Img3 = new Mat(ImgPaths.ElementAt(2), ImreadModes.Grayscale);
            Mat Img4 = new Mat(ImgPaths.ElementAt(3), ImreadModes.Grayscale);
            var NDArrayImg1 = ToNDArray(Img1);
            var NDArrayImg2 = ToNDArray(Img2);
            var NDArrayImg3 = ToNDArray(Img3);
            var NDArrayImg4 = ToNDArray(Img4);
            var s = NDArrayImg1.shape;
            int height = s[0];
            int width = s[1];
            NDArrayImg1 = NDArrayImg1.ravel();
            NDArrayImg2 = NDArrayImg2.ravel();
            NDArrayImg3 = NDArrayImg3.ravel();
            NDArrayImg4 = NDArrayImg4.ravel();
            NDArray M = np.vstack(NDArrayImg1, NDArrayImg2, NDArrayImg3, NDArrayImg4);
            return (M, height, width);
        }
        private double[,] loadTxt(string path)
        {
            double[,] data = new double[4, 3];
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    int j = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        int k = 0;
                        string[] s = line.Split(' ');
                        if (s.Length == 1)
                        {
                            s = line.Split('\t');
                        }
                        for (int i = 0; i < s.Length; i++)
                        {
                            if (s[i] != "")
                            {
                                var ss = s[i];
                                double d = System.Convert.ToDouble(s[i]);
                                data[j, k] = d;
                                k++;
                            }
                        }
                        j++;
                    }
                }
                return data;
            }
            catch
            {
                return null;
            }
        }
        public struct Task
        {
            public double[,] Light;
            public string cpath;
            public List<string> paths;
            public string outExt;
        }
        public static NDArray ToNDArray(Mat mat)
        {
            var matType = mat.Type();
            var channels = mat.Channels();
            var size = mat.Rows * mat.Cols * channels;
            var shape = channels == 1 ? new Shape(mat.Rows, mat.Cols) : new Shape(mat.Rows, mat.Cols, channels);
            if (matType == MatType.CV_32SC1 || matType == MatType.CV_32SC2)
            {
                var managedArray = new int[size];
                Marshal.Copy(mat.Data, managedArray, 0, size);
                var aslice = ArraySlice.FromArray(managedArray);
                return new NDArray(aslice, shape);
            }
            if (matType == MatType.CV_32FC1)
            {
                var managedArray = new float[size];
                Marshal.Copy(mat.Data, managedArray, 0, size);
                var aslice = ArraySlice.FromArray(managedArray);
                return new NDArray(aslice, shape);
            }
            if (matType == MatType.CV_64FC1)
            {
                var managedArray = new double[size];
                Marshal.Copy(mat.Data, managedArray, 0, size);
                var aslice = ArraySlice.FromArray(managedArray);
                return new NDArray(aslice, shape);
            }
            if (matType == MatType.CV_8UC1 || matType == MatType.CV_8UC3 || matType == MatType.CV_8UC4)
            {
                var managedArray = new byte[size];
                Marshal.Copy(mat.Data, managedArray, 0, size);
                var aslice = ArraySlice.FromArray(managedArray);
                return new NDArray(aslice, shape);
            }

            throw new Exception($"mat data type = {matType} is not supported");
        }

        public double[,] MatInverse(double[,] n) //矩阵求逆函数  元组法改进  
        {
            //前提判断： 是否为方阵  是否可逆
            int m = n.GetLength(0);
            double[,] q = new double[m, m]; //求逆结果
            int i, j, k;//计数君
            double u, temp;//临时变量

            //初始单位阵
            for (i = 0; i < m; i++)
                for (j = 0; j <= m - 1; j++)
                    q[i, j] = (i == j) ? 1 : 0;

            /// 求左下
            ///
            for (i = 0; i <= m - 2; i++)
            {
                //提取该行的主对角线元素
                u = n[i, i];   //可能为0
                if (u == 0)  //为0 时，在下方搜索一行不为0的行并交换
                {
                    for (i = 0; i < m; i++)
                    {
                        k = i;
                        for (j = i + 1; j < m; j++)
                        {
                            if (n[j, i] != 0) //不为0的元素
                            {
                                k = j;
                                break;
                            }
                        }

                        if (k != i) //如果没有发生交换： 情况1 下方元素也全是0
                        {
                            for (j = 0; j < m; j++)
                            {
                                //行交换
                                temp = n[i, j];
                                n[i, j] = n[k, j];
                                n[k, j] = temp;
                                //伴随交换
                                temp = q[i, j];
                                q[i, j] = q[k, j];
                                q[k, j] = temp;
                            }
                        }
                        else //满足条件1 弹窗提示
                            MessageBox.Show("不可逆矩阵", "ERROR");

                    }
                }

                for (j = 0; j < m; j++)//该行除以主对角线元素的值 使主对角线元素为1  
                {
                    n[i, j] = n[i, j] / u;   //分母不为0
                    q[i, j] = q[i, j] / u;  //伴随矩阵
                }

                for (k = i + 1; k < m; k++)  //下方的每一行减去  该行的倍数
                {
                    u = n[k, i];   //下方的某一行的主对角线元素
                    for (j = 0; j < m; j++)
                    {
                        n[k, j] = n[k, j] - u * n[i, j];  //下方的每一行减去该行的倍数  使左下角矩阵化为0
                        q[k, j] = q[k, j] - u * q[i, j];  //左下伴随矩阵
                    }
                }
            }


            u = n[m - 1, m - 1];  //最后一行最后一个元素

            if (u == 0) //条件2 初步计算后最后一行全是0 在只上步骤中没有计算最后一行，所以可能会遗漏
                MessageBox.Show("不可逆矩阵", "ERROR");
            n[m - 1, m - 1] = 1;
            for (j = 0; j < m; j++)
            {
                q[m - 1, j] = q[m - 1, j] / u;
            }

            // 求右上
            for (i = m - 1; i >= 0; i--)
            {
                for (k = i - 1; k >= 0; k--)
                {
                    u = n[k, i];
                    for (j = 0; j < m; j++)
                    {
                        n[k, j] = n[k, j] - u * n[i, j];
                        q[k, j] = q[k, j] - u * q[i, j];
                    }
                }
            }
            return q;
        }
    }
}
