﻿using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;


/// <summary>
/// This class represents a markup extension that converts a string representation of a color into a NativeMauiColor object.
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(NativeMauiColor))]
public class MauiColor : MauiExtensionBase
{
	/// <summary>
	/// Gets or sets the string representation of the color value.
	/// </summary>
	public string Value { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName)
	{
		if (!string.IsNullOrEmpty(Value) || !NativeMauiColor.TryParse(Value, out var color))
		{
			var canLog = Logger.IsEnabled(LogLevel.Warning);
			if (string.IsNullOrEmpty(Value) && canLog)
			{
				Logger.LogWarning(Properties.Resources.NoColorValueProvided);
			}
			else if (canLog)
			{
				Logger.LogWarning(Properties.Resources.UnableToConvertValueToColor, Value);
			}
			return;
		}

		view.SetValue(property, color);
	}
}
