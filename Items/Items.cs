using Godot;
using System.Collections.Generic;


public class Items : Node
{
	public class Instance
	{
		public Items.TYPE Type = Items.TYPE.ERROR;
		public int Temperature = 0;
		public int Count = 1;
		public int UsesRemaining = 0;
		public string Description = "This item description is a bug and should not exists";

		public Instance(Items.TYPE TypeArg)
		{
			this.Type = TypeArg;
		}
	}


	public enum TYPE {ERROR, PLATFORM, WALL, SLOPE}

	public static Dictionary<TYPE, Mesh> Meshes = new Dictionary<TYPE, Mesh>();
	private static Dictionary<TYPE, Texture> Thumbnails = new Dictionary<TYPE, Texture>();
	public static Dictionary<TYPE, Texture> Textures { get; private set; } = new Dictionary<TYPE, Texture>();

	public static Shader StructureShader { get; private set; }
	private static PackedScene DroppedItemScene;

	Items()
	{
		if(Engine.EditorHint) {return;}

		StructureShader = GD.Load<Shader>("res://Building/Materials/StructureShader.shader");
		DroppedItemScene = GD.Load<PackedScene>("res://Items/DroppedItem.tscn");

		foreach(Items.TYPE Type in System.Enum.GetValues(typeof(TYPE)))
		{
			Meshes.Add(Type, GD.Load<Mesh>($"res://Items/Meshes/{Type}.obj"));
			//Assume that every item has a mesh, will throw exception on game startup if not
		}

		foreach(TYPE Type in System.Enum.GetValues(typeof(TYPE)))
		{
			Thumbnails.Add(Type, GD.Load<Texture>($"res://Items/Thumbnails/{Type}.png"));
			//Assume that every item has a thumbnail, will throw exception on game startup if not
		}

		foreach(TYPE Type in System.Enum.GetValues(typeof(TYPE)))
		{
			Textures.Add(Type, GD.Load<Texture>($"res://Items/Textures/{Type}.png"));
			//Assume that every item has a texture, will throw exception on game startup if not
		}
	}


	public static Texture Thumbnail(TYPE Type)
	{
		return Thumbnails[Type];
	}


	public static void Drop(Instance ItemInstance, Vector3 Position)
	{
		DroppedItem ToDrop = DroppedItemScene.Instance() as DroppedItem;
		ToDrop.Translation = Position;
		ToDrop.Momentum = new Vector3(0,-5,0);
		ToDrop.Item = ItemInstance;
		ToDrop.GetNode<MeshInstance>("MeshInstance").Mesh = Meshes[ItemInstance.Type];
		Game.StructureRoot.AddChild(ToDrop);
	}
}
