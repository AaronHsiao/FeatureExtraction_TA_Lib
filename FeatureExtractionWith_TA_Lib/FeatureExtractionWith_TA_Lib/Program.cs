using System;
using System.IO;
using System.Collections.Generic;
using TicTacTec.TA.Library;

namespace FeatureExtractionWith_TA_Lib
{
	class MainClass
	{
		public static void Main (string[] args)
		{				
			List<int[]> raw_Data = new List<int[]> ();	//原始資料
			List<int> list_Date = new List<int> ();		//每個交易日 取聯集 20010102 20010103....
			List<int> list_OneDayRecords = new List<int> (); //每個交易日有幾筆資料(分鐘)

			int counter = 0;

			//讀取資料 處理日期
			using(StreamReader sr = new StreamReader ("2001-2012.csv"))
			{
				sr.ReadLine ();
				int temp_Date = 0;
				int temp_OneDayRecords = 1;

				while(!sr.EndOfStream)
				{
					raw_Data.Add (new int[6]);				
					String[] s = sr.ReadLine ().Split(',');
									
					//日期資料只取到日 "yyyy/MM/dd" (從第0個字元開始往後取8)
					s[0] = s [0].Substring (0,8);

					raw_Data[counter][0] = Int32.Parse(s[0]);	//日期
					raw_Data[counter][1] = Int32.Parse(s[1]);	//開
					raw_Data[counter][2] = Int32.Parse(s[2]);	//高
					raw_Data[counter][3] = Int32.Parse(s[3]);	//低
					raw_Data[counter][4] = Int32.Parse(s[4]);	//收
					raw_Data[counter][5] = Int32.Parse(s[5]);	//量

					//第一次日期不用比較 直接加入 此後都要做比對
					if (temp_Date == 0)
						list_Date.Add (raw_Data [counter] [0]);
					//日期有所變動時(20010102 => 20010103)
					else if (temp_Date != raw_Data [counter] [0]) {
						list_Date.Add (raw_Data [counter] [0]);
						list_OneDayRecords.Add (temp_OneDayRecords);
						temp_OneDayRecords = 1;
					} else {
						temp_OneDayRecords++;
						if(sr.EndOfStream)
							list_OneDayRecords.Add (temp_OneDayRecords);
					}

					//暫存日期
					temp_Date = raw_Data[counter][0];


					counter++;

				}
					
				sr.Close ();
				sr.Dispose ();
			}

			//記錄目前處理到哪一筆資料(分鐘)
			int accumulate_Record = 0;
			//看要捨棄前面多少分鐘(t-1) 決定now是第幾分鐘(t)
			int t = 100;
			int delay_Decision = 20;
			int buy_Hold = 10;
			int search_Count = 100000;
			int k = 10000;

			//List of List讓Feature與Target可以彈性增加
			List<List<Double>> list_Feature = new List<List<Double>> ();
			List<List<Double>> list_Target = new List<List<Double>> ();

			//產出Feature [不跨日,計算當日技術指標]
			//list_Date.Count
			for(int i = 0; i<1; i++)
			{
				List<Double> list_fReturn= new List<Double> ();
				List<Double> list_fTA = new List<Double> ();

				int[] frequency = new int[]{100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 1};

				//算出各種頻率的報酬當Feature
				for (int f = 0; f<frequency.Length; f++)
				{
					//為了算報酬率每天前面的(t分鐘)被捨棄，且要預測未來，後面(買進持有+延遲決策)的分鐘也捨棄
					for(int j=accumulate_Record+t; j<accumulate_Record+list_OneDayRecords[i]-buy_Hold-delay_Decision; j++)
					{
						list_fReturn.Add (Convert.ToDouble (raw_Data [j] [4]) / Convert.ToDouble (raw_Data [j - frequency[f]] [4]) - 1.0);
					}

					//每算好一種頻率的報酬，就加到最大張的List of List
					list_Feature.Add (list_fReturn);
					//不能用List.clear() 要保留其值，故改用new出新的一塊List給下一個feature使用
					list_fReturn = new List<Double> ();
				}
					
				double[] inReal = new double[list_OneDayRecords[i]];	//輸入的資料(一天)
				double[] outResult = new double[list_OneDayRecords[i]];	//輸出的結果(一天)

				//由於技術指標是一次輸入一個陣列，並回傳結果陣列，所以我必須將原始資料Records整理成"一天"，再餵入Function進行處理
				for (int z=accumulate_Record; z<accumulate_Record+list_OneDayRecords[i]; z++)
				{
					inReal [z] = raw_Data [z] [4];
				}

				//確認每天存的資料筆數
				//Console.WriteLine (list_OneDayRecords [i]);
				//Console.ReadLine ();

				int startIdx = 0;
				int endIdx = 0;
				endIdx = list_OneDayRecords[i]-1;
				int outBeg = 0;
				int outNbElement = 0;
				int max_Frequency = frequency [0];
				List<Double[]> list_tempOutput = new List<Double[]> (); 

				//算出各種技術指標當Feature
				for (int f = 0; f < frequency.Length; f++) 
				{
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Dema, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Ema, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Kama, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Mama, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Sma, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.T3, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Tema, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Trima, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.MovingAverage (startIdx, endIdx, inReal, frequency[f], Core.MAType.Wma, out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);
					Core.Rsi(startIdx, endIdx, inReal, frequency[f], out outBeg, out outNbElement, outResult);
					list_tempOutput.Add (outResult);

					//把暫存在list_tempOutput的各種技術指標，從Double[]取出，抓出正確的資料範圍，存入List，處理好單一Feature再存入List of List
					foreach(double[] arr in list_tempOutput)
					{
						//由於30MA的TA-Lib會自動忽略無法算值之前29筆，
						//output最少的100MA的TA-Lib自動忽略無法算值之前99筆，
						//為了確保每條Feature個數一樣多，所以30MA的結果，還要放棄前面(99-29)筆資料，達到Feature對齊
						//從1開始，是因為Return跟其他技術指標差一，故捨棄第一筆資料(index=0)
						for (int h = 1+(max_Frequency-frequency[f]); h < outNbElement-buy_Hold-delay_Decision; h++) 
						{
							list_fTA.Add (arr [h]);
						}
							
						list_Feature.Add(list_fTA);
						list_fTA = new List<Double> ();
					}

					//算好一個頻率的各技術指標後，Reset暫存用的list
					list_tempOutput = new List<double[]>();
						
				}
					
				//記錄前一天有幾筆資料，下一天要接續
				accumulate_Record += list_OneDayRecords[i];
			}
				
			int ccc = 0;

			Console.WriteLine (list_Feature.Count);
			Console.ReadLine ();


			foreach(List<Double> lst in list_Feature)
			{
				Console.WriteLine (ccc + ", " + lst.Count);
				ccc += 1;
			}

			Console.ReadLine ();
		}
	}
}
	