namespace FantasyFootball.Helpers;

/// <summary>
/// Governs what to do when an item is selected
/// SHOW_DETAILS - Display details of selected item (for example, open Team or Country detail page)
/// RETURN_ID - Return the id of the selected item to the calling page on the stack
/// </summary>
public enum SelectionType
{
	SHOW_DETAILS,
	RETURN_ID,
}
