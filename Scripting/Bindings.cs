using Godot;
using System;
using System.Collections.Generic;


public class Bindings : Node
{
	public enum BIND_TYPE {UNSET, SCANCODE, MOUSEBUTTON, MOUSEWHEEL, AXIS}
	private static List<string> MouseButtonList = new List<string>{"MouseOne", "MouseTwo", "MouseThree"};
	private static List<string> MouseWheelList = new List<string>{"WheelUp", "WheelDown"};
	private static List<string> AxisList = new List<string>{"MouseUp", "MouseDown", "MouseRight", "MouseLeft"};
	private static List<BindingObject> BindingsWithArg = new List<BindingObject>();
	private static List<BindingObject> BindingsWithoutArg = new List<BindingObject>();

	private static Bindings Self;
	private Bindings()
	{
		Self = this;
	}


	public static void Bind(string KeyName, string FunctionName)
	{
		//First we check if the function provided even exists
		dynamic Variable;
		Scripting.ConsoleScope.TryGetVariable(FunctionName, out Variable);
		if(Variable == null || !(Variable is Delegate || Variable is IronPython.Runtime.PythonFunction))
		{
			Console.ThrowPrint($"'{FunctionName}' is not a valid function");
			return;
		}

		//Then we grab the number of aruments the function takes
		Nullable<int> ArgCount = null; //null for the sanity check
		if(Variable is Delegate)
		{
			ArgCount = (Variable as Delegate).Method.GetParameters().Length;
		}
		else if(Variable is IronPython.Runtime.PythonFunction)
		{
			ArgCount = Scripting.ConsoleEngine.Execute($"len({FunctionName}.func_code.co_varnames)");
		}

		if(ArgCount == null) //Sanity check
		{
			Console.ThrowPrint($"Cannot find argument count of '{FunctionName}', please contact the developers'");
			return;
		}

		//Then we verify that the function takes either zero or one arguments
		if(ArgCount != 0 && ArgCount != 1)
		{
			Console.ThrowPrint($"Function '{FunctionName}' must take either one or two arguments");
			return;
		}
		if(ArgCount == 1 && Variable is Delegate) //and if it does take a variable we make sure it can take a float
		{
			if(!((Delegate)Variable).Method.GetParameters()[0].ParameterType.IsInstanceOfType(new float()))
			{
				Console.ThrowPrint($"Builtin command '{FunctionName}' has a single non-float argument");
				return;
			}
		}


		BindingObject NewBind = new BindingObject(KeyName, FunctionName);
		Nullable<ButtonList> ButtonValue = null; //Making it null by default prevents a compile warning further down
		int Scancode = 0;
		switch(KeyName) //Checks custom string literals first then assumes Scancode
		{
			case("MouseOne"): {
				NewBind.Type = BIND_TYPE.MOUSEBUTTON;
				ButtonValue = ButtonList.Left;
				break;
			}
			case("MouseTwo"): {
				NewBind.Type = BIND_TYPE.MOUSEBUTTON;
				ButtonValue = ButtonList.Right;
				break;
			}
			case("MouseThree"): {
				NewBind.Type = BIND_TYPE.MOUSEBUTTON;
				ButtonValue = ButtonList.Middle;
				break;
			}

			default: {
				//Does not match any custom string literal must either be a Scancode or is invalid
				int LocalScancode = OS.FindScancodeFromString(KeyName);
				if(LocalScancode != 0)
				{
					//Is a valid Scancode
					NewBind.Type = BIND_TYPE.SCANCODE;
					Scancode = LocalScancode;
				}
				else
				{
					//If not a valid Scancode then the provided key must not be a valid key
					return;
				}
				break;
			}
		}
		//Now we have everything we need to setup the bind with Godot's input system

		//First clear any bind with the same key
		UnBind(KeyName);

		//Then add new bind
		InputMap.AddAction(KeyName);
		switch(NewBind.Type)
		{
			case(BIND_TYPE.SCANCODE): {
				InputEventKey Event = new InputEventKey();
				Event.Scancode = Scancode;
				InputMap.ActionAddEvent(KeyName, Event);
				break;
			}

			case(BIND_TYPE.MOUSEBUTTON): {
				InputEventMouseButton Event = new InputEventMouseButton();
				Event.ButtonIndex = (int)ButtonValue;
				InputMap.ActionAddEvent(KeyName, Event);
				break;
			}
		}
		if(ArgCount == 0)
		{
			BindingsWithoutArg.Add(NewBind);
		}
		else if(ArgCount == 1)
		{
			BindingsWithArg.Add(NewBind);
		}
		else
		{
			//Sanity check
			Console.ThrowPrint("Unsupported number of arguments when adding new bind to bindings lists");
		}
	}


	public static void UnBind(string KeyName)
	{
		if(InputMap.HasAction(KeyName))
		{
			InputMap.EraseAction(KeyName);

			List<BindingObject> Removing = new List<BindingObject>();
			foreach(BindingObject Bind in BindingsWithArg)
			{
				if(Bind.Name == KeyName)
				{
					Removing.Add(Bind);
				}
			}
			foreach(BindingObject Bind in Removing)
			{
				BindingsWithArg.Remove(Bind);
			}

			Removing.Clear();

			foreach(BindingObject Bind in BindingsWithoutArg)
			{
				if(Bind.Name == KeyName)
				{
					Removing.Add(Bind);
				}
			}
			foreach(BindingObject Bind in Removing)
			{
				BindingsWithoutArg.Remove(Bind);
			}
		}
	}


	private static float GreaterEqualZero(float In)
	{
		if(In < 0f)
		{
			return 0f;
		}
		return In;
	}


	public override void _Process(float Delta)
	{
		if(!Game.BindsEnabled)
		{
			return;
		}

		foreach(BindingObject Binding in BindingsWithArg)
		{
			if(Binding.Type == BIND_TYPE.SCANCODE || Binding.Type == BIND_TYPE.MOUSEBUTTON)
			{
				if(Input.IsActionJustPressed(Binding.Name))
				{
					Scripting.ConsoleEngine.Execute($"{Binding.Function}(1)", Scripting.ConsoleScope);
				}
				else if(Input.IsActionJustReleased(Binding.Name))
				{
					Scripting.ConsoleEngine.Execute($"{Binding.Function}(0)", Scripting.ConsoleScope);
				}
			}
			else if(Binding.Type == BIND_TYPE.MOUSEWHEEL)
			{
				if(Input.IsActionJustReleased(Binding.Name))
				{
					Scripting.ConsoleEngine.Execute($"{Binding.Function}(1)", Scripting.ConsoleScope);
				}
			}
		}

		foreach(BindingObject Binding in BindingsWithoutArg)
		{
			if(Binding.Type == BIND_TYPE.SCANCODE || Binding.Type == BIND_TYPE.MOUSEBUTTON)
			{
				if(Input.IsActionJustPressed(Binding.Name))
				{
					Scripting.ConsoleEngine.Execute($"{Binding.Function}()", Scripting.ConsoleScope);
				}
			}
			else if(Binding.Type == BIND_TYPE.MOUSEWHEEL)
			{
				if(Input.IsActionJustReleased(Binding.Name))
				{
					Scripting.ConsoleEngine.Execute($"{Binding.Function}()", Scripting.ConsoleScope);
				}
			}
		}
	}


	public override void _Input(InputEvent Event)
	{
		if(!Game.BindsEnabled)
		{
			return;
		}

		if(Event is InputEventMouseMotion MotionEvent)
		{
			foreach(BindingObject Binding in BindingsWithArg)
			{
				if(Binding.Type == BIND_TYPE.AXIS)
				{
					switch(Binding.AxisDirection)
					{
						case(BindingObject.DIRECTION.UP):
							Scripting.ConsoleEngine.Execute($"{Binding.Function}({(float)new decimal (GreaterEqualZero(MotionEvent.Relative.y*-1))})", Scripting.ConsoleScope);
							break;
						case(BindingObject.DIRECTION.DOWN):
							Scripting.ConsoleEngine.Execute($"{Binding.Function}({(float)new decimal (GreaterEqualZero(MotionEvent.Relative.y))})", Scripting.ConsoleScope);
							break;
						case(BindingObject.DIRECTION.RIGHT):
							Scripting.ConsoleEngine.Execute($"{Binding.Function}({(float)new decimal (GreaterEqualZero(MotionEvent.Relative.x))})", Scripting.ConsoleScope);
							break;
						case(BindingObject.DIRECTION.LEFT):
							Scripting.ConsoleEngine.Execute($"{Binding.Function}({(float)new decimal (GreaterEqualZero(MotionEvent.Relative.x*-1))})", Scripting.ConsoleScope);
							break;
					}
				}
			}

			foreach(BindingObject Binding in BindingsWithoutArg)
			{
				if(Binding.Type == BIND_TYPE.AXIS)
				{
					//Don't need to switch on the direction as it doesn't want an argument anyway
					Scripting.ConsoleEngine.Execute($"{Binding.Function}()", Scripting.ConsoleScope);
				}
			}
		}
	}
}
