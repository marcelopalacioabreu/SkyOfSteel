using Godot;


public class PauseMenu : VBoxContainer
{
	public Button TeamButton;
	public LineEdit TeamEdit;


	public override void _Ready()
	{
		TeamButton = GetNode<Button>("TeamSwitchBox/ChangeButton");
		TeamEdit = GetNode<LineEdit>("TeamSwitchBox/LineEdit");
		TeamEdit.Text = $"{Game.PossessedPlayer.Team}";

		GetNode<Label>("Version").Text = $"Version: {Game.Version}";

		Label PlayingOn = GetNode<Label>("PlayingOn");
		if(Net.Work.IsNetworkServer())
		{
			PlayingOn.Text = $"Hosting map: {World.SaveName}";
		}
		else
		{
			PlayingOn.Text = $"Connected to server at: {Net.Ip}";
		}

		if(!Net.Work.IsNetworkServer())
		{
			Button SaveButton = GetNode<Button>("SaveButton");
			SaveButton.Disabled = true;
			SaveButton.HintTooltip = "Cannot save as client";
			SaveButton.MouseDefaultCursorShape = CursorShape.Arrow;
		}
	}


	public void ReturnPressed()
	{
		Menu.Close();
	}


	public void TeamChanged()
	{
		int ProspectiveTeam = 1;
		if(!int.TryParse(TeamEdit.Text, out ProspectiveTeam))
		{
			Console.ThrowLog("Attempted to change to a non-int team");
			return;
		}

		Game.PossessedPlayer.Team = ProspectiveTeam;
	}


	public void TeamChanged(string Text)
	{
		TeamChanged();
	}


	public void SavePressed()
	{
		if(Net.Work.IsNetworkServer())
			World.Save(World.SaveName);
	}


	public void DisconnectPressed()
	{
		Net.Disconnect();
	}


	public void QuitPressed()
	{
		Game.Quit();
	}


	public override void _Process(float Delta)
	{
		int ProspectiveTeam = 1;
		if(int.TryParse(TeamEdit.Text, out ProspectiveTeam))
			TeamButton.Disabled = false;
		else
			TeamButton.Disabled = true;
	}
}
