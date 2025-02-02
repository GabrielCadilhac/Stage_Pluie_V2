using UnityEngine;

[ExecuteInEditMode]
public class RainImpact : MonoBehaviour
{
    // Constants
    const int NB_IMPACTS = 1;

    private ComputeBuffer _impPos, _impNor;
    [SerializeField] private Material _impMaterial;

	// Rendering
	[SerializeField] private Vector3 _normal;
	private RenderParams _rp;

	// Animation
	[SerializeField] private float _animSpeed;
	private float _oldAnimSpeed;
	private float _currentTime = 0f;

    void OnEnable()
    {
        _rp = new RenderParams(_impMaterial);
		_rp.matProps = new MaterialPropertyBlock();
		_rp.worldBounds = new Bounds(transform.position, Vector3.one * 10f);

		_impPos = new ComputeBuffer(NB_IMPACTS, NB_IMPACTS * 3 * sizeof(float));
		_impNor = new ComputeBuffer(NB_IMPACTS, NB_IMPACTS * 3 * sizeof(float));
		Vector3[] tempPos = new Vector3[NB_IMPACTS];
		Vector3[] tempNor = new Vector3[NB_IMPACTS];
		for (int i = 0; i < NB_IMPACTS; i++)
		{
			tempPos[i] = new Vector3(0f, 0f, 0f);
			tempNor[i] = new Vector3(0f, 1f, 0f);
		}
		_impPos.SetData(tempPos);
		_impNor.SetData(tempNor);

		_rp.material.SetBuffer("Normal", _impNor);
		_rp.material.SetBuffer("Position", _impPos);
	}

	void Update()
    {
		if (_oldAnimSpeed != _animSpeed)
		{
			_oldAnimSpeed = _animSpeed;
			_rp.material.SetFloat("_AnimSpeed", _animSpeed);
		}

		_currentTime += Time.deltaTime* _animSpeed;
		_rp.material.SetFloat("_iTime", _currentTime);
		_rp.material.SetVector("_Normal", transform.up);

		Graphics.RenderPrimitives(_rp, MeshTopology.Quads, 4, NB_IMPACTS);
	}

	private void OnDisable()
	{
        _impPos.Dispose();
        _impPos = null;

		_impNor.Dispose();
		_impNor = null;
	}
}
