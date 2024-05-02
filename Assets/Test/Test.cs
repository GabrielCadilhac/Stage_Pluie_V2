using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Test : MonoBehaviour
{
    GameObject _plane;
    Texture2D _planeTexture;

    int[,] _splashsCount;

    Vector2Int _textureSize = new Vector2Int(512, 512);

    private void Start()
    {
        _plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _plane.transform.localScale = new Vector3(5.5f, 0f, 5.5f);
        _plane.transform.position   = new Vector3(0f, 0f, 17.5f);

        _planeTexture = new Texture2D(_textureSize.x, _textureSize.y);
        _splashsCount = new int[_textureSize.y, _textureSize.x];

        for (int i = 0; i < _textureSize.x; i++)
        {
            for (int j = 0; j < _textureSize.y; j++)
            {
                Color c = new Color((float)i / (float)_textureSize.x, (float)j / (float)_textureSize.y, 0f);
                _planeTexture.SetPixel(i, j, c);
                _splashsCount[j, i] = 0;
            }
        }
        _planeTexture.Apply();

        Renderer renderer = _plane.GetComponent<Renderer>();
        renderer.material.color = Color.white;
        renderer.material.mainTexture = _planeTexture;
    }

    public void AddSplashs(GraphicsBuffer p_splashs)
    {
        Vector3[] newSplashs = new Vector3[p_splashs.count];
        p_splashs.GetData(newSplashs);

        // Ajouter le nombre de splashs dans un tableau
        int maxSplashs = 0;

        for (int i = 0; i < p_splashs.count; i++)
        {
            int x = (int)(((newSplashs[i].x + 36f) / 72f) * _textureSize.x);
            int z = (int)(((newSplashs[i].z + 36f) / 72f) * _textureSize.y);

            _splashsCount[z, x]++;

            if (maxSplashs < _splashsCount[z, x])
                maxSplashs = _splashsCount[z, x];
        }

        for (int i = 0; i < _textureSize.x; i++)
        {
            for (int j = 0; j < _textureSize.y; j++)
            {
                float x = _splashsCount[j, i] / maxSplashs;
                Color c = new Color(x, x, x);
                _planeTexture.SetPixel(j, i, c);
            }
        }
        _planeTexture.Apply();
    }
}
