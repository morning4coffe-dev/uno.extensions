﻿namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	private RouteInfo? First { get; }
	protected IList<RouteInfo> Mappings { get; } = new List<RouteInfo>();

	protected ILogger Logger { get; }

	protected IRouteRegistry RouteMaps { get; }
	protected IViewRegistry ViewMaps { get; }

	public RouteResolver(
		ILogger<RouteResolver> logger,
		IRouteRegistry routes,
		IViewRegistry views
		) : this((ILogger)logger, routes, views)
	{
	}

	protected RouteResolver(ILogger logger, IRouteRegistry routes, IViewRegistry views)
	{
		Logger = logger;
		RouteMaps = routes;
		ViewMaps = views;

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Resolving viewmaps - start");
		var maps = ResolveViewMaps(routes.Items);
		if (Logger.IsEnabled(LogLevel.Debug)) PrintViewMaps(maps);
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Resolving viewmaps - complete");

		if (maps is not null)
		{
			// Set the first routemap to be either the first with IsDefault, if
			// if none have IsDefault then just return the first
			First = maps.FirstOrDefault(x => x.IsDefault) ?? maps.FirstOrDefault();

			Mappings.AddRange(maps.Flatten());
		}

		// Make sure the message dialog is added to the flat list of mappings
		var messageDialogRoute = new RouteInfo(
			Path: RouteConstants.MessageDialogUri,
			View: () => typeof(MessageDialog),
			ResultData: typeof(MessageDialog)
		);
		Mappings.Add(messageDialogRoute);

	}

	private void PrintViewMaps(IEnumerable<RouteInfo> maps, string prefix = "")
	{
		if (maps is null)
		{
			return;
		}

		foreach (var map in maps)
		{
			Logger.LogDebugMessage($"{prefix} {map}");
			PrintViewMaps(map.Nested, prefix + "-");
		}

	}


	protected RouteInfo[] ResolveViewMaps(IEnumerable<RouteMap> maps)
	{
		if (!(maps?.Any() ?? false))
		{
			return Array.Empty<RouteInfo>();
		}
		var rmaps =(
				from drm in maps
				select FromRouteMap(drm)
				).ToArray();
		var dependencies = new Dictionary<string, RouteInfo>();
		foreach (var map in rmaps)
		{
			if (!string.IsNullOrWhiteSpace(map.Path))
			{
				dependencies[map.Path] = map;
			}
			if (dependencies.TryGetValue(map.DependsOn, out var dependee))
			{
				map.DependsOnRoute = dependee;
			}
		}
		return rmaps;
	}

	protected virtual RouteInfo FromRouteMap(RouteMap drm)
	{
		var viewFunc = (drm.View?.View is not null) ?
										() => drm.View.View :
										drm.View?.ViewSelector;
		return AssignParentRouteInfo(new RouteInfo(
			Path: drm.Path,
			View: viewFunc,
			ViewAttributes: drm.View?.ViewAttributes,
			ViewModel: drm.View?.ViewModel,
			Data: drm.View?.Data?.Data,
			ToQuery: drm.View?.Data?.UntypedToQuery,
			FromQuery: drm.View?.Data?.UntypedFromQuery,
			ResultData: drm.View?.ResultData,
			IsDefault: drm.IsDefault,
			DependsOn: drm.DependsOn,
			Init: drm.Init,
			IsDialogViewType: () =>
			{
				return IsDialogViewType(viewFunc?.Invoke());
			},
			Nested: ResolveViewMaps(drm.Nested)));
	}

	protected static RouteInfo AssignParentRouteInfo(RouteInfo info)
	{
		var dependencies = new Dictionary<string, RouteInfo>();
		foreach (var nestedInfo in info.Nested)
		{
			nestedInfo.Parent = info;

			if (!string.IsNullOrWhiteSpace(nestedInfo.Path))
			{
				dependencies[nestedInfo.Path] = nestedInfo;
			}
			if (dependencies.TryGetValue(nestedInfo.DependsOn, out var dependee))
			{
				nestedInfo.DependsOnRoute = dependee;
			}
		}
		return info;
	}

	protected static bool IsDialogViewType(Type? viewType = null)
	{
		if (viewType is null)
		{
			return false; ;
		}

		return viewType == typeof(MessageDialog) ||
			viewType == typeof(ContentDialog) ||
			viewType.IsSubclassOf(typeof(ContentDialog)) ||
			viewType == typeof(Flyout) ||
			viewType.IsSubclassOf(typeof(Flyout));
	}

	public RouteInfo? FindByPath(string? path)
	{
		return InternalFindByPath(path);
	}

	protected virtual RouteInfo? InternalFindByPath(string? path)
	{
		if (path is null)
		{
			return null;
		}

		if (path == string.Empty)
		{
			return First;
		}

		path = path.ExtractBase(out var _, out var _);

		return Mappings.FirstOrDefault(x => x.Path == path);

	}

	private RouteInfo? BestNavigatorRouteInfo(RouteInfo[] maps, INavigator? navigator)
	{
		if (maps.Length == 0)
		{
			return default;
		}
		else if (maps.Length == 1 ||
			navigator is null)
		{
			return maps[0];
		}
		else
		{
			// Need to locate the mapping that's most appropriate to the current route of the supplied navigator
			var ancestors = navigator.Ancestors(this);
			var bestRoute = (0, default(RouteInfo?));
			foreach (var map in maps)
			{
				var routeAncestors = map.Ancestors(this);
				var match = ancestors.Intersect(routeAncestors).Count();
				if (match > bestRoute.Item1)
				{
					bestRoute = (match, map);
				}
			}
			return bestRoute.Item2 ?? maps.FirstOrDefault();
		}
	}

	public RouteInfo? FindByViewModel(Type? viewModelType, INavigator? navigator)
	{
		var maps = InternalFindByViewModel(viewModelType);
		return BestNavigatorRouteInfo(maps, navigator);
	}

	protected virtual RouteInfo[] InternalFindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.ViewModel);
	}

	public RouteInfo? FindByView(Type? viewType, INavigator? navigator)
	{
		var maps = InternalFindByView(viewType);
		return BestNavigatorRouteInfo(maps, navigator);
	}

	protected virtual RouteInfo[] InternalFindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.RenderView);
	}

	public RouteInfo? FindByData(Type? dataType, INavigator? navigator)
	{
		var maps = FindRouteByType(dataType, map => map.Data);
		return BestNavigatorRouteInfo(maps, navigator);
	}

	public RouteInfo? FindByResultData(Type? dataType, INavigator? navigator)
	{
		var maps = FindRouteByType(dataType, map => map.ResultData);
		return BestNavigatorRouteInfo(maps, navigator);
	}

	private RouteInfo[] FindRouteByType(Type? typeToFind, Func<RouteInfo, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

	private TMap[] FindByInheritedTypes<TMap>(IList<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		return mappings.FindByInheritedTypes(typeToFind, mapType);
	}
}

public static class TempHelpers
{
	internal static RouteInfo[] Ancestors(this RouteInfo routeInfo, IRouteResolver resolver)
	{
		var routes = new List<RouteInfo>();
		routeInfo.NavigatorAncestors(resolver, routes);
		return routes.ToArray();
	}

	private static void NavigatorAncestors(this RouteInfo routeInfo, IRouteResolver resolver, IList<RouteInfo> routes)
	{
		routes.Insert(0, routeInfo);

		while (routeInfo?.DependsOnRoute is { } dependee)
		{
			routes.Insert(0, dependee);
			routeInfo = dependee;
		}

		if (routeInfo?.Parent is { } parent)
		{
			parent.NavigatorAncestors(resolver, routes);
		}
	}


	internal static RouteInfo[] Ancestors(this INavigator navigator, IRouteResolver resolver)
	{
		var routes = new List<RouteInfo>();
		navigator.NavigatorAncestors(resolver, routes);
		return routes.ToArray();
	}

	private static void NavigatorAncestors(this INavigator navigator, IRouteResolver resolver, IList<RouteInfo> routes)
	{
		var route = (navigator is IStackNavigator deepNav) ? deepNav.FullRoute : navigator?.Route;
		while (!(route?.IsEmpty() ?? true))
		{
			var info = resolver.FindByPath(route.Base);
			if (info is not null)
			{
				routes.Insert(0, info);
			}
			route = route.Next();
		}

		if (navigator?.GetParent() is { } parent)
		{
			parent.NavigatorAncestors(resolver, routes);
		}
	}
}
