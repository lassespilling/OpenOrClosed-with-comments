﻿#if NET5_0_OR_GREATER
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
#else
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Composing;
#endif
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenOrClosed.Core.PropertyValueConverters
{
	public class SpecialHoursConverter : PropertyValueConverterBase
	{

#if NET5_0_OR_GREATER
		private readonly IDataTypeService dataTypeService;

		public SpecialHoursConverter(IDataTypeService dataTypeService)
		{
			this.dataTypeService = dataTypeService;
		}
#endif


		public override bool IsConverter(IPublishedPropertyType propertyType)
			=> Constants.PropertyEditors.Aliases.SpecialHours == propertyType.EditorAlias;

		public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
			=> typeof(IEnumerable<ViewModels.SpecialDaysViewModel>);

		public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
			=> PropertyCacheLevel.Element;

		public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object source, bool preview)
		{
			var sourceString = source?.ToString();
			if (string.IsNullOrWhiteSpace(sourceString))
			{
				return Enumerable.Empty<ViewModels.SpecialDaysViewModel>();
			}
			var data = JsonConvert.DeserializeObject<IEnumerable<ViewModels.SpecialDaysViewModel>>(sourceString);
#if NET5_0_OR_GREATER

			var picker = dataTypeService.GetDataType(propertyType.DataType.Id);
#else
			var picker = Current.Services.DataTypeService.GetDataType(propertyType.DataType.Id);
#endif

			var dataTypePrevalues = picker.Editor.GetConfigurationEditor().ToValueEditor(picker.Configuration);
			bool removeOldDates = dataTypePrevalues.FirstOrDefault(x => x.Key == Constants.PropertyEditors.PreValues.RemoveOldDates).Value?.ToString() == "1";

			var currDate = DateTime.Now.Date.Date;

			// Go through and adjust the dates for each set of hours.
			foreach (var date in data)
			{
				if (removeOldDates)
				{
					if (date.Date.Date < currDate)
					{
						continue;
					}
				}

				foreach (var hours in date.HoursOfBusiness)
				{
					hours.OpensAt = new DateTime(date.Date.Year, date.Date.Month, date.Date.Day, hours.OpensAt.Hour, hours.OpensAt.Minute, hours.OpensAt.Second);
					hours.ClosesAt = new DateTime(date.Date.Year, date.Date.Month, date.Date.Day, hours.ClosesAt.Hour, hours.ClosesAt.Minute, hours.ClosesAt.Second);
				}
			}

			//Only all dates in the future 
			if (removeOldDates)
			{
				return data.Where(x => x.Date.Date >= DateTime.Now.Date.Date).ToList();
			}

			return data;
		}
	}
}
