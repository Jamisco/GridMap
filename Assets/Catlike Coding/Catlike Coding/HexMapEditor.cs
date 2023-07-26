using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

	public Color[] colors;

	public HexGrid hexGrid;

	public int activeElevation;
	public int colorIndex;

	Color activeColor;
	private void OnValidate()
	{
		colorIndex = Mathf.Clamp(colorIndex, 0, colors.Length - 1);
		SelectColor(colorIndex);

		SetElevation(activeElevation);
	}
	public void SelectColor(int index) 
	{
		activeColor = colors[index];
	}

	public void SetElevation(float elevation) 
	{
		activeElevation = (int)elevation;
	}

	void Awake () 
	{
		SelectColor(colorIndex);
	}

	void Update () 
	{
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			HandleInput();
		}
	}

	void HandleInput () 
	{
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		if (Physics.Raycast(inputRay, out hit)) 
		{
			EditCell(hexGrid.GetCell(hit.point));
		}
	}

	void EditCell (HexCell cell) 
	{
		cell.color = activeColor;
		cell.Elevation = activeElevation;
		hexGrid.Refresh();
	}
}