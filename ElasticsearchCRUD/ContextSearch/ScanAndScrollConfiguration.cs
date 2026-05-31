using System.Collections.Generic;
using ElasticsearchCRUD.Model.Units;

namespace ElasticsearchCRUD.ContextSearch
{
	public class ScanAndScrollConfiguration
	{
		private readonly TimeUnit _lengthOfTime;
		private readonly int _size = 50;

		public ScanAndScrollConfiguration(TimeUnit lengthOfTime, int size)
		{
			_lengthOfTime = lengthOfTime;
			_size = size;
		}

		public string GetScrollScanUrlForSetup()
		{
			return $"search_type=scan&scroll={_lengthOfTime.GetTimeUnit()}&size={_size}";
		}

		public string GetScrollScanUrlForRunning()
		{
			return $"_search/scroll?scroll={_lengthOfTime.GetTimeUnit()}&scroll_id=";
		}

	}
}

