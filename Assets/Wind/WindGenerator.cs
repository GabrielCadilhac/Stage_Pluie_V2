using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindGenerator
{
    private List<SuperPrimitive> _primitives;
    private BezierCurve _bezierCurve;

    private Grid _windsGrid;
    private Bounds _box;
    private Vector3 _min, _cellSize;

    private float   _deltaTime;
    private float _localWindForce, _globalWindForce, _primitiveSpeed;

    private int _nbMaxPrimitives = 4;

    private Vector3[] _hodographPoints;

    private WindPrimitiveType[] _newPrimitives;

    public WindGenerator(Bounds p_box,
                         BezierCurve p_bezierCurve,
                         float p_primitiveSpeed = 1f,
                         float p_globalWindForce = 1f,
                         float p_localWindForce = 1f,
                         float p_deltaTime = 1f)
    {
        Random.InitState(0);

        // Init Parameters
        _box = p_box;
        _min = _box.center - _box.size / 2.0f;
        _cellSize = Common.Divide(Common.NB_CELLS, _box.size);

        _localWindForce = p_localWindForce;
        _primitiveSpeed = p_primitiveSpeed;
        _newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.SINK, WindPrimitiveType.SOURCE, WindPrimitiveType.UNIFORM };

        _globalWindForce = p_globalWindForce;
        _deltaTime = p_deltaTime;
        _bezierCurve = p_bezierCurve;

        _windsGrid = new Grid(Common.NB_CELLS, p_box);

        // Init Prmitives
        _primitives = new List<SuperPrimitive>();

        // Générer les primitives de bases
        for (int i = 0; i < 1; i++)
            _primitives.Add(GenerateSuperPrimitive(_primitiveSpeed));
    }

    private SuperPrimitive GenerateSuperPrimitive(float p_primitiveSpeed)
    {
        int newPrimId = Random.Range(0, 3);
        // A super primitive composed with a vortex and another type
        WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(_newPrimitives[newPrimId], 2f),
                                                          new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                         };
        float randSize = 0.4f + Random.Range(-0.04f, 0.04f);
        float randLerp = Random.Range(0f, 1f);
        float startingStrenght = 1f;
        return new SuperPrimitive(_bezierCurve, primComp, p_primitiveSpeed, startingStrenght, randSize, randLerp);
    }

    private (float, int) ComputeLerp(float p_t, int p_nbPoints)
    {
        float range = 1f / (float) p_nbPoints;
        float tempT = range;
        int id = 0;
        while (tempT < p_t)
        {
            tempT += range;
            id++;
        }

        return ((p_t - (float) id * range) / range, id);
    }

    private void ComputeGlobalWind()
    {
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float t = (float) j / (float) (Common.NB_CELLS.x-1);
                    int hodoId;

                    (t, hodoId) = ComputeLerp(t, Constants.HODOGRAPH_POINTS - 1);

                    Vector3 newWind = t * _hodographPoints[hodoId + 1] + (1f - t) * _hodographPoints[hodoId];
                    newWind = Common.Multiply(newWind, new Vector3(1f, 0f, 1f));

                    _windsGrid.Set(i, j, k, newWind * _globalWindForce);
                }
    }

    public void Update()
    {
        ComputeGlobalWind();

        // Ajouter une nouvelle primitive
        if (Random.Range(0f, 1f) < 1f / (100f * (float)_primitives.Count) && _primitives.Count > 0 && _primitives.Count < _nbMaxPrimitives)
        {
            Debug.Log($"Nouvelle primitive !");
            _primitives.Add(GenerateSuperPrimitive(_primitiveSpeed));
        }

        // Mettre à jours les primitives
        for (int i = 0; i < _primitives.Count; i++)
        {
            _primitives[i].Update(_deltaTime, _min, _cellSize);
            _primitives[i].CheckCollision();
        }

        // Ajouter des perturbations à la grille pour créer des rafales
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = (float) j / Common.NB_CELLS.x;
                    float y = (float) i / Common.NB_CELLS.y;
                    float z = (float) k / Common.NB_CELLS.z;

                    Vector3 direction = Vector3.zero;
                    foreach (SuperPrimitive prim in _primitives)
                        direction += prim.GetValue(x, y, z);

                    _windsGrid.Add(i, j, k, direction * _localWindForce);
                }
    }

    private List<SuperPrimitive> DividePrimitive(SuperPrimitive p_superPrim)
    {
        // Ajouter un facteur de dissipation car il y a toujours un peu d'énergie perdu
        float k = p_superPrim.GetSpeed() * p_superPrim.GetSpeed() * Constants.KINETIC_COEFF * Constants.KINETIC_DUMPING;
        int nbNewPrim = Random.Range(2, 6);

        // Fonction discrète de valeurs cumulé pour distribuer l'énergie cinétique
        float[] kinEnergies = new float[nbNewPrim];
        float totalEnergy = 0f;
        for (int i = 0; i < nbNewPrim; i++)
        {
            kinEnergies[i] = Random.Range(0f, 1f);
            totalEnergy += kinEnergies[i];
        }

        List<SuperPrimitive> windPrimitives = new List<SuperPrimitive>();
        for (int i = 0; i < nbNewPrim; i++)
        {
            float newEnergy = k * kinEnergies[i] / totalEnergy;
            // Créer une nouvelle primitive si assez d'énergie
            // Sinon l'énergie est dissipé par la viscosité
            if (newEnergy > Constants.KINETIC_MIN)
            {
                int newPrimId = Random.Range(0, 3);
                WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(_newPrimitives[newPrimId], 2f),
                                                                  new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                                };
                float newSize = kinEnergies[i] * p_superPrim.GetSize();
                float newSpeed  = Mathf.Sqrt(newEnergy / Constants.KINETIC_COEFF);
                windPrimitives.Add(new SuperPrimitive(_bezierCurve, primComp, newSpeed, newEnergy * 8f, newSize, p_superPrim.GetLerp()));
            }
        }

        return windPrimitives;
    }

    public void SetLocalWindForce(float p_localWindForce)
    {
        _localWindForce = p_localWindForce;
    }
    public void SetGlobalWindForce(float p_windForce)
    {
        _globalWindForce = p_windForce;
    }

    public void SetDeltaTime(float p_deltaTime)
    {
        _deltaTime = p_deltaTime;
    }

    public void SetPrimitiveSpeed(float p_primitiveSpeed)
    {
        foreach (SuperPrimitive prim in _primitives)
            prim.SetSpeed(p_primitiveSpeed);
    }

    public void SetHodograph(Vector3[] p_hodographPoints)
    {
        _hodographPoints = p_hodographPoints;
    }

    public Vector3[] GetWinds()
    {
        return _windsGrid.GetGrid();
    }

    public Grid GetGrid()
    {
        return _windsGrid;
    }
}
