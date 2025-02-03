using System;
using UnityEngine;

namespace Drop_Impact
{
	[ExecuteInEditMode]
	public class RainImpact : MonoBehaviour
	{
		private static readonly int NormalArray = Shader.PropertyToID("Normal");
		private static readonly int Position = Shader.PropertyToID("Position");
		private static readonly int AnimSpeed = Shader.PropertyToID("_AnimSpeed");
		private static readonly int ITime = Shader.PropertyToID("_iTime");
		private static readonly int Normal = Shader.PropertyToID("_Normal");

		// Constants
		private const int NbImpacts = 1;

		private ComputeBuffer _impPos, _impNor;
		[SerializeField] private Material impMaterial;

		// Rendering
		private RenderParams _rp;

		// Animation
		[SerializeField] private float animSpeed;
		private float _oldAnimSpeed;
		private float _currentTime;

		void OnEnable()
		{
			_rp = new(impMaterial)
			{
				matProps = new MaterialPropertyBlock(),
				worldBounds = new Bounds(transform.position, Vector3.one * 10f)
			};

			_impPos = new ComputeBuffer(NbImpacts, NbImpacts * 3 * sizeof(float));
			_impNor = new ComputeBuffer(NbImpacts, NbImpacts * 3 * sizeof(float));
			Vector3[] tempPos = new Vector3[NbImpacts];
			Vector3[] tempNor = new Vector3[NbImpacts];
			for (int i = 0; i < NbImpacts; i++)
			{
				tempPos[i] = new Vector3(0f, 0f, 0f);
				tempNor[i] = new Vector3(0f, 1f, 0f);
			}
			_impPos.SetData(tempPos);
			_impNor.SetData(tempNor);

			_rp.material.SetBuffer(NormalArray, _impNor);
			_rp.material.SetBuffer(Position, _impPos);
		}

		void Update()
		{
			if (Math.Abs(_oldAnimSpeed - animSpeed) > 0.1f)
			{
				_oldAnimSpeed = animSpeed;
				_rp.material.SetFloat(AnimSpeed, animSpeed);
			}
			
			_currentTime += Time.deltaTime * animSpeed;
			_rp.material.SetFloat(ITime, _currentTime);
			_rp.material.SetVector(Normal, transform.up);

			Graphics.RenderPrimitives(_rp, MeshTopology.Quads, 4, NbImpacts);
		}

		private void OnDisable()
		{
			_impPos.Dispose();
			_impPos = null;

			_impNor.Dispose();
			_impNor = null;
		}
	}
}
