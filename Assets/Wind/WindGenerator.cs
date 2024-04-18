using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindGenerator
{
    private List<SuperPrimitive> _primitives;
    private BezierCurve _bezierCurve;

    private List<Vector3Int> _curvePos;

    private Grid _windsGrid;
    private Bounds _box;

    private Vector3 _globalWind;
    private float   _deltaTime;

    private Vector3[] _hodographPoints;

    public WindGenerator(Bounds p_box,
                         BezierCurve p_bezierCurve,
                         Vector3? p_globalWind = null,
                         int p_nbPrimitives = 1,
                         float p_primitiveSpeed = 1f,
                         float p_localWindForce = 1f,
                         float p_deltaTime = 1f)
    {
        Random.InitState(0);

        // Init Parameters
        _box = p_box;
        _globalWind = p_globalWind ?? Vector3.zero;
        _deltaTime = p_deltaTime;
        _bezierCurve = p_bezierCurve;

        _curvePos = new List<Vector3Int>();

        _windsGrid = new Grid(Common.NB_CELLS, p_box);

        // Init Prmitives
        _primitives = new List<SuperPrimitive>();

        // A super primitive composed with a vortex and a source
        WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(WindPrimitiveType.SINK, 1f),
                                                          new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                        };

        _primitives.Add(new SuperPrimitive(p_bezierCurve, primComp, p_primitiveSpeed, p_localWindForce, 0.3f));
    }

    private void ComputeGlobalWind()
    {
        float range = 1f / 3f;
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float t = ((float) j / (float) (Common.NB_CELLS.x-1));

                    float tempT = range;
                    int hodoId = 0;
                    while (tempT < t)
                    {
                        tempT += range;
                        hodoId++;
                    }
                    t = (t - (float) hodoId * range) / range;
    
                    Vector3 newWind = t * _hodographPoints[hodoId + 1] + (1f - t) * _hodographPoints[hodoId];
                    newWind = Common.Multiply(newWind, new Vector3(1f, 0f, 1f));

                    //Vector3 newWind = new Vector3(t * _globalWind.x, 0f, (1f - t) * _globalWind.z);
                    _windsGrid.Set(i, j, k, newWind * _globalWind.magnitude * 0.1f);
                }
    }

    public void Update()
    {
        ComputeGlobalWind();

        Vector3 min  = _box.center - _box.size / 2.0f;
        Vector3 temp = Common.Divide(Common.NB_CELLS, _box.size);

        // Reset Grid
        foreach (Vector3Int idx in _curvePos)
        {
            float t = ((float) idx.y / (float) (Common.NB_CELLS.x-1));
            Vector3 newWind = Common.Multiply(new Vector3(t, 0f, (1f - t)), _globalWind);
            _windsGrid.Set(idx.x, idx.y, idx.z, newWind * _globalWind.magnitude);
        }

        _curvePos.Clear();

        //Update Primitives
        for (int i = 0; i < _primitives.Count; i++)
        {
            bool needDivide = _primitives[i].Update(_deltaTime, min, temp);
            _primitives[i].CheckCollision();

            if (needDivide)
            {
                List<SuperPrimitive> newPrimitives = DividePrimitive(_primitives[i]);
                _primitives[i].DestroySphere();
                _primitives.RemoveAt(i);

                float totalEnergies = 0f;
                foreach (SuperPrimitive prim in newPrimitives)
                {
                    _primitives.Add(prim);
                    totalEnergies += prim.GetSpeed() * prim.GetSpeed() * Constants.COEFF_KINETIC;
                }
            }
        }

        // Update Wind Grid
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

                    if (direction.magnitude != 0f)
                    {
                        _windsGrid.Add(i, j, k, direction * direction.magnitude);
                        _curvePos.Add(new Vector3Int(i, j, k));
                    }
                }
    }

    private List<SuperPrimitive> DividePrimitive(SuperPrimitive p_superPrim)
    {
        // Ajouter un facteur de dissipation car il y a toujours un peu d'énergie perdu
        float k = p_superPrim.GetSpeed() * p_superPrim.GetSpeed() * Constants.COEFF_KINETIC * 0.95f;
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
        WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(WindPrimitiveType.SINK, 1f),
                                                          new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                        };
        for (int i = 0; i < nbNewPrim; i++)
        {
            float newEnergy = k * kinEnergies[i] / totalEnergy;
            // Créer une nouvelle primitive si assez d'énergie
            // Sinon l'énergie est dissipé par la viscosité
            if (newEnergy > 0.05f)
            {
                float newSize = kinEnergies[i] * p_superPrim.GetSize();
                float newSpeed  = Mathf.Sqrt(newEnergy / Constants.COEFF_KINETIC);
                windPrimitives.Add(new SuperPrimitive(_bezierCurve, primComp, newSpeed, newEnergy, newSize));
            }
        }

        return windPrimitives;
    }

    public void SetGlobalWind(Vector3 p_globalWind)
    {
        _globalWind = p_globalWind;
        ComputeGlobalWind();
    }

    public void SetLocalWindForce(float p_localWindForce)
    {
        foreach (SuperPrimitive prim in _primitives)
            prim.SetForce(p_localWindForce);
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
