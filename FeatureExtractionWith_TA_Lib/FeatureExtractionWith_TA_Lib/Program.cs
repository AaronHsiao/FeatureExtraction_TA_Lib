using System;
using System.IO;
using System.Collections.Generic;
using TicTacTec.TA.Library;

namespace FeatureExtractionWith_TA_Lib
{
	class MainClass
	{	
		public static int totalRows = 0;	//儲存時用 列是每分鐘資料 很多筆
		public static int totalCols = 0;    //儲存時用 欄是特徵數量 165
		public static int std_Mins = 295;  //每天資料筆數不一樣，為避免計算上的麻煩，統一只拿295筆，小於295者，直接忽略，大於者強迫只算295
		public static int days = 0;

		public static void Main (string[] args)
		{	
			String str_File = "2013";
			//String str_File = "2001-2012";

			List<int[]> list_RawData = new List<int[]> ();	//原始資料
			List<int> list_Date = new List<int> ();		//每個交易日 取聯集 20010102 20010103....
			List<int> list_OneDayRecords = new List<int> (); //每個交易日有幾筆資料(分鐘)

			int counter = 0;	//記錄當前 讀取哪一筆資料 76萬筆 就加到76萬 不會重置

			//技術指標的頻率
			int[] freq_TA = new int[]{100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5};	
			int[] freq_Son = new int[]{95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 2};
			int[] freq_Mom = new int[]{100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5};

			int[] previous_Return = new int[]{10, 5, 1};

			/* list_Feature最大張的二維List，讓Feature(包含Target)可以彈性增加
			 * 					  第一天的165分鐘	第二天的165分鐘	弟三天的165分鐘 ....	
			 *  10-Return (過去)
			 *   5-Return (過去)
	 		 *   1-Return (過去)
			 * 100-RSI
			 * 100-ATR
			 * 100-W%R
			 * 100-ADX
			 * 100-SMA
			 * 100-EMA
			 * 100-WMA
			 * 100-MFI
			 *  KD-(9,3,3)
			 * MACD-(12,26,9)
			 *  30-Return (未來)
			 * */
			List<List<double>> list_Feature = new List<List<double>> ();

			List<List<double>> list_RSI = new List<List<double>> ();
			List<List<double>> list_ATR = new List<List<double>> ();
			List<List<double>> list_WillR = new List<List<double>> ();
			List<List<double>> list_ADX = new List<List<double>> ();
			List<List<double>> list_SMA = new List<List<double>> ();
			List<List<double>> list_EMA = new List<List<double>> ();
			List<List<double>> list_WMA = new List<List<double>> ();
			List<List<double>> list_MFI = new List<List<double>> ();
			List<double> list_KD = new List<double> ();
			List<double> list_MACD = new List<double> ();

			List<double> list_fReturn_10 = new List<double>();	//10分前的報酬率
			List<double> list_fReturn_5 = new List<double>();	//5分前的報酬率
			List<double> list_fReturn_1 = new List<double>();	//1分前的報酬率
			List<double> list_tReturn = new List<double> ();	//未來30分鐘報酬
			int max_TA_Frequency = 0;

			List<double> temp = new List<double>();

			//過去幾分鐘的Return最先加入到大張LIST
			list_Feature.Add (list_fReturn_10);
			list_Feature.Add (list_fReturn_5);
			list_Feature.Add (list_fReturn_1);


			//各種頻率的TA初使化
			for (int h = 0; h < freq_TA.Length; h++) {
				list_RSI.Add (temp);
				temp = new List<double>();
				list_ATR.Add (temp);
				temp = new List<double>();
				list_WillR.Add (temp);
				temp = new List<double>();
				list_ADX.Add (temp);
				temp = new List<double>();
				list_SMA.Add (temp);
				temp = new List<double>();
				list_EMA.Add (temp);
				temp = new List<double>();
				list_WMA.Add (temp);
				temp = new List<double>();
				list_MFI.Add (temp);
				temp = new List<double>();
			}

			//各種頻率的TA加到最大張的LIST
			for (int h = 0; h < freq_TA.Length; h++) {
				list_Feature.Add (list_RSI[h]);
				list_Feature.Add (list_ATR[h]);
				list_Feature.Add (list_WillR[h]);
				list_Feature.Add (list_ADX[h]);
				list_Feature.Add (list_SMA[h]);
				list_Feature.Add (list_EMA[h]);
				list_Feature.Add (list_WMA[h]);
				list_Feature.Add (list_MFI[h]);
			}

			//單一頻率的KD、MACD加入list
			list_Feature.Add (list_KD);
			list_Feature.Add (list_MACD);

			//最後是Target Return加到大張LIST
			list_Feature.Add (list_tReturn);

			//166個Feature當成欄位
			totalCols = list_Feature.Count;

			//Check for TA number = 166
			Console.WriteLine ("list_Feature Count =" + list_Feature.Count);

			//把Excel資料讀取放到list_RawData陣列，日期則放在list_Date
			using(StreamReader sr = new StreamReader (str_File + ".csv"))
			{
				sr.ReadLine ();
				int temp_Date = 0;	//初使為0 識別第一筆資料 之後記錄當前資料之日期(年月日)
				int temp_OneDayRecords = 1;	//記錄每個交易日 有幾筆資料 Ex:297、296、291(分鐘)

				while(!sr.EndOfStream)
				{
					list_RawData.Add (new int[6]);				
					String[] s = sr.ReadLine ().Split(',');
									
					//日期資料只取到日 "yyyy/MM/dd" (從第0個字元開始往後取8)
					s[0] = s [0].Substring (0,8);

					list_RawData[counter][0] = Int32.Parse(s[0]);	//日期
					list_RawData[counter][1] = Int32.Parse(s[1]);	//開
					list_RawData[counter][2] = Int32.Parse(s[2]);	//高
					list_RawData[counter][3] = Int32.Parse(s[3]);	//低
					list_RawData[counter][4] = Int32.Parse(s[4]);	//收
					list_RawData[counter][5] = Int32.Parse(s[5]);	//量

					//第一次日期不用比較 直接加入 此後都要做比對 (有盲點，如果弟一天沒有>295 會資料不整齊)
					if (temp_Date == 0)
						list_Date.Add (list_RawData [counter] [0]);
					//日期有所變動時(20010102 => 20010103) + 判斷資料>=295
					else if (temp_Date != list_RawData [counter] [0]) {
						list_Date.Add (list_RawData [counter] [0]);
						list_OneDayRecords.Add (temp_OneDayRecords);
						temp_OneDayRecords = 1;
					} 
					//日期為同一天時(20050101 == 20050101)
					else {
						temp_OneDayRecords++;

						//最後一筆 不會有日期變動 走不到else if 會少算一筆資料 防堵措施
						if(sr.EndOfStream)
							list_OneDayRecords.Add (temp_OneDayRecords);
					}

					//暫存日期
					temp_Date = list_RawData[counter][0];

					counter++;

				}
					
				sr.Close ();
				sr.Dispose ();
			}
				
			int zz = 0;
			int aa = 0;
			foreach (var item in list_OneDayRecords) {

//				aa++;
//				Console.WriteLine (item);
//
//				if(aa % 100 == 0)
//					Console.ReadLine ();

				if(item >= std_Mins)
					zz++;
				
			}

			Console.WriteLine (">=std_Mins total的數量" + zz);
			Console.ReadLine ();

			int accumulate_Record = 0;	//記錄目前處理到哪一筆資料(分鐘)
			int now = 100;	//看要捨棄前面多少分鐘(t-1) 決定now是第幾分鐘(t)
			int target_Return = 30;

			//看一下每天有幾分鐘
//			int qqq = 0;
//			for (int i = 0; i < list_Date.Count; i++) {
//
//				if (list_OneDayRecords [i] == 297)
//					qqq++;

//				Console.WriteLine ((i+1) + ", " + list_OneDayRecords[i]);
//
//				if (i % 20 == 0)
//					Console.ReadLine ();
//			}

//			Console.WriteLine ("297 total:" + qqq);
//			Console.ReadLine ();

			Console.WriteLine ("Day Count" + list_Date.Count);
			Console.ReadLine ();

			//產出Feature [不跨日,計算當日技術指標]
			for(int i = 0; i<list_Date.Count; i++)
			{
				//為了讓Adaboost接收的Train二維陣列資料 "整齊"
				//只取筆數 >= 295的這些日期的分K資料
				if(list_OneDayRecords[i] < std_Mins)
				{
					//index仍要增加 只是不做計算
					accumulate_Record += list_OneDayRecords[i];
					continue;
				}

				//超過要修正成295筆資料 統一捨棄後面多出來的分鐘
				if(list_OneDayRecords[i] >= std_Mins)
				{
					list_OneDayRecords [i] = std_Mins;
					days++;
				}
					
				//算出各種頻率的投資報酬率(Reutrn)當Feature
				//為了算報酬率每天前面的(t分鐘)被捨棄，且要預測未來，後面30分鐘也捨棄
				for(int j=accumulate_Record+now; j<accumulate_Record+list_OneDayRecords[i]-target_Return; j++)
				{
					list_fReturn_10.Add ((Convert.ToDouble (list_RawData [j] [4]) / Convert.ToDouble (list_RawData [j - previous_Return[0]] [4])) - 1.0);
					list_fReturn_5.Add ((Convert.ToDouble (list_RawData [j] [4]) / Convert.ToDouble (list_RawData [j - previous_Return[1]] [4])) - 1.0);
					list_fReturn_1.Add ((Convert.ToDouble (list_RawData [j] [4]) / Convert.ToDouble (list_RawData [j - previous_Return[2]] [4])) - 1.0);
					list_tReturn.Add ((Convert.ToDouble (list_RawData [j+target_Return][4]) / Convert.ToDouble (list_RawData [j][4])) - 1.0);
				}
					
				double[] inReal = new double[list_OneDayRecords[i]];	//輸入的價格資料(一天)
				double[] inHigh = new double[list_OneDayRecords[i]];	//最高價
				double[] inLow = new double[list_OneDayRecords[i]];		//最低價
				double[] inVolume = new double[list_OneDayRecords[i]];	//量

				double[] outResult = new double[list_OneDayRecords[i]];	//輸出的結果(一天)
				double[] out_MAMom = new double[list_OneDayRecords[i]]; //MA分母
				double[] out_MASon = new double[list_OneDayRecords[i]]; //MA分子
				double[] out_MA = new double[list_OneDayRecords[i]];	//MA結果
				double[] out_K = new double[list_OneDayRecords[i]];		//K值
				double[] out_D = new double[list_OneDayRecords [i]];	//D值
				double[] out_KD = new double[list_OneDayRecords[i]];
				double[] out_MACD = new double[list_OneDayRecords [i]];
				double[] out_MACD_Signal = new double[list_OneDayRecords [i]];
				double[] out_MACD_Hit = new double[list_OneDayRecords [i]];

				//由於技術指標是一次輸入一個陣列，並回傳結果陣列，所以我必須將原始資料Records整理成"一天"，再餵入Function進行處理
				for (int y=0,z=accumulate_Record; z<accumulate_Record+list_OneDayRecords[i]; z++,y++)
				{
					inHigh [y] = list_RawData [z] [2];
					inLow [y] = list_RawData [z] [3];
					inReal [y] = list_RawData [z] [4];
					inVolume [y] = list_RawData [z] [5];
				}

				//確認每天存的資料筆數
				//Console.WriteLine ("PerDay: " + list_OneDayRecords [i]);
				//Console.WriteLine ("InReal: " + inReal.Length);
				//Console.ReadLine ();

				//參數說明詳見 http://ta-lib.org/d_api/d_api.html

				int startIdx = 0;						//從矩陣資料中的第幾個索引開始計算TA
				int endIdx = list_OneDayRecords[i]-1;   //矩陣資料中的第幾個索引結束
				int outBeg = 0;	 	  					//計算此頻率，導致前面多少筆資料沒有值
				int outNbElement = 0; 					//總共算出幾個資料的TA

				max_TA_Frequency = freq_TA [0];

//				int f1_Use = 0;
//				Console.WriteLine ("Test TA Number");



				//算出各種技術指標當Feature
				for (int f = 0; f < freq_TA.Length; f++) 
				{
					//前面故定捨棄100、95、90、85、80...
					startIdx = freq_TA [f];

					/*
						int z = 0;
						foreach(double d in outResult)
						{
							z++;
							Console.WriteLine (z + ": " + d);

							if (z % 100 == 0)
								Console.ReadLine ();
						}
				 	*/

					/*
						關於Function => JustifyRange
						-----------------------------------
						30MA的TA-Lib會自動忽略無法算值之前29筆，
						100MA的TA-Lib自動忽略無法算值之前99筆，
						為了確保每條Feature個數一樣多，所以30MA的結果，還要放棄前面(99-29)筆資料，達到Feature對齊
						Ex:舉例來說每個list有值的範圍隨著Timeframe而不同 (0~196)、(0~201)、(0~206) 要把多餘的資料列刪除
						將資料copy value到對應的List裡面
					*/

					//RSI(EX: 30RSI需要捨棄30筆分鐘資料，Return也是)
					Core.Rsi(startIdx, endIdx, inReal, freq_TA[f], out outBeg, out outNbElement, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_RSI[f], outResult);
					//Console.WriteLine ("RSI, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (outResult, "RSI");

					//ATR 單位沒被除掉 要像MA一樣做百分比正規化處理
					Core.Atr (startIdx, endIdx, inHigh, inLow, inReal, freq_Son[f], out outBeg, out outNbElement, out_MASon);
					Core.Atr (startIdx, endIdx, inHigh, inLow, inReal, freq_Mom[f], out outBeg, out outNbElement, out_MAMom);
					RemoveDependent (outBeg, outNbElement, out_MASon, out_MAMom, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_ATR[f], outResult);
					//Console.WriteLine ("ATR, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (outResult, "ATR");

					//William's %R
					Core.WillR (startIdx, endIdx, inHigh, inLow, inReal, freq_TA[f], out outBeg, out outNbElement, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_WillR[f], outResult);
					//Console.WriteLine ("Will%R, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (outResult, "Will%R");

					//ADX趨勢指標 單位有被除掉 但是頻率不知道會甚麼要除以二才正常 = =? 
					Core.Adx (startIdx, endIdx, inHigh, inLow, inReal, freq_TA[f]/2, out outBeg, out outNbElement, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_ADX[f], outResult);
					//Console.WriteLine ("ADX, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (outResult, "ADX");

					//SMA
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Son[f], Core.MAType.Sma, out outBeg, out outNbElement, out_MASon);
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Mom[f], Core.MAType.Sma, out outBeg, out outNbElement, out_MAMom);
					RemoveDependent (outBeg, outNbElement, out_MASon, out_MAMom, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_SMA[f], outResult);
					//Console.WriteLine ("SMA, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (out_MA, "SMA");

					//EMA
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Son[f], Core.MAType.Ema, out outBeg, out outNbElement, out_MASon);
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Mom[f], Core.MAType.Ema, out outBeg, out outNbElement, out_MAMom);
					RemoveDependent (outBeg, outNbElement, out_MASon, out_MAMom, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_EMA[f], outResult);
					//Console.WriteLine ("EMA, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (out_MA, "EMA");

					//WMA
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Son[f], Core.MAType.Wma, out outBeg, out outNbElement, out_MASon);
					Core.MovingAverage (startIdx, endIdx, inReal, freq_Mom[f], Core.MAType.Wma, out outBeg, out outNbElement, out_MAMom);
					RemoveDependent (outBeg, outNbElement, out_MASon, out_MAMom, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_WMA[f], outResult);
					//Console.WriteLine ("WMA, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (out_MA, "WMA");

					//MFI
					Core.Mfi(startIdx, endIdx, inHigh, inLow, inReal, inVolume, freq_TA[f], out outBeg, out outNbElement, outResult);
					RemoveDependent (outBeg, outNbElement, out_MASon, out_MAMom, outResult);
					JustifyDataRange (max_TA_Frequency, f, freq_TA, outNbElement, target_Return, list_MFI[f], outResult);
					//Console.WriteLine ("MFI, outBeg:" + outBeg + " outNbElement:" + outNbElement);
					//showNumOfTA (outResult, "MFI");

					//[無法採納] OBV能量潮 單位沒有被除掉 要像MA一樣用比率計算 但公式中沒有週期的參數可以調整 永遠是前一天 白搭了 = =
					//Core.Obv (startIdx, endIdx, inReal, inVolume, out outBeg, out outNbElement, outResult);

					//Console.ReadLine ();

					//把暫存在list_tempOutput的各種技術指標，從Double[]取出，抓出正確的資料範圍，存入最大張的list_Feature
					//一次只處理同頻率的各TA指標，list_tempOutput每處理完一個頻率，會重置
							
				}

				//不隨頻率變動的技術指標

				//KD 參數不好設定 100 3 3 將會造成省略103筆資料，乾脆就用最常見的(9,3,3)
				Core.Stoch (startIdx, endIdx, inHigh, inLow, inReal, 9, 3, Core.MAType.Wma, 3, Core.MAType.Wma, out outBeg, out outNbElement, out_K, out_D);

				for (int k = 0; k < outNbElement; k++) 
					out_KD [k] = out_K [k] - out_D [k];
										
				//Console.WriteLine ("KD, outBeg:" + outBeg + ", outNbElement:" + outNbElement);
				JustifyDataRange (max_TA_Frequency, 12, outNbElement, target_Return, list_KD, out_KD);

				//MACD
				Core.Macd (startIdx, endIdx, inReal, 12, 26, 9, out outBeg, out outNbElement, out_MACD, out_MACD_Signal, out_MACD_Hit); 
				//Console.WriteLine ("MACD, outBeg:" + outBeg + ", outNbElement:" + outNbElement);
				JustifyDataRange (max_TA_Frequency, 33, outNbElement, target_Return, list_MACD, out_MACD);

				//記錄前一天有幾筆資料，下一天要接續
				accumulate_Record += list_OneDayRecords[i];
			}

			//檢查每個list中的數量是否一置 Ex:2013年 (一天有165分鐘) * (共有233天) = 38445
//			int fuck = 0;
//			foreach (List<Double> lst in list_Feature) 
//			{
//				Console.WriteLine (fuck + ", " + lst.Count);		
//				fuck++;
//				String feature_Row = "";
//
//				int idx_endData = lst.Count; 
//				int count = 0;
//
//				foreach (double d in lst) 
//				{
//					count++;
//					if (count == idx_endData)
//						feature_Row += d;
//					else
//						feature_Row += d + ",";
//				}
//
//				Console.WriteLine ("count=" + count);
//				Console.ReadLine ();
//			}

			totalRows = (std_Mins - max_TA_Frequency - target_Return) * days;
//			Console.WriteLine ("totalRows " + totalRows);
//			Console.ReadLine ();

			SaveData (list_Feature, str_File);

			Console.WriteLine ("TA total number:" + list_Feature.Count);
			Console.ReadLine ();


		}

		//存檔Function
		public static void SaveData(List<List<double>> list_Feature, String str_File)
		{
			/* list_Feature最大張的二維List，讓Feature(包含Target)可以彈性增加
					 * 					  第一天的165分鐘	第二天的165分鐘	弟三天的165分鐘 ....	
					 *  10-Return (過去)
					 *   5-Return (過去)
			 		 *   1-Return (過去)
					 * 100-RSI
					 * 100-ATR
					 * 100-W%R
					 * 100-ADX
					 * 100-SMA
					 * 100-EMA
					 * 100-WMA
					 * 100-MFI
					 *  KD-(9,3,3)
					 * MACD-(12,26,9)
					 *  30-Return (未來)
					 * */

			/* 真正儲存需要轉置!
			* 					  10-Return (過去) 5-Return (過去) 1-Return (過去) 100-RSI
			*  第一天的165分鐘		 ....	
			*  第二天的165分鐘 
			*  弟三天的165分鐘 
			* 
			* */

			StreamWriter sw_Postive = new StreamWriter (str_File + "_Train_PF.csv");
			StreamWriter sw_Negative = new StreamWriter (str_File + "_Train_NF.csv");

			for (int row = 0; row < totalRows ; row++) 
			{
				String records_Postive = "";
				String records_Negative = "";

				//正資料走這
				if (list_Feature [totalCols - 1] [row] > 0) 
				{
					//col = Feature f10 f5 f1 100-RSI 100-ATR ....
					for (int col = 0; col < totalCols; col++) 
					{
						if(col == totalCols)
							records_Postive += list_Feature [col] [row];
						else
							records_Postive += list_Feature [col] [row] + ",";
					}
					sw_Postive.WriteLine (records_Postive);
				}
				//負資料在這
				else if (list_Feature [totalCols - 1] [row] < 0) 
				{
					//col = Feature f10 f5 f1 100-RSI 100-ATR ....
					for (int col = 0; col < totalCols; col++) 
					{
						if (col == totalCols)
							records_Negative += list_Feature [col] [row];
						else
							records_Negative += list_Feature [col] [row] + ",";
					}
					sw_Negative.WriteLine (records_Negative);
				}
					
				Console.WriteLine ("Running!  " + row);
			}
				
			sw_Postive.Close ();
			sw_Postive.Dispose ();

			sw_Negative.Close ();
			sw_Negative.Dispose ();
		}

		//除掉殘存的單位 分子/分母
		public static void RemoveDependent(int outBeg, int outNbElement, double[] out_Son, double[] out_Mom, double[] out_Result)
		{
			for (int i = 0; i<outNbElement; i++)
			{
				out_Result [i] = out_Son[i] / out_Mom[i];
			}
		}

		//多型Method1. 給單一頻率用的
		public static void JustifyDataRange(int max_TA_Frequency, int freq, int outNbElement, int target_Return, List<double> list_fTA, double[] outResult)
		{
			//Ex:舉例來說每個list有值的範圍隨著Timeframe而不同 (0~196)、(0~201)、(0~206) 要把多餘的資料列刪除
			for (int h = (max_TA_Frequency-freq); h < outNbElement-target_Return; h++) 
			{
				list_fTA.Add(outResult[h]);
			}
		}

		//多型Method2. 給各個頻率用的
		public static void JustifyDataRange(int max_TA_Frequency, int f, int[] freq_TA, int outNbElement, int target_Return, List<double> list_fTA, double[] outResult)
		{
			//Ex:舉例來說每個list有值的範圍隨著Timeframe而不同 (0~196)、(0~201)、(0~206) 要把多餘的資料列刪除
			for (int h = (max_TA_Frequency-freq_TA[f]); h < outNbElement-target_Return; h++) 
			{
				list_fTA.Add(outResult[h]);
			}
		}

		public static void showNumOfTA(double[] arr, string name_TA)
		{
			Console.WriteLine ("TA: " + name_TA);
			for(int a = 0; a<arr.Length; a++)
			{
				Console.WriteLine (a + ": " + arr[a]);
			}
			Console.ReadLine ();
		}


	}
}
	