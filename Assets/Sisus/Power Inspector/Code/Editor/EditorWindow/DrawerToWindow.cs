using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class for easily displaying Drawer in an EditorWindow
	/// </summary>
	public class DrawerToWindow : EditorWindow
	{
		private Action onClosed;

		private DrawerGroup drawer;

		public DrawerGroup Drawer
		{
			get
			{
				return drawer;
			}
		}
		
		public static TDrawerWindow Create<TDrawerWindow>(IInspector inspector, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			return Create<TDrawerWindow>(DrawerGroup.Create(inspector), doOnClosed);
		}

		public static TDrawerWindow Create<TDrawerWindow>(GameObject target, IInspector inspector, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			var drawers = DrawerGroup.Create(inspector);
			var members = drawers.Members;
			DrawerArrayPool.InsertAt(ref members, 0, inspector.DrawerProvider.GetForGameObject(inspector, target, drawers), false);
			drawers.SetMembers(members);
			return Create<TDrawerWindow>(drawers, doOnClosed);
		}

		public static TDrawerWindow Create<TDrawerWindow>([NotNull]GameObject[] targets, [NotNull]IInspector inspector, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			var drawers = DrawerGroup.Create(inspector);
			var members = drawers.Members;
			DrawerArrayPool.InsertAt(ref members, 0, GameObjectDrawer.Create(targets, drawers, inspector), false);
			drawers.SetMembers(members);
			return Create<TDrawerWindow>(drawers, doOnClosed);
		}

		public static TDrawerWindow Create<TDrawerWindow>(Component target, [NotNull]IInspector inspector, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			var drawers = DrawerGroup.Create(inspector);
			var members = drawers.Members;
			DrawerArrayPool.InsertAt(ref members, 0, inspector.DrawerProvider.GetForComponent(inspector, target, drawers), false);
			drawers.SetMembers(members);
			return Create<TDrawerWindow>(drawers, doOnClosed);
		}

		public static TDrawerWindow Create<TDrawerWindow>([NotNull]Component[] targets, [NotNull]IInspector inspector, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			var drawers = DrawerGroup.Create(inspector);
			var members = drawers.Members;
			DrawerArrayPool.InsertAt(ref members, 0, inspector.DrawerProvider.GetForComponents(inspector, targets, drawers), false);
			drawers.SetMembers(members);
			return Create<TDrawerWindow>(drawers, doOnClosed);
		}

		public static TDrawerWindow Create<TDrawerWindow>(DrawerGroup drawer, Action doOnClosed = null) where TDrawerWindow : DrawerToWindow
		{
			var result = CreateInstance<TDrawerWindow>();
			result.drawer = drawer;
			result.onClosed = doOnClosed;
			return result;
		}

		protected virtual void Draw(Rect drawPosition)
		{
			drawer.Draw(drawPosition);
		}

		protected virtual void Dispose()
		{
			if(onClosed != null)
			{
				var action = onClosed;
				onClosed = null;
				action();
			}
			drawer.Dispose();
			drawer = null;
		}
		
		[UsedImplicitly]
		private void OnGUI()
		{
			var drawPosition = position;
			drawPosition.x = 0f;
			drawPosition.y = 0f;
			Draw(drawPosition);
		}

		[UsedImplicitly]
		protected virtual void OnInspectorUpdate()
		{
			UpdateCachedValuesFromFields();
		}

		private void UpdateCachedValuesFromFields()
		{
			drawer.UpdateCachedValuesFromFieldsRecursively();
		}

		[UsedImplicitly]
		private void OnDestroy()
		{
			Dispose();
		}
	}
}