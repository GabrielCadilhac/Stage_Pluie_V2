using System.Collections;
using UnityEngine;

public class Grid
{
    // Sizes
    private Vector3Int _nbCells;
    private Vector3 _cellSize;
    private Vector3 _gridSize;

    private Vector3[] _grid;

    public Grid(Vector3Int pNbCells, Vector3 pSize, Vector3? pDefaultValue = null)
    {
        _nbCells = pNbCells;
        _gridSize = pSize;
        _cellSize = Common.Divide(_gridSize, _nbCells);

        _grid = new Vector3[_nbCells.x * _nbCells.y * _nbCells.z];
    }

    public void Reset(Vector3? pDefaultValue = null)
    {
        Vector3 value = pDefaultValue == null ? Vector3.zero : (Vector3) pDefaultValue;

        for (int k = 0; k < _nbCells.z; k++)
            for (int i = 0; i < _nbCells.y; i++)
                for (int j = 0; j < _nbCells.x; j++)
                {
                    Set(i, j, k, value);
                }
    }

    public Vector3 FloatTo01(Vector3 pCoords)
    {
        return Common.Divide(pCoords, _gridSize);
    }

    public void Set(int i, int j, int k, Vector3 pValue)
    {
        int index = (k * _nbCells.y + i) * _nbCells.x + j;
        _grid[index] = pValue;
    }

    public void Add(int i, int j, int k, Vector3 pValue)
    {
        int index = (k * _nbCells.y + i) * _nbCells.x + j;
        _grid[index] += pValue;
    }

    public void Set(int pIndex, Vector3 pValue)
    {
        _grid[pIndex] = pValue;
    }

    public Vector3 Get(int pI, int pJ, int pK)
    {
        int index = (pK * _nbCells.y + pI) * _nbCells.x + pJ;
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

    public Vector3 GetCellCenter(Vector3 pCell)
    {
        return new Vector3(
                pCell.x + 0.5f * _cellSize.x,
                pCell.y + 0.5f * _cellSize.y,
                pCell.z + 0.5f * _cellSize.z
            );
    }
}
