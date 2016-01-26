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
				List<Double> list_fReturn_100= new List<Double> ();
				List<Double> list_MA_100 = new List<Double> ();

				//為了算報酬率每天前面的(t分鐘)被捨棄，且要預測未來，後面(買進持有+延遲決策)的分鐘也捨棄
				for(int j=accumulate_Record+t; j<accumulate_Record+list_OneDayRecords[i]-buy_Hold-delay_Decision; j++)
				{
					list_fReturn_100.Add (Convert.ToDouble (raw_Data [j] [4]) / Convert.ToDouble (raw_Data [j - 100] [4]) - 1.0);
				}

				double[] inReal = new double[list_OneDayRecords[i]];	//輸入的資料
				double[] outResult = new double[list_OneDayRecords[i]];	//輸出的結果
				for (int z=accumulate_Record; z<accumulate_Record+list_OneDayRecords[i]; z++)
				{
					inReal [z] = raw_Data [z] [4];
				}

				Console.WriteLine (list_OneDayRecords [i]);
				Console.ReadLine ();

				int startIdx = 0;
				int endIdx = 0;
				endIdx = list_OneDayRecords[i]-1;
				int outBeg = 0;
				int outNbElement = 0;


				String s = Core.MovingAverage (0, endIdx, inReal, 30, Core.MAType.Sma, out outBeg, out outNbElement, outResult).ToString();

				Console.WriteLine (s);


				list_Feature.Add (list_fReturn_100);

				//記錄前一天有幾筆資料，下一天要接續
				accumulate_Record += list_OneDayRecords[i];

				for (int x = 0; x<297; x++)
				{
					Console.WriteLine (x + ": " +outResult[x]);

				}
				Console.ReadLine ();
			}
				



			foreach(List<Double> lst in list_Feature)
			{
				int i = 1;
				foreach (Double d in lst) {
					Console.WriteLine (i + ": " + d);
					i++;
				}
				Console.ReadLine ();
			}

			Console.ReadLine ();
		}
	}
}
