using System.Collections;
using UnityEngine;

public class Grid
{
    // Sizes
    private Vector3Int _nbCells;
    private Vector3 _cellSize;
    private Bounds _gridSize;

    private Vector3[] _grid;

    public Grid(Vector3Int p_nbCells, Bounds p_size, Vector3? p_defaultValue = null)
    {
        _nbCells = p_nbCells;
        _gridSize = p_size;
        _cellSize = Common.Divide(_gridSize.size, _nbCells);

        _grid = new Vector3[_nbCells.x * _nbCells.y * _nbCells.z];

        Reset(p_defaultValue);
    }

    public void Reset(Vector3? p_defaultValue = null)
    {
        p_defaultValue = p_defaultValue == null ? Vector3.zero : p_defaultValue;

        for (int k = 0; k < _nbCells.z; k++)
            for (int i = 0; i < _nbCells.y; i++)
                for (int j = 0; j < _nbCells.x; j++)
                {
                    Set(i, j, k, (Vector3) p_defaultValue);
                }
    }

    public void Set(int i, int j, int k, Vector3 p_value)
    {
        int index = (k * _nbCells.y + i) * _nbCells.x + j;
        _grid[index] = p_value;
    }

    public void Add(int i, int j, int k, Vector3 p_value)
    {
        int index = (k * _nbCells.y + i) * _nbCells.x + j;
        _grid[index] += p_value;
    }

    public void Set(int p_index, Vector3 p_value)
    {
        _grid[p_index] = p_value;
    }

    public Vector3 Get(int p_i, int p_j, int p_k)
    {
        int index = (p_k * _nbCells.y + p_i) * _nbCells.x + p_j;
        return _grid[index];
    }

    public Vector3 GetCellSize()
    {
        return _cellSize;
    }

    public Vector3[] GetGrid()
    {
        return _grid;
    }

    public Vector3 GetCellCenter(Vector3 p_cell)
    {
        return new Vector3(
                (p_cell.x + 0.5f) * _cellSize.x,
                (p_cell.y + 0.5f) * _cellSize.y,
                (p_cell.z + 0.5f) * _cellSize.z
            );
    }
}
