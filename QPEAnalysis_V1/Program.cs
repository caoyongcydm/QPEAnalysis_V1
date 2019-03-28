using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CaoYong.DataType;
using CaoYong.DataProc;
using CaoYong.SimpleLog;
using System.IO;

namespace QPEAnalysis_V1
{
    class Program
    {
        static void Main(string[] args)
        {
            ///////////////////////////////////////////////////////////////////////////////
            //介绍性开头
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("+++    Hour QPE Analysis V1.0                      +++");
            Console.WriteLine("+++++  Supproted By CaoYong 2018.11.12       +++++++++");
            Console.WriteLine("+++++  QQ: 403637605                         +++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            //打开计时器
            Stopwatch sw = new Stopwatch();  //创建计时器
            sw.Start();                      //开启计数器
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            string appDir = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;                                                                                  //程序启动文件夹
            //string appDir = @"E:/HourQPE_V1/";
            System.Environment.CurrentDirectory = appDir;                                                                                                                     //设置shell所在文件夹
            string logPath = appDir + @"log/" + DateTime.Now.Year.ToString("d4") + DateTime.Now.Month.ToString("d2") + DateTime.Now.Day.ToString("d2") + @".txt";             //日志文件夹地址
            Log simpleLog = new Log(logPath);                                                                                                                                 //建立log对象，用于日志的记录                                                                                                                               //输出站点ID计算信息
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            try
            {
                ///////////////////////////////////////////////////////////////////////////
                //时间处理(北京时)
                DateTime dtNow = DateTime.Now;                         //程序启动时间（北京时）                          
                if (args.Length == 0)                                  //实时运算处理
                {
                    dtNow = DateTime.Now;
                    //dtNow = new DateTime(2018, 11, 12, 14, 00, 00);
                }
                else if (args.Length == 1 && args[0].Length == 12)     //指定日期运算处理
                {
                    try
                    {
                        int argYr = int.Parse(args[0].Substring(0, 4));
                        int argMo = int.Parse(args[0].Substring(4, 2));
                        int argDy = int.Parse(args[0].Substring(6, 2));
                        int argHr = int.Parse(args[0].Substring(8, 2));
                        int argMn = int.Parse(args[0].Substring(10, 2));
                        dtNow = new DateTime(argYr, argMo, argDy, argHr, argMn, 0);
                    }
                    catch(Exception ex)
                    {
                        simpleLog.WriteError(ex.Message, 1);
                        simpleLog.WriteError("date args content is not right!", 1);
                        return;
                    }
                }
                else
                {
                    simpleLog.WriteError("date args is not right!", 1);
                    return;
                }
                ///////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////
                //读取控制文件
                string paraFilePath = appDir + @"para/para.ini";                      //控制文件地址
                string inputSamplePath = null;                                        //自动站站点数据输入地址
                string outputSamplePath = null;                                       //QPE分析格点输出地址

                if (!File.Exists(paraFilePath))
                {
                    simpleLog.WriteError("para file is not exist!", 1);
                    return;
                }
                else
                {
                    FileStream paraFS = new FileStream(paraFilePath, FileMode.Open, FileAccess.Read);
                    StreamReader paraSR = new StreamReader(paraFS, Encoding.GetEncoding("gb2312"));
                    {
                        try
                        {
                            string strTmp = paraSR.ReadLine();
                            string[] strArrayTmp = strTmp.Split(new char[] { '=' });
                            inputSamplePath = strArrayTmp[1].Trim();                           //获取输入地址
                            strTmp = paraSR.ReadLine();
                            strArrayTmp = strTmp.Split(new char[] { '=' });
                            outputSamplePath = strArrayTmp[1].Trim();                          //获取输出地址
                        }
                        catch
                        {
                            simpleLog.WriteError("para content is not right!", 1);
                            return;
                        }
                    }
                    paraSR.Close();
                    paraFS.Close();
                }
                ///////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////
                string stainfoPath = appDir + @"info/sta.info";                     //此处以完整第3类数据站点数据为站点信息                 
                DateTime dtStart = dtNow.AddHours(-6);                
                DateTime dtEnd = dtNow;
                DateTime dtCurrent = dtStart;
                while (DateTime.Compare(dtCurrent, dtEnd) <= 0)                    //冗余设置，分析之前6小时至当前的结果
                {
                    DateTime dtInput = dtCurrent.AddHours(-8);                     //原始数据为世界时    
                    DateTime dtOutput = dtCurrent.AddHours(-8);                    //输出结果为世界时
                    string inputFilePath = StringProcess.DateReplace(inputSamplePath, dtInput, 000);
                    string outputFilePath = StringProcess.DateReplace(outputSamplePath, dtOutput, 000);

                    if (File.Exists(outputFilePath + ".m4"))  //存在输出文件则不进行分析
                    {
                        dtCurrent = dtCurrent.AddHours(1);
                        continue;
                    }
                    Console.WriteLine("inputPath=" + inputFilePath);
                    Console.WriteLine("OutputPath=" + outputFilePath);
                    ScatterData sdInputData = new ScatterData(stainfoPath);
                    sdInputData.ClearToNum(0.0);
                    sdInputData.ReadValFromMicaps3(inputFilePath);
                    sdInputData.ClearToNumGreaterThan(0.0, 100.0);                 //简单质量控制
                    sdInputData.ClearToNumLessThan(0.0, 0.10);                     //简单质量控制，注意冬季情况的适用性
                    GridData gdOutputData = new GridData(70.0, 140.0, 0.0, 60.0, 0.05, 0.05);
                    gdOutputData.ClearToNum(0.0);
                    gdOutputData = SpatialAnalisis.GressManInterpolationForRain(sdInputData, gdOutputData, 0.2, 2.0, new double[] { 1.0, 0.8, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1 }); //分析25-5公里尺度
                    gdOutputData.WriteFloatValToBin(outputFilePath + ".bin");
                    string strHeader = StringProcess.DateReplace(@"diamond 4 YYYYMMDDHH00_01hour_QPE YYYY MM DD HH NN 000 0.05 0.05 70.0 140.0 0.0 60.0 1401 1201 2.0 -2.0 20.0 1 00", dtOutput, 000);
                    gdOutputData.WriteValToMicaps4(outputFilePath + ".m4", strHeader);


                    ///////////////////////////////////////////////////////////////////////////////
                    //检验程序
                    ScatterData sdFromQPE = new ScatterData(stainfoPath);
                    sdFromQPE.ClearToNum(0.0);
                    sdFromQPE.BilinearInterpolationFromGridData(gdOutputData);
                    double varError = 0.0;
                    for (int n = 0; n < sdFromQPE.StaData.Length; n++)
                    {
                        varError = varError + (sdFromQPE.StaData[n].Value - sdInputData.StaData[n].Value) * (sdFromQPE.StaData[n].Value - sdInputData.StaData[n].Value);
                    }
                    varError = varError / sdFromQPE.StaData.Length;
                    varError = Math.Sqrt(varError);
                    simpleLog.WriteInfo("Error is " + varError, 1);
                    ///////////////////////////////////////////////////////////////////////////////

                    dtCurrent = dtCurrent.AddHours(1);
                }
                ///////////////////////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                simpleLog.WriteError(ex.Message, 1);
            }
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            sw.Stop();
            simpleLog.WriteInfo("time elasped: " + sw.Elapsed, 1);
            ///////////////////////////////////////////////////////////////////////////////
        }
    }
}
